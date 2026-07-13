using System;

namespace Steam
{
    // Resultado de validar una clave: si es valida, el motivo y los datos que trae dentro.
    public class ResultadoValidacion
    {
        public bool Valida { get; }
        public string Mensaje { get; }
        public string? Producto { get; }
        public string? Cliente { get; }
        public DateTime? Expira { get; }

        private ResultadoValidacion(bool valida, string mensaje,
                                    string? producto = null, string? cliente = null, DateTime? expira = null)
        {
            Valida = valida;
            Mensaje = mensaje;
            Producto = producto;
            Cliente = cliente;
            Expira = expira;
        }

        public static ResultadoValidacion Correcta(string producto, string cliente, DateTime expira)
        {
            return new ResultadoValidacion(true, "Licencia VALIDA y verificada.", producto, cliente, expira);
        }

        public static ResultadoValidacion Fallida(string motivo)
        {
            return new ResultadoValidacion(false, motivo);
        }
    }
}
