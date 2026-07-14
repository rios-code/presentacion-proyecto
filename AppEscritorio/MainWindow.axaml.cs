using System;
using System.IO;
using System.Collections.Generic;
using System.Net.Http;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Steam;

namespace TiendaJuegos
{
    public partial class MainWindow : Window
    {
        private readonly string _usuario;
        private readonly BibliotecaServicio _biblioteca;
        private readonly LicenciaServicio _licencias = new LicenciaServicio(); // validador (clave publica)
        private readonly SteamServicio _steam = new SteamServicio();

        // El emisor (clave privada) se crea solo cuando el admin genera una licencia.
        private EmisorServicio? _emisor;
        private EmisorServicio Emisor => _emisor ??= new EmisorServicio();

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

            // La seccion ADMIN solo aparece para la cuenta "admin".
            NavAdmin.IsVisible = string.Equals(usuario, "admin", StringComparison.OrdinalIgnoreCase);

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

        private void OnVerAdmin(object? sender, RoutedEventArgs e)
        {
            if (CmbJuego.ItemCount == 0)
            {
                List<string> nombres = new List<string>();
                foreach (Juego j in Catalogo.Juegos) nombres.Add(j.Nombre);
                CmbJuego.ItemsSource = nombres;
                CmbJuego.SelectedIndex = 0;
            }
            MostrarSolo(PanelAdmin, NavAdmin);
        }

