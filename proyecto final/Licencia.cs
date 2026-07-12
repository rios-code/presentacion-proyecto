using System;

namespace Steam
{
    // Representa una licencia de activacion de un juego de Steam.
    internal class Licencia
    {
        public string Clave { get; set; }
        public string Producto { get; set; }
        public string Cliente { get; set; }
        public DateTime FechaEmision { get; set; }
        public DateTime FechaExpiracion { get; set; }
        public bool Activa { get; set; }

        // Fecha usada para representar una licencia que nunca vence.
        public static readonly DateTime PERMANENTE = new DateTime(9999, 12, 31);

        public Licencia(string clave, string producto, string cliente,
                        DateTime fechaEmision, DateTime fechaExpiracion, bool activa = true)
        {
            Clave = clave;
            Producto = producto;
            Cliente = cliente;
            FechaEmision = fechaEmision;
            FechaExpiracion = fechaExpiracion;
            Activa = activa;
        }

        public bool EsPermanente => FechaExpiracion.Date >= PERMANENTE.Date;

        // Vigente = activa (no revocada) y todavia no vencida.
        public bool EstaVigente()
        {
            return Activa && (EsPermanente || FechaExpiracion.Date >= DateTime.Now.Date);
        }

        public void Revocar()
        {
            Activa = false;
        }

        public override string ToString()
        {
            string vencimiento = EsPermanente ? "Sin vencimiento" : FechaExpiracion.ToString("yyyy-MM-dd");
            string estado = !Activa ? "REVOCADA" : (EstaVigente() ? "VIGENTE" : "VENCIDA");
            return $"{Producto} | {Cliente} | Emitida: {FechaEmision:yyyy-MM-dd} | Vence: {vencimiento} | Estado: {estado}\n   {Clave}";
        }
    }
}
