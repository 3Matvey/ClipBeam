using ClipBeam.Presentation.WinForms.Tray;
using WinFormsApplication = System.Windows.Forms.Application;

namespace ClipBeam.Presentation.WinForms
{
    public static class Bootstrapper
    {
        public static void Run(IServiceProvider services)
        {
            ArgumentNullException.ThrowIfNull(services);

            WinFormsApplication.EnableVisualStyles();
            WinFormsApplication.SetCompatibleTextRenderingDefault(false);

            var ctx = services.GetService(typeof(TrayAppContext)) as ApplicationContext 
                ?? throw new InvalidOperationException("TrayAppContext is not registered in DI.");

            WinFormsApplication.Run(ctx);
        }
    }
}