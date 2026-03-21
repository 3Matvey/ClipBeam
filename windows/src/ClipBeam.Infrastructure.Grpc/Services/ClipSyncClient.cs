using ClipBeam.Application.Abstractions.Transport;
using ClipBeam.Domain.Clips;
using ClipBeam.Domain.Devices;
using ClipBeam.Proto;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using Capabilities = ClipBeam.Proto.Capabilities;
using DomainClipMeta = ClipBeam.Domain.Clips.ClipMeta;
using ProtoClipMeta = ClipBeam.Proto.ClipMeta;
using ProtoAuthScheme = ClipBeam.Proto.AuthScheme;
using ClipBeam.Infrastructure.Grpc.Mapping;

namespace ClipBeam.Infrastructure.Grpc.Services
{
    /// <summary>
    /// gRPC transport implementation for ClipSync protocol (bidi streaming).
    /// Client-side: sends clipboard data to a remote peer.
    /// </summary>
    internal sealed class ClipSyncClient : IClipSyncClient, IAsyncDisposable
    {
        private readonly ClipSyncClientOptions _options;

        private GrpcChannel? _channel;
        private AsyncDuplexStreamingCall<Envelope, Envelope>? _call;
        private CancellationTokenSource? _cts;
        private Task? _readLoop;
        private TaskCompletionSource<HelloAck>? _helloAckTcs;

        public Capabilities? NegotiatedCapabilities { get; private set; }

        // Protects RequestStream from concurrent writes
        private readonly SemaphoreSlim _writeLock = new(1, 1);

