using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;

namespace Renombrador
{
    public partial class MainWindow : Window
    {
        private readonly List<string> _archivos = new List<string>();
        private bool _iniciado;

        public MainWindow()
        {
            InitializeComponent();
            _iniciado = true;
        }

        // ---------- Carga de carpeta ----------

        private async void OnExaminar(object? sender, RoutedEventArgs e)
        {
            IReadOnlyList<IStorageFolder> carpetas = await StorageProvider.OpenFolderPickerAsync(
                new FolderPickerOpenOptions { AllowMultiple = false, Title = "Elegi la carpeta" });

            if (carpetas.Count > 0)
            {
                TxtCarpeta.Text = carpetas[0].Path.LocalPath;
                CargarCarpeta();
            }
        }

        private void OnCargar(object? sender, RoutedEventArgs e) => CargarCarpeta();

        private void CargarCarpeta()
        {
            _archivos.Clear();
            string ruta = (TxtCarpeta.Text ?? "").Trim();

            if (!Directory.Exists(ruta))
            {
                PanelLista.Children.Clear();
                LblEstado.Text = "Esa carpeta no existe.";
                return;
            }

            foreach (string f in Directory.GetFiles(ruta))
                _archivos.Add(f);

            ActualizarPreview();
        }

        // ---------- Vista previa ----------

        private void OnCambio(object? sender, RoutedEventArgs e)
        {
            if (_iniciado) ActualizarPreview();
        }

        private ReglaRenombrado ReglaActual()
        {
            if (!int.TryParse(TxtDesde.Text, out int desde) || desde < 0) desde = 1;
            if (!int.TryParse(TxtDigitos.Text, out int dig) || dig < 1) dig = 1;

            CambioCase caso = CmbCaso.SelectedIndex switch
            {
                1 => CambioCase.Mayusculas,
                2 => CambioCase.Minusculas,
                _ => CambioCase.SinCambio
            };

            return new ReglaRenombrado
            {
                Buscar = TxtBuscar.Text ?? "",
                Reemplazar = TxtReemplazar.Text ?? "",
                Prefijo = TxtPrefijo.Text ?? "",
                Sufijo = TxtSufijo.Text ?? "",
                Numerar = ChkNumerar.IsChecked == true,
                Desde = desde,
                Digitos = dig,
                Caso = caso
            };
        }

        private void ActualizarPreview()
        {
            PanelLista.Children.Clear();

            if (_archivos.Count == 0)
            {
                LblEstado.Text = "Carga una carpeta con archivos.";
                return;
            }

            ReglaRenombrado r = ReglaActual();
            HashSet<string> vistos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int colisiones = 0;

            for (int i = 0; i < _archivos.Count; i++)
            {
                string orig = Path.GetFileName(_archivos[i]);
                string nuevo = MotorRenombrado.NuevoNombre(orig, r, i);
                bool duplicado = !vistos.Add(nuevo);
                if (duplicado) colisiones++;
                PanelLista.Children.Add(Fila(orig, nuevo, duplicado));
            }

            LblEstado.Text = colisiones > 0
                ? $"{_archivos.Count} archivos - ATENCION: {colisiones} nombres repetidos (no se aplicara asi)."
                : $"{_archivos.Count} archivos listos para renombrar.";
        }

        // ---------- Aplicar ----------

        private void OnAplicar(object? sender, RoutedEventArgs e)
        {
            if (_archivos.Count == 0)
            {
                LblEstado.Text = "No hay archivos cargados.";
                return;
            }

            ReglaRenombrado r = ReglaActual();
            List<(string origen, string destino)> mapa = new List<(string, string)>();
            HashSet<string> vistos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < _archivos.Count; i++)
            {
                string dir = Path.GetDirectoryName(_archivos[i]) ?? "";
                string nuevo = MotorRenombrado.NuevoNombre(Path.GetFileName(_archivos[i]), r, i);
                if (!vistos.Add(nuevo))
                {
                    LblEstado.Text = "Hay nombres repetidos. Ajusta las reglas (por ejemplo activa 'Numerar').";
                    return;
                }
                mapa.Add((_archivos[i], Path.Combine(dir, nuevo)));
            }

            int hechos = 0, saltados = 0;
            foreach ((string origen, string destino) in mapa)
            {
                if (string.Equals(origen, destino, StringComparison.Ordinal)) continue;
                try
                {
                    if (File.Exists(destino)) { saltados++; continue; } // nunca sobrescribe
                    File.Move(origen, destino);
                    hechos++;
                }
                catch { saltados++; }
            }

            CargarCarpeta();
            LblEstado.Text = $"Renombrados {hechos}." +
                             (saltados > 0 ? $" Saltados {saltados} (ya existian o dieron error)." : "");
        }

        // ---------- Auxiliar de interfaz ----------

        private static Control Fila(string original, string nuevo, bool duplicado)
        {
            Grid g = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto,*") };

            TextBlock izq = new TextBlock
            {
                Text = original,
                Foreground = new SolidColorBrush(Color.Parse("#9BA7B4")),
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            Grid.SetColumn(izq, 0);

            TextBlock flecha = new TextBlock
            {
                Text = "  →  ",
                Foreground = new SolidColorBrush(Color.Parse("#4C9AFF")),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(flecha, 1);

            bool sinCambio = string.Equals(original, nuevo, StringComparison.Ordinal);
            string color = duplicado ? "#E5484D" : (sinCambio ? "#6b7684" : "#FFFFFF");
            TextBlock der = new TextBlock
            {
                Text = nuevo,
                Foreground = new SolidColorBrush(Color.Parse(color)),
                FontWeight = duplicado ? FontWeight.Bold : FontWeight.Normal,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            Grid.SetColumn(der, 2);

            g.Children.Add(izq);
            g.Children.Add(flecha);
            g.Children.Add(der);
            return g;
        }
    }
}
