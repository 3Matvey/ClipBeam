using System;
using System.Linq;

using DomainPlatform = ClipBeam.Domain.Devices.Platform;
using ProtoPlatform = ClipBeam.Proto.Platform;

using DomainContentType = ClipBeam.Domain.Clips.ContentType;
using ProtoContentType = ClipBeam.Proto.ContentType;

using DomainCaps = ClipBeam.Domain.Devices.Capabilities;
using ProtoCaps = ClipBeam.Proto.Capabilities;

using DomainAuthScheme = ClipBeam.Domain.Devices.AuthScheme;
using ProtoAuthScheme = ClipBeam.Proto.AuthScheme;

using DomainHashAlgo = ClipBeam.Domain.Clips.HashAlgo;
using ProtoHashAlgo = ClipBeam.Proto.HashAlgo;

using DomainHash = ClipBeam.Domain.Clips.Hash;
using ProtoHash = ClipBeam.Proto.Hash;

using DomainClipMeta = ClipBeam.Domain.Clips.ClipMeta;
using ProtoClipMeta = ClipBeam.Proto.ClipMeta;

using DomainImageMeta = ClipBeam.Domain.Clips.Image.ImageMeta;
using ProtoImageMeta = ClipBeam.Proto.ImageMeta;

using DomainImageFormat = ClipBeam.Domain.Clips.Image.ImageFormat;
using ProtoImageFormat = ClipBeam.Proto.ImageFormat;

using DomainExifOrientation = ClipBeam.Domain.Clips.Image.ExifOrientation;
using ProtoExifOrientation = ClipBeam.Proto.ExifOrientation;

