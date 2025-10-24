using ClipBeam.Domain.Clips;

namespace ClipBeam.Domain.Abstractions
{
    public interface IHasherProvider
    {
        IContentHasher Get(HashAlgo algo);
        bool TryGet(HashAlgo algo, out IContentHasher? hasher);
    }
}
