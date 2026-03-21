using ClipBeam.Application.Abstractions.Clipboard;
using ClipBeam.Application.Services.Devices;
using ClipBeam.Application.Services.Sync;
using ClipBeam.Domain.Clips;
using ClipBeam.Domain.Devices;
using ClipBeam.Domain.Shared;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Channels;

namespace ClipBeam.Application.Services.Orchestration
{
    public sealed class CentralOrchestrator(
            IClipboardPort clipboard,
            SyncCoordinator sync,
            DeviceRegistry devices,
            Device local) : IAsyncDisposable
    {
        private readonly EchoGuard _echo = new();

        private readonly Channel<Clip> _inbound = Channel.CreateBounded<Clip>(
           new BoundedChannelOptions(128)
           {
               SingleReader = true,
               SingleWriter = false,
               FullMode = BoundedChannelFullMode.DropOldest
           });

        private CancellationTokenSource? _cts;
        private Task? _clipboardLoop;
        private Task? _inboundLoop;

        public void Start(CancellationToken ct)
        {
            if (_cts is not null) return;

            _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

            _clipboardLoop = Task.Run(() => ClipboardToNetworkLoop(_cts.Token), _cts.Token);
            _inboundLoop = Task.Run(() => InboundToClipboardLoop(_cts.Token), _cts.Token);
        }

        public ValueTask EnqueueRemoteAsync(Clip clip, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(clip);
            if (clip.Meta.ContentHash is null) 
                throw new DomainException("ContentHash is required for echo-guard.");

            if (_inbound.Writer.TryWrite(clip))
                return ValueTask.CompletedTask;

            return _inbound.Writer.WriteAsync(clip, ct);
        }

        private async Task InboundToClipboardLoop(CancellationToken ct)
        {
            await foreach (var clip in _inbound.Reader.ReadAllAsync(ct).ConfigureAwait(false))
            {
                var hash = clip.Meta.ContentHash
                        ?? throw new DomainException("ContentHash is required for echo-guard.");

                _echo.MarkApplied(hash);

                await clipboard.SetAsync(clip, ct).ConfigureAwait(false);
            }
        }

        private async Task ClipboardToNetworkLoop(CancellationToken ct)
        {
            await foreach (var clip in clipboard.WatchChangesAsync(ct).ConfigureAwait(false))
            {
                var hash = clip.Meta.ContentHash
                        ?? throw new DomainException("ContentHash is required for echo-guard.");

                if (_echo.IsEcho(hash))
                    continue;

                var active = await devices.GetActiveDeviceAsync(ct).ConfigureAwait(false);

                if (active is not null)
                {
                    await SendToPeerIfAllowed(clip, active, ct).ConfigureAwait(false);
                    continue;
                }

                var peers = await devices.ListAsync(ct).ConfigureAwait(false);

                foreach (var peer in peers)
                    await SendToPeerIfAllowed(clip, peer, ct).ConfigureAwait(false);
            }
        }

        private async Task SendToPeerIfAllowed(Clip clip, Device peer, CancellationToken ct)
        {
            if (peer.Id == local.Id) return;
            if (!peer.CanReceive(clip.Meta.Type)) return;

            await sync.SendAsync(clip, local, peer, ct).ConfigureAwait(false);
        }

        public async ValueTask DisposeAsync()
        {
            if (_cts is null) return;

            _cts.Cancel();

            try { if (_clipboardLoop is not null) await _clipboardLoop.ConfigureAwait(false); } catch { }
            try { if (_inboundLoop is not null) await _inboundLoop.ConfigureAwait(false); } catch { }

            _cts.Dispose();
            _cts = null;
        }
    }
}
