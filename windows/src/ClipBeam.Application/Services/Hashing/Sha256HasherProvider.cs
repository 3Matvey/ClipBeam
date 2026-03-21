using ClipBeam.Domain.Abstractions;
using ClipBeam.Domain.Clips;
using System.Security.Cryptography;

namespace ClipBeam.Application.Services.Hashing
{
    public sealed class Sha256HasherProvider : IHasherProvider
    {
        private static readonly IContentHasher _sha256 = new Sha256ContentHasher();  

        public IContentHasher Get(HashAlgo algo) => 
            algo == HashAlgo.Sha256
                ? _sha256
                : throw new NotSupportedException($"Hash algorithm '{algo}' is not supported.");


        public bool TryGet(HashAlgo algo, out IContentHasher? hasher)
        {
            if (algo == HashAlgo.Sha256)
            {
                hasher = _sha256;
                return true;
            }

            hasher = null;
            return false; 
        }

        private sealed class Sha256ContentHasher : IContentHasher
        {
            public HashAlgo Algo => HashAlgo.Sha256;

            public Hash Compute(ReadOnlySpan<byte> data)
            {
                byte[] digest = SHA256.HashData(data);
                return new Hash(HashAlgo.Sha256, digest);
            }
        }

    }
}
