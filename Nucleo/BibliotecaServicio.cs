using System;
using System.Globalization;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;

namespace Steam
{
    // Biblioteca de juegos de un usuario (persistida en SQLite).
    // Cada cuenta tiene su propia biblioteca; los juegos se obtienen en la tienda o canjeando licencias.
    public class BibliotecaServicio
    {
        private readonly string _usuario;

        // Sin argumentos: biblioteca unica (uso de consola).
        public BibliotecaServicio() : this("local") { }

        // Por usuario: cada cuenta tiene su propia biblioteca.
        public BibliotecaServicio(string usuario)
        {
            _usuario = usuario.Trim();
        }

        public List<JuegoLocal> LeerTodos()
        {
            List<JuegoLocal> lista = new List<JuegoLocal>();
            using SqliteConnection con = BaseDatos.Abrir();
            using SqliteCommand cmd = con.CreateCommand();
            cmd.CommandText = @"SELECT nombre, fecha_activacion, clave_usada, instalado
                                FROM biblioteca WHERE usuario = $u ORDER BY rowid";
            cmd.Parameters.AddWithValue("$u", _usuario);

            using SqliteDataReader rd = cmd.ExecuteReader();
            while (rd.Read())
            {
                DateTime fecha = DateTime.ParseExact(rd.GetString(1), "yyyy-MM-dd HH:mm:ss",
                                                     CultureInfo.InvariantCulture);
                lista.Add(new JuegoLocal(rd.GetString(0), fecha, rd.GetString(2), rd.GetInt32(3) == 1));
            }
            return lista;
        }

        // Una misma licencia no puede canjearse dos veces.
        public bool ClaveYaCanjeada(string clave)
        {
            string objetivo = Normalizar(clave);
            foreach (JuegoLocal j in LeerTodos())
                if (Normalizar(j.ClaveUsada) == objetivo) return true;
            return false;
        }

        public bool TieneJuego(string nombre)
        {
            using SqliteConnection con = BaseDatos.Abrir();
            using SqliteCommand cmd = con.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM biblioteca WHERE usuario = $u AND nombre = $n";
            cmd.Parameters.AddWithValue("$u", _usuario);
            cmd.Parameters.AddWithValue("$n", nombre);
            return Convert.ToInt64(cmd.ExecuteScalar()) > 0;
        }

        public void Agregar(JuegoLocal juego)
        {
            using SqliteConnection con = BaseDatos.Abrir();
            using SqliteCommand cmd = con.CreateCommand();
            cmd.CommandText = @"INSERT OR IGNORE INTO biblioteca
                (usuario,nombre,fecha_activacion,clave_usada,instalado)
                VALUES($u,$n,$f,$c,$i)";
            cmd.Parameters.AddWithValue("$u", _usuario);
            cmd.Parameters.AddWithValue("$n", juego.Nombre);
            cmd.Parameters.AddWithValue("$f", juego.FechaActivacion.ToString("yyyy-MM-dd HH:mm:ss",
                                                CultureInfo.InvariantCulture));
            cmd.Parameters.AddWithValue("$c", juego.ClaveUsada);
            cmd.Parameters.AddWithValue("$i", juego.Instalado ? 1 : 0);
            cmd.ExecuteNonQuery();
        }

        // Cambia el estado instalado/no instalado de un juego (por nombre).
        public bool CambiarInstalado(string nombre, bool instalado)
        {
            using SqliteConnection con = BaseDatos.Abrir();
            using SqliteCommand cmd = con.CreateCommand();
            cmd.CommandText = "UPDATE biblioteca SET instalado = $i WHERE usuario = $u AND nombre = $n";
            cmd.Parameters.AddWithValue("$i", instalado ? 1 : 0);
            cmd.Parameters.AddWithValue("$u", _usuario);
            cmd.Parameters.AddWithValue("$n", nombre);
            return cmd.ExecuteNonQuery() > 0;
        }

        private static string Normalizar(string clave)
        {
            return clave.Replace("-", "").Replace(" ", "").ToUpperInvariant();
        }
    }
}
