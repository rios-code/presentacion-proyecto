using System;
using System.Text;
using System.Collections.Generic;

namespace LicenciaKit
{
    // Codificacion Base32 (RFC 4648, sin relleno). Convierte bytes en texto legible
    // usando solo A-Z y 2-7: la clave queda en mayusculas, sin caracteres confusos.
    internal static class Base32
    {
        private const string ALFABETO = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

        public static string Codificar(byte[] datos)
        {
            StringBuilder sb = new StringBuilder();
            int buffer = 0, bits = 0;

            foreach (byte b in datos)
            {
                buffer = (buffer << 8) | b;
                bits += 8;
                while (bits >= 5)
                {
                    bits -= 5;
                    sb.Append(ALFABETO[(buffer >> bits) & 31]);
                }
            }
            if (bits > 0)
                sb.Append(ALFABETO[(buffer << (5 - bits)) & 31]);
            return sb.ToString();
        }

        public static byte[] Decodificar(string texto)
        {
            List<byte> salida = new List<byte>();
            int buffer = 0, bits = 0;

            foreach (char c in texto)
            {
                int valor = ALFABETO.IndexOf(char.ToUpperInvariant(c));
                if (valor < 0)
                    throw new FormatException($"Caracter invalido en la clave: '{c}'");
                buffer = (buffer << 5) | valor;
                bits += 5;
                if (bits >= 8)
                {
                    bits -= 8;
                    salida.Add((byte)((buffer >> bits) & 0xFF));
                }
            }
            return salida.ToArray();
        }
    }
}
