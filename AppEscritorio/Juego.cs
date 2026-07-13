using System.Collections.Generic;

namespace TiendaJuegos
{
    // Un juego de la tienda.
    public class Juego
    {
        public string Nombre { get; }
        public string Precio { get; }

        public Juego(string nombre, string precio)
        {
            Nombre = nombre;
            Precio = precio;
        }
    }

    // Catalogo de la tienda (por ahora, una lista fija de ejemplo).
    public static class Catalogo
    {
        public static List<Juego> Juegos { get; } = new List<Juego>
        {
            new Juego("Aventura Estelar", "$9.99"),
            new Juego("Reinos Ocultos", "Gratis"),
            new Juego("Velocidad Total", "$19.99"),
            new Juego("Puzzle Mind", "$4.99"),
            new Juego("Guerra Tactica", "$29.99"),
            new Juego("Cielo Infinito", "Gratis"),
        };
    }
}
