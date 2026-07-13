using System;
using System.IO;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace Steam
{
    // Registro e inicio de sesion.
    // Las contrasenas se guardan HASHEADAS con PBKDF2 + salt aleatorio, NUNCA en texto plano.
    public class CuentaServicio
    {
        private static readonly string RUTA = Rutas.EnDatos("cuentas.txt"); // por linea: usuario|salt|hash
        private const int ITERACIONES = 100_000;

        public bool ExisteUsuario(string usuario)
        {
            foreach ((string u, _, _) in LeerTodas())
                if (string.Equals(u, usuario.Trim(), StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

        // Devuelve null si el registro fue exitoso, o un mensaje de error.
        public string? Registrar(string usuario, string password)
        {
            usuario = usuario.Trim();
            if (usuario.Length < 3) return "El usuario debe tener al menos 3 caracteres.";
            if (usuario.Contains('|')) return "El usuario no puede contener el caracter '|'.";
            if (password.Length < 6) return "La contrasena debe tener al menos 6 caracteres.";
            if (ExisteUsuario(usuario)) return "Ese usuario ya existe.";

            byte[] salt = RandomNumberGenerator.GetBytes(16);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, ITERACIONES, HashAlgorithmName.SHA256, 32);
            File.AppendAllText(RUTA,
                $"{usuario}|{Convert.ToBase64String(salt)}|{Convert.ToBase64String(hash)}\n");
            return null;
        }

        // Verifica usuario + contrasena en tiempo constante.
        public bool Validar(string usuario, string password)
        {
            foreach ((string u, string saltB64, string hashB64) in LeerTodas())
            {
                if (!string.Equals(u, usuario.Trim(), StringComparison.OrdinalIgnoreCase)) continue;

                byte[] salt = Convert.FromBase64String(saltB64);
                byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, ITERACIONES, HashAlgorithmName.SHA256, 32);
                return CryptographicOperations.FixedTimeEquals(hash, Convert.FromBase64String(hashB64));
            }
            return false;
        }

        private static IEnumerable<(string usuario, string salt, string hash)> LeerTodas()
        {
            if (!File.Exists(RUTA)) yield break;
            foreach (string linea in File.ReadAllLines(RUTA))
            {
                if (string.IsNullOrWhiteSpace(linea)) continue;
                string[] p = linea.Split('|');
                if (p.Length == 3) yield return (p[0], p[1], p[2]);
            }
        }
    }
}
