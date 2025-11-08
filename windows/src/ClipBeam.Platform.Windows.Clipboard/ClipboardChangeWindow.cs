using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading.Channels;
using System.Windows.Forms;

namespace ClipBeam.Platform.Windows.Clipboard
{
    internal readonly record struct ClipboardChange(DateTime UtcTimestamp);

    internal sealed partial class ClipboardChangeWindow : NativeWindow, IDisposable
    {
        private const int WM_CLIPBOARDUPDATE = 0x031D;
        private const int WM_DESTROY = 0x0002;
        private const int WM_CLOSE = 0x0010;

        [LibraryImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool AddClipboardFormatListener(IntPtr hwnd);

        [LibraryImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool RemoveClipboardFormatListener(IntPtr hwnd);

        [LibraryImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool PostMessageW(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        private readonly Channel<ClipboardChange> _channel;
        private readonly int _ownerThreadId;

        public ClipboardChangeWindow(int capacity = 64)
        {
            _channel = Channel.CreateBounded<ClipboardChange>(new BoundedChannelOptions(capacity)
            {
                SingleWriter = true,
                SingleReader = false,
                FullMode = BoundedChannelFullMode.DropOldest
            });

            _ownerThreadId = Environment.CurrentManagedThreadId;

            CreateHandle(new CreateParams
            {
                Caption = "ClipBeamClipboardWatcherHidden",
                X = 0,
                Y = 0,
                Height = 0,
                Width = 0,
                Style = 0
            });
        }

        public IAsyncEnumerable<ClipboardChange> Subscribe(CancellationToken ct = default)
            => _channel.Reader.ReadAllAsync(ct);

        protected override void OnHandleChange()
        {
            base.OnHandleChange();

            if (Handle != IntPtr.Zero)
            {
                if (!AddClipboardFormatListener(Handle))
                {
                    int err = Marshal.GetLastPInvokeError();
                    DestroyHandle();
                    throw new Win32Exception(err, "AddClipboardFormatListener failed.");
                }
            }
        }

        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case WM_CLIPBOARDUPDATE:
                    _channel.Writer.TryWrite(new ClipboardChange(DateTime.UtcNow));
                    break;

                case WM_CLOSE:
                    if (Handle != IntPtr.Zero)
                        DestroyHandle(); 
                    break;

                case WM_DESTROY:
                    if (Handle != IntPtr.Zero)
                        RemoveClipboardFormatListener(Handle);
                    _channel.Writer.TryComplete();
                    break;
            }

            base.WndProc(ref m);
        }

        public void Dispose()
        {
            if (Environment.CurrentManagedThreadId == _ownerThreadId)
            {
                if (Handle != IntPtr.Zero)
                {
                    RemoveClipboardFormatListener(Handle);
                    DestroyHandle();
                }

                _channel.Writer.TryComplete();
                GC.SuppressFinalize(this);
                return;
            }

            if (Handle != IntPtr.Zero)
                PostMessageW(Handle, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);

            GC.SuppressFinalize(this);
        }
    }
}
