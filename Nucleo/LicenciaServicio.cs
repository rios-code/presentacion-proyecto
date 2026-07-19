using System;
using System.IO;
using System.Text;
using System.Globalization;
using System.Collections.Generic;
using System.Security.Cryptography;
using Microsoft.Data.Sqlite;

namespace Steam
{
    // Rol CLIENTE / VALIDADOR: solo tiene la clave PUBLICA.
    // Puede VALIDAR licencias (verificar firma, vencimiento y revocacion) y revocar,
    // pero NO puede generar licencias: eso requiere la clave privada del emisor.
    //
    // Seguridad: sin la clave privada es imposible fabricar una clave que pase la validacion,
    // y cualquier modificacion de los datos invalida la firma.
    public class LicenciaServicio
    {
        private static readonly string RUTA_PUBLICA = Rutas.EnDatos("clave_publica.pem");

        private ECDsa? _ecdsa;

        // Carga la clave publica cuando esta disponible (puede crearse despues, por el emisor).
        private ECDsa? ObtenerClavePublica()
        {
            if (_ecdsa != null) return _ecdsa;
            if (!File.Exists(RUTA_PUBLICA)) return null;

            ECDsa e = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            e.ImportFromPem(File.ReadAllText(RUTA_PUBLICA));
            _ecdsa = e;
            return _ecdsa;
        }

        // ---------- VALIDACION ----------

        public ResultadoValidacion Validar(string claveIngresada)
        {
            ECDsa? ecdsa = ObtenerClavePublica();
            if (ecdsa == null)
                return ResultadoValidacion.Fallida(
                    "Falta la clave publica del emisor (todavia no se genero ninguna licencia).");

            byte[] blob;
            try
            {
                blob = Base32.Decodificar(FormatoClave.Normalizar(claveIngresada));
            }
            catch (FormatException ex)
            {
                return ResultadoValidacion.Fallida("Formato de clave invalido: " + ex.Message);
            }

            if (blob.Length < 2)
                return ResultadoValidacion.Fallida("Clave demasiado corta o corrupta.");

            int largoPayload = (blob[0] << 8) | blob[1];
            if (largoPayload <= 0 || 2 + largoPayload > blob.Length)
                return ResultadoValidacion.Fallida("Clave corrupta (estructura invalida).");

            byte[] payload = new byte[largoPayload];
            Array.Copy(blob, 2, payload, 0, largoPayload);

            int largoFirma = blob.Length - 2 - largoPayload;
            if (largoFirma <= 0)
                return ResultadoValidacion.Fallida("Clave corrupta (falta la firma).");

            byte[] firma = new byte[largoFirma];
            Array.Copy(blob, 2 + largoPayload, firma, 0, largoFirma);

            // 1) Verificacion criptografica con la clave PUBLICA.
            bool firmaOk;
            try
            {
                firmaOk = ecdsa.VerifyData(payload, firma, HashAlgorithmName.SHA256);
            }
            catch (CryptographicException)
            {
                firmaOk = false;
            }

            if (!firmaOk)
                return ResultadoValidacion.Fallida("FIRMA INVALIDA: clave falsificada o alterada.");

            // 2) Leer los datos firmados.
            string[] partes = Encoding.UTF8.GetString(payload).Split(FormatoClave.SEP);
            if (partes.Length != 3)
                return ResultadoValidacion.Fallida("Contenido de la clave invalido.");

            string producto = partes[0];
            string cliente = partes[1];
            if (!DateTime.TryParseExact(partes[2], "yyyy-MM-dd", CultureInfo.InvariantCulture,
                                        DateTimeStyles.None, out DateTime expira))
                return ResultadoValidacion.Fallida("Fecha de expiracion invalida.");

            bool permanente = expira.Date >= Licencia.PERMANENTE.Date;

            // 3) Vencimiento.
            if (!permanente && expira.Date < DateTime.Now.Date)
                return ResultadoValidacion.Fallida($"Licencia VENCIDA el {expira:yyyy-MM-dd}.");

            // 4) Revocacion: aunque la firma sea valida, puede estar revocada en el registro.
            Licencia? registro = Buscar(claveIngresada);
            if (registro != null && !registro.Activa)
                return ResultadoValidacion.Fallida("Licencia REVOCADA por el emisor.");

            return ResultadoValidacion.Correcta(producto, cliente, expira);
        }

