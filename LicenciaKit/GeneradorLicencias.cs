using System;
using System.Text;
using System.Security.Cryptography;

namespace LicenciaKit
{
    // Lado VENDEDOR. Con la clave PRIVADA genera claves de licencia firmadas.
    // Esta clase NO debe distribuirse a los clientes (tiene la clave privada).
    public class GeneradorLicencias
    {
        private readonly ECDsa _ecdsa;

        // Recibe la clave privada en formato PEM (la que devuelve ClavesLicencia).
        public GeneradorLicencias(string clavePrivadaPem)
        {
            _ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            _ecdsa.ImportFromPem(clavePrivadaPem);
        }

        // Genera una licencia. diasValidez <= 0 significa "permanente".
        public Licencia Generar(string producto, string cliente, int diasValidez = 0)
        {
            producto = FormatoClave.Limpiar(producto);
            cliente = FormatoClave.Limpiar(cliente);

            DateTime emision = DateTime.Now;
            DateTime expira = diasValidez <= 0 ? Licencia.PERMANENTE : emision.Date.AddDays(diasValidez);

            // Datos protegidos por la firma.
            string payloadTexto = $"{producto}{FormatoClave.SEP}{cliente}{FormatoClave.SEP}{expira:yyyy-MM-dd}";
            byte[] payload = Encoding.UTF8.GetBytes(payloadTexto);

            // Firma digital con la clave privada.
            byte[] firma = _ecdsa.SignData(payload, HashAlgorithmName.SHA256);

            // Empaqueta datos + firma en una clave legible.
            byte[] blob = FormatoClave.Empaquetar(payload, firma);
            string clave = FormatoClave.FormatearEnBloques(Base32.Codificar(blob));

            return new Licencia(clave, producto, cliente, emision, expira);
        }
    }
}
