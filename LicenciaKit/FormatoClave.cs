using System;
using System.Text;

namespace LicenciaKit
{
    // Utilidades internas del formato de la clave.
    internal static class FormatoClave
    {
        // Separador interno de los campos firmados (Unit Separator, no aparece en texto normal).
        public const char SEP = (char)31;

        // [2 bytes con el largo del payload][payload][firma]
        public static byte[] Empaquetar(byte[] payload, byte[] firma)
        {
            byte[] blob = new byte[2 + payload.Length + firma.Length];
            blob[0] = (byte)((payload.Length >> 8) & 0xFF);
            blob[1] = (byte)(payload.Length & 0xFF);
            Array.Copy(payload, 0, blob, 2, payload.Length);
            Array.Copy(firma, 0, blob, 2 + payload.Length, firma.Length);
            return blob;
        }

        // Inserta un guion cada 5 caracteres: ABCDE-FGHIJ-...
        public static string FormatearEnBloques(string texto)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < texto.Length; i++)
            {
                if (i > 0 && i % 5 == 0) sb.Append('-');
                sb.Append(texto[i]);
            }
            return sb.ToString();
        }

        // Quita guiones y espacios y pasa a mayusculas.
        public static string Normalizar(string clave)
            => clave.Replace("-", "").Replace(" ", "").ToUpperInvariant();

        // Evita que el texto rompa el separador de campos.
        public static string Limpiar(string texto)
            => texto.Replace("|", "/").Replace(SEP.ToString(), " ").Trim();
    }
}
