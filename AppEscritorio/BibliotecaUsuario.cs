using System;
using System.IO;
using System.Collections.Generic;

namespace TiendaJuegos
{
    // Biblioteca de juegos de un usuario, guardada en un archivo propio por cuenta.
    public class BibliotecaUsuario
    {
        private readonly string _ruta;

        public BibliotecaUsuario(string usuario)
        {
            string seguro = usuario.ToLowerInvariant();
            foreach (char c in Path.GetInvalidFileNameChars())
                seguro = seguro.Replace(c, '_');
            _ruta = $"biblioteca_{seguro}.txt";
        }

        public List<string> Juegos()
        {
            List<string> lista = new List<string>();
            if (!File.Exists(_ruta)) return lista;
            foreach (string linea in File.ReadAllLines(_ruta))
                if (!string.IsNullOrWhiteSpace(linea))
                    lista.Add(linea.Trim());
            return lista;
        }

        public bool Tiene(string juego)
        {
            return Juegos().Exists(j => string.Equals(j, juego, StringComparison.OrdinalIgnoreCase));
        }

        public void Agregar(string juego)
        {
            if (Tiene(juego)) return;
            File.AppendAllText(_ruta, juego + "\n");
        }
    }
}