        private void MostrarSolo(Control panel, Button? nav)
        {
            PanelTienda.IsVisible = panel == PanelTienda;
            PanelDetalle.IsVisible = panel == PanelDetalle;
            PanelBiblioteca.IsVisible = panel == PanelBiblioteca;
            PanelCanjear.IsVisible = panel == PanelCanjear;
            PanelSteam.IsVisible = panel == PanelSteam;
            PanelAdmin.IsVisible = panel == PanelAdmin;

            foreach (Button b in new[] { NavTienda, NavBiblioteca, NavCanjear, NavSteam, NavAdmin })
                b.Classes.Remove("activo");
            nav?.Classes.Add("activo");
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

            // La portada es un boton: al hacer clic se abre el detalle.
            Button portada = new Button
            {
                Padding = new Thickness(0),
                BorderThickness = new Thickness(0),
                Background = Brushes.Transparent,
                Cursor = new Cursor(StandardCursorType.Hand),
                Content = CrearPortada(j.Nombre, j.Genero, j.Color),
                Tag = j.Nombre
            };
            portada.Click += OnVerDetalle;
            card.Children.Add(portada);

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

        // ---------- Detalle de un juego ----------

        private void OnVerDetalle(object? sender, RoutedEventArgs e)
        {
            if (sender is Button b && b.Tag is string nombre)
            {
                Juego? j = Catalogo.Juegos.Find(g => g.Nombre == nombre);
                if (j != null) ConstruirDetalle(j);
            }
        }

        private void ConstruirDetalle(Juego j)
        {
            PanelDetalle.Children.Clear();

            Button volver = new Button { Content = "← Volver a la tienda" };
            volver.Classes.Add("secundario");
            volver.Click += OnVerTienda;
            PanelDetalle.Children.Add(volver);

            Grid cuerpo = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*"), Margin = new Thickness(0, 8, 0, 0) };

            Border portada = CrearPortada(j.Nombre, j.Genero, j.Color, 300, 400);
            Grid.SetColumn(portada, 0);
            cuerpo.Children.Add(portada);

            StackPanel info = new StackPanel { Spacing = 14, Margin = new Thickness(28, 0, 0, 0) };
            info.Children.Add(new TextBlock { Text = j.Nombre, FontSize = 32, FontWeight = FontWeight.Bold, Foreground = Colores.Texto });
            info.Children.Add(new TextBlock { Text = "Genero: " + j.Genero, Foreground = Colores.TextoSuave, FontSize = 15 });
            info.Children.Add(new TextBlock { Text = j.Descripcion, Foreground = Colores.B("#C6D4DF"), TextWrapping = TextWrapping.Wrap, FontSize = 15, MaxWidth = 520, HorizontalAlignment = HorizontalAlignment.Left });
            info.Children.Add(new TextBlock { Text = j.Precio, Foreground = Colores.Precio, FontSize = 26, FontWeight = FontWeight.Bold, Margin = new Thickness(0, 8, 0, 0) });

            bool tiene = _biblioteca.TieneJuego(j.Nombre);
            Button obtener = new Button
            {
                Content = tiene ? "Ya esta en tu biblioteca" : "Obtener juego",
                IsEnabled = !tiene,
                Tag = j.Nombre,
                FontSize = 15,
                Padding = new Thickness(26, 12)
            };
            obtener.Classes.Add(tiene ? "secundario" : "primario");
            obtener.Click += OnObtenerDesdeDetalle;
            info.Children.Add(obtener);

            Grid.SetColumn(info, 1);
            cuerpo.Children.Add(info);
            PanelDetalle.Children.Add(cuerpo);

            MostrarSolo(PanelDetalle, NavTienda);
        }

        private void OnObtenerDesdeDetalle(object? sender, RoutedEventArgs e)
        {
            if (sender is Button b && b.Tag is string nombre)
            {
                if (!_biblioteca.TieneJuego(nombre))
                    _biblioteca.Agregar(new JuegoLocal(nombre, DateTime.Now, "(tienda)", false));

                Juego? j = Catalogo.Juegos.Find(g => g.Nombre == nombre);
                if (j != null) ConstruirDetalle(j); // refresca el boton a "ya esta en tu biblioteca"
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

        // ---------- Admin: generar licencia ----------

        private void OnGenerar(object? sender, RoutedEventArgs e)
        {
            string? producto = CmbJuego.SelectedItem as string;
            string cliente = (TxtCliente.Text ?? "").Trim();

            if (string.IsNullOrWhiteSpace(producto))
            {
                TxtClaveGenerada.Text = "Elegi un juego.";
                return;
            }
            if (string.IsNullOrWhiteSpace(cliente))
            {
                TxtClaveGenerada.Text = "Ingresa el cliente.";
                return;
            }
            if (!int.TryParse((TxtDias.Text ?? "0").Trim(), out int dias) || dias < 0)
                dias = 0;

            Licencia lic = Emisor.Emitir(producto, cliente, dias);
            TxtClaveGenerada.Text = lic.Clave;
            BtnCopiar.IsEnabled = true;
        }

        private async void OnCopiar(object? sender, RoutedEventArgs e)
        {
            string clave = TxtClaveGenerada.Text ?? "";
            if (string.IsNullOrWhiteSpace(clave)) return;

            TopLevel? top = TopLevel.GetTopLevel(this);
            if (top?.Clipboard != null)
            {
                DataTransfer datos = new DataTransfer();
                datos.Add(DataTransferItem.CreateText(clave));
                await top.Clipboard.SetDataAsync(datos);
                BtnCopiar.Content = "Copiado!";
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

        // ---------- Portada del juego ----------
        // Si existe una imagen propia en la carpeta de datos (covers/<juego>.png|jpg) la usa;
        // si no, genera una portada abstracta (degradado + formas + inicial), estilo capsula de Steam.

        private static Border CrearPortada(string nombre, string genero, string colorHex,
                                           double ancho = 200, double alto = 240)
        {
            Color baseColor = Color.Parse(colorHex);
            Panel capa = new Panel();

            string? imagen = RutaImagen(nombre);
            Bitmap? bmp = CargarBitmap(imagen);

            IBrush fondo;
            if (bmp != null)
            {
                capa.Children.Add(new Image { Source = bmp, Stretch = Stretch.UniformToFill });
                fondo = Brushes.Black;
            }
            else
            {
                LinearGradientBrush degradado = new LinearGradientBrush
                {
                    StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                    EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative)
                };
                degradado.GradientStops.Add(new GradientStop(baseColor, 0));
                degradado.GradientStops.Add(new GradientStop(Oscurecer(baseColor, 0.4), 1));
                fondo = degradado;

                // Formas abstractas para dar aspecto de arte.
                Canvas deco = new Canvas();
                deco.Children.Add(Circulo(ancho * 0.55, -alto * 0.12, ancho * 0.85, Color.FromArgb(28, 255, 255, 255)));
                deco.Children.Add(Circulo(-ancho * 0.25, alto * 0.5, ancho * 0.75, Color.FromArgb(40, 0, 0, 0)));
                capa.Children.Add(deco);

                // Inicial del titulo como marca de agua grande.
                capa.Children.Add(new TextBlock
                {
                    Text = Inicial(nombre),
                    FontSize = alto * 0.55,
                    FontWeight = FontWeight.Bold,
                    Foreground = new SolidColorBrush(Color.FromArgb(34, 255, 255, 255)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                });
            }

            // Velo oscuro inferior + titulo (legible sobre cualquier fondo).
            LinearGradientBrush velo = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative)
            };
            velo.GradientStops.Add(new GradientStop(Color.FromArgb(0, 0, 0, 0), 0));
            velo.GradientStops.Add(new GradientStop(Color.FromArgb(210, 0, 0, 0), 1));

            StackPanel textos = new StackPanel { Spacing = 2 };
            textos.Children.Add(new TextBlock
            {
                Text = nombre,
                Foreground = Brushes.White,
                FontSize = alto > 300 ? 24 : 16,
                FontWeight = FontWeight.Bold,
                TextWrapping = TextWrapping.Wrap
            });
            if (!string.IsNullOrWhiteSpace(genero))
                textos.Children.Add(new TextBlock
                {
                    Text = genero.ToUpperInvariant(),
                    Foreground = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)),
                    FontSize = 10,
                    FontWeight = FontWeight.SemiBold
                });

            Grid g = new Grid { RowDefinitions = new RowDefinitions("*,Auto") };
            Border franja = new Border { Background = velo, Padding = new Thickness(12, 24, 12, 12), Child = textos };
            Grid.SetRow(franja, 1);
            g.Children.Add(franja);
            capa.Children.Add(g);

            return new Border
            {
                Width = ancho,
                Height = alto,
                CornerRadius = new CornerRadius(6),
                ClipToBounds = true,
                Background = fondo,
                Child = capa
            };
        }

        private static Ellipse Circulo(double x, double y, double diametro, Color color)
        {
            Ellipse el = new Ellipse
            {
                Width = diametro,
                Height = diametro,
                Fill = new SolidColorBrush(color)
            };
            Canvas.SetLeft(el, x);
            Canvas.SetTop(el, y);
            return el;
        }

        private static string Inicial(string nombre)
        {
            string t = nombre.Trim();
            return t.Length == 0 ? "?" : t.Substring(0, 1).ToUpperInvariant();
        }

        // Busca una imagen propia para el juego en <datos>/covers/.
        private static string? RutaImagen(string nombre)
        {
            string seguro = nombre.ToLowerInvariant();
            foreach (char c in System.IO.Path.GetInvalidFileNameChars()) seguro = seguro.Replace(c, '_');
            foreach (string ext in new[] { ".png", ".jpg", ".jpeg" })
            {
                string ruta = Rutas.EnDatos(System.IO.Path.Combine("covers", seguro + ext));
                if (File.Exists(ruta)) return ruta;
            }
            return null;
        }

        private static Bitmap? CargarBitmap(string? ruta)
        {
            if (ruta == null) return null;
            try { return new Bitmap(ruta); }
            catch { return null; }
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
