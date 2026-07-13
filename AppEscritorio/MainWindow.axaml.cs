using System.Collections.Generic;
using System.Net.Http;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Steam;

namespace TiendaJuegos
{
    public partial class MainWindow : Window
    {
        private readonly string _usuario;
        private readonly BibliotecaServicio _biblioteca;
        private readonly LicenciaServicio _licencias = new LicenciaServicio();
        private readonly SteamServicio _steam = new SteamServicio();

        // Constructor sin parametros: solo para el disenador de Avalonia.
        public MainWindow() : this("invitado") { }

        public MainWindow(string usuario)
        {
            InitializeComponent();
            _usuario = usuario;
            _biblioteca = new BibliotecaServicio(usuario); // biblioteca propia de esta cuenta

            LblUsuario.Text = "\U0001F464 " + usuario;
            Title = $"MiTienda - {usuario}";

            ConstruirTienda();
        }

        // ---------- Navegacion ----------

        private void OnVerTienda(object? sender, RoutedEventArgs e)
        {
            ConstruirTienda();
            MostrarSolo(PanelTienda);
        }

        private void OnVerBiblioteca(object? sender, RoutedEventArgs e)
        {
            ConstruirBiblioteca();
            MostrarSolo(PanelBiblioteca);
        }

        private void OnVerCanjear(object? sender, RoutedEventArgs e) => MostrarSolo(PanelCanjear);

        private void OnVerSteam(object? sender, RoutedEventArgs e) => MostrarSolo(PanelSteam);

        private void MostrarSolo(Control panel)
        {
            PanelTienda.IsVisible = panel == PanelTienda;
            PanelBiblioteca.IsVisible = panel == PanelBiblioteca;
            PanelCanjear.IsVisible = panel == PanelCanjear;
            PanelSteam.IsVisible = panel == PanelSteam;
        }

        // ---------- Tienda ----------

        private void ConstruirTienda()
        {
            PanelTienda.Children.Clear();
            PanelTienda.Children.Add(Titulo("Tienda"));

            foreach (Juego j in Catalogo.Juegos)
            {
                bool tiene = _biblioteca.TieneJuego(j.Nombre);

                Grid grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto") };

                grid.Children.Add(EnColumna(Texto(j.Nombre, "#FFFFFF", 16), 0));
                grid.Children.Add(EnColumna(Texto(j.Precio, "#A4D007", 14, new Thickness(0, 0, 16, 0)), 1));

                Button boton = new Button
                {
                    Content = tiene ? "En tu biblioteca" : "Obtener",
                    IsEnabled = !tiene,
                    Tag = j.Nombre,
                    Background = Pincel("#66C0F4"),
                    Foreground = Pincel("#0E141B"),
                    FontWeight = FontWeight.Bold,
                    Padding = new Thickness(16, 8)
                };
                boton.Click += OnObtener;
                grid.Children.Add(EnColumna(boton, 2));

                PanelTienda.Children.Add(Tarjeta(grid));
            }
        }

        private void OnObtener(object? sender, RoutedEventArgs e)
        {
            if (sender is Button b && b.Tag is string juego && !_biblioteca.TieneJuego(juego))
            {
                // Juego gratuito/obtenido en la tienda (no proviene de una licencia).
                _biblioteca.Agregar(new JuegoLocal(juego, System.DateTime.Now, "(tienda)", false));
                ConstruirTienda();
            }
        }

        // ---------- Biblioteca ----------

        private void ConstruirBiblioteca()
        {
            PanelBiblioteca.Children.Clear();
            PanelBiblioteca.Children.Add(Titulo("Biblioteca de " + _usuario));

            List<JuegoLocal> juegos = _biblioteca.LeerTodos();
            if (juegos.Count == 0)
            {
                PanelBiblioteca.Children.Add(Texto(
                    "Tu biblioteca esta vacia. Consegui juegos en la Tienda o canjea una licencia.",
                    "#8F98A0", 14));
                return;
            }

            foreach (JuegoLocal j in juegos)
            {
                Grid grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };

                string estado = j.Instalado ? "  [INSTALADO]" : "";
                grid.Children.Add(EnColumna(Texto(j.Nombre + estado, "#FFFFFF", 16), 0));

                Button accion = new Button
                {
                    Content = j.Instalado ? "▶ Jugar" : "Instalar",
                    Tag = j.Nombre,
                    Background = Pincel(j.Instalado ? "#5C7E10" : "#2A475E"),
                    Foreground = Pincel("#FFFFFF"),
                    FontWeight = FontWeight.Bold,
                    Padding = new Thickness(16, 8)
                };
                accion.Click += OnInstalarOJugar;
                grid.Children.Add(EnColumna(accion, 1));

                PanelBiblioteca.Children.Add(Tarjeta(grid));
            }
        }

