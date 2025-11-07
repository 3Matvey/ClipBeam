using ClipBeam.Application.Abstractions.Clipboard;
using ClipBeam.Domain.Clips;

namespace ClipBeam.Platform.Windows.Clipboard
{
    internal class WindowsClipboardPort : IClipboardPort
    {
        public Task<Clip?> ReadAsync(CancellationToken ct)
        {
            

            throw new NotImplementedException();
        }

        public Task SetAsync(Clip clip, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        public IAsyncEnumerable<Clip> WatchChangesAsync(CancellationToken ct)
        {
            throw new NotImplementedException();
        }
    }
}
