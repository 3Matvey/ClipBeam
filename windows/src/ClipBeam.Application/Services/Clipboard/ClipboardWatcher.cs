using System;
using System.Threading;
using System.Threading.Tasks;
using ClipBeam.Application.Abstractions.Clipboard;
using ClipBeam.Application.Services.Devices;
using ClipBeam.Domain.Clips;
using ClipBeam.Domain.Devices;

namespace ClipBeam.Application.Services.Clipboard   // TODO 
{
    /// <summary>
    /// Наблюдатель системного буфера. При каждом изменении:
    /// - получает доменный Clip от IClipboardPort;
    /// - выбирает целевое устройство (DeviceRegistry);
    /// - пересылает клип через ClipboardSyncService.
    /// </summary>
    public sealed class ClipboardWatcher
    {
        private readonly IClipboardPort _clipboard;
        private readonly ClipboardSyncService _clipboardSync;
        private readonly DeviceRegistry _devices;

        private Task? _loop;
        private CancellationTokenSource? _cts;

        // Для MVP: пушим автоматически каждое изменение
        private const bool AutoPushOnChange = true;

        public ClipboardWatcher(
            IClipboardPort clipboard,
            ClipboardSyncService clipboardSync,
            DeviceRegistry devices)
        {
            _clipboard = clipboard ?? throw new ArgumentNullException(nameof(clipboard));
            _clipboardSync = clipboardSync ?? throw new ArgumentNullException(nameof(clipboardSync));
            _devices = devices ?? throw new ArgumentNullException(nameof(devices));
        }

        /// <summary>
        /// Запустить наблюдение. Можно дергать из Hosted Service/Composition Root.
        /// </summary>
        public Task StartAsync(CancellationToken ct = default)
        {
            if (_loop is not null && !_loop.IsCompleted) return Task.CompletedTask;

            _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            _loop = Task.Run(() => RunAsync(_cts.Token), _cts.Token);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Остановить наблюдение и дождаться корректного завершения цикла.
        /// </summary>
        public async Task StopAsync()
        {
            if (_cts is null) return;
            try { _cts.Cancel(); }
            catch { /* ignore */ }

            if (_loop is not null)
            {
                try { await _loop.ConfigureAwait(false); } catch { /* ignore */ }
            }

            _loop = null;
            _cts.Dispose();
            _cts = null;
        }

        private async Task RunAsync(CancellationToken ct)
        {
            try
            {
                await foreach (Clip clip in _clipboard.WatchChangesAsync(ct).ConfigureAwait(false))
                {
                    if (!AutoPushOnChange) continue;

                    // Выбираем целевое устройство. Для MVP — "предпочитаемое" из реестра.
                    Device? target = await _devices.GetPreferredTargetAsync(ct).ConfigureAwait(false);
                    if (target is null) continue; // не к кому отправлять

                    try
                    {
                        await _clipboardSync.PushLocalClipAsync(clip, target, ct).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (ct.IsCancellationRequested)
                    {
                        // нормальное завершение
                        break;
                    }
                    catch (Exception)
                    {
                        // Тут можно залогировать/поднять уведомление, но Application-слой минимален.
                        // Решение: проглатываем, чтобы watcher не падал от единичной ошибки.
                    }
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                // нормальная остановка
            }
        }
    }
}