        // ---------- REVOCACION ----------

        public bool Revocar(string claveIngresada)
        {
            Licencia? lic = Buscar(claveIngresada);
            if (lic == null || !lic.Activa) return false;
            lic.Revocar();
            return Actualizar(lic);
        }

        // ---------- PERSISTENCIA (base de datos SQLite) ----------

        public List<Licencia> LeerTodas()
        {
            List<Licencia> lista = new List<Licencia>();
            using SqliteConnection con = BaseDatos.Abrir();
            using SqliteCommand cmd = con.CreateCommand();
            cmd.CommandText = @"SELECT clave,producto,cliente,fecha_emision,fecha_expiracion,activa
                                FROM licencias ORDER BY rowid";
            using SqliteDataReader rd = cmd.ExecuteReader();
            while (rd.Read())
                lista.Add(LeerFila(rd));
            return lista;
        }

        public Licencia? Buscar(string claveIngresada)
        {
            string objetivo = FormatoClave.Normalizar(claveIngresada);
            foreach (Licencia lic in LeerTodas())
            {
                if (FormatoClave.Normalizar(lic.Clave) == objetivo) return lic;
            }
            return null;
        }

        private bool Actualizar(Licencia lic)
        {
            using SqliteConnection con = BaseDatos.Abrir();
            using SqliteCommand cmd = con.CreateCommand();
            cmd.CommandText = @"UPDATE licencias SET producto=$p, cliente=$c,
                fecha_emision=$e, fecha_expiracion=$x, activa=$a WHERE clave=$k";
            string expira = lic.EsPermanente ? "PERMANENTE" : lic.FechaExpiracion.ToString("yyyy-MM-dd");
            cmd.Parameters.AddWithValue("$k", lic.Clave);
            cmd.Parameters.AddWithValue("$p", lic.Producto);
            cmd.Parameters.AddWithValue("$c", lic.Cliente);
            cmd.Parameters.AddWithValue("$e", lic.FechaEmision.ToString("yyyy-MM-dd HH:mm:ss",
                                                CultureInfo.InvariantCulture));
            cmd.Parameters.AddWithValue("$x", expira);
            cmd.Parameters.AddWithValue("$a", lic.Activa ? 1 : 0);
            return cmd.ExecuteNonQuery() > 0;
        }

        private static Licencia LeerFila(SqliteDataReader rd)
        {
            DateTime emision = DateTime.ParseExact(rd.GetString(3), "yyyy-MM-dd HH:mm:ss",
                                                   CultureInfo.InvariantCulture);
            DateTime expira = rd.GetString(4) == "PERMANENTE"
                ? Licencia.PERMANENTE
                : DateTime.ParseExact(rd.GetString(4), "yyyy-MM-dd", CultureInfo.InvariantCulture);
            return new Licencia(rd.GetString(0), rd.GetString(1), rd.GetString(2),
                                emision, expira, rd.GetInt32(5) == 1);
        }

        // ---------- Exportacion ----------

        // Exporta todas las licencias emitidas a un CSV (se abre en Excel/LibreOffice).
        public int ExportarCsv(string ruta)
        {
            List<Licencia> lista = LeerTodas();

            List<string> lineas = new List<string>
            {
                "Clave,Producto,Cliente,FechaEmision,FechaExpiracion,Estado"
            };
            foreach (Licencia l in lista)
            {
                string vence = l.EsPermanente ? "PERMANENTE" : l.FechaExpiracion.ToString("yyyy-MM-dd");
                string estado = !l.Activa ? "REVOCADA" : (l.EstaVigente() ? "VIGENTE" : "VENCIDA");
                lineas.Add(string.Join(",",
                    Csv(l.Clave),
                    Csv(l.Producto),
                    Csv(l.Cliente),
                    l.FechaEmision.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                    vence,
                    estado));
            }

            File.WriteAllLines(ruta, lineas);
            return lista.Count;
        }

        // Envuelve un valor entre comillas y escapa las internas (formato CSV).
        private static string Csv(string s)
        {
            return "\"" + s.Replace("\"", "\"\"") + "\"";
        }
    }
}
