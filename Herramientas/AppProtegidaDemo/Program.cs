using System;
using System.IO;
using LicenciaKit;

namespace AppProtegidaDemo
{
    // Ejemplo de una app de un CLIENTE que integra LicenciaKit.
    // Al iniciar pide una clave de licencia; si es valida deja usar el programa, si no lo bloquea.
    //
    // En un producto real, la CLAVE PUBLICA va embebida como constante (asi el usuario no la puede
    // cambiar). Aca, para la demo, se lee del archivo publica.pem que genero el vendedor.
    internal class Program
    {
        // >>> En produccion, pega aca el contenido de publica.pem como una constante <<<
        private const string CLAVE_PUBLICA_EMBEBIDA = "";

        private static void Main(string[] args)
        {
            Console.WriteLine("============================================");
            Console.WriteLine("   MI PROGRAMA GENIAL  (app protegida demo)");
            Console.WriteLine("============================================");

            string? clavePublica = ObtenerClavePublica();
            if (clavePublica == null)
            {
                Console.WriteLine("\n[!] No encuentro la clave publica.");
                Console.WriteLine("    Ejecuta primero el Generador y copia 'publica.pem' junto a esta app,");
                Console.WriteLine("    o pega su contenido en CLAVE_PUBLICA_EMBEBIDA.");
                return;
            }

            ValidadorLicencias validador = new ValidadorLicencias(clavePublica);

            // La clave puede venir por argumento (para pruebas) o se pide por teclado.
            string clave = args.Length > 0 ? args[0] : Leer("\nIngresa tu clave de licencia: ");
            ResultadoValidacion r = validador.Validar(clave);

            if (!r.EsValida)
            {
                Console.WriteLine($"\n[X] Acceso denegado: {r.Mensaje}");
                Console.WriteLine("    Compra una licencia para usar el programa.");
                return;
            }

            Console.WriteLine("\n[OK] Licencia valida. Bienvenido!");
            Console.WriteLine($"     Producto: {r.Producto}");
            Console.WriteLine($"     Licencia de: {r.Cliente}");
            Console.WriteLine("\n--- (aca corre tu programa real) ---");
        }

        private static string? ObtenerClavePublica()
        {
            if (!string.IsNullOrWhiteSpace(CLAVE_PUBLICA_EMBEBIDA))
                return CLAVE_PUBLICA_EMBEBIDA;
            if (File.Exists("publica.pem"))
                return File.ReadAllText("publica.pem");
            return null;
        }

        private static string Leer(string mensaje)
        {
            Console.Write(mensaje);
            return (Console.ReadLine() ?? "").Trim();
        }
    }
}
