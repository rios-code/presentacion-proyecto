using Avalonia.Media;

namespace TiendaJuegos
{
    // Paleta central usada por el codigo que construye la interfaz.
    public static class Colores
    {
        public static readonly IBrush Texto = B("#FFFFFF");
        public static readonly IBrush TextoSuave = B("#8F98A0");
        public static readonly IBrush Acento = B("#66C0F4");
        public static readonly IBrush Precio = B("#A4D007");
        public static readonly IBrush Tarjeta = B("#16202D");
        public static readonly IBrush Error = B("#E74C3C");
        public static readonly IBrush Ok = B("#A4D007");

        public static IBrush B(string hex) => new SolidColorBrush(Color.Parse(hex));
    }
}
