using ClipBeam.Domain.Clips;

namespace ClipBeam.Application.Abstractions.Persistence
{
    /// <summary>
    /// Storage and retrieval abstraction for Clip history and cache.
    /// Implementations may persist clips to disk, database or in-memory for testing.
    /// </summary>
    public interface IClipStore
    {
        /// <summary>
        /// Persists the provided clip. If a clip with the same id exists, replace or ignore per implementation.
        /// </summary>
        /// <param name="clip">Clip to store.</param>
        /// <param name="ct">Cancellation token.</param>
        Task AddAsync(Clip clip, CancellationToken ct);

        /// <summary>
        /// Retrieves a clip by its identifier.
        /// </summary>
        /// <param name="clipId">Clip identifier.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>The clip if found; otherwise null.</returns>
        Task<Clip?> GetByIdAsync(Guid clipId, CancellationToken ct);

        /// <summary>
        /// Returns recent clips ordered by creation time descending.
        /// </summary>
        /// <param name="limit">Maximum number of clips to return. Use a reasonable default when null.</param>
        /// <param name="ct">Cancellation token.</param>
        Task<IEnumerable<Clip>> GetRecentAsync(int? limit, CancellationToken ct);

        /// <summary>
        /// Finds clips by content hash (useful for deduplication).
        /// </summary>
        /// <param name="hash">Content hash to search for.</param>
        /// <param name="ct">Cancellation token.</param>
        Task<IEnumerable<Clip>> QueryByHashAsync(Hash hash, CancellationToken ct);

        /// <summary>
        /// Removes a clip by id.
        /// </summary>
        /// <param name="clipId">Clip identifier.</param>
        /// <param name="ct">Cancellation token.</param>
        Task<bool> RemoveAsync(Guid clipId, CancellationToken ct);

        /// <summary>
        /// Deletes clips older than the provided UTC cutoff.
        /// </summary>
        /// <param name="utcCutoff">UTC cutoff; clips created before this time should be removed.</param>
        /// <param name="ct">Cancellation token.</param>
        Task PruneOlderThanAsync(DateTime utcCutoff, CancellationToken ct);

        /// <summary>
        /// Streams all clips from the store (implementation may be paged).
        /// </summary>
        /// <param name="ct">Cancellation token.</param>
        IAsyncEnumerable<Clip> StreamAllAsync(CancellationToken ct);
    }
}
