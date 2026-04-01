using ClipBeam.Application.Abstractions.Pairing;
using Clipbeam.Infrastructure.Pairing;
using Microsoft.Extensions.DependencyInjection;

namespace Clipbeam.Infrastructure.Pairing
{
    public static class DependencyInjection
    {
        extension(IServiceCollection services)
        {
            public IServiceCollection AddInfrastructurePairing()
            {
                services.AddSingleton<IPairingTokenService, InMemoryPairingTokenService>();
                services.AddSingleton<IQrCodeGenerator, QrCoderGenerator>();

                return services;
            }
        }
    }
}