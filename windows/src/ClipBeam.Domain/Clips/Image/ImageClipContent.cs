using ClipBeam.Domain.Shared;

namespace ClipBeam.Domain.Clips.Image
{
    public sealed class ImageClipContent : ClipContent
    {
        public ImageMeta Meta { get; }
        private readonly ReadOnlyMemory<byte> _raw;

        public override ReadOnlyMemory<byte> Raw => _raw;
        public override ContentType Type => ContentType.Image;

        internal ImageClipContent(ImageMeta meta, ReadOnlyMemory<byte> encodedBytes)
        {
            Meta = meta ?? throw new ArgumentNullException(nameof(meta));

            if (encodedBytes.Length <= 0)
                throw new DomainException("Empty image bytes.");
            _raw = encodedBytes;
        }
    }
}
