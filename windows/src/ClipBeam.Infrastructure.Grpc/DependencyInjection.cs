using ClipBeam.Application.Abstractions.Transport;
using ClipBeam.Application.Services.Orchestration;
using ClipBeam.Infrastructure.Grpc.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ClipBeam.Infrastructure.Grpc
{
    public static class DependencyInjection
    {
        extension(IServiceCollection services)
        {
            public IServiceCollection AddInfrastructureGrpc()
            {
                services.AddSingleton<ClipSyncClientOptions>();
                services.AddTransient<IClipSyncClient, ClipSyncClient>();

                services.AddSingleton<ClipSyncEndpoint>();

                return services;
            }
        }
    }
}
