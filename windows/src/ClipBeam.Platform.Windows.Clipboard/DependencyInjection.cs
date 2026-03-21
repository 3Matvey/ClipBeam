using ClipBeam.Application.Abstractions.Clipboard;
using ClipBeam.Domain.Abstractions;
using ClipBeam.Domain.Devices;
using Microsoft.Extensions.DependencyInjection;
using System.Collections;

namespace ClipBeam.Platform.Windows.Clipboard
{
    public static class DependencyInjection
    {
        extension(IServiceCollection services)
        {
            public IServiceCollection AddWinClipboard()
            {
                //services.AddSingleton<IClipboardPort, WindowsClipboardPort>();
                services.AddSingleton<IClipboardPort>(sp =>
                    new WindowsClipboardPort(
                        sp.GetRequiredService<IHasherProvider>(),
                        sp.GetRequiredService<Device>().Id
                    )
                );

                return services;
            }
        }
    }
}
