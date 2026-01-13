using ClipBeam.Application.Abstractions.Transport;
using ClipBeam.Domain.Clips;
using ClipBeam.Domain.Devices;
using ClipBeam.Proto;
using Google.Protobuf;
using Grpc.Core;
using Grpc.Net.Client;
using Capabilities = ClipBeam.Proto.Capabilities;

namespace ClipBeam.Infrastructure.Grpc
{
    /// <summary>
    /// gRPC transport implementation for ClipSync protocol (bidi streaming).
    /// </summary>
    internal sealed class GrpcClipSyncClient : IClipSyncClient, IAsyncDisposable
    {
        private readonly GrpcClipSyncClientOptions _options;

        private GrpcChannel? _channel;
        private AsyncDuplexStreamingCall<Envelope, Envelope>? _call;
        private CancellationTokenSource? _cts;
        private Task? _readLoop; // background task that continuously reads inbound messages
        private TaskCompletionSource<HelloAck>? _helloAckTcs;
        public Capabilities? NegotiatedCapabilities { get; private set; }

        public GrpcClipSyncClient(GrpcClipSyncClientOptions? options = null)
        {
            _options = options ?? new GrpcClipSyncClientOptions();
        }

        public Task StartAsync(Device target, CancellationToken ct)
        {
            if (_call is not null) 
                return;

            _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

            string address = BuildAddress(target);

            _channel = GrpcChannel.ForAddress(address, new GrpcChannelOptions
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
                            await requests.WriteAsync(new Envelope
                            {
                                Pong = new Pong
                                {
                                    EchoUtc = msg.Ping.SendUtc
                                }
                            }, ct);
                            break;

                        case Envelope.KindOneofCase.Pong:

                            break;
                            // TODO
                        default: 
                            
                            break;

                    }
                }
            }
            catch (OperationCanceledException)
            {
                //ignore
            }
            catch (RpcException ex)
            {
                _helloAckTcs?.TrySetException(
                    new InvalidOperationException(
                        $"gRPC ошибка: {ex.StatusCode} — {ex.Message}", ex)
                );
            }
            catch (Exception ex)
            {
                _helloAckTcs?.TrySetException(ex);
            }
        }


        public ValueTask DisposeAsync()
        {
            throw new NotImplementedException();
        }

        public Task SendChunkAsync(Guid clipId, ulong offset, ReadOnlyMemory<byte> data, bool last, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        public Task SendDataStartAsync(Clip clip, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        public Task SendHelloAsync(Device local, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        

        public Task StopAsync(CancellationToken ct)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Options for gRPC transport.
    /// Keep them here so you can configure from appsettings later.
    /// </summary>
    internal sealed class GrpcClipSyncClientOptions
    {
        /// <summary>
        /// If true => address is https:// (TLS). If false => http:// (no TLS).
        /// For LAN MVP you may start with http (false), then later add TLS.
        /// </summary>
        public bool UseTls { get; init; } = false;

        /// <summary>
        /// Proto version that you put into Hello/ClipMeta.
        /// </summary>
        public uint ProtoVersion { get; init; } = 1;

        /// <summary>
        /// Chunking params you advertise.
        /// Actual chunk size you use should be min(localPreferred, negotiatedMax).
        /// </summary>
        public int PreferredChunkBytes { get; init; } = 128 * 1024;
        public int MaxChunkBytes { get; init; } = 256 * 1024;

        public bool SupportsHashDedup { get; init; } = false;

        /// <summary>
        /// gRPC message size limits.
        /// Your DataBody.data must be <= this.
        /// With 128KB chunks you're always safe.
        /// </summary>
        public int? MaxReceiveMessageSizeBytes { get; init; } = 8 * 1024 * 1024;
        public int? MaxSendMessageSizeBytes { get; init; } = 8 * 1024 * 1024;

        /// <summary>
        /// How long we wait for HelloAck before failing handshake.
        /// </summary>
        public TimeSpan? HelloAckTimeout { get; init; } = TimeSpan.FromSeconds(3);

        /// <summary>
        /// How long StopAsync waits for read-loop to finish.
        /// </summary>
        public TimeSpan ShutdownGracePeriod { get; init; } = TimeSpan.FromSeconds(2);

        /// <summary>
        /// Custom HTTP handler (optional):
        /// - If you do TLS with self-signed certs, you usually need to override cert validation here.
        /// - If you use plain http, you can leave null.
        /// </summary>
        public HttpMessageHandler? HttpHandler { get; init; }
    }
}
