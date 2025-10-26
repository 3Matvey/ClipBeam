using ClipBeam.Domain.Devices;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ClipBeam.Application.Abstractions.Devices
{
    /// <summary>
    /// Persistent store for trusted devices (result of pairing).
    /// Implementations are responsible for durable storage, concurrency and integrity.
    /// </summary>
    public interface IDeviceStore
    {
        /// <summary>
        /// Returns all devices known to the store.
        /// </summary>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A collection of devices.</returns>
        Task<IEnumerable<Device>> GetAllAsync(CancellationToken ct);

        /// <summary>
        /// Finds a device by identifier.
        /// </summary>
        /// <param name="id">Device identifier.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>The device when found; otherwise <c>null</c>.</returns>
        Task<Device?> GetByIdAsync(string id, CancellationToken ct);

        /// <summary>
        /// Adds a new device or updates an existing one atomically.
        /// </summary>
        /// <param name="device">Device to add or update.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A task that completes after the operation.</returns>
        Task AddOrUpdateAsync(Device device, CancellationToken ct);

        /// <summary>
        /// Removes the device with the specified id.
        /// </summary>
        /// <param name="id">Device identifier.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>True if the device existed and was removed; otherwise false.</returns>
        Task<bool> RemoveAsync(string id, CancellationToken ct);

        /// <summary>
        /// Persists the identifier of the currently active device (or null to clear).
        /// </summary>
        /// <param name="deviceId">Active device id or null.</param>
        /// <param name="ct">Cancellation token.</param>
        Task SetActiveDeviceIdAsync(string? deviceId, CancellationToken ct);

        /// <summary>
        /// Reads the currently active device identifier.
        /// </summary>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Active device id or null if none set.</returns>
        Task<string?> GetActiveDeviceIdAsync(CancellationToken ct);

        /// <summary>
        /// Checks whether a device exists in the store.
        /// </summary>
        /// <param name="id">Device identifier.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>True if the device exists; otherwise false.</returns>
        Task<bool> ExistsAsync(string id, CancellationToken ct);
    }
}
