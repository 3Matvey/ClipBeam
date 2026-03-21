using ClipBeam.Presentation.WinForms.Tray;

namespace ClipBeam.Presentation.WinForms
{
    public static class Bootstrapper
    {
        public static void Run(IServiceProvider services, CancellationTokenSource shutdownCts)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(shutdownCts);

            var ctx = services.GetService(typeof(TrayAppContext)) as TrayAppContext
                ?? throw new InvalidOperationException("TrayAppContext is not registered in DI.");

            ctx.AttachShutdown(shutdownCts);

            System.Windows.Forms.Application.Run(ctx);
        }
    }
}
