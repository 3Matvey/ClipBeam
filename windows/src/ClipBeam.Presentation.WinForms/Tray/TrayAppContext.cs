using System;
using System.Drawing;
using System.Windows.Forms;
using System.Threading;

namespace ClipBeam.Presentation.WinForms.Tray
{
    /// <summary>
    /// Application context for tray-only WinForms app.
    /// </summary>
    public sealed class TrayAppContext : ApplicationContext
    {
        private readonly NotifyIcon _tray;
        private readonly Func<ConnectForm> _connectFormFactory;

        private ConnectForm? _connectForm;

        // Used to stop Kestrel / background loops etc.
        private CancellationTokenSource? _shutdownCts;

        // Prevent double-exit race
        private int _exiting; // 0 = no, 1 = yes

        public TrayAppContext(Func<ConnectForm> connectFormFactory)
        {
            _connectFormFactory = connectFormFactory ?? throw new ArgumentNullException(nameof(connectFormFactory));

            var menu = new ContextMenuStrip();

            menu.Items.Add(
                text: "Connect (QR)…",
                image: null,
                onClick: (_, _) => ShowConnect());

            menu.Items.Add(new ToolStripSeparator());

            menu.Items.Add(
                text: "Exit",
                image: null,
                onClick: (_, _) => Exit());

            _tray = new NotifyIcon
            {
                Icon = SystemIcons.Application, // TODO: заменить на свой .ico
                Text = "ClipBeam",
                ContextMenuStrip = menu,
                Visible = true
            };

            // Double click on icon = open connect window
            _tray.DoubleClick += (_, _) => ShowConnect();
        }

        /// <summary>
        /// Called by Bootstrapper/Program to connect tray exit with app shutdown.
        /// </summary>
        public void AttachShutdown(CancellationTokenSource shutdownCts)
            => _shutdownCts = shutdownCts ?? throw new ArgumentNullException(nameof(shutdownCts));

        private void ShowConnect()
        {
            // if already created and not disposed -> activate
            if (_connectForm is { IsDisposed: false } existing)
            {
                if (!existing.Visible)
                    existing.Show();

                existing.Activate();
                return;
            }

            var form = _connectFormFactory.Invoke();
            _connectForm = form;

            form.StartPosition = FormStartPosition.CenterScreen;

            // when user closes it, we just drop reference
            form.FormClosed += (_, _) =>
            {
                if (ReferenceEquals(_connectForm, form))
                    _connectForm = null;
            };

            form.Show();
        }

        private void Exit()
        {
            // guard against double-clicks/races
            if (Interlocked.Exchange(ref _exiting, 1) == 1)
                return;

            // 1) Tell host/server to stop
            try { _shutdownCts?.Cancel(); } catch { /* ignore */ }

            // 2) Dispose tray icon
            try
            {
                _tray.Visible = false;
                _tray.Dispose();
            }
            catch { /* ignore */ }

            // 3) Close/Dispose connect form safely
            var form = _connectForm;
            _connectForm = null;

            if (form is not null && !form.IsDisposed)
            {
                try
                {
                    // Ensure it's on UI thread
                    if (form.InvokeRequired)
                    {
                        form.Invoke(new Action(() =>
                        {
                            try { form.Close(); } catch { }
                            try { form.Dispose(); } catch { }
                        }));
                    }
                    else
                    {
                        try { form.Close(); } catch { }
                        try { form.Dispose(); } catch { }
                    }
                }
                catch
                {
                    // do not crash on exit
                }
            }

            // 4) End WinForms message loop
            ExitThread();
        }
    }
}
