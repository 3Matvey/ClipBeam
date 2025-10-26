using ClipBeam.Domain.Clips;
using ClipBeam.Domain.Shared;

namespace ClipBeam.Domain.Devices
{
    public sealed class Capabilities
    {
        public bool SupportsImages { get; }
        public uint PreferredChunkBytes { get; }
        public uint? MaxChunkBytes { get; }
        public bool SupportsAckWindow { get; }
        public bool SupportsHashDedup { get; }
        public IReadOnlyCollection<ContentType> SupportedTypes { get; }
        public IReadOnlyCollection<ChunkCompression> SupportedChunkCompressions { get; }

        public Capabilities(
            bool supportsImages,
            uint preferredChunkBytes,
            uint? maxChunkBytes,
            bool supportsAckWindow,
            bool supportsHashDedup,
            IEnumerable<ContentType> supportedTypes,
            IEnumerable<ChunkCompression> supportedCompressions)
        {
            if (preferredChunkBytes == 0)
                throw new DomainException("PreferredChunkBytes must be > 0.");

            if (maxChunkBytes is { } m && m < preferredChunkBytes)
                throw new DomainException("MaxChunkBytes must be >= PreferredChunkBytes.");

            var types = (supportedTypes ?? [])
                .Distinct()
                .ToArray();
            if (types.Length == 0)
                throw new DomainException("At least one SupportedType is required.");

            var compressions = (supportedCompressions ?? [])
                .Distinct()
                .ToArray();

            SupportsImages = supportsImages;
            PreferredChunkBytes = preferredChunkBytes;
            MaxChunkBytes = maxChunkBytes;
            SupportsAckWindow = supportsAckWindow;
            SupportsHashDedup = supportsHashDedup;
            SupportedTypes = types;
            SupportedChunkCompressions = compressions;
        }

        public bool Supports(ContentType type) => SupportedTypes.Contains(type);
    }
}

