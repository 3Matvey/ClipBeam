using ClipBeam.Domain.Clips;
using ClipBeam.Domain.Shared;

namespace ClipBeam.Domain.Devices
{
    public sealed class Device
    {
        public string Id { get; }
        public string Name { get; private set; }
        public Platform Platform { get; }
        public uint AppVersionMajor { get; }
        public uint AppVersionMinor { get; }
        public uint AppVersionPatch { get; }
        public uint ProtoVersion { get; }
        public Capabilities Capabilities { get; }
        public AuthScheme AuthScheme { get; }
        public ReadOnlyMemory<byte>? CertFingerprintSha256 { get; }
        public string? TokenId { get; }

        public Device(
            string id,
            string name,
            Platform platform,
            uint appVersionMajor,
            uint appVersionMinor,
            uint appVersionPatch,
            uint protoVersion,
            Capabilities capabilities,
            AuthScheme authScheme,
            ReadOnlyMemory<byte>? certFingerprintSha256 = null,
            string? tokenId = null)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new DomainException($"{nameof(id)} must be non-empty.");

            if (string.IsNullOrWhiteSpace(name))
                throw new DomainException($"{nameof(name)} must be non-empty.");

            if (platform == Platform.Unspecified)
                throw new DomainException("Platform must be specified.");

            Id = id.Trim();
            Name = name.Trim();
            Platform = platform;
            AppVersionMajor = appVersionMajor;
            AppVersionMinor = appVersionMinor;
            AppVersionPatch = appVersionPatch;
            ProtoVersion = protoVersion;
            Capabilities = capabilities ?? throw new ArgumentNullException(nameof(capabilities));
            AuthScheme = authScheme;
            CertFingerprintSha256 = certFingerprintSha256;
            TokenId = tokenId;
        }

        public void Rename(string newName)
        {
            if (string.IsNullOrWhiteSpace(newName))
                throw new DomainException($"{nameof(newName)} must be non-empty.");
            Name = newName.Trim();
        }

        public bool CanReceive(ContentType type) => Capabilities.Supports(type);
    }
}
