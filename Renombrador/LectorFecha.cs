using System;
using System.IO;
using System.Linq;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;

namespace Renombrador
{
    // Lee la fecha de una foto desde sus metadatos EXIF (la fecha en que se saco).
    // Si no hay EXIF (o no es una imagen), usa la fecha de modificacion del archivo.
    public static class LectorFecha
    {
        public static DateTime? FechaFoto(string ruta)
        {
            try
            {
                var directorios = ImageMetadataReader.ReadMetadata(ruta);
                var exif = directorios.OfType<ExifSubIfdDirectory>().FirstOrDefault();
                if (exif != null &&
                    exif.TryGetDateTime(ExifDirectoryBase.TagDateTimeOriginal, out DateTime fecha))
                    return fecha;
            }
            catch
            {
                // No es imagen o no tiene EXIF: se usa el fallback de abajo.
            }

            try { return File.GetLastWriteTime(ruta); }
            catch { return null; }
        }
    }
}
