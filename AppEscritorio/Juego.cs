using System.Collections.Generic;

namespace TiendaJuegos
{
    // Un juego de la tienda.
    public class Juego
    {
        public string Nombre { get; }
        public string Precio { get; }
        public string Genero { get; }
        public string Color { get; }        // color de acento para la portada
        public string Descripcion { get; }

        public Juego(string nombre, string precio, string genero, string color, string descripcion)
        {
            Nombre = nombre;
            Precio = precio;
            Genero = genero;
            Color = color;
            Descripcion = descripcion;
        }
    }

    // Catalogo de la tienda (lista de ejemplo, con generos, colores y descripcion).
    public static class Catalogo
    {
        public static List<Juego> Juegos { get; } = new List<Juego>
        {
            new Juego("Aventura Estelar", "$9.99",  "Aventura",  "#3A6EA5", "Explora galaxias, descubre planetas y sobrevive en el espacio profundo en esta epica aventura."),
            new Juego("Reinos Ocultos",   "Gratis", "RPG",       "#7B4397", "Un RPG de fantasia donde forjas tu heroe, subes de nivel y desentranas reinos perdidos."),
            new Juego("Velocidad Total",  "$19.99", "Carreras",  "#C0392B", "Carreras a maxima velocidad con autos personalizables y circuitos por todo el mundo."),
            new Juego("Puzzle Mind",      "$4.99",  "Puzzle",    "#16A085", "Cientos de acertijos que ponen a prueba tu logica. Facil de aprender, dificil de dominar."),
            new Juego("Guerra Tactica",   "$29.99", "Estrategia","#2C3E50", "Estrategia por turnos: comanda ejercitos, administra recursos y conquista el mapa."),
            new Juego("Cielo Infinito",   "Gratis", "Simulacion","#2980B9", "Simulador de vuelo relajante con paisajes generados y clima dinamico."),
            new Juego("Sombras del Norte","$24.99", "Accion",    "#4A235A", "Accion y sigilo en tierras heladas. Enfrenta enemigos y desvela una historia oscura."),
            new Juego("Granja Feliz",     "$7.99",  "Casual",    "#27AE60", "Cultiva, cria animales y construi la granja de tus suenos a tu propio ritmo."),
            new Juego("Circuito Neon",    "$14.99", "Arcade",    "#D35400", "Arcade retro de reflejos con estetica neon y una banda sonora electrizante."),
            new Juego("Leyendas del Mar", "$34.99", "Aventura",  "#1F618D", "Navega mares abiertos, comanda tu barco y busca tesoros legendarios."),
            new Juego("Torre Infinita",   "Gratis", "Roguelike", "#884EA0", "Sube pisos generados al azar. Cada partida es distinta y la muerte es permanente."),
            new Juego("Ultimo Bastion",   "$39.99", "Shooter",   "#922B21", "Shooter cooperativo: defende el ultimo bastion de la humanidad contra oleadas enemigas."),
        };
    }
}
