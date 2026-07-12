using System;
using System.IO;
using System.Text;
using System.Globalization;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace Steam
{
    // Servicio de licencias con FIRMA DIGITAL FUERTE (ECDSA sobre curva P-256).
    //
    // Como funciona la seguridad:
    //  - Al iniciar se crea (una sola vez) un par de claves: privada y publica.
    //  - Para EMITIR una licencia se firman los datos con la clave PRIVADA.
    //  - Para VALIDAR se verifica la firma con la clave PUBLICA.
    //  - Sin la clave privada es imposible fabricar una clave que pase la validacion,
    //    y cualquier modificacion de los datos invalida la firma.
    //  - La clave es autosuficiente: lleva los datos + su firma dentro, se valida sin conexion.
    internal class LicenciaServicio
    {
        private const string RUTA = "licencias.txt";
        private const string RUTA_PRIVADA = "clave_privada.pem";
        private const string RUTA_PUBLICA = "clave_publica.pem";

        // Separador interno de los campos firmados (Unit Separator, no aparece en texto normal).
        private const char SEP = (char)31;

        private readonly ECDsa _ecdsa;

        public LicenciaServicio()
        {
            _ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            CargarOCrearClaves();
        }

        // Carga el par de claves del disco, o lo genera la primera vez.
        private void CargarOCrearClaves()
        {
            if (File.Exists(RUTA_PRIVADA))
            {
                _ecdsa.ImportFromPem(File.ReadAllText(RUTA_PRIVADA));
            }
            else
            {
                File.WriteAllText(RUTA_PRIVADA, _ecdsa.ExportECPrivateKeyPem());
                File.WriteAllText(RUTA_PUBLICA, _ecdsa.ExportSubjectPublicKeyInfoPem());
            }
        }

        // ---------- EMISION ----------

        public Licencia Emitir(string producto, string cliente, int diasValidez)
        {
            // Limpia caracteres que romperian el formato de almacenamiento/firma.
            producto = Limpiar(producto);
            cliente = Limpiar(cliente);

            DateTime emision = DateTime.Now;
            DateTime expira = diasValidez <= 0 ? Licencia.PERMANENTE : emision.Date.AddDays(diasValidez);

            // 1) Datos a proteger.
            string payloadTexto = $"{producto}{SEP}{cliente}{SEP}{expira:yyyy-MM-dd}";
            byte[] payload = Encoding.UTF8.GetBytes(payloadTexto);

            // 2) Firma digital de esos datos con la clave PRIVADA.
            byte[] firma = _ecdsa.SignData(payload, HashAlgorithmName.SHA256);

            // 3) Empaqueta datos + firma y lo convierte en una clave legible.
            byte[] blob = Empaquetar(payload, firma);
            string clave = FormatearEnBloques(Base32.Codificar(blob));

            Licencia lic = new Licencia(clave, producto, cliente, emision, expira, true);
            Guardar(lic);
            return lic;
        }

        // ---------- VALIDACION ----------

        public ResultadoValidacion Validar(string claveIngresada)
        {
            byte[] blob;
            try
            {
                blob = Base32.Decodificar(Normalizar(claveIngresada));
            }
            catch (FormatException ex)
            {
                return ResultadoValidacion.Fallida("Formato de clave invalido: " + ex.Message);
            }

            // Debe alcanzar al menos para el encabezado de longitud (2 bytes).
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

            // 1) Verificacion criptografica: la firma corresponde a estos datos y a NUESTRA clave.
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
                return ResultadoValidacion.Fallida("FIRMA INVALIDA: clave falsificada o alterada.");

            // 2) Leer los datos firmados.
            string[] partes = Encoding.UTF8.GetString(payload).Split(SEP);
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

            // 4) Revocacion: aunque la firma sea valida, puede estar revocada en el registro local.
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

        // ---------- PERSISTENCIA (archivo de texto, un registro por linea) ----------

        public List<Licencia> LeerTodas()
        {
            List<Licencia> lista = new List<Licencia>();
            if (!File.Exists(RUTA)) return lista;

            foreach (string linea in File.ReadAllLines(RUTA))
            {
                Licencia? lic = Parsear(linea);
                if (lic != null) lista.Add(lic);
            }
            return lista;
        }

        public Licencia? Buscar(string claveIngresada)
        {
            string objetivo = Normalizar(claveIngresada);
            foreach (Licencia lic in LeerTodas())
            {
                if (Normalizar(lic.Clave) == objetivo) return lic;
            }
            return null;
        }

        private void Guardar(Licencia lic)
        {
            File.AppendAllText(RUTA, Serializar(lic) + "\n");
        }

        private bool Actualizar(Licencia lic)
        {
            if (!File.Exists(RUTA)) return false;
            string objetivo = Normalizar(lic.Clave);

            List<string> lineas = new List<string>(File.ReadAllLines(RUTA));
            bool encontrada = false;
            for (int i = 0; i < lineas.Count; i++)
            {
                Licencia? actual = Parsear(lineas[i]);
                if (actual != null && Normalizar(actual.Clave) == objetivo)
                {
                    lineas[i] = Serializar(lic);
                    encontrada = true;
                    break;
                }
            }

            if (!encontrada) return false;
            File.WriteAllLines(RUTA, lineas);
            return true;
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

        // ---------- Auxiliares ----------

        private static string Serializar(Licencia l)
        {
            string expira = l.EsPermanente ? "PERMANENTE" : l.FechaExpiracion.ToString("yyyy-MM-dd");
            return string.Join("|",
                l.Clave,
                l.Producto,
                l.Cliente,
                l.FechaEmision.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                expira,
                l.Activa ? "1" : "0");
        }

        private static Licencia? Parsear(string linea)
        {
            if (string.IsNullOrWhiteSpace(linea)) return null;
            string[] p = linea.Split('|');
            if (p.Length != 6) return null;

            DateTime emision = DateTime.ParseExact(p[3], "yyyy-MM-dd HH:mm:ss",
                                                   CultureInfo.InvariantCulture);
            DateTime expira = p[4] == "PERMANENTE"
                ? Licencia.PERMANENTE
                : DateTime.ParseExact(p[4], "yyyy-MM-dd", CultureInfo.InvariantCulture);

            return new Licencia(p[0], p[1], p[2], emision, expira, p[5] == "1");
        }

        // Une los bytes en un solo bloque: [2 bytes con el largo del payload][payload][firma].
        private static byte[] Empaquetar(byte[] payload, byte[] firma)
        {
            byte[] blob = new byte[2 + payload.Length + firma.Length];
            blob[0] = (byte)((payload.Length >> 8) & 0xFF);
            blob[1] = (byte)(payload.Length & 0xFF);
            Array.Copy(payload, 0, blob, 2, payload.Length);
            Array.Copy(firma, 0, blob, 2 + payload.Length, firma.Length);
            return blob;
        }

        // Inserta un guion cada 5 caracteres: ABCDE-FGHIJ-KLMNO-...
        private static string FormatearEnBloques(string texto)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < texto.Length; i++)
            {
                if (i > 0 && i % 5 == 0) sb.Append('-');
                sb.Append(texto[i]);
            }
            return sb.ToString();
        }

        // Quita guiones y espacios y pasa a mayusculas para comparar/decodificar.
        private static string Normalizar(string clave)
        {
            return clave.Replace("-", "").Replace(" ", "").ToUpperInvariant();
        }

        // Evita que el texto rompa el separador de campos o el del archivo.
        private static string Limpiar(string texto)
        {
            return texto.Replace("|", "/").Replace(SEP.ToString(), " ").Trim();
        }
    }
}
