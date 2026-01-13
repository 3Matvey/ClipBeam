using ClipBeam.Domain.Clips;
using ClipBeam.Domain.Devices;

namespace ClipBeam.Application.Abstractions.Transport
{
    public interface IClipSyncServer
    {
        Task OnHelloAsync(Device remote, CancellationToken ct);
        Task OnDataStartAsync(ClipMeta meta, CancellationToken ct);
        Task OnDataBodyAsync(string clipId, ulong offset, ReadOnlyMemory<byte> data, bool last, CancellationToken ct);
    }
}
