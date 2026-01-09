using ClipBeam.Application.Abstractions.Pairing;
using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace Clipbeam.Infrastructure.Pairing
{
    public class InMemoryPairingTokenService : IPairingTokenService
    {
        private sealed record Entry(byte[] Raw, DateTime ExpiresUtc, bool Used);

        private readonly ConcurrentDictionary<string, Entry> _tokens = [];
        private static readonly TimeSpan TokenLifeTime = TimeSpan.FromMinutes(5);

        public Task<(string tokenId, byte[] tokenRaw, DateTime expiresUtc)> IssueAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            var tokenId = Guid.NewGuid().ToString("N");
            var raw = RandomNumberGenerator.GetBytes(32);
            var expires = DateTime.UtcNow.Add(TokenLifeTime);

            _tokens[tokenId] = new Entry(raw, expires, Used: false);
            return Task.FromResult((tokenId, raw, expires));
        }

        public Task<bool> ValidateAsync(string tokenId, ReadOnlyMemory<byte> tokenRaw, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            if (!_tokens.TryGetValue(tokenId, out var e))
                return Task.FromResult(false);

            if (e.Used) return Task.FromResult(false);
            if (DateTime.UtcNow > e.ExpiresUtc) return Task.FromResult(false);

            // constant-time
            if (!CryptographicOperations.FixedTimeEquals(e.Raw, tokenRaw.Span))
                return Task.FromResult(false);

            _tokens[tokenId] = e with { Used = true };
            return Task.FromResult(true);
        }
    }
}
