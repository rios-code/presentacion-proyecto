using System;
using System.IO;

namespace Renombrador
{
    public enum CambioCase { SinCambio, Mayusculas, Minusculas }

    // Reglas de renombrado elegidas por el usuario.
    public class ReglaRenombrado
    {
        public string Buscar { get; set; } = "";
        public string Reemplazar { get; set; } = "";
        public string Prefijo { get; set; } = "";
        public string Sufijo { get; set; } = "";
        public bool Numerar { get; set; } = false;
        public int Digitos { get; set; } = 2;
        public int Desde { get; set; } = 1;
        public CambioCase Caso { get; set; } = CambioCase.SinCambio;

        // Reemplazar los espacios del nombre por otro caracter (ej: "_").
        public bool ReemplazarEspacios { get; set; } = false;
        public string EspacioPor { get; set; } = "_";

        // Usar la fecha de la foto (EXIF) como nombre base.
        public bool UsarFechaFoto { get; set; } = false;
        public string FormatoFecha { get; set; } = "yyyy-MM-dd_HH-mm-ss";
    }

    // Motor puro: calcula el nombre nuevo de un archivo segun las reglas. Sin efectos en disco.
    public static class MotorRenombrado
    {
        // 'fechaFoto' es la fecha del archivo (EXIF o modificacion); solo se usa si UsarFechaFoto.
        public static string NuevoNombre(string nombreArchivo, ReglaRenombrado r, int indice, DateTime? fechaFoto = null)
        {
            string ext = Path.GetExtension(nombreArchivo);
            string baseN = Path.GetFileNameWithoutExtension(nombreArchivo);

            // Si se usa la fecha, el nombre base pasa a ser esa fecha formateada.
            if (r.UsarFechaFoto && fechaFoto.HasValue)
                baseN = fechaFoto.Value.ToString(r.FormatoFecha).Replace(":", "-");

            if (r.Buscar.Length > 0)
                baseN = baseN.Replace(r.Buscar, r.Reemplazar);

            baseN = r.Caso switch
            {
                CambioCase.Mayusculas => baseN.ToUpperInvariant(),
                CambioCase.Minusculas => baseN.ToLowerInvariant(),
                _ => baseN
            };

            baseN = r.Prefijo + baseN + r.Sufijo;

            if (r.Numerar)
            {
                int n = r.Desde + indice;
                baseN += "_" + n.ToString().PadLeft(Math.Max(1, r.Digitos), '0');
            }

            if (r.ReemplazarEspacios)
                baseN = baseN.Replace(" ", r.EspacioPor);

            return baseN + ext;
        }
    }
}
