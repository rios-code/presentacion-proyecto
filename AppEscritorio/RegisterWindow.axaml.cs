using Avalonia.Controls;
using Avalonia.Interactivity;
using Steam;

namespace TiendaJuegos
{
    public partial class RegisterWindow : Window
    {
        private readonly CuentaServicio _cuentas = new CuentaServicio();

        // Queda con el nombre del usuario si se creo la cuenta; null si se cancelo.
        public string? UsuarioCreado { get; private set; }

        public RegisterWindow()
        {
            InitializeComponent();
        }

        private void OnCrear(object? sender, RoutedEventArgs e)
        {
            LblMensaje.Text = "";
            string usuario = TxtUsuario.Text ?? "";
            string password = TxtPassword.Text ?? "";
            string password2 = TxtPassword2.Text ?? "";

            if (password != password2)
            {
                LblMensaje.Text = "Las contrasenas no coinciden.";
                return;
            }

            string? error = _cuentas.Registrar(usuario, password);
            if (error != null)
            {
                LblMensaje.Text = error;
                return;
            }

            UsuarioCreado = usuario.Trim();
            Close();
        }

        private void OnCancelar(object? sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