using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace ClipBeam.Infrastructure.Grpc.Mapping
{
    internal static class MappingExtensions
    {
        // ---------------- Platform ----------------

        public static ProtoPlatform ToProto(this DomainPlatform platform) =>
            platform switch
            {
                DomainPlatform.Windows => ProtoPlatform.Windows,
                DomainPlatform.Android => ProtoPlatform.Android,
                _ => ProtoPlatform.Unspecified
            };

        public static DomainPlatform FromProto(this ProtoPlatform platform) =>
            platform switch
            {
                ProtoPlatform.Windows => DomainPlatform.Windows,
                ProtoPlatform.Android => DomainPlatform.Android,
                _ => DomainPlatform.Unspecified
            };

        // ---------------- ContentType ----------------

        public static ProtoContentType ToProto(this DomainContentType type) =>
            type switch
            {
                DomainContentType.Text => ProtoContentType.Text,
                DomainContentType.Image => ProtoContentType.Image,
                _ => ProtoContentType.Unspecified
            };

        public static DomainContentType FromProto(this ProtoContentType type) =>
            type switch
            {
                ProtoContentType.Text => DomainContentType.Text,
                ProtoContentType.Image => DomainContentType.Image,
                _ => DomainContentType.Unspecified
            };

        // ---------------- AuthScheme ----------------
        // В proto: AUTH_SCHEME_MTLS
        // В домене: MTls

        public static ProtoAuthScheme ToProto(this DomainAuthScheme scheme) =>
            scheme switch
            {
                DomainAuthScheme.Tls => ProtoAuthScheme.Tls,
                DomainAuthScheme.MTls => ProtoAuthScheme.Mtls,
                DomainAuthScheme.Token => ProtoAuthScheme.Token,
                _ => ProtoAuthScheme.Unspecified
            };

        public static DomainAuthScheme FromProto(this ProtoAuthScheme scheme) =>
            scheme switch
            {
                ProtoAuthScheme.Tls => DomainAuthScheme.Tls,
                ProtoAuthScheme.Mtls => DomainAuthScheme.MTls,
                ProtoAuthScheme.Token => DomainAuthScheme.Token,
                _ => DomainAuthScheme.Unspecified
            };

        // ---------------- Capabilities ----------------
        // СТРОГО по твоему proto Capabilities:
        // preferred_chunk_bytes, max_chunk_bytes, supports_hash_dedup, supported_types

        public static ProtoCaps ToProto(this DomainCaps caps)
        {
            if (caps is null) throw new ArgumentNullException(nameof(caps));

            var proto = new ProtoCaps
            {
                PreferredChunkBytes = caps.PreferredChunkBytes,
                SupportsHashDedup = caps.SupportsHashDedup
            };

            if (caps.MaxChunkBytes is { } max)
                proto.MaxChunkBytes = max;

            proto.SupportedTypes.AddRange(
                caps.SupportedTypes.Select(t => t.ToProto())
            );

            return proto;
        }

        // Если понадобится: Capabilities из Proto обратно в Domain
        public static DomainCaps FromProto(this ProtoCaps caps)
        {
            if (caps is null) throw new ArgumentNullException(nameof(caps));

            return new DomainCaps(
                preferredChunkBytes: caps.PreferredChunkBytes,
                maxChunkBytes: caps.HasMaxChunkBytes ? caps.MaxChunkBytes : (uint?)null,
                supportsHashDedup: caps.SupportsHashDedup,
                supportedTypes: caps.SupportedTypes.Select(t => t.FromProto())
            );
        }

        // ---------------- HashAlgo ----------------

        public static ProtoHashAlgo ToProto(this DomainHashAlgo algo) =>
            algo switch
            {
                DomainHashAlgo.Sha256 => ProtoHashAlgo.Sha256,
                DomainHashAlgo.Crc32 => ProtoHashAlgo.Crc32,
                _ => ProtoHashAlgo.Unspecified
            };

        public static DomainHashAlgo FromProto(this ProtoHashAlgo algo) =>
            algo switch
            {
                ProtoHashAlgo.Sha256 => DomainHashAlgo.Sha256,
                ProtoHashAlgo.Crc32 => DomainHashAlgo.Crc32,
                _ => DomainHashAlgo.Unspecified
            };

        // ---------------- Hash ----------------

        public static ProtoHash ToProto(this DomainHash hash)
        {
            return new ProtoHash
            {
                Algo = hash.Algo.ToProto(),
                Value = ByteString.CopyFrom(hash.Value.Span)
            };
        }

        public static DomainHash ToDomain(this ProtoHash hash)
        {
            if (hash is null) throw new ArgumentNullException(nameof(hash));
            return new DomainHash(hash.Algo.FromProto(), hash.Value?.Memory ?? ReadOnlyMemory<byte>.Empty);
        }

        // ---------------- ImageFormat ----------------

        public static ProtoImageFormat ToProto(this DomainImageFormat f) =>
            f switch
            {
                DomainImageFormat.Png => ProtoImageFormat.Png,
                DomainImageFormat.Webp => ProtoImageFormat.Webp,
                DomainImageFormat.Jpeg => ProtoImageFormat.Jpeg,
                _ => ProtoImageFormat.Unspecified
            };

        public static DomainImageFormat FromProto(this ProtoImageFormat f) =>
            f switch
            {
                ProtoImageFormat.Png => DomainImageFormat.Png,
                ProtoImageFormat.Webp => DomainImageFormat.Webp,
                ProtoImageFormat.Jpeg => DomainImageFormat.Jpeg,
                _ => DomainImageFormat.Unspecified
            };

        // ---------------- ExifOrientation ----------------

        public static ProtoExifOrientation ToProto(this DomainExifOrientation o) =>
            o switch
            {
                DomainExifOrientation.Normal => ProtoExifOrientation.Normal,
                DomainExifOrientation.MirrorHorizontal => ProtoExifOrientation.MirrorHorizontal,
                DomainExifOrientation.Rotate180 => ProtoExifOrientation.Rotate180,
                DomainExifOrientation.MirrorVertical => ProtoExifOrientation.MirrorVertical,
                DomainExifOrientation.MirrorHorizontalRotate270Cw => ProtoExifOrientation.MirrorHorizontalRotate270Cw,
                DomainExifOrientation.Rotate90Cw => ProtoExifOrientation.Rotate90Cw,
                DomainExifOrientation.MirrorHorizontalRotate90Cw => ProtoExifOrientation.MirrorHorizontalRotate90Cw,
                DomainExifOrientation.Rotate270Cw => ProtoExifOrientation.Rotate270Cw,
                _ => ProtoExifOrientation.Unspecified
            };

        public static DomainExifOrientation FromProto(this ProtoExifOrientation o) =>
            o switch
            {
                ProtoExifOrientation.Normal => DomainExifOrientation.Normal,
                ProtoExifOrientation.MirrorHorizontal => DomainExifOrientation.MirrorHorizontal,
                ProtoExifOrientation.Rotate180 => DomainExifOrientation.Rotate180,
                ProtoExifOrientation.MirrorVertical => DomainExifOrientation.MirrorVertical,
                ProtoExifOrientation.MirrorHorizontalRotate270Cw => DomainExifOrientation.MirrorHorizontalRotate270Cw,
                ProtoExifOrientation.Rotate90Cw => DomainExifOrientation.Rotate90Cw,
                ProtoExifOrientation.MirrorHorizontalRotate90Cw => DomainExifOrientation.MirrorHorizontalRotate90Cw,
                ProtoExifOrientation.Rotate270Cw => DomainExifOrientation.Rotate270Cw,
                _ => DomainExifOrientation.Unspecified
            };

        // ---------------- ImageMeta ----------------

        public static ProtoImageMeta ToProto(this DomainImageMeta meta)
        {
            if (meta is null) throw new ArgumentNullException(nameof(meta));

            return new ProtoImageMeta
            {
                Format = meta.Format.ToProto(),
                Width = checked((uint)meta.Width),
                Height = checked((uint)meta.Height),
                Orientation = meta.Orientation.ToProto(),
                Mime = meta.Mime ?? ""
            };
        }

        public static DomainImageMeta ToDomain(this ProtoImageMeta meta)
        {
            if (meta is null) throw new ArgumentNullException(nameof(meta));

            return new DomainImageMeta(
                format: meta.Format.FromProto(),
                width: checked((int)meta.Width),
                height: checked((int)meta.Height),
                orientation: meta.Orientation.FromProto(),
                mime: meta.Mime ?? ""
            );
        }

        // ---------------- ClipMeta ----------------
        // Hash теперь optional: Hash? contentHash

        public static DomainClipMeta ToDomain(this ProtoClipMeta meta)
        {
            if (meta is null) throw new ArgumentNullException(nameof(meta));

            if (!Guid.TryParse(meta.ClipId, out var clipId) || clipId == Guid.Empty)
                throw new ArgumentException("Proto ClipMeta.clip_id must be a non-empty GUID.", nameof(meta));

            var createdUtc = meta.CreatedUtc is null
                ? DateTime.UtcNow
                : DateTime.SpecifyKind(meta.CreatedUtc.ToDateTime(), DateTimeKind.Utc);

            var type = meta.Type.FromProto();
            if (type == DomainContentType.Unspecified)
                throw new ArgumentException("Proto ClipMeta.type must be specified.", nameof(meta));

            DomainHash? contentHash = meta.ContentHash is null ? null : meta.ContentHash.ToDomain();
            DomainImageMeta? imageMeta = meta.Image is null ? null : meta.Image.ToDomain();

            // ВАЖНО: ctor ClipMeta у тебя internal, так что эта сборка должна иметь доступ.
            return new DomainClipMeta(
                clipId: clipId,
                originDeviceId: meta.OriginDeviceId ?? string.Empty,
                seq: meta.Seq,
                type: type,
                contentHash: contentHash,
                totalSize: meta.TotalSize,
                createdUtc: createdUtc,
                protoVersion: meta.ProtoVersion,
                imageMeta: imageMeta
            );
        }

        public static ProtoClipMeta ToProto(this DomainClipMeta meta)
        {
            if (meta is null) throw new ArgumentNullException(nameof(meta));

            var pm = new ProtoClipMeta
            {
                ClipId = meta.ClipId.ToString("D"),
                OriginDeviceId = meta.OriginDeviceId,
                Seq = meta.Seq,
                Type = meta.Type.ToProto(),
                TotalSize = meta.TotalSize,
                ProtoVersion = meta.ProtoVersion,
                CreatedUtc = Timestamp.FromDateTime(meta.CreatedUtc) // у тебя CreatedUtc уже UTC по доменным правилам
            };

            if (meta.ContentHash is { } h)
                pm.ContentHash = h.ToProto();

            if (meta.ImageMeta is { } im)
                pm.Image = im.ToProto();

            return pm;
        }
    }
}
