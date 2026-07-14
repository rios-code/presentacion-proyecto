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
            // Si ya hay una licencia guardada y valida, entra directo; si no, pide activacion.
            ResultadoValidacion? estado = GestorLicencia.EstadoGuardado();
            desktop.MainWindow = (estado != null && estado.EsValida)
                ? new MainWindow()
                : new ActivacionWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
