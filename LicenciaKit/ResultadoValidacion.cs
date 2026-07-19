using System;

namespace LicenciaKit
{
    // Estado posible de una licencia validada.
    public enum EstadoLicencia
    {
        Valida,
        FormatoInvalido,   // no se pudo decodificar la clave
        FirmaInvalida,     // clave falsificada o alterada
        Vencida,           // la firma es valida pero ya expiro
        Revocada           // el emisor la anulo
    }

    // Resultado de validar una licencia. Incluye el estado, un mensaje y los datos que trae la clave.
    public class ResultadoValidacion
    {
        public EstadoLicencia Estado { get; }
        public string Mensaje { get; }
        public string? Producto { get; }
        public string? Cliente { get; }
        public DateTime? Expira { get; }

        public bool EsValida => Estado == EstadoLicencia.Valida;

        private ResultadoValidacion(EstadoLicencia estado, string mensaje,
                                    string? producto = null, string? cliente = null, DateTime? expira = null)
        {
            Estado = estado;
            Mensaje = mensaje;
            Producto = producto;
            Cliente = cliente;
            Expira = expira;
        }

        public static ResultadoValidacion Correcta(string producto, string cliente, DateTime expira)
            => new ResultadoValidacion(EstadoLicencia.Valida, "Licencia valida.", producto, cliente, expira);

        public static ResultadoValidacion Fallida(EstadoLicencia estado, string mensaje)
            => new ResultadoValidacion(estado, mensaje);
    }
}
