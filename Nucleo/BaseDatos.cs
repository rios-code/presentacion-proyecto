using System;
using System.IO;
using System.Globalization;
using Microsoft.Data.Sqlite;

namespace Steam
{
    // Acceso central a la base de datos SQLite (un unico archivo en la carpeta de datos).
    // Crea el esquema la primera vez y migra los datos viejos de los .txt si existen.
    public static class BaseDatos
    {
        private static readonly string Cadena = $"Data Source={Rutas.EnDatos("mitienda.db")}";
        private static bool _listo;
        private static readonly object _candado = new object();

        // Abre una conexion lista para usar (asegura el esquema en el primer uso).
        public static SqliteConnection Abrir()
        {
            SqliteConnection con = new SqliteConnection(Cadena);
            con.Open();

            if (!_listo)
            {
                lock (_candado)
                {
                    if (!_listo)
                    {
                        CrearEsquema(con);
                        Migracion.Importar(con);
                        _listo = true;
                    }
                }
            }
            return con;
        }

        private static void CrearEsquema(SqliteConnection con)
        {
            using SqliteCommand cmd = con.CreateCommand();
            cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS cuentas (
    usuario TEXT PRIMARY KEY COLLATE NOCASE,
    salt    TEXT NOT NULL,
    hash    TEXT NOT NULL
);
CREATE TABLE IF NOT EXISTS licencias (
    clave            TEXT PRIMARY KEY,
    producto         TEXT NOT NULL,
    cliente          TEXT NOT NULL,
    fecha_emision    TEXT NOT NULL,
    fecha_expiracion TEXT NOT NULL,
    activa           INTEGER NOT NULL
);
CREATE TABLE IF NOT EXISTS biblioteca (
    usuario          TEXT NOT NULL COLLATE NOCASE,
    nombre           TEXT NOT NULL COLLATE NOCASE,
    fecha_activacion TEXT NOT NULL,
    clave_usada      TEXT NOT NULL,
    instalado        INTEGER NOT NULL,
    PRIMARY KEY (usuario, nombre)
);";
            cmd.ExecuteNonQuery();
        }
    }

    // Migracion unica de los datos guardados en .txt hacia la base SQLite.
    // Es best-effort: si algo falla no interrumpe el arranque.
    internal static class Migracion
    {
        public static void Importar(SqliteConnection con)
        {
            try { ImportarCuentas(con); } catch { }
            try { ImportarLicencias(con); } catch { }
            try { ImportarBibliotecas(con); } catch { }
        }

        private static bool Vacia(SqliteConnection con, string tabla)
        {
            using SqliteCommand cmd = con.CreateCommand();
            cmd.CommandText = $"SELECT COUNT(*) FROM {tabla}";
            return Convert.ToInt64(cmd.ExecuteScalar()) == 0;
        }

        private static void ImportarCuentas(SqliteConnection con)
        {
            string ruta = Rutas.EnDatos("cuentas.txt");
            if (!File.Exists(ruta) || !Vacia(con, "cuentas")) return;

            foreach (string linea in File.ReadAllLines(ruta))
            {
                string[] p = linea.Split('|');
                if (p.Length != 3) continue;
                using SqliteCommand cmd = con.CreateCommand();
                cmd.CommandText = "INSERT OR IGNORE INTO cuentas(usuario,salt,hash) VALUES($u,$s,$h)";
                cmd.Parameters.AddWithValue("$u", p[0]);
                cmd.Parameters.AddWithValue("$s", p[1]);
                cmd.Parameters.AddWithValue("$h", p[2]);
                cmd.ExecuteNonQuery();
            }
        }

        private static void ImportarLicencias(SqliteConnection con)
        {
            string ruta = Rutas.EnDatos("licencias.txt");
            if (!File.Exists(ruta) || !Vacia(con, "licencias")) return;

            foreach (string linea in File.ReadAllLines(ruta))
            {
                string[] p = linea.Split('|');
                if (p.Length != 6) continue;
                using SqliteCommand cmd = con.CreateCommand();
                cmd.CommandText = @"INSERT OR IGNORE INTO licencias
                    (clave,producto,cliente,fecha_emision,fecha_expiracion,activa)
                    VALUES($k,$p,$c,$e,$x,$a)";
                cmd.Parameters.AddWithValue("$k", p[0]);
                cmd.Parameters.AddWithValue("$p", p[1]);
                cmd.Parameters.AddWithValue("$c", p[2]);
                cmd.Parameters.AddWithValue("$e", p[3]);
                cmd.Parameters.AddWithValue("$x", p[4]);
                cmd.Parameters.AddWithValue("$a", p[5] == "1" ? 1 : 0);
                cmd.ExecuteNonQuery();
            }
        }

        private static void ImportarBibliotecas(SqliteConnection con)
        {
            foreach (string archivo in Directory.GetFiles(Rutas.CarpetaDatos, "biblioteca_*.txt"))
            {
                string nombreArchivo = Path.GetFileNameWithoutExtension(archivo);
                string usuario = nombreArchivo.Substring("biblioteca_".Length);
                if (usuario.Length == 0) continue;

                // Importa solo si ese usuario todavia no tiene juegos en la base.
                using (SqliteCommand chk = con.CreateCommand())
                {
                    chk.CommandText = "SELECT COUNT(*) FROM biblioteca WHERE usuario = $u";
                    chk.Parameters.AddWithValue("$u", usuario);
                    if (Convert.ToInt64(chk.ExecuteScalar()) > 0) continue;
                }

                foreach (string linea in File.ReadAllLines(archivo))
                {
                    string[] p = linea.Split('|');
                    if (p.Length != 4) continue;
                    using SqliteCommand cmd = con.CreateCommand();
                    cmd.CommandText = @"INSERT OR IGNORE INTO biblioteca
                        (usuario,nombre,fecha_activacion,clave_usada,instalado)
                        VALUES($u,$n,$f,$c,$i)";
                    cmd.Parameters.AddWithValue("$u", usuario);
                    cmd.Parameters.AddWithValue("$n", p[0]);
                    cmd.Parameters.AddWithValue("$f", p[1]);
                    cmd.Parameters.AddWithValue("$c", p[2]);
                    cmd.Parameters.AddWithValue("$i", p[3] == "1" ? 1 : 0);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
