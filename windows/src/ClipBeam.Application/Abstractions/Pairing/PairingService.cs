using System.Globalization;

namespace ClipBeam.Application.Abstractions.Pairing
{
    public sealed class PairingService(IPairingTokenService tokens, IQrCodeGenerator qr, IPairingEndpointProvider endpoint)
    {
        public async Task<PairingQr> CreateQrAsync(CancellationToken ct)
        {
            var (host, port) = endpoint.GetEndpoint();

            var (tokenId, tokenRaw, expiresUtc) = await tokens.IssueAsync(ct).ConfigureAwait(false);

            string tokenB64 = Base64Url(tokenRaw);

            string exp = expiresUtc.ToString("O", CultureInfo.InvariantCulture);

            string payload =

                $"clipbeam://pair?v=1&host={Uri.EscapeDataString(host)}&port={port}" 
                +
                $"&tokenId={Uri.EscapeDataString(tokenId)}&token={Uri.EscapeDataString(tokenB64)}&exp={Uri.EscapeDataString(exp)}"
            ;

            byte[] pngBytes = qr.Render(payload);

            return new PairingQr(payload, pngBytes, expiresUtc);

        }

        private static string Base64Url(byte[] bytes)
        {
            var s = Convert.ToBase64String(bytes);
            return s.TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        public sealed record PairingQr(
            string Payload,
            byte[] QrImageBytes,
            DateTime ExpiresUtc
        );
    }
}
