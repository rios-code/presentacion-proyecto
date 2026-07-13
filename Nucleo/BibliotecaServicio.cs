using System;
using System.IO;
using System.Globalization;
using System.Collections.Generic;

namespace Steam
{
    // Biblioteca LOCAL: juegos activados canjeando una licencia valida dentro de esta app.
    // No tiene ninguna relacion con Steam; es la simulacion "genero clave -> canjeo -> aparece el juego".
    public class BibliotecaServicio
    {
        private readonly string RUTA;

        // Sin argumentos: biblioteca unica (uso de consola).
        public BibliotecaServicio() : this("local") { }

        // Por usuario: cada cuenta tiene su propia biblioteca.
        public BibliotecaServicio(string usuario)
        {
            string seguro = usuario.ToLowerInvariant();
            foreach (char c in Path.GetInvalidFileNameChars())
                seguro = seguro.Replace(c, '_');
            RUTA = Rutas.EnDatos($"biblioteca_{seguro}.txt");
        }

        public List<JuegoLocal> LeerTodos()
        {
            List<JuegoLocal> lista = new List<JuegoLocal>();
            if (!File.Exists(RUTA)) return lista;

            foreach (string linea in File.ReadAllLines(RUTA))
            {
                JuegoLocal? j = Parsear(linea);
                if (j != null) lista.Add(j);
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
            foreach (JuegoLocal j in LeerTodos())
                if (string.Equals(j.Nombre, nombre, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        public void Agregar(JuegoLocal juego)
        {
            File.AppendAllText(RUTA, Serializar(juego) + "\n");
        }

        // Cambia el estado instalado/no instalado de un juego (por nombre).
        public bool CambiarInstalado(string nombre, bool instalado)
        {
            if (!File.Exists(RUTA)) return false;

            List<JuegoLocal> lista = LeerTodos();
            bool encontrado = false;
            foreach (JuegoLocal j in lista)
            {
                if (string.Equals(j.Nombre, nombre, StringComparison.OrdinalIgnoreCase))
                {
                    j.Instalado = instalado;
                    encontrado = true;
                }
            }
            if (!encontrado) return false;

            List<string> lineas = new List<string>();
            foreach (JuegoLocal j in lista) lineas.Add(Serializar(j));
            File.WriteAllLines(RUTA, lineas);
            return true;
        }

        // ---------- Auxiliares ----------

        private static string Serializar(JuegoLocal j)
        {
            return string.Join("|",
                j.Nombre,
                j.FechaActivacion.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                j.ClaveUsada,
                j.Instalado ? "1" : "0");
        }

        private static JuegoLocal? Parsear(string linea)
        {
            if (string.IsNullOrWhiteSpace(linea)) return null;
            string[] p = linea.Split('|');
            if (p.Length != 4) return null;

            DateTime fecha = DateTime.ParseExact(p[1], "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            return new JuegoLocal(p[0], fecha, p[2], p[3] == "1");
        }

        private static string Normalizar(string clave)
        {
            return clave.Replace("-", "").Replace(" ", "").ToUpperInvariant();
        }
    }
}
