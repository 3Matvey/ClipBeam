using ClipBeam.Domain.Abstractions;
using ClipBeam.Domain.Clips;

namespace ClipBeam.Application.Services.Sync
{
    /// <summary>
    /// The clip collector on the receiving side
    /// </summary>
    public sealed class ChunckAssembler(IHasherProvider hasherProvider)
    {
        /// <summary>
        /// Assembly status during transfer
        /// </summary>
        private sealed record Inflight(ClipMeta Meta, byte[] Buffer);

        private readonly Dictionary<Guid, Inflight> _inflight = [];

        public void Begin(ClipMeta meta)
        {
            if (meta.TotalSize is > int.MaxValue or <= 0)
                throw new ArgumentOutOfRangeException(
                    paramName: nameof(meta),
                    message: $"{nameof(meta.TotalSize)} must be > 0 and < {int.MaxValue}.");

            byte[] buffer = GC.AllocateUninitializedArray<byte>((int)meta.TotalSize);

            _inflight[meta.ClipId] = new Inflight(meta, buffer);
        }

        /// <summary>
        /// Adds an <strong>already decompressed</strong> chunk for a clip and validates integrity.
        /// Returns assembled Clip when the last chunk arrives; otherwise returns null.
        /// </summary>
        public Clip? AddChunk(Guid clipId, ulong offset, ReadOnlyMemory<byte> data, bool last)
        {
            if (!_inflight.TryGetValue(clipId, out var inflight))
                return null;

            byte[] buffer = inflight.Buffer;

            int destOffset = checked((int)offset);
            int length = data.Length;

            if (destOffset + length > buffer.Length)
                throw new ArgumentOutOfRangeException(
                    nameof(offset),
                    "Chunk goes out of bounds of allocated clip buffer.");

            data.Span.CopyTo(buffer.AsSpan(destOffset, length));

            if (!last)
                return null;

            _inflight.Remove(clipId);

            byte[] bytes = buffer;
            ClipMeta meta = inflight.Meta;

            return ClipFactory.FromMeta(meta, bytes, hasherProvider);
        }
    }
}
