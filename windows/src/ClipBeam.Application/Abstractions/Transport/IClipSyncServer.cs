using ClipBeam.Domain.Clips;
using ClipBeam.Domain.Devices;

namespace ClipBeam.Application.Abstractions.Transport
{
    public interface IClipSyncServer
    {
        Task OnHelloAsync(Device remote, CancellationToken ct);
        Task OnDataStartAsync(ClipMeta meta, CancellationToken ct);
        Task OnDataBodyAsync(string clipId, ulong offset, ReadOnlyMemory<byte> data, bool last, uint crc32c, CancellationToken ct);
        Task OnAckAsync(string clipId, ulong ackedUpTo, CancellationToken ct);
        Task OnNackAsync(string clipId, IEnumerable<(ulong start, ulong end)> ranges, CancellationToken ct);
    }
}
