using System;

namespace LicenciaKit
{
    // Una licencia emitida: la clave de activacion mas sus datos.
    public class Licencia
    {
        public string Clave { get; }
        public string Producto { get; }
        public string Cliente { get; }
        public DateTime FechaEmision { get; }
        public DateTime FechaExpiracion { get; }

        // Fecha usada para representar una licencia que nunca vence.
        public static readonly DateTime PERMANENTE = new DateTime(9999, 12, 31);

        public Licencia(string clave, string producto, string cliente,
                        DateTime fechaEmision, DateTime fechaExpiracion)
        {
            Clave = clave;
            Producto = producto;
            Cliente = cliente;
            FechaEmision = fechaEmision;
            FechaExpiracion = fechaExpiracion;
        }

        public bool EsPermanente => FechaExpiracion.Date >= PERMANENTE.Date;

        public override string ToString()
        {
            string vence = EsPermanente ? "sin vencimiento" : FechaExpiracion.ToString("yyyy-MM-dd");
            return $"{Producto} | {Cliente} | vence: {vence}";
        }
    }
}
