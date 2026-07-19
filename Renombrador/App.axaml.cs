using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using LicenciaKit;

namespace Renombrador;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Entra siempre a la app: con licencia valida queda completa, si no en modo prueba.
            ResultadoValidacion? estado = GestorLicencia.EstadoGuardado();
            bool licenciado = estado != null && estado.EsValida;
            desktop.MainWindow = new MainWindow(licenciado);
        }

        base.OnFrameworkInitializationCompleted();
    }
}
