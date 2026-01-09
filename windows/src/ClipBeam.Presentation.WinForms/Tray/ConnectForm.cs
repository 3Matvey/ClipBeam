using ClipBeam.Application.Abstractions.Pairing;


namespace ClipBeam.Presentation.WinForms.Tray
{
    public sealed class ConnectForm : Form
    {
        private readonly PairingService _pairing;
        private readonly PictureBox _pic = new()
        {
            Dock = DockStyle.Fill,
            SizeMode = PictureBoxSizeMode.Zoom
        };

        public ConnectForm(PairingService pairing)
        {
            _pairing = pairing;

            Text = "Connect (QR)";
            Width = 420;
            Height = 640;

            Controls.Add(_pic);

            Load += async (_, __) => await RefreshQrAsync();
        }

        private async Task RefreshQrAsync()
        {
            var qr = await _pairing.CreateQrAsync(CancellationToken.None);

            using var ms = new MemoryStream(qr.QrImageBytes);
            _pic.Image?.Dispose();
            _pic.Image = Image.FromStream(ms);
        }
    }
}
