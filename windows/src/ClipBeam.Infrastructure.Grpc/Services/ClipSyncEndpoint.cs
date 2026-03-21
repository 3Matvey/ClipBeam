using ClipBeam.Application.Services.Orchestration;
using ClipBeam.Application.Services.Sync;
using ClipBeam.Domain.Clips;
using ClipBeam.Infrastructure.Grpc.Mapping;
using ClipBeam.Proto;
using Grpc.Core;

namespace ClipBeam.Infrastructure.Grpc.Services
{
    public sealed class ClipSyncEndpoint(
        SyncCoordinator coordinator,
        CentralOrchestrator centralOrchestrator) : ClipSync.ClipSyncBase
    {
        /// <summary>
        /// Per-connection state machine.
        /// One gRPC Sync() call = one logical connection between two peers.
        /// </summary>
        private enum ConnState
        {
            WaitingHello = 0,   // must receive Hello first
            Ready = 1,          // handshake done; can receive DataStart
            ReceivingClip = 2   // after DataStart until last chunk arrives
        }

        public override async Task Sync(
            IAsyncStreamReader<Envelope> requestStream,
            IServerStreamWriter<Envelope> responseStream,
            ServerCallContext context)
        {
            var ct = context.CancellationToken;

            ConnState state = ConnState.WaitingHello;

            Guid currentClipId = Guid.Empty;
            bool hasCurrentClip = false;

            try
            {
                while (await requestStream.MoveNext(ct).ConfigureAwait(false))
                {
                    Envelope env = requestStream.Current;

                    switch (env.KindCase)
                    {
                        // handshake
                        case Envelope.KindOneofCase.Hello:
                            {
                                // We only allow Hello once per connection.
                                if (state != ConnState.WaitingHello)
                                    throw RpcFailedPrecondition("Hello already processed for this connection.");

                                // MVP: accept everything. Later you can validate token/cert and negotiate caps.
                                var ack = new HelloAck
                                {
                                    Accepted = true,
                                    Reason = "",
                                    Negotiated = env.Hello.Capabilities // MVP mirror
                                };

                                await responseStream.WriteAsync(new Envelope { HelloAck = ack }, ct)
                                    .ConfigureAwait(false);

                                state = ConnState.Ready;
                                break;
                            }

                        //start of clip transfer
                        case Envelope.KindOneofCase.DataStart:
                            {
                                Ensure(state != ConnState.WaitingHello, StatusCode.FailedPrecondition,
                                    "Handshake not completed. Expected Hello first.");

                                // For MVP we do NOT allow parallel transfers on the same stream.
                                Ensure(state == ConnState.Ready && !hasCurrentClip, StatusCode.FailedPrecondition,
                                    "DataStart received while another transfer is active. Parallel transfers are not supported.");

                                Ensure(env.DataStart is not null && env.DataStart.Meta is not null, StatusCode.InvalidArgument,
                                    "DataStart.meta is required.");

                                var domainMeta = env.DataStart!.Meta!.ToDomain();

                                // Start assembler buffer in application layer
                                await coordinator.OnDataStartAsync(domainMeta, ct).ConfigureAwait(false);

                                currentClipId = domainMeta.ClipId;
                                hasCurrentClip = true;
                                state = ConnState.ReceivingClip;

                                break;
                            }

                        // clip data chunk
                        case Envelope.KindOneofCase.DataBody:
                            {
                                Ensure(state != ConnState.WaitingHello, StatusCode.FailedPrecondition,
                                    "Handshake not completed. Expected Hello first.");

                                Ensure(state == ConnState.ReceivingClip && hasCurrentClip, StatusCode.FailedPrecondition,
                                    "DataBody received but no active clip transfer. Expected DataStart first.");

                                var body = env.DataBody;
                                Ensure(body is not null, StatusCode.InvalidArgument, "DataBody frame is missing.");

                                if (!Guid.TryParse(body?.ClipId, out var clipId) || clipId == Guid.Empty)
                                    throw RpcInvalidArgument("Invalid clip_id GUID in DataBody.");

                                // Protocol safety: all DataBody must belong to current active clip_id.
                                Ensure(clipId == currentClipId, StatusCode.FailedPrecondition,
                                    "DataBody.clip_id does not match active transfer clip_id.");

                                // bytes payload
                                var chunk = body.Data.Memory;

                                Clip? assembled;
                                try
                                {
                                    assembled = coordinator.OnDataBody(clipId, body.Offset, chunk, body.Last);
                                }
                                catch (ArgumentOutOfRangeException ex)
                                {
                                    // client sent invalid offsets/sizes
                                    throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
                                }
                                catch (InvalidOperationException ex)
                                {
                                    // client violated transfer rules
                                    throw new RpcException(new Status(StatusCode.FailedPrecondition, ex.Message));
                                }

                                if (body.Last)
                                {
                                    hasCurrentClip = false;
                                    currentClipId = Guid.Empty;
                                    state = ConnState.Ready;
                                }

                                if (assembled is not null)
                                {
                                    // it will slow ??? TODO doublecheck
                                    await centralOrchestrator.EnqueueRemoteAsync(assembled, ct);
                                }

                                break;
                            }

                        // keepalive
                        case Envelope.KindOneofCase.Ping:
                            {
                                await responseStream.WriteAsync(
                                    new Envelope { Pong = new Pong { EchoUtc = env.Ping.SendUtc } },
                                    ct).ConfigureAwait(false);
                                break;
                            }

                        case Envelope.KindOneofCase.Pong:
                        default:
                            // ignore unknown frames (forward compatible)
                            break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Normal cancellation/shutdown
            }
            catch (RpcException)
            {
                // Already a gRPC status -> rethrow
                throw;
            }
            catch (Exception ex)
            {
                // Unexpected server error
                throw new RpcException(new Status(StatusCode.Internal, ex.Message));
            }
        }

        private static void Ensure(bool condition, StatusCode code, string message)
        {
            if (!condition)
                throw new RpcException(new Status(code, message));
        }

        private static RpcException RpcInvalidArgument(string message)
            => new(new Status(StatusCode.InvalidArgument, message));

        private static RpcException RpcFailedPrecondition(string message)
            => new(new Status(StatusCode.FailedPrecondition, message));
    }
}
