using Clipbeam.Infrastructure.Pairing;
using ClipBeam.Application.Abstractions.Pairing;
using ClipBeam.Host.Win;
using ClipBeam.Presentation.WinForms;
using ClipBeam.Presentation.WinForms.Tray;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows.Forms;

internal static class Program
{
    [STAThread]
    public static void Main()
    {
        var services = new ServiceCollection();

        services.AddSingleton<IPairingTokenService, InMemoryPairingTokenService>();
        services.AddSingleton<IQrCodeGenerator, QrCoderGenerator>();
        services.AddSingleton<PairingService>();

        services.AddTransient<ConnectForm>();
        services.AddSingleton<TrayAppContext>();

        const int GrpcPort = 5157;
        services.AddSingleton<IPairingEndpointProvider>(_ => new PairingEndpointProvider(GrpcPort));


        // Фабрика формы
        services.AddSingleton<Func<ConnectForm>>(sp => () => sp.GetRequiredService<ConnectForm>());

        // Контекст трея
        services.AddSingleton<TrayAppContext>();


        using var provider = services.BuildServiceProvider();

        Bootstrapper.Run(provider);
    }
}
