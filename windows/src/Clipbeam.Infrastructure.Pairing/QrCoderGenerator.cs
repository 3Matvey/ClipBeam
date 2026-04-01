using ClipBeam.Application.Abstractions.Pairing;
using QRCoder;

namespace Clipbeam.Infrastructure.Pairing
{
    public class QrCoderGenerator : IQrCodeGenerator
    {
        public byte[] Render(string payload)
        {
            using var generator = new QRCodeGenerator();
            using var data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.M);

            using var qr = new PngByteQRCode(data);
            return qr.GetGraphic(pixelsPerModule: 8);
        }
    }
}
