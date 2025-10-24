using ClipBeam.Domain.Shared;

namespace ClipBeam.Domain.Clips
{
    public sealed class ClipMeta
    {
        public Guid ClipId { get; }
        public string OriginDeviceId { get; }
        public ulong Seq { get; }
        public ContentType Type { get; }
        public Hash ContentHash { get; }
        public ulong TotalSize { get; }
        public DateTime CreatedUtc { get; }
        public uint ProtoVersion { get; }

        internal ClipMeta(
            Guid clipId,
            string originDeviceId,
            ulong seq,
            ContentType type,
            Hash contentHash,
            ulong totalSize,
            DateTime createdUtc,
            uint protoVersion)
        {
            if (clipId == Guid.Empty)
                throw new DomainException("ClipId is required.");

            if (string.IsNullOrWhiteSpace(originDeviceId))
                throw new DomainException("OriginDeviceId must be non-empty.");

            if (type == ContentType.Unspecified)
                throw new DomainException("ContentType must be specified.");

            if (totalSize == 0)
                throw new DomainException("TotalSize must be greater than zero.");

            if (createdUtc.Kind != DateTimeKind.Utc)
                throw new DomainException("CreatedUtc must be in UTC.");

            if (createdUtc > DateTime.UtcNow + TimeSpan.FromMinutes(5))
                throw new DomainException("CreatedUtc cannot be in the far future.");

            ClipId = clipId;
            OriginDeviceId = originDeviceId.Trim();
            Seq = seq;
            Type = type;
            ContentHash = contentHash;
            TotalSize = totalSize;
            CreatedUtc = createdUtc;
            ProtoVersion = protoVersion;
        }
    }
}
