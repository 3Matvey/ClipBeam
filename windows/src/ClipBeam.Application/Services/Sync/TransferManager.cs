using ClipBeam.Domain.Clips;

namespace ClipBeam.Application.Services.Sync
{
    internal static class TransferManager
    {
        /// <summary>
        /// Preffered chunck size = 64 kb
        /// </summary>
        private const int ChunckSize = 64 * 1024;

        /// <summary>
        /// Returns a sequence of chunks
        /// </summary>
        /// <param name="clip"></param>
        /// <returns></returns>
        public static IEnumerable<(ulong Offset, ReadOnlyMemory<byte> Data, bool isLast)> Split(Clip clip)
        {
            ReadOnlyMemory<byte> raw = clip.Content.Raw;
            ulong offset = 0;

            for (int i = 0; i < raw.Length; i += ChunckSize)
            {
                int len = Math.Min(ChunckSize, raw.Length - 1);
                bool last = (i + len) == raw.Length;

                yield return (offset, raw.Slice(i, len), last);

                offset += (ulong)len;
            }
        }
    }
}
