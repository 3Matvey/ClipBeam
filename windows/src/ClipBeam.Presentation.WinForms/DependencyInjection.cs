using ClipBeam.Presentation.WinForms.Tray;
using Microsoft.Extensions.DependencyInjection;

namespace ClipBeam.Presentation.WinForms
{
    public static class DependencyInjection
    {
        extension(IServiceCollection services)
        {
            public IServiceCollection AddWinFormsPresentation()
            {
                services.AddTransient<ConnectForm>();

                services.AddSingleton<Func<ConnectForm>>(sp => () => sp.GetRequiredService<ConnectForm>());

                services.AddSingleton<TrayAppContext>();

                return services;
            }
        }
    }
}
