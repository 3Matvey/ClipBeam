using System.Globalization;
using ClipBeam.Application.Abstractions.Pairing;

namespace ClipBeam.Application.Services.Pairing
{
    public sealed class PairingService(
        IPairingTokenService tokens,
        IQrCodeGenerator qr,
        IPairingEndpointProvider endpoint)
    {
        public async Task<PairingQr> CreateQrAsync(CancellationToken ct)
        {
            var (host, port) = endpoint.GetEndpoint();

            var (tokenId, tokenRaw, expiresUtc) = await tokens.IssueAsync(ct).ConfigureAwait(false);

            var tokenB64Url = Base64Url(tokenRaw);

            var expUnix = new DateTimeOffset(expiresUtc).ToUnixTimeSeconds();

            var payload = BuildPayload(host, port, tokenId, tokenB64Url, expUnix);

            var pngBytes = qr.Render(payload);

            return new PairingQr(payload, pngBytes, expiresUtc);
        }

        private static string BuildPayload(string host, int port, string tokenId, string tokenB64Url, long expUnix)
        {
            if (string.IsNullOrWhiteSpace(host)) throw new ArgumentException("Host is required.", nameof(host));
            if (string.IsNullOrWhiteSpace(tokenId)) throw new ArgumentException("TokenId is required.", nameof(tokenId));
            if (string.IsNullOrWhiteSpace(tokenB64Url)) throw new ArgumentException("Token is required.", nameof(tokenB64Url));

            var query =
                $"v=1" +
                $"&host={Uri.EscapeDataString(host)}" +
                $"&port={port.ToString(CultureInfo.InvariantCulture)}" +
                $"&tokenId={Uri.EscapeDataString(tokenId)}" +
                $"&token={Uri.EscapeDataString(tokenB64Url)}" +
                $"&exp={expUnix.ToString(CultureInfo.InvariantCulture)}";

            return $"clipbeam://pair?{query}";
        }

        private static string Base64Url(byte[] bytes)
        {
            if (bytes is null) throw new ArgumentNullException(nameof(bytes));
            if (bytes.Length == 0) throw new ArgumentException("Empty token.", nameof(bytes));

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