        private void OnInstalarOJugar(object? sender, RoutedEventArgs e)
        {
            if (sender is Button b && b.Tag is string juego)
            {
                JuegoLocal? actual = _biblioteca.LeerTodos().Find(j =>
                    string.Equals(j.Nombre, juego, System.StringComparison.OrdinalIgnoreCase));
                if (actual == null) return;

                if (!actual.Instalado)
                    _biblioteca.CambiarInstalado(juego, true); // "Instalar"
                // Si ya esta instalado, "Jugar" no cambia estado (aca iria lanzar el juego).

                ConstruirBiblioteca();
            }
        }

        // ---------- Canjear licencia ----------

        private void OnCanjear(object? sender, RoutedEventArgs e)
        {
            string clave = TxtClave.Text ?? "";
            LblCanje.Foreground = Pincel("#E74C3C");

            ResultadoValidacion r = _licencias.Validar(clave);
            if (!r.Valida)
            {
                LblCanje.Text = "No se pudo canjear: " + r.Mensaje;
                return;
            }
            if (_biblioteca.ClaveYaCanjeada(clave))
            {
                LblCanje.Text = "Esa licencia ya fue canjeada.";
                return;
            }
            if (_biblioteca.TieneJuego(r.Producto!))
            {
                LblCanje.Text = $"'{r.Producto}' ya esta en tu biblioteca.";
                return;
            }

            _biblioteca.Agregar(new JuegoLocal(r.Producto!, System.DateTime.Now, clave, false));
            LblCanje.Foreground = Pincel("#A4D007");
            LblCanje.Text = $"Listo. '{r.Producto}' se agrego a tu biblioteca.";
            TxtClave.Text = "";
        }

        // ---------- Steam (API publica) ----------

        private async void OnBuscarSteam(object? sender, RoutedEventArgs e)
        {
            string appId = (TxtAppId.Text ?? "").Trim();
            if (string.IsNullOrWhiteSpace(appId))
            {
                LblSteam.Text = "Ingresa un AppID.";
                return;
            }

            LblSteam.Text = "Consultando Steam...";
            try
            {
                LblSteam.Text = await _steam.InfoJuegoAsync(appId);
            }
            catch (HttpRequestException ex)
            {
                LblSteam.Text = "No se pudo conectar con Steam: " + ex.Message;
            }
        }

        // ---------- Cerrar sesion ----------

        private void OnCerrarSesion(object? sender, RoutedEventArgs e)
        {
            LoginWindow login = new LoginWindow();
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                desktop.MainWindow = login;
            login.Show();
            Close();
        }

        // ---------- Ayudas de interfaz ----------

        private static Control EnColumna(Control control, int columna)
        {
            Grid.SetColumn(control, columna);
            return control;
        }

        private static Border Tarjeta(Control contenido)
        {
            return new Border
            {
                Background = Pincel("#16202D"),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(16),
                Child = contenido
            };
        }

        private static TextBlock Titulo(string texto)
        {
            return new TextBlock
            {
                Text = texto,
                FontSize = 24,
                FontWeight = FontWeight.Bold,
                Foreground = Pincel("#FFFFFF"),
                Margin = new Thickness(0, 0, 0, 10)
            };
        }

        private static TextBlock Texto(string texto, string hex, double tamano, Thickness? margen = null)
        {
            return new TextBlock
            {
                Text = texto,
                Foreground = Pincel(hex),
                FontSize = tamano,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = margen ?? new Thickness(0)
            };
        }

        private static IBrush Pincel(string hex) => new SolidColorBrush(Color.Parse(hex));
    }
}
