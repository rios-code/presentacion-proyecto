using System;
using System.IO;
using LicenciaKit;

namespace Generador
{
    // Herramienta del VENDEDOR: crea el par de claves y emite licencias.
    // Guarda: privada.pem (secreta), publica.pem (para embeber), licencias_emitidas.csv (registro).
    internal class Program
    {
        private const string RUTA_PRIVADA = "privada.pem";
        private const string RUTA_PUBLICA = "publica.pem";
        private const string RUTA_REGISTRO = "licencias_emitidas.csv";

        private static void Main()
        {
            Console.WriteLine("=========================================");
            Console.WriteLine("   GENERADOR DE LICENCIAS  (vendedor)");
            Console.WriteLine("=========================================");

            AsegurarClaves();
            GeneradorLicencias generador = new GeneradorLicencias(File.ReadAllText(RUTA_PRIVADA));

            int opcion = 0;
            do
            {
                Console.WriteLine("\n1. Emitir una licencia");
                Console.WriteLine("2. Ver la clave publica (para embeber en tu app)");
                Console.WriteLine("3. Salir");
                Console.Write("Opcion: ");

                if (!int.TryParse(Console.ReadLine(), out opcion)) continue;
                switch (opcion)
                {
                    case 1: EmitirLicencia(generador); break;
                    case 2: MostrarClavePublica(); break;
                    case 3: Console.WriteLine("Listo."); break;
                    default: Console.WriteLine("Opcion no valida."); break;
                }
            } while (opcion != 3);
        }

        private static void AsegurarClaves()
        {
            if (File.Exists(RUTA_PRIVADA)) return;

            Console.WriteLine("\nPrimera vez: creando tu par de claves...");
            ClavesLicencia.CrearParEnArchivos(RUTA_PRIVADA, RUTA_PUBLICA);
            Console.WriteLine($"  - {RUTA_PRIVADA}  -> SECRETA. No la compartas ni la subas a internet.");
            Console.WriteLine($"  - {RUTA_PUBLICA}  -> se embebe en la app que distribuis.");
        }

        private static void EmitirLicencia(GeneradorLicencias generador)
        {
            string producto = Leer("Producto: ");
            string cliente = Leer("Cliente (nombre o email): ");
            Console.Write("Dias de validez (0 = permanente): ");
            int.TryParse(Console.ReadLine(), out int dias);

            Licencia lic = generador.Generar(producto, cliente, dias);

            bool nuevo = !File.Exists(RUTA_REGISTRO);
            using (StreamWriter w = new StreamWriter(RUTA_REGISTRO, append: true))
            {
                if (nuevo) w.WriteLine("Fecha,Producto,Cliente,Vence,Clave");
                string vence = lic.EsPermanente ? "PERMANENTE" : lic.FechaExpiracion.ToString("yyyy-MM-dd");
                w.WriteLine($"{lic.FechaEmision:yyyy-MM-dd HH:mm},\"{lic.Producto}\",\"{lic.Cliente}\",{vence},{lic.Clave}");
            }

            Console.WriteLine("\n===== LICENCIA EMITIDA =====");
            Console.WriteLine(lic);
            Console.WriteLine("\nClave para el cliente:");
            Console.WriteLine(lic.Clave);
            Console.WriteLine($"\n(Registrada en {RUTA_REGISTRO})");
        }

        private static void MostrarClavePublica()
        {
            Console.WriteLine("\n----- CLAVE PUBLICA (embebe este texto en tu app) -----");
            Console.WriteLine(File.ReadAllText(RUTA_PUBLICA));
        }

        private static string Leer(string mensaje)
        {
            string valor;
            do
            {
                Console.Write(mensaje);
                valor = Console.ReadLine() ?? "";
            } while (string.IsNullOrWhiteSpace(valor));
            return valor.Trim();
        }
    }
}
