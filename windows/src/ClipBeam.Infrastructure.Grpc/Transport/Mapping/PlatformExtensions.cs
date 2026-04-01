using ClipBeam.Domain.Devices;
using ProtoPlatform = ClipBeam.Proto.Platform;

namespace ClipBeam.Infrastructure.Grpc.Transport.Mapping
{
    internal static class PlatformExtensions
    {
        public static ProtoPlatform ToProto(this Platform platform)
        {
            return platform switch
            {
                Platform.Windows => ProtoPlatform.Windows,
                Platform.Android => ProtoPlatform.Android,
                _ => ProtoPlatform.Unspecified
            };
        }

        public static Platform FromProto(this ProtoPlatform platform)
        {
            return platform switch
            {
                ProtoPlatform.Windows => Platform.Windows,
                ProtoPlatform.Android => Platform.Android,
                _ => Platform.Unspecified
            };
        }
    }
}
