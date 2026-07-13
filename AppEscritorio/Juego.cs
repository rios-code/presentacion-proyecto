using System.Collections.Generic;

namespace TiendaJuegos
{
    // Un juego de la tienda.
    public class Juego
    {
        public string Nombre { get; }
        public string Precio { get; }
        public string Genero { get; }
        public string Color { get; }   // color de acento para la portada

        public Juego(string nombre, string precio, string genero, string color)
        {
            Nombre = nombre;
            Precio = precio;
            Genero = genero;
            Color = color;
        }
    }

    // Catalogo de la tienda (lista de ejemplo, con generos y colores).
    public static class Catalogo
    {
        public static List<Juego> Juegos { get; } = new List<Juego>
        {
            new Juego("Aventura Estelar", "$9.99",  "Aventura",  "#3A6EA5"),
            new Juego("Reinos Ocultos",   "Gratis", "RPG",       "#7B4397"),
            new Juego("Velocidad Total",  "$19.99", "Carreras",  "#C0392B"),
            new Juego("Puzzle Mind",      "$4.99",  "Puzzle",    "#16A085"),
            new Juego("Guerra Tactica",   "$29.99", "Estrategia","#2C3E50"),
            new Juego("Cielo Infinito",   "Gratis", "Simulacion","#2980B9"),
            new Juego("Sombras del Norte","$24.99", "Accion",    "#4A235A"),
            new Juego("Granja Feliz",     "$7.99",  "Casual",    "#27AE60"),
            new Juego("Circuito Neon",    "$14.99", "Arcade",    "#D35400"),
            new Juego("Leyendas del Mar", "$34.99", "Aventura",  "#1F618D"),
            new Juego("Torre Infinita",   "Gratis", "Roguelike", "#884EA0"),
            new Juego("Ultimo Bastion",   "$39.99", "Shooter",   "#922B21"),
        };
    }
}
