using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;

namespace TiendaJuegos
{
    public partial class MainWindow : Window
    {
        private readonly string _usuario;
        private readonly BibliotecaUsuario _biblioteca;

        // Constructor sin parametros: solo para el disenador de Avalonia.
        public MainWindow() : this("invitado") { }

        public MainWindow(string usuario)
        {
            InitializeComponent();
            _usuario = usuario;
            _biblioteca = new BibliotecaUsuario(usuario);

            LblUsuario.Text = "\U0001F464 " + usuario;
            Title = $"MiTienda - {usuario}";

            ConstruirTienda();
            ConstruirBiblioteca();
        }

        private void OnVerTienda(object? sender, RoutedEventArgs e)
        {
            PanelTienda.IsVisible = true;
            PanelBiblioteca.IsVisible = false;
        }

        private void OnVerBiblioteca(object? sender, RoutedEventArgs e)
        {
            ConstruirBiblioteca();
            PanelTienda.IsVisible = false;
            PanelBiblioteca.IsVisible = true;
        }

        private void ConstruirTienda()
        {
            PanelTienda.Children.Clear();
            PanelTienda.Children.Add(Titulo("Tienda"));

            foreach (Juego j in Catalogo.Juegos)
            {
                bool tiene = _biblioteca.Tiene(j.Nombre);

                Grid grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto") };

                TextBlock nombre = new TextBlock
                {
                    Text = j.Nombre,
                    Foreground = Pincel("#FFFFFF"),
                    FontSize = 16,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(nombre, 0);

                TextBlock precio = new TextBlock
                {
                    Text = j.Precio,
                    Foreground = Pincel("#A4D007"),
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 16, 0)
                };
                Grid.SetColumn(precio, 1);

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
                Grid.SetColumn(boton, 2);

                grid.Children.Add(nombre);
                grid.Children.Add(precio);
                grid.Children.Add(boton);

                PanelTienda.Children.Add(Tarjeta(grid));
            }
        }

        private void OnObtener(object? sender, RoutedEventArgs e)
        {
            if (sender is Button b && b.Tag is string juego)
            {
                _biblioteca.Agregar(juego);
                ConstruirTienda();
                ConstruirBiblioteca();
            }
        }

        private void ConstruirBiblioteca()
        {
            PanelBiblioteca.Children.Clear();
            PanelBiblioteca.Children.Add(Titulo("Biblioteca de " + _usuario));

            List<string> juegos = _biblioteca.Juegos();
            if (juegos.Count == 0)
            {
                PanelBiblioteca.Children.Add(new TextBlock
                {
                    Text = "Tu biblioteca esta vacia. Consegui juegos en la Tienda.",
                    Foreground = Pincel("#8F98A0")
                });
                return;
            }

            foreach (string j in juegos)
            {
                Grid grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };

                TextBlock nombre = new TextBlock
                {
                    Text = j,
                    Foreground = Pincel("#FFFFFF"),
                    FontSize = 16,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(nombre, 0);

                Button jugar = new Button
                {
                    Content = "▶ Jugar",
                    Background = Pincel("#5C7E10"),
                    Foreground = Pincel("#FFFFFF"),
                    FontWeight = FontWeight.Bold,
                    Padding = new Thickness(16, 8)
                };
                Grid.SetColumn(jugar, 1);

                grid.Children.Add(nombre);
                grid.Children.Add(jugar);

                PanelBiblioteca.Children.Add(Tarjeta(grid));
            }
        }

        private void OnCerrarSesion(object? sender, RoutedEventArgs e)
        {
            LoginWindow login = new LoginWindow();
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                desktop.MainWindow = login;
            login.Show();
            Close();
        }

        // ---------- Ayudas de estilo ----------

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

        private static IBrush Pincel(string hex) => new SolidColorBrush(Color.Parse(hex));
    }
}
