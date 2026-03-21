using ClipBeam.Application.Services.Devices;
using ClipBeam.Application.Services.Hashing;
using ClipBeam.Application.Services.Orchestration;
using ClipBeam.Application.Services.Pairing;
using ClipBeam.Application.Services.Sync;
using ClipBeam.Domain.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace ClipBeam.Application
{
    public static class DependencyInjection
    {
        extension (IServiceCollection services)
        {
            public IServiceCollection AddApplication()
            {
                services.AddSingleton<ChunckAssembler>();
                services.AddSingleton<SyncCoordinator>();
                services.AddSingleton<DeviceRegistry>();
                services.AddSingleton<CentralOrchestrator>();
                services.AddSingleton<PairingService>();

                services.AddSingleton<IHasherProvider, Sha256HasherProvider>();
                
                return services;
            }
        }
    }
}
