//using System;
//using System.Threading;
//using System.Threading.Tasks;
//using ClipBeam.Application.Abstractions.Clipboard;
//using ClipBeam.Domain.Clips;
//using ClipBeam.Domain.Devices;

//namespace ClipBeam.Application.Services.Clipboard  // TODO 
//{
//    /// <summary>
//    /// Мост между системным буфером (Windows) и механизмом синхронизации.
//    /// - Отправляет локально скопированный Clip на выбранное устройство.
//    /// - Применяет входящий Clip в системный буфер (если политика это допускает).
//    /// </summary>
//    public sealed class ClipboardSyncService
//    {
//        private readonly IClipboardPort _clipboard;
//        private readonly Sync.SyncCoordinator _sync;

//        // Простые "политики" как константы для MVP.
//        private const bool AutoApplyIncoming = true;   // входящие клипы кладём в системный буфер

//        public ClipboardSyncService(IClipboardPort clipboard, Sync.SyncCoordinator sync)
//        {
//            _clipboard = clipboard ?? throw new ArgumentNullException(nameof(clipboard));
//            _sync = sync ?? throw new ArgumentNullException(nameof(sync));
//        }

//        /// <summary>
//        /// Отправить локальный клип на конкретное устройство.
//        /// Сюда обычно зовёт ClipboardWatcher при изменении буфера.
//        /// </summary>
//        public async Task PushLocalClipAsync(Clip clip, Device target, CancellationToken ct)
//        {
//            if (clip is null) throw new ArgumentNullException(nameof(clip));
//            if (target is null) throw new ArgumentNullException(nameof(target));

//            // Здесь можно вставить дедупликацию по hash, проверку совместимости и т.д.
//            // Для MVP — просто отправляем.
//            await _sync.SendAsync(clip, target, ct).ConfigureAwait(false);
//        }

//        /// <summary>
//        /// Применить входящий клип в системный буфер обмена (если политика разрешает).
//        /// Вызывается из SyncCoordinator, когда удалённый клип полностью собран.
//        /// </summary>
//        public async Task ApplyRemoteClipAsync(Clip clip, CancellationToken ct)
//        {
//            if (clip is null) throw new ArgumentNullException(nameof(clip));
//            if (!AutoApplyIncoming) return;

//            await _clipboard.SetAsync(clip, ct).ConfigureAwait(false);
//        }
//    }
//}
