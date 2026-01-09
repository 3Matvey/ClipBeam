using ClipBeam.Application.Abstractions.Transport;
using ClipBeam.Domain.Clips;
using ClipBeam.Domain.Devices;

namespace ClipBeam.Application.Services.Sync
{
    public sealed class SyncCoordinator(
        IClipSyncClient client,
        IClipSyncServer server,
        ChunckAssembler assembler)
    {
        public async Task SendAsync(Clip clip, Device target, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(clip);
            ArgumentNullException.ThrowIfNull(target);

            await client.StartAsync(target, ct).ConfigureAwait(false);
            await client.SendHelloAsync(target, ct).ConfigureAwait(false);
            await client.SendDataStartAsync(clip, ct).ConfigureAwait(false);

            foreach (var (Offset, Data, isLast) in TransferManager.Split(clip))
            {
                await client.SendChunkAsync(
                    clip.Meta.ClipId,
                    Offset,
                    Data,
                    isLast,
                    ct).ConfigureAwait(false);  
            }

            // TODO: ACK/NACK позже

        }

        public Task OnDataStartAsync(ClipMeta meta, CancellationToken ct)
        {
            assembler.Begin(meta);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Обработать очередной чанк.
        /// Возвращает Clip, когда он полностью собран, иначе null.
        /// </summary>
        public Clip? OnDataBody(
            Guid clipId,
            ulong offset,
            ReadOnlyMemory<byte> data,
            bool last)
        {
            return assembler.AddChunk(clipId, offset, data, last);
        }
    }
}
