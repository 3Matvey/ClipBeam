namespace ClipBeam.Infrastructure.Grpc.Services
{
    /// <summary>
    /// Transport-level options (network, limits).
    /// </summary>
    public sealed class ClipSyncClientOptions
    {
        /// <summary>
        /// gRPC server address (e.g. http://192.168.1.10:5107).
        /// </summary>
        public required string Address { get; init; }

        public int PreferredChunkBytes { get; init; } = 128 * 1024;
        public int MaxChunkBytes { get; init; } = 256 * 1024;

        public int? MaxReceiveMessageSizeBytes { get; init; } = 8 * 1024 * 1024;
        public int? MaxSendMessageSizeBytes { get; init; } = 8 * 1024 * 1024;

        public TimeSpan? HelloAckTimeout { get; init; } = TimeSpan.FromSeconds(3);
        public TimeSpan ShutdownGracePeriod { get; init; } = TimeSpan.FromSeconds(2);

        public HttpMessageHandler? HttpHandler { get; init; }
    }
}
