using ClipBeam.Domain.Abstractions;
using ClipBeam.Domain.Clips.Image;
using ClipBeam.Domain.Clips.Text;
using ClipBeam.Domain.Shared;

namespace ClipBeam.Domain.Clips
{
    public static class ClipFactory
    {
        public const uint CurrentProtoVersion = 1;

        public static Clip CreateText(
            string originDeviceId,
            ulong seq,
            string? text,
            IHasherProvider hashers,
            HashAlgo algo = HashAlgo.Sha256,
            DateTime? createdUtcOverride = null)
        {
            ArgumentNullException.ThrowIfNull(hashers);

            var content = TextClipContent.FromRaw(text);

            var hasher = hashers.Get(algo);
            var hash = hasher.Compute(content.Raw.Span);

            var createdUtc = createdUtcOverride ?? DateTime.UtcNow;
            if (createdUtc.Kind != DateTimeKind.Utc)
                createdUtc = DateTime.SpecifyKind(createdUtc, DateTimeKind.Utc);

            var meta = new ClipMeta(
                clipId: Guid.NewGuid(),
                originDeviceId: originDeviceId,
                seq: seq,
                type: ContentType.Text,
                contentHash: hash,
                totalSize: (ulong)content.Raw.Length,
                createdUtc: createdUtc,
                protoVersion: CurrentProtoVersion
            );

            return new Clip(meta, content);
        }

        public static Clip CreateImage(
            string originDeviceId,
            ulong seq,
            ImageMeta imageMeta,
            ReadOnlyMemory<byte> encodedImageBytes,
            IHasherProvider hashers,
            HashAlgo algo = HashAlgo.Sha256,
            DateTime? createdUtcOverride = null)
        {
            ArgumentNullException.ThrowIfNull(hashers);
            ArgumentNullException.ThrowIfNull(imageMeta);
            if (encodedImageBytes.Length <= 0)
                throw new DomainException("Image bytes must be non-empty.");

            var content = new ImageClipContent(imageMeta, encodedImageBytes);

            var hasher = hashers.Get(algo);
            var hash = hasher.Compute(content.Raw.Span);

            var createdUtc = createdUtcOverride ?? DateTime.UtcNow;
            if (createdUtc.Kind != DateTimeKind.Utc)
                createdUtc = DateTime.SpecifyKind(createdUtc, DateTimeKind.Utc);

            var meta = new ClipMeta(
                clipId: Guid.NewGuid(),
                originDeviceId: originDeviceId,
                seq: seq,
                type: ContentType.Image,
                contentHash: hash,
                totalSize: (ulong)content.Raw.Length,
                createdUtc: createdUtc,
                protoVersion: CurrentProtoVersion
            );

            return new Clip(meta, content);
        }
    }
}
