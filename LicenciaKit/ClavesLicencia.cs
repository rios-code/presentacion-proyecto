using System;
using System.IO;
using System.Security.Cryptography;

namespace LicenciaKit
{
    // Manejo del par de claves (ECDSA P-256) en formato PEM.
    //
    //  - La clave PRIVADA la guarda solo el vendedor y sirve para GENERAR licencias.
    //  - La clave PUBLICA se embebe en la app que se distribuye y sirve para VALIDAR.
    public static class ClavesLicencia
    {
        // Crea un par nuevo. Devuelve (clavePrivadaPem, clavePublicaPem).
        public static (string privada, string publica) CrearPar()
        {
            using ECDsa ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            return (ecdsa.ExportECPrivateKeyPem(), ecdsa.ExportSubjectPublicKeyInfoPem());
        }

        // Crea el par y lo guarda en dos archivos .pem.
        public static (string privada, string publica) CrearParEnArchivos(string rutaPrivada, string rutaPublica)
        {
            (string priv, string pub) = CrearPar();
            File.WriteAllText(rutaPrivada, priv);
            File.WriteAllText(rutaPublica, pub);
            return (priv, pub);
        }
    }
}
