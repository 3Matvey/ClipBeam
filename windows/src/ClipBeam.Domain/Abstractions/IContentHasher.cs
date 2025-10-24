using ClipBeam.Domain.Clips;

namespace ClipBeam.Domain.Abstractions
{
    public interface IContentHasher
    {
        HashAlgo Algo { get; }
        Hash Compute(ReadOnlySpan<byte> data);
    }
}
