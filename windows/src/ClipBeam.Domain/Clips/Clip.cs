using ClipBeam.Domain.Shared;

namespace ClipBeam.Domain.Clips
{
    public sealed class Clip
    {
        public ClipMeta Meta { get; }
        public ClipContent Content { get; }

        internal Clip(ClipMeta meta, ClipContent content)
        {
            Meta = meta ?? throw new ArgumentNullException(nameof(meta));
            Content = content ?? throw new ArgumentNullException(nameof(content));

            if (Meta.Type != Content.Type)
                throw new DomainException("Meta.Type doesn't match content type.");
            if (Meta.TotalSize != (ulong)Content.Raw.Length)
                throw new DomainException("Meta.TotalSize doesn't match content length.");
        }

        public bool MatchesHash(Hash hash) => Meta.ContentHash.Equals(hash);
    }
}
