using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using ClipBeam.Application.Abstractions.Clipboard;
using ClipBeam.Domain.Abstractions;
using ClipBeam.Domain.Clips;
using ClipBeam.Domain.Clips.Image;
using ClipBeam.Domain.Clips.Text;

using FormsClipboard = System.Windows.Forms.Clipboard;

namespace ClipBeam.Platform.Windows.Clipboard
{
    [SupportedOSPlatform("windows")]
    public sealed class WindowsClipboardPort(IHasherProvider hashers, string originDeviceId) : IClipboardPort, IDisposable
    {
        private readonly StaThreadRunner _sta = new();
        private readonly ClipboardChangeWindow _watcher = new();

        private readonly IHasherProvider _hashers = hashers 
            ?? throw new ArgumentNullException(nameof(hashers));
        private readonly string _originDeviceId = originDeviceId 
            ?? throw new ArgumentNullException(nameof(originDeviceId));

        public Task<Clip?> ReadAsync(CancellationToken ct)
            => _sta.RunAsync(() =>
            {
                if (Retry( FormsClipboard.ContainsText ))
                {
                    string raw = Retry(() => FormsClipboard.GetText(TextDataFormat.UnicodeText)) ?? string.Empty;
                    var clip = ClipFactory.CreateText(
                        originDeviceId: _originDeviceId,
                        seq: 0UL,
                        text: raw,
                        hashers: _hashers
                    );
                    return (Clip?)clip;
                }

                if (Retry(FormsClipboard.ContainsImage))
                {
                    using var img = Retry(FormsClipboard.GetImage);
                    if (img is not null)
                    {
                        var encoded = EncodeImageToPng(img);

                        var meta = new ImageMeta(
                            format: ImageFormat.Png,
                            width: img.Width,
                            height: img.Height,
                            orientation: ExifOrientation.Normal,
                            mime: "image/png"
                        );

                        var clip = ClipFactory.CreateImage(
                            originDeviceId: _originDeviceId,
                            seq: 0UL,
                            imageMeta: meta,
                            encodedImageBytes: encoded,
                            hashers: _hashers
                        );

                        return (Clip?)clip;
                    }
                }

                return null;
            }, ct);

        public Task SetAsync(Clip clip, CancellationToken ct)
            => _sta.RunAsync(() =>
            {
                switch (clip.Content)
                {
                    case TextClipContent text:
                        Retry(() => FormsClipboard.SetText(text.TextNfcLf));
                        return;
                    case ImageClipContent image:
                        using (var bmp = DecodeBitmapFromEncoded(image))
                            Retry(() => FormsClipboard.SetImage(bmp));
                        return;

                    default: 
                        return;
                }
            }, ct);

        public async IAsyncEnumerable<Clip> WatchChangesAsync([EnumeratorCancellation] CancellationToken ct)
        {
            var eventsStream = _watcher.Subscribe(ct);
            await foreach (var _ in eventsStream.WithCancellation(ct).ConfigureAwait(false))
            {
                var clip = await ReadAsync(ct).ConfigureAwait(false);

                if (clip is not null) 
                    yield return clip;
            }
        }

        public void Dispose()
        {
            _watcher.Dispose();
            _sta.Dispose();
        }

        #region helpers 
        private static ReadOnlyMemory<byte> EncodeImageToPng(Image img)
        {
            using var ms = new MemoryStream();
            img.Save(ms, System.Drawing.Imaging.ImageFormat.Png);

            if (ms.TryGetBuffer(out ArraySegment<byte> seg) && seg.Array is not null)
                return new ReadOnlyMemory<byte>(seg.Array, 0, (int)ms.Length);
            else
                return new ReadOnlyMemory<byte>(ms.ToArray());
        }

        private static Bitmap DecodeBitmapFromEncoded(ImageClipContent image)
        {
            if (MemoryMarshal.TryGetArray(image.Raw, out ArraySegment<byte> seg) && seg.Array is not null)
            {
                using var mem = new MemoryStream(seg.Array, seg.Offset, seg.Count, writable: false, publiclyVisible: true);
                using var img = Image.FromStream(mem, useEmbeddedColorManagement: true, validateImageData: true);
                return new Bitmap(img);
            }
            else
            {
                var arr = image.Raw.ToArray();
                using var mem = new MemoryStream(arr, writable: false);
                using var img = Image.FromStream(mem, useEmbeddedColorManagement: true, validateImageData: true);
                return new Bitmap(img);
            }
        }


        private static T? Retry<T> (Func<T> action, int attempt = 10, int delayMs = 100)
        {
            for (int i = 0; i < attempt; i++)
            {
                try
                {
                    return action();
                }
                catch (ExternalException) when (i + 1 < attempt)
                {
                    Thread.Sleep(delayMs);
                }
            }

            return action();
        }

        private static void Retry(Action action, int attempt = 10, int delayMs = 100)
        {
            for (int i = 0; i < attempt; i++)
            {
                try
                {
                    action();
                    return;
                }
                catch (ExternalException) when (i + 1 < attempt)
                {
                    Thread.Sleep(delayMs);
                }
            }

            action();
        }
        #endregion
    }
}
