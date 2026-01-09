using System;
using System.Drawing;
using System.Windows.Forms;

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

            // двойной клик по иконке = открыть окно подключения
            _tray.DoubleClick += (_, _) => ShowConnect();
        }

        private void ShowConnect()
        {
            // если окно уже есть — просто активируем
            if (_connectForm is { IsDisposed: false })
            {
                if (!_connectForm.Visible)
                    _connectForm.Show();

                _connectForm.Activate();
                return;
            }

            _connectForm = _connectFormFactory.Invoke();
            _connectForm.StartPosition = FormStartPosition.CenterScreen;

            // при закрытии формы мы НЕ выходим из приложения
            _connectForm.FormClosed += (_, _) => _connectForm = null;

            _connectForm.Show();
        }

        private void Exit()
        {
            _tray.Visible = false;
            _tray.Dispose();

            if (_connectForm is { IsDisposed: false })
            {
                _connectForm.Close();
                _connectForm.Dispose();
            }

            ExitThread(); // корректно завершает ApplicationContext
        }
    }
}
