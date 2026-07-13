using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Steam;

namespace TiendaJuegos
{
    public partial class LoginWindow : Window
    {
        private readonly CuentaServicio _cuentas = new CuentaServicio();

        public LoginWindow()
        {
            InitializeComponent();
        }

        private void OnLogin(object? sender, RoutedEventArgs e)
        {
            string usuario = TxtUsuario.Text ?? "";
            string password = TxtPassword.Text ?? "";
            LblMensaje.Foreground = Avalonia.Media.Brushes.IndianRed;

            if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(password))
            {
                LblMensaje.Text = "Completa usuario y contrasena.";
                return;
            }
            if (!_cuentas.Validar(usuario, password))
            {
                LblMensaje.Text = "Usuario o contrasena incorrectos.";
                return;
            }

            AbrirPrincipal(usuario.Trim());
        }

        private async void OnAbrirRegistro(object? sender, RoutedEventArgs e)
        {
            RegisterWindow reg = new RegisterWindow();
            await reg.ShowDialog(this);

            if (reg.UsuarioCreado != null)
            {
                TxtUsuario.Text = reg.UsuarioCreado;
                TxtPassword.Text = "";
                LblMensaje.Foreground = Avalonia.Media.Brushes.LightGreen;
                LblMensaje.Text = "Cuenta creada. Ya podes iniciar sesion.";
            }
        }

        private void AbrirPrincipal(string usuario)
        {
            MainWindow principal = new MainWindow(usuario);
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                desktop.MainWindow = principal;
            principal.Show();
            Close();
        }
    }
}
