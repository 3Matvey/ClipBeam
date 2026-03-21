using ClipBeam.Application.Abstractions.Devices;
using ClipBeam.Domain.Devices;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClipBeam.Host.Win.Devices
{
    public sealed class JsonFileDeviceStore(string path) : IDeviceStore
    {
        private readonly string _path = path
            ?? throw new ArgumentNullException(nameof(path));

        private readonly SemaphoreSlim _mutex = new(1, 1);

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public async Task<IEnumerable<Device>> GetAllAsync(CancellationToken ct)
        {
            var state = await LoadAsync(ct).ConfigureAwait(false);
            return state.Devices.Values.ToArray();
        }

        public async Task<Device?> GetByIdAsync(string id, CancellationToken ct)
        {
            var state = await LoadAsync(ct).ConfigureAwait(false);
            return state.Devices.TryGetValue(id, out var device) ? device : null;
        }

        public async Task AddOrUpdateAsync(Device device, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(device);

            await MutateAsync(state =>
            {
                state.Devices[device.Id] = device;
            }, ct).ConfigureAwait(false);
        }

        public async Task<bool> RemoveAsync(string id, CancellationToken ct)
        {
            bool removed = false;

            await MutateAsync(state =>
            {
                removed = state.Devices.Remove(id);

                if (state.ActiveDeviceId == id)
                    state.ActiveDeviceId = null;
            }, ct).ConfigureAwait(false);

            return removed;
        }

        public async Task SetActiveDeviceIdAsync(string? deviceId, CancellationToken ct)
        {
            await MutateAsync(state =>
            {
                state.ActiveDeviceId = deviceId;
            }, ct).ConfigureAwait(false);
        }

        public async Task<string?> GetActiveDeviceIdAsync(CancellationToken ct)
        {
            var state = await LoadAsync(ct).ConfigureAwait(false);
            return state.ActiveDeviceId;
        }

        public async Task<bool> ExistsAsync(string id, CancellationToken ct)
        {
            var state = await LoadAsync(ct).ConfigureAwait(false);
            return state.Devices.ContainsKey(id);
        }



        private async Task<DeviceStoreState> LoadAsync(CancellationToken ct)
        {
            await _mutex.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                if (!File.Exists(_path))
                    return new DeviceStoreState();

                await using var fs = File.OpenRead(_path);
                return await JsonSerializer.DeserializeAsync<DeviceStoreState>(fs, JsonOptions, ct)
                       ?? new DeviceStoreState();
            }
            finally
            {
                _mutex.Release();
            }
        }

        private async Task MutateAsync(Action<DeviceStoreState> mutator, CancellationToken ct)
        {
            await _mutex.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                var state = File.Exists(_path)
                    ? await LoadInternalAsync(ct).ConfigureAwait(false)
                    : new DeviceStoreState();

                mutator(state);

                var tmp = _path + ".tmp";

                var dir = Path.GetDirectoryName(_path);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);

                await using (var fs = File.Create(tmp))
                {
                    await JsonSerializer.SerializeAsync(fs, state, JsonOptions, ct)
                        .ConfigureAwait(false);
                }

                File.Move(tmp, _path, overwrite: true);
            }
            finally
            {
                _mutex.Release();
            }
        }

        private async Task<DeviceStoreState> LoadInternalAsync(CancellationToken ct)
        {
            await using var fs = File.OpenRead(_path);
            return await JsonSerializer.DeserializeAsync<DeviceStoreState>(fs, JsonOptions, ct)
                   ?? new DeviceStoreState();
        }

        private sealed class DeviceStoreState
        {
            public string? ActiveDeviceId { get; set; }
            public Dictionary<string, Device> Devices { get; set; } = [];
        }
    }
}
