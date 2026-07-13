using System;
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
            _biblioteca = new BibliotecaServicio(usuario);

            LblUsuario.Text = "\U0001F464 " + usuario;
            LblTituloBiblioteca.Text = "Biblioteca de " + usuario;
            Title = $"MiTienda - {usuario}";

            ConstruirTienda();
        }

        // ---------- Navegacion ----------

        private void OnVerTienda(object? sender, RoutedEventArgs e)
        {
            ConstruirTienda(TxtBuscar.Text ?? "");
            MostrarSolo(PanelTienda, NavTienda);
        }

        private void OnVerBiblioteca(object? sender, RoutedEventArgs e)
        {
            ConstruirBiblioteca();
            MostrarSolo(PanelBiblioteca, NavBiblioteca);
        }

        private void OnVerCanjear(object? sender, RoutedEventArgs e) => MostrarSolo(PanelCanjear, NavCanjear);

        private void OnVerSteam(object? sender, RoutedEventArgs e) => MostrarSolo(PanelSteam, NavSteam);

        private void MostrarSolo(Control panel, Button nav)
        {
            PanelTienda.IsVisible = panel == PanelTienda;
            PanelBiblioteca.IsVisible = panel == PanelBiblioteca;
            PanelCanjear.IsVisible = panel == PanelCanjear;
            PanelSteam.IsVisible = panel == PanelSteam;

            foreach (Button b in new[] { NavTienda, NavBiblioteca, NavCanjear, NavSteam })
                b.Classes.Remove("activo");
            nav.Classes.Add("activo");
        }

        // ---------- Tienda ----------

        private void OnBuscar(object? sender, TextChangedEventArgs e) => ConstruirTienda(TxtBuscar.Text ?? "");

        private void ConstruirTienda(string filtro = "")
        {
            GridTienda.Children.Clear();
            filtro = filtro.Trim().ToLowerInvariant();

            int mostrados = 0;
            foreach (Juego j in Catalogo.Juegos)
            {
                if (filtro.Length > 0 && !j.Nombre.ToLowerInvariant().Contains(filtro)) continue;
                GridTienda.Children.Add(TarjetaTienda(j));
                mostrados++;
            }

            if (mostrados == 0)
                GridTienda.Children.Add(new TextBlock
                {
                    Text = "No hay juegos que coincidan con la busqueda.",
                    Foreground = Colores.TextoSuave
                });
        }

        private Control TarjetaTienda(Juego j)
        {
            bool tiene = _biblioteca.TieneJuego(j.Nombre);

            StackPanel card = new StackPanel { Width = 200, Margin = new Thickness(0, 0, 18, 18), Spacing = 8 };
            card.Children.Add(CrearPortada(j.Nombre, j.Genero, j.Color));

            Grid fila = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
            TextBlock precio = new TextBlock
            {
                Text = j.Precio,
                Foreground = Colores.Precio,
                FontWeight = FontWeight.Bold,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(precio, 0);

            Button boton = new Button
            {
                Content = tiene ? "En biblioteca" : "Obtener",
                Tag = j.Nombre,
                IsEnabled = !tiene
            };
            boton.Classes.Add(tiene ? "secundario" : "primario");
            boton.Click += OnObtener;
            Grid.SetColumn(boton, 1);

            fila.Children.Add(precio);
            fila.Children.Add(boton);
            card.Children.Add(fila);
            return card;
        }

        private void OnObtener(object? sender, RoutedEventArgs e)
        {
            if (sender is Button b && b.Tag is string juego && !_biblioteca.TieneJuego(juego))
            {
                _biblioteca.Agregar(new JuegoLocal(juego, DateTime.Now, "(tienda)", false));
                ConstruirTienda(TxtBuscar.Text ?? "");
            }
        }

        // ---------- Biblioteca ----------

        private void ConstruirBiblioteca()
        {
            GridBiblioteca.Children.Clear();
            List<JuegoLocal> juegos = _biblioteca.LeerTodos();

            if (juegos.Count == 0)
            {
                GridBiblioteca.Children.Add(new TextBlock
                {
                    Text = "Tu biblioteca esta vacia. Consegui juegos en la Tienda o canjea una licencia.",
                    Foreground = Colores.TextoSuave
                });
                return;
            }

            foreach (JuegoLocal j in juegos)
                GridBiblioteca.Children.Add(TarjetaBiblioteca(j));
        }

        private Control TarjetaBiblioteca(JuegoLocal j)
        {
            Juego? enCatalogo = Catalogo.Juegos.Find(c =>
                string.Equals(c.Nombre, j.Nombre, StringComparison.OrdinalIgnoreCase));
            string color = enCatalogo?.Color ?? "#33587A";
            string genero = enCatalogo?.Genero ?? "";

            StackPanel card = new StackPanel { Width = 200, Margin = new Thickness(0, 0, 18, 18), Spacing = 8 };
            card.Children.Add(CrearPortada(j.Nombre, genero, color));

            Button accion = new Button
            {
                Content = j.Instalado ? "▶ Jugar" : "Instalar",
                Tag = j.Nombre,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Center
            };
            accion.Classes.Add(j.Instalado ? "exito" : "secundario");
            accion.Click += OnInstalarOJugar;
            card.Children.Add(accion);

            card.Children.Add(new TextBlock
            {
                Text = j.Instalado ? "Instalado" : "No instalado",
                Foreground = j.Instalado ? Colores.Ok : Colores.TextoSuave,
                FontSize = 12,
                HorizontalAlignment = HorizontalAlignment.Center
            });
            return card;
        }

        private void OnInstalarOJugar(object? sender, RoutedEventArgs e)
        {
            if (sender is Button b && b.Tag is string juego)
            {
                JuegoLocal? actual = _biblioteca.LeerTodos().Find(j =>
                    string.Equals(j.Nombre, juego, StringComparison.OrdinalIgnoreCase));
                if (actual == null) return;

                if (!actual.Instalado)
                    _biblioteca.CambiarInstalado(juego, true); // Instalar
                // Si ya esta instalado, "Jugar" (aca iria el lanzamiento real del juego).

                ConstruirBiblioteca();
            }
        }

        // ---------- Canjear licencia ----------

        private void OnCanjear(object? sender, RoutedEventArgs e)
        {
            string clave = TxtClave.Text ?? "";
            LblCanje.Foreground = Colores.Error;

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

            _biblioteca.Agregar(new JuegoLocal(r.Producto!, DateTime.Now, clave, false));
            LblCanje.Foreground = Colores.Ok;
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

        // ---------- Portada generada (sin imagenes externas) ----------

        private static Border CrearPortada(string nombre, string genero, string colorHex)
        {
            Color baseColor = Color.Parse(colorHex);

            LinearGradientBrush degradado = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative)
            };
            degradado.GradientStops.Add(new GradientStop(baseColor, 0));
            degradado.GradientStops.Add(new GradientStop(Oscurecer(baseColor, 0.45), 1));

            Grid contenido = new Grid { RowDefinitions = new RowDefinitions("*,Auto") };

            TextBlock titulo = new TextBlock
            {
                Text = nombre,
                Foreground = Brushes.White,
                FontSize = 20,
                FontWeight = FontWeight.Bold,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(14)
            };
            Grid.SetRow(titulo, 0);
            contenido.Children.Add(titulo);

            if (!string.IsNullOrWhiteSpace(genero))
            {
                Border franja = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(110, 0, 0, 0)),
                    Padding = new Thickness(10, 5),
                    Child = new TextBlock
                    {
                        Text = genero.ToUpperInvariant(),
                        Foreground = Brushes.White,
                        FontSize = 11,
                        FontWeight = FontWeight.SemiBold
                    }
                };
                Grid.SetRow(franja, 1);
                contenido.Children.Add(franja);
            }

            return new Border
            {
                Width = 200,
                Height = 240,
                CornerRadius = new CornerRadius(6),
                ClipToBounds = true,
                Background = degradado,
                Child = contenido
            };
        }

        private static Color Oscurecer(Color c, double factor)
        {
            return Color.FromRgb(
                (byte)(c.R * factor),
                (byte)(c.G * factor),
                (byte)(c.B * factor));
        }
    }
}
