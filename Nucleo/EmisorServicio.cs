using System;
using System.IO;
using System.Text;
using System.Globalization;
using System.Security.Cryptography;
using Microsoft.Data.Sqlite;

namespace Steam
{
    // Rol EMISOR: tiene la clave PRIVADA y es el UNICO que puede generar licencias validas.
    // En un despliegue real esto vive solo del lado del vendedor (o un servidor),
    // nunca en la app que se distribuye a los usuarios.
    public class EmisorServicio
    {
        private static readonly string RUTA_PRIVADA = Rutas.EnDatos("clave_privada.pem");
        private static readonly string RUTA_PUBLICA = Rutas.EnDatos("clave_publica.pem");

        private readonly ECDsa _ecdsa;

        public EmisorServicio()
        {
            _ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            CargarOCrearClaves();
        }

        // Carga la clave privada, o crea el par la primera vez.
        // Siempre deja publicada la clave publica para que el validador (cliente) pueda usarla.
        private void CargarOCrearClaves()
        {
            if (File.Exists(RUTA_PRIVADA))
            {
                _ecdsa.ImportFromPem(File.ReadAllText(RUTA_PRIVADA));
                if (!File.Exists(RUTA_PUBLICA))
                    File.WriteAllText(RUTA_PUBLICA, _ecdsa.ExportSubjectPublicKeyInfoPem());
            }
            else
            {
                File.WriteAllText(RUTA_PRIVADA, _ecdsa.ExportECPrivateKeyPem());
                File.WriteAllText(RUTA_PUBLICA, _ecdsa.ExportSubjectPublicKeyInfoPem());
            }
        }

        public Licencia Emitir(string producto, string cliente, int diasValidez)
        {
            producto = FormatoClave.Limpiar(producto);
            cliente = FormatoClave.Limpiar(cliente);

            DateTime emision = DateTime.Now;
            DateTime expira = diasValidez <= 0 ? Licencia.PERMANENTE : emision.Date.AddDays(diasValidez);

            // 1) Datos a proteger.
            string payloadTexto = $"{producto}{FormatoClave.SEP}{cliente}{FormatoClave.SEP}{expira:yyyy-MM-dd}";
            byte[] payload = Encoding.UTF8.GetBytes(payloadTexto);

            // 2) Firma digital con la clave PRIVADA.
            byte[] firma = _ecdsa.SignData(payload, HashAlgorithmName.SHA256);

            // 3) Empaqueta datos + firma y lo convierte en una clave legible.
            byte[] blob = FormatoClave.Empaquetar(payload, firma);
            string clave = FormatoClave.FormatearEnBloques(Base32.Codificar(blob));

            Licencia lic = new Licencia(clave, producto, cliente, emision, expira, true);
            Guardar(lic);
            return lic;
        }

        private static void Guardar(Licencia lic)
        {
            using SqliteConnection con = BaseDatos.Abrir();
            using SqliteCommand cmd = con.CreateCommand();
            cmd.CommandText = @"INSERT OR REPLACE INTO licencias
                (clave,producto,cliente,fecha_emision,fecha_expiracion,activa)
                VALUES($k,$p,$c,$e,$x,$a)";
            string expira = lic.EsPermanente ? "PERMANENTE" : lic.FechaExpiracion.ToString("yyyy-MM-dd");
            cmd.Parameters.AddWithValue("$k", lic.Clave);
            cmd.Parameters.AddWithValue("$p", lic.Producto);
            cmd.Parameters.AddWithValue("$c", lic.Cliente);
            cmd.Parameters.AddWithValue("$e", lic.FechaEmision.ToString("yyyy-MM-dd HH:mm:ss",
                                                CultureInfo.InvariantCulture));
            cmd.Parameters.AddWithValue("$x", expira);
            cmd.Parameters.AddWithValue("$a", lic.Activa ? 1 : 0);
            cmd.ExecuteNonQuery();
        }
    }
}
