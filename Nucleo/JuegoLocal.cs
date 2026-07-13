using System;

namespace Steam
{
    // Un juego activado en la biblioteca LOCAL del usuario (dentro de esta app, no en Steam).
    // Se obtiene canjeando una licencia valida.
    public class JuegoLocal
    {
        public string Nombre { get; set; }
        public DateTime FechaActivacion { get; set; }
        public string ClaveUsada { get; set; }   // evita canjear dos veces la misma licencia
        public bool Instalado { get; set; }

        public JuegoLocal(string nombre, DateTime fechaActivacion, string claveUsada, bool instalado = false)
        {
            Nombre = nombre;
            FechaActivacion = fechaActivacion;
            ClaveUsada = claveUsada;
            Instalado = instalado;
        }

        public override string ToString()
        {
            string estado = Instalado ? "INSTALADO" : "no instalado";
            return $"{Nombre}  [{estado}]  (activado {FechaActivacion:yyyy-MM-dd})";
        }
    }
}
