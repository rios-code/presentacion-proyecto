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
    }

    // Motor puro: calcula el nombre nuevo de un archivo segun las reglas. Sin efectos en disco.
    public static class MotorRenombrado
    {
        public static string NuevoNombre(string nombreArchivo, ReglaRenombrado r, int indice)
        {
            string ext = Path.GetExtension(nombreArchivo);
            string baseN = Path.GetFileNameWithoutExtension(nombreArchivo);

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

            return baseN + ext;
        }
    }
}
