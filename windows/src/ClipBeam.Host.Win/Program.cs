using System;
using System.IO;
using System.Threading;
using Clipbeam.Infrastructure.Pairing;
using ClipBeam.Application;
using ClipBeam.Application.Abstractions.Devices;
using ClipBeam.Application.Abstractions.Pairing;
using ClipBeam.Application.Services.Hashing;
using ClipBeam.Application.Services.Orchestration;
using ClipBeam.Domain.Abstractions;
using ClipBeam.Domain.Clips;
using ClipBeam.Domain.Devices;
using ClipBeam.Host.Win;
using ClipBeam.Host.Win.Devices;
using ClipBeam.Infrastructure.Grpc;
using ClipBeam.Infrastructure.Grpc.Services;
using ClipBeam.Platform.Windows.Clipboard;
using ClipBeam.Presentation.WinForms;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

internal static class Program
{
    [STAThread]
    public static void Main()
    {
        // ✅ MUST be first: prevents WinForms text rendering exception
        ApplicationConfiguration.Initialize();

        const int GrpcPort = 5157;

        var builder = WebApplication.CreateBuilder();

        builder.WebHost.UseUrls($"http://0.0.0.0:{GrpcPort}");

        // gRPC server
        builder.Services.AddGrpc();

        // ---- Host-specific registrations ----

        builder.Services.AddSingleton<IDeviceStore>(_ =>
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ClipBeam");

            var path = Path.Combine(dir, "devices.json");
            return new JsonFileDeviceStore(path);
        });

        builder.Services.AddSingleton(_ =>
            new Device(
                id: Environment.MachineName,
                name: Environment.MachineName,
                platform: Platform.Windows,
                appVersionMajor: 1,
                appVersionMinor: 0,
                appVersionPatch: 0,
                protoVersion: 1,
                capabilities: new Capabilities(
                    preferredChunkBytes: 64 * 1024,
                    maxChunkBytes: 256 * 1024,
                    supportsHashDedup: true,
                    supportedTypes: new[] { ContentType.Text, ContentType.Image }
                ),
                authScheme: AuthScheme.Token,
                tokenId: "pairing"
            )
        );

        builder.Services.AddSingleton<IPairingEndpointProvider>(_ => new PairingEndpointProvider(GrpcPort));
        builder.Services.AddSingleton<IHasherProvider, Sha256HasherProvider>();

        builder.Services.AddSingleton(new ClipSyncClientOptions
        {
            Address = $"http://127.0.0.1:{GrpcPort}"
        });

        // ---- Layer DI ----
        builder.Services
            .AddInfrastructurePairing()
            .AddInfrastructureGrpc()
            .AddWinClipboard()
            .AddWinFormsPresentation()
            .AddApplication();

        var app = builder.Build();

        app.MapGrpcService<ClipSyncEndpoint>();

        using var shutdownCts = new CancellationTokenSource();

        // Start Kestrel without blocking UI (bound to shutdown token)
        var serverTask = app.RunAsync(shutdownCts.Token);

        // Start orchestrator loops (clipboard<->network)
        app.Services.GetRequiredService<CentralOrchestrator>()
            .Start(shutdownCts.Token);

        try
        {
            // Run WinForms tray (blocks until exit)
            // Exit в трее вызовет shutdownCts.Cancel()
            Bootstrapper.Run(app.Services, shutdownCts);
        }
        finally
        {
            // на всякий случай (если WinForms упал/исключение)
            if (!shutdownCts.IsCancellationRequested)
                shutdownCts.Cancel();
        }

        // Stop host gracefully
        app.StopAsync().GetAwaiter().GetResult();

        // Ensure server completes
        try
        {
            serverTask.GetAwaiter().GetResult();
        }
        catch (OperationCanceledException)
        {
            // expected on shutdown
        }
    }
}
