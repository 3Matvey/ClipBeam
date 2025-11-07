using ClipBeam.Application.Abstractions.Devices;
using ClipBeam.Domain.Devices;

namespace ClipBeam.Application.Services.Devices //TODO
{
    /// <summary>
    /// Application-level registry for trusted devices built on top of IDeviceStore.
    /// Contains higher-level operations used across the application.
    /// </summary>
    internal class DeviceRegistry(IDeviceStore store)
    {
        private readonly IDeviceStore _store = store ?? throw new ArgumentNullException(nameof(store));

        public async Task<IEnumerable<Device>> ListAsync(CancellationToken ct)
            => await _store.GetAllAsync(ct).ConfigureAwait(false);

        public async Task<Device?> GetAsync(string id, CancellationToken ct)
            => await _store.GetByIdAsync(id, ct).ConfigureAwait(false);

        public async Task RegisterOrUpdateAsync(Device device, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(device);
            await _store.AddOrUpdateAsync(device, ct).ConfigureAwait(false);
        }

        public async Task<bool> RemoveAsync(string id, CancellationToken ct)
            => await _store.RemoveAsync(id, ct).ConfigureAwait(false);

        public async Task SetActiveAsync(string? id, CancellationToken ct)
            => await _store.SetActiveDeviceIdAsync(id, ct).ConfigureAwait(false);

        public async Task<Device?> GetActiveDeviceAsync(CancellationToken ct)
        {
            var activeId = await _store.GetActiveDeviceIdAsync(ct).ConfigureAwait(false);
            if (string.IsNullOrEmpty(activeId)) return null;
            return await _store.GetByIdAsync(activeId, ct).ConfigureAwait(false);
        }
    }
}
