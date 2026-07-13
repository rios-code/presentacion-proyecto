using System;
using System.IO;

namespace Steam
{
    // Carpeta de datos COMPARTIDA por todas las apps (emisor de consola y cliente de escritorio).
    // Vive en los datos del usuario (no en el repositorio), asi las claves y archivos son los
    // mismos para las dos apps: una licencia generada por el emisor se valida en el cliente.
    public static class Rutas
    {
        public static string CarpetaDatos { get; }

        static Rutas()
        {
            string baseDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            if (string.IsNullOrEmpty(baseDir))
                baseDir = AppContext.BaseDirectory;

            CarpetaDatos = Path.Combine(baseDir, "MiTienda");
            Directory.CreateDirectory(CarpetaDatos);
        }

        // Ruta completa de un archivo de datos dentro de la carpeta compartida.
        public static string EnDatos(string archivo) => Path.Combine(CarpetaDatos, archivo);
    }
}
