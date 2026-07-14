using System;
using System.Security.Cryptography;
using Microsoft.Data.Sqlite;

namespace Steam
{
    // Registro e inicio de sesion (persistido en SQLite).
    // Las contrasenas se guardan HASHEADAS con PBKDF2 + salt aleatorio, NUNCA en texto plano.
    public class CuentaServicio
    {
        private const int ITERACIONES = 100_000;

        public bool ExisteUsuario(string usuario)
        {
            using SqliteConnection con = BaseDatos.Abrir();
            using SqliteCommand cmd = con.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM cuentas WHERE usuario = $u";
            cmd.Parameters.AddWithValue("$u", usuario.Trim());
            return Convert.ToInt64(cmd.ExecuteScalar()) > 0;
        }

        // Devuelve null si el registro fue exitoso, o un mensaje de error.
        public string? Registrar(string usuario, string password)
        {
            usuario = usuario.Trim();
            if (usuario.Length < 3) return "El usuario debe tener al menos 3 caracteres.";
            if (password.Length < 6) return "La contrasena debe tener al menos 6 caracteres.";
            if (ExisteUsuario(usuario)) return "Ese usuario ya existe.";

            byte[] salt = RandomNumberGenerator.GetBytes(16);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, ITERACIONES, HashAlgorithmName.SHA256, 32);

            using SqliteConnection con = BaseDatos.Abrir();
            using SqliteCommand cmd = con.CreateCommand();
            cmd.CommandText = "INSERT INTO cuentas(usuario,salt,hash) VALUES($u,$s,$h)";
            cmd.Parameters.AddWithValue("$u", usuario);
            cmd.Parameters.AddWithValue("$s", Convert.ToBase64String(salt));
            cmd.Parameters.AddWithValue("$h", Convert.ToBase64String(hash));
            cmd.ExecuteNonQuery();
            return null;
        }

        // Verifica usuario + contrasena en tiempo constante.
        public bool Validar(string usuario, string password)
        {
            using SqliteConnection con = BaseDatos.Abrir();
            using SqliteCommand cmd = con.CreateCommand();
            cmd.CommandText = "SELECT salt, hash FROM cuentas WHERE usuario = $u";
            cmd.Parameters.AddWithValue("$u", usuario.Trim());

            using SqliteDataReader rd = cmd.ExecuteReader();
            if (!rd.Read()) return false;

            byte[] salt = Convert.FromBase64String(rd.GetString(0));
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, ITERACIONES, HashAlgorithmName.SHA256, 32);
            return CryptographicOperations.FixedTimeEquals(hash, Convert.FromBase64String(rd.GetString(1)));
        }
    }
}
