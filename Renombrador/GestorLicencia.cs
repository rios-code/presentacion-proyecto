using System;
using System.IO;
using LicenciaKit;

namespace Renombrador
{
    // Gestiona la licencia de la app usando LicenciaKit.
    // La CLAVE PUBLICA va embebida (no es secreta). Para vender TU version, reemplazala por la
    // tuya: corre el Generador, copia el contenido de publica.pem aca, y emiti licencias con tu privada.
    public static class GestorLicencia
    {
        public const string CLAVE_PUBLICA =
@"-----BEGIN PUBLIC KEY-----
MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEfnEXZxYBc5kQe7QfFvYB1lYVmfy8
4nli4kQF8OgTCKYgVRrJPnn7FbB7e/Alk8IC0Yz29R3ulRqmh4PZ3Z0ZFg==
-----END PUBLIC KEY-----";

        private static string RutaLicencia()
        {
            string dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Renombrador");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "licencia.txt");
        }

        public static ResultadoValidacion Validar(string clave)
            => new ValidadorLicencias(CLAVE_PUBLICA).Validar(clave);

        public static void Guardar(string clave)
            => File.WriteAllText(RutaLicencia(), clave.Trim());

        // Devuelve el resultado de la licencia guardada, o null si no hay ninguna guardada.
        public static ResultadoValidacion? EstadoGuardado()
        {
            string ruta = RutaLicencia();
            if (!File.Exists(ruta)) return null;
            string clave = File.ReadAllText(ruta).Trim();
            if (clave.Length == 0) return null;
            return Validar(clave);
        }
    }
}
