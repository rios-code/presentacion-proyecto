using System;
using System.Text;
using System.Globalization;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace LicenciaKit
{
    // Lado CLIENTE. Con solo la clave PUBLICA valida claves de licencia.
    // Esto es lo que se embebe en la app que se distribuye: NO puede generar licencias.
    //
    // La validacion es autosuficiente y offline: la clave lleva sus datos + la firma adentro.
    public class ValidadorLicencias
    {
        private readonly ECDsa _ecdsa;
        private readonly HashSet<string> _revocadas;

        // Recibe la clave publica en PEM y, opcionalmente, una lista de claves revocadas.
        public ValidadorLicencias(string clavePublicaPem, IEnumerable<string>? clavesRevocadas = null)
        {
            _ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            _ecdsa.ImportFromPem(clavePublicaPem);

            _revocadas = new HashSet<string>();
            if (clavesRevocadas != null)
                foreach (string c in clavesRevocadas)
                    _revocadas.Add(FormatoClave.Normalizar(c));
        }

        public ResultadoValidacion Validar(string claveIngresada)
        {
            byte[] blob;
            try
            {
                blob = Base32.Decodificar(FormatoClave.Normalizar(claveIngresada));
            }
            catch (FormatException ex)
            {
                return ResultadoValidacion.Fallida(EstadoLicencia.FormatoInvalido,
                    "Formato de clave invalido: " + ex.Message);
            }

            if (blob.Length < 2)
                return ResultadoValidacion.Fallida(EstadoLicencia.FormatoInvalido, "Clave demasiado corta o corrupta.");

            int largoPayload = (blob[0] << 8) | blob[1];
            if (largoPayload <= 0 || 2 + largoPayload > blob.Length)
                return ResultadoValidacion.Fallida(EstadoLicencia.FormatoInvalido, "Clave corrupta (estructura invalida).");

            byte[] payload = new byte[largoPayload];
            Array.Copy(blob, 2, payload, 0, largoPayload);

            int largoFirma = blob.Length - 2 - largoPayload;
            if (largoFirma <= 0)
                return ResultadoValidacion.Fallida(EstadoLicencia.FormatoInvalido, "Clave corrupta (falta la firma).");

            byte[] firma = new byte[largoFirma];
            Array.Copy(blob, 2 + largoPayload, firma, 0, largoFirma);

            // 1) Verificacion criptografica con la clave publica.
            bool firmaOk;
            try
            {
                firmaOk = _ecdsa.VerifyData(payload, firma, HashAlgorithmName.SHA256);
            }
            catch (CryptographicException)
            {
                firmaOk = false;
            }

            if (!firmaOk)
                return ResultadoValidacion.Fallida(EstadoLicencia.FirmaInvalida,
                    "Firma invalida: clave falsificada o alterada.");

            // 2) Datos firmados.
            string[] partes = Encoding.UTF8.GetString(payload).Split(FormatoClave.SEP);
            if (partes.Length != 3)
                return ResultadoValidacion.Fallida(EstadoLicencia.FormatoInvalido, "Contenido de la clave invalido.");

            string producto = partes[0];
            string cliente = partes[1];
            if (!DateTime.TryParseExact(partes[2], "yyyy-MM-dd", CultureInfo.InvariantCulture,
                                        DateTimeStyles.None, out DateTime expira))
                return ResultadoValidacion.Fallida(EstadoLicencia.FormatoInvalido, "Fecha de expiracion invalida.");

            // 3) Revocacion (lista opcional).
            if (_revocadas.Contains(FormatoClave.Normalizar(claveIngresada)))
                return ResultadoValidacion.Fallida(EstadoLicencia.Revocada, "Licencia revocada por el emisor.");

            // 4) Vencimiento.
            bool permanente = expira.Date >= Licencia.PERMANENTE.Date;
            if (!permanente && expira.Date < DateTime.Now.Date)
                return ResultadoValidacion.Fallida(EstadoLicencia.Vencida, $"Licencia vencida el {expira:yyyy-MM-dd}.");

            return ResultadoValidacion.Correcta(producto, cliente, expira);
        }
    }
}