        public ClipSyncClient(ClipSyncClientOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Opens gRPC channel and starts Sync bidi-stream.
        /// </summary>
        public async Task StartAsync(Device target, CancellationToken ct)
        {
            if (_call is not null)
                return;

            if (string.IsNullOrWhiteSpace(_options.Address))
                throw new InvalidOperationException("ClipSyncClientOptions.Address must be set.");

            _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

            _channel = GrpcChannel.ForAddress(_options.Address, new GrpcChannelOptions
            {
                MaxReceiveMessageSize = _options.MaxReceiveMessageSizeBytes,
                MaxSendMessageSize = _options.MaxSendMessageSizeBytes,
                HttpHandler = _options.HttpHandler
            });

            var client = new ClipSync.ClipSyncClient(_channel);

            _call = client.Sync(cancellationToken: _cts.Token);

            _readLoop = Task.Run(
                () => ReadLoopAsync(_call.ResponseStream, _call.RequestStream, _cts.Token),
                _cts.Token
            );

            await Task.CompletedTask;
        }

        /// <summary>
        /// Sends Hello and waits for HelloAck.
        /// </summary>
        public async Task SendHelloAsync(Device local, CancellationToken ct)
        {
            EnsureStarted();

            _helloAckTcs = new TaskCompletionSource<HelloAck>(
                TaskCreationOptions.RunContinuationsAsynchronously
            );

            var hello = MapHello(local);

            await WriteAsync(new Envelope { Hello = hello }, ct);

            using var timeoutCts = _options.HelloAckTimeout is null
                ? null
                : new CancellationTokenSource(_options.HelloAckTimeout.Value);

            using var linkedCts = timeoutCts is null
                ? CancellationTokenSource.CreateLinkedTokenSource(ct)
                : CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

            HelloAck ack;
            try
            {
                ack = await _helloAckTcs.Task.WaitAsync(linkedCts.Token);
            }
            catch (OperationCanceledException)
            {
                throw new TimeoutException("HelloAck was not received in time.");
            }

            if (!ack.Accepted)
                throw new InvalidOperationException($"Handshake rejected: {ack.Reason}");

            NegotiatedCapabilities = ack.Negotiated;
        }

        /// <summary>
        /// Announces start of clip transfer.
        /// </summary>
        public Task SendDataStartAsync(Clip clip, CancellationToken ct)
        {
            EnsureStarted();

            var msg = new DataStart
            {
                Meta = MapClipMeta(clip)
            };

            return WriteAsync(new Envelope { DataStart = msg }, ct);
        }

        /// <summary>
        /// Sends one chunk of clip data.
        /// </summary>
        public Task SendChunkAsync(
            Guid clipId,
            ulong offset,
            ReadOnlyMemory<byte> data,
            bool last,
            CancellationToken ct)
        {
            EnsureStarted();

            var body = new DataBody
            {
                ClipId = clipId.ToString("D"),
                Offset = offset,
                Data = ByteString.CopyFrom(data.Span),
                Last = last
            };

            return WriteAsync(new Envelope { DataBody = body }, ct);
        }

        /// <summary>
        /// Stops stream and closes channel.
        /// </summary>
        public async Task StopAsync(CancellationToken ct)
        {
            if (_call is null)
                return;

            try
            {
                _cts?.Cancel();

                try
                {
                    await _writeLock.WaitAsync(ct);
                    try
                    {
                        await _call.RequestStream.CompleteAsync();
                    }
                    finally
                    {
                        _writeLock.Release();
                    }
                }
                catch
                {
                    // ignore shutdown errors
                }

                if (_readLoop is not null)
                    await Task.WhenAny(_readLoop, Task.Delay(_options.ShutdownGracePeriod, ct));
            }
            finally
            {
                try { _call.Dispose(); } catch { }
                _call = null;

                if (_channel is not null)
                {
                    try { await _channel.ShutdownAsync(); } catch { }
                    _channel = null;
                }

                _cts?.Dispose();
                _cts = null;
                _helloAckTcs = null;
                NegotiatedCapabilities = null;
            }
        }

        public async ValueTask DisposeAsync()
        {
            await StopAsync(CancellationToken.None);
            _writeLock.Dispose();
        }


        private async Task WriteAsync(Envelope envelope, CancellationToken ct)
        {
            var call = _call ?? throw new InvalidOperationException("Client not started.");

            await _writeLock.WaitAsync(ct);
            try
            {
                await call.RequestStream.WriteAsync(envelope, ct);
            }
            finally
            {
                _writeLock.Release();
            }
        }

        private void EnsureStarted()
        {
            if (_call is null)
                throw new InvalidOperationException("gRPC transport not started.");
        }

        private async Task ReadLoopAsync(
            IAsyncStreamReader<Envelope> responses,
            IClientStreamWriter<Envelope> requests,
            CancellationToken ct)
        {
            try
            {
                while (await responses.MoveNext(ct))
                {
                    var msg = responses.Current;

                    switch (msg.KindCase)
                    {
                        case Envelope.KindOneofCase.HelloAck:
                            _helloAckTcs?.TrySetResult(msg.HelloAck);
                            break;

                        case Envelope.KindOneofCase.Ping:
                            await WriteAsync(new Envelope
                            {
                                Pong = new Pong { EchoUtc = msg.Ping.SendUtc }
                            }, ct);
                            break;

                        case Envelope.KindOneofCase.Pong:
                        default:
                            break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // normal shutdown
            }
            catch (Exception ex)
            {
                _helloAckTcs?.TrySetException(ex);
            }
        }

        private static Hello MapHello(Device local)
        {
            return new Hello
            {
                DeviceId = local.Id,
                DeviceName = local.Name,
                Platform = local.Platform.ToProto(),
                ProtoVersion = local.ProtoVersion,

                AppVersion = new SemanticVersion
                {
                    Major = local.AppVersionMajor,
                    Minor = local.AppVersionMinor,
                    Patch = local.AppVersionPatch
                },

                Capabilities = local.Capabilities.ToProto(),

                AuthScheme = ProtoAuthScheme.Token,
                Token = new TokenAuth
                {
                    TokenId = local.TokenId ?? "pairing",
                    Raw = ByteString.Empty
                }
            };
        }

        private static ProtoClipMeta MapClipMeta(Clip clip)
        {
            DomainClipMeta meta = clip.Meta;

            var pm = new ProtoClipMeta
            {
                ClipId = meta.ClipId.ToString("D"),
                OriginDeviceId = meta.OriginDeviceId,
                Seq = meta.Seq,
                Type = meta.Type.ToProto(),
                TotalSize = meta.TotalSize,
                ProtoVersion = meta.ProtoVersion,
                CreatedUtc = Timestamp.FromDateTime(
                    DateTime.SpecifyKind(meta.CreatedUtc, DateTimeKind.Utc)
                )
            };

            return pm;
        }
    }
}
