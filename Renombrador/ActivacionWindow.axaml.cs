using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using LicenciaKit;

namespace Renombrador
{
    public partial class ActivacionWindow : Window
    {
        public ActivacionWindow()
        {
            InitializeComponent();
        }

        private void OnActivar(object? sender, RoutedEventArgs e)
        {
            string clave = TxtClave.Text ?? "";
            ResultadoValidacion r = GestorLicencia.Validar(clave);

            if (!r.EsValida)
            {
                LblMensaje.Foreground = Avalonia.Media.Brushes.IndianRed;
                LblMensaje.Text = "No se pudo activar: " + r.Mensaje;
                return;
            }

            GestorLicencia.Guardar(clave);
            AbrirPrincipal();
        }

        private void AbrirPrincipal()
        {
            MainWindow principal = new MainWindow(true);
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                desktop.MainWindow = principal;
            principal.Show();
            Close();
        }
    }
}
