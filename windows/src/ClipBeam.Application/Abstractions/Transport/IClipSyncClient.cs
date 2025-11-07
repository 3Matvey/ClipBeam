using ClipBeam.Domain.Clips;
using ClipBeam.Domain.Devices;

namespace ClipBeam.Application.Abstractions.Transport
{
    /// <summary>
    /// Client-side transport abstraction used to send clip synchronization messages to a remote device.
    /// </summary>
    public interface IClipSyncClient
    {
        /// <summary>
        /// Prepares and opens a transport session to the specified target device.
        /// </summary>
        /// <param name="target">The remote device to which this client will attempt to connect.</param>
        /// <param name="ct">Cancellation token to cancel the start operation.</param>
        /// <returns>A task that completes when the client is ready to send messages.</returns>
        Task StartAsync(Device target, CancellationToken ct);

        /// <summary>
        /// Sends a "hello" / handshake message containing the local device information.
        /// </summary>
        /// <param name="local">Local device information that will be sent to the remote peer.</param>
        /// <param name="ct">Cancellation token to cancel the send operation.</param>
        /// <returns>A task that completes when the message has been sent (or the operation aborted).</returns>
        Task SendHelloAsync(Device local, CancellationToken ct);

        /// <summary>
        /// Announces the start of a clip transfer to the remote peer.
        /// </summary>
        /// <param name="clip">The clip (including metadata and optional small content) that will be transferred.</param>
        /// <param name="ct">Cancellation token to cancel the announce operation.</param>
        /// <returns>A task that completes when the start message has been sent.</returns>
        Task SendDataStartAsync(Clip clip, CancellationToken ct);

        /// <summary>
        /// Sends a chunk (body) of clip content to the remote peer.
        /// </summary>
        /// <param name="clipId">Identifier of the clip being transmitted (use the ClipMeta.ClipId GUID).</param>
        /// <param name="offset">Byte offset within the clip content for this chunk.</param>
        /// <param name="data">A readonly memory buffer containing the chunk bytes.</param>
        /// <param name="last">True if this chunk is the final chunk in the transfer.</param>
        /// <param name="crc32c">CRC32C checksum for the chunk. Used by receivers to verify integrity.</param>
        /// <param name="ct">Cancellation token to cancel the send operation.</param>
        /// <returns>A task that completes when the chunk has been sent.</returns>
        Task SendChunkAsync(Guid clipId, ulong offset, ReadOnlyMemory<byte> data, bool last, uint crc32c, CancellationToken ct);

        /// <summary>
        /// Sends an acknowledgement (ACK) indicating that the receiver has persisted data up to the specified offset.
        /// </summary>
        /// <param name="clipId">Identifier of the clip for which the acknowledgement applies.</param>
        /// <param name="ackedUpTo">The highest byte offset (inclusive or exclusive per protocol) that the receiver has accepted.</param>
        /// <param name="ct">Cancellation token to cancel the send operation.</param>
        /// <returns>A task that completes when the ack has been sent.</returns>
        Task SendAckAsync(Guid clipId, ulong ackedUpTo, CancellationToken ct);

        /// <summary>
        /// Sends a negative acknowledgement (NACK) containing ranges of bytes that were not received/validated.
        /// </summary>
        /// <param name="clipId">Identifier of the clip for which the NACK applies.</param>
        /// <param name="ranges">A collection of (start, end) ranges representing missing or corrupted byte ranges.</param>
        /// <param name="ct">Cancellation token to cancel the send operation.</param>
        /// <returns>A task that completes when the NACK has been sent.</returns>
        Task SendNackAsync(Guid clipId, IEnumerable<(ulong start, ulong end)> ranges, CancellationToken ct);

        /// <summary>
        /// Stops and disposes the transport client, closing any active session.
        /// </summary>
        /// <param name="ct">Cancellation token to cancel the stop operation.</param>
        /// <returns>A task that completes when the client has been stopped.</returns>
        Task StopAsync(CancellationToken ct);
    }
}
