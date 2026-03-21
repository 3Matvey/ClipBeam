using ClipBeam.Domain.Clips;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace ClipBeam.Application.Services.Orchestration
{
    internal sealed class EchoGuard
    {
        private readonly ConcurrentDictionary<Hash, DateTime> _recent = new();
        private readonly TimeSpan _ttl = TimeSpan.FromSeconds(3);

        public void MarkApplied(Hash hash)
        {
            Cleanup();
            _recent[hash] = DateTime.UtcNow;
        }

        public bool IsEcho(Hash hash)
        {
            Cleanup();
            return _recent.ContainsKey(hash);
        }

        private void Cleanup()
        {
            var now = DateTime.UtcNow;
            foreach (var kv in _recent)
                if (now - kv.Value > _ttl)
                    _recent.TryRemove(kv.Key, out _);
        }
    }
}
