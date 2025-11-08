using ClipBeam.Domain.Clips;

namespace ClipBeam.Application.Abstractions.Clipboard
{
    /// <summary>
    /// Platform-agnostic clipboard port used by the application layer to read from,
    /// write to, and observe clipboard changes on the host system.
    /// </summary>
    public interface IClipboardPort
    {
        /// <summary>
        /// Reads the current clipboard contents.
        /// </summary>
        /// <param name="ct">Cancellation token to cancel the read operation.</param>
        /// <returns>
        /// The current <see cref="Clip"/> if available; otherwise <c>null</c> when the clipboard is empty or unsupported.
        /// </returns>
        Task<Clip?> ReadAsync(CancellationToken ct);

        /// <summary>
        /// Sets the clipboard contents to the provided clip.
        /// </summary>
        /// <param name="clip">The clip to place on the clipboard.</param>
        /// <param name="ct">Cancellation token to cancel the set operation.</param>
        /// <returns>A task that completes when the clipboard has been updated.</returns>
        Task SetAsync(Clip clip, CancellationToken ct);

        /// <summary>
        /// Observes clipboard changes and yields new clips as they occur.
        /// </summary>
        /// <param name="ct">Cancellation token to stop watching for changes.</param>
        /// <returns>An asynchronous sequence of <see cref="Clip"/> instances representing clipboard changes.</returns>
        IAsyncEnumerable<Clip> WatchChangesAsync(CancellationToken ct);
    }
}
