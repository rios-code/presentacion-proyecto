using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Steam
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            await Operar();
        }

        // ---------- Lectura de datos por consola ----------

        public static string LeerTexto(string mensaje)
        {
            string valor;
            do
            {
                Console.Write(mensaje);
                valor = Console.ReadLine() ?? "";
            } while (string.IsNullOrWhiteSpace(valor));
            return valor.Trim();
        }

        public static int LeerEnteroNoNegativo(string mensaje)
        {
            int valor;
            do
            {
                Console.Write(mensaje);
            } while (!int.TryParse(Console.ReadLine(), out valor) || valor < 0);
            return valor;
        }

        // ========== LICENCIAS (local, firma digital) ==========

        public static void GenerarLicencia(LicenciaServicio servicio)
        {
            string producto = LeerTexto("Juego / producto de Steam: ");
            string cliente = LeerTexto("Cliente (nombre o cuenta): ");
            int dias = LeerEnteroNoNegativo("Dias de validez (0 = permanente): ");

            Licencia lic = servicio.Emitir(producto, cliente, dias);

            Console.WriteLine("\n===== LICENCIA GENERADA Y FIRMADA =====");
            Console.WriteLine(lic.ToString());
            Console.WriteLine("\nEntregue esta clave al cliente. Se valida sin conexion.");
        }

        public static void ValidarLicencia(LicenciaServicio servicio)
        {
            string clave = LeerTexto("Ingrese la clave a validar: ");
            ResultadoValidacion r = servicio.Validar(clave);

            Console.WriteLine("\n===== RESULTADO =====");
            if (r.Valida)
            {
                Console.WriteLine("[OK] " + r.Mensaje);
                Console.WriteLine($"     Juego : {r.Producto}");
                Console.WriteLine($"     Cliente: {r.Cliente}");
                string vence = r.Expira!.Value.Date >= Licencia.PERMANENTE.Date
                    ? "Sin vencimiento"
                    : r.Expira.Value.ToString("yyyy-MM-dd");
                Console.WriteLine($"     Vence  : {vence}");
            }
            else
            {
                Console.WriteLine("[X] " + r.Mensaje);
            }
        }

        public static void ListarLicencias(LicenciaServicio servicio)
        {
            List<Licencia> lista = servicio.LeerTodas();
            Console.WriteLine("\n===== LICENCIAS EMITIDAS =====");
            if (lista.Count == 0)
            {
                Console.WriteLine("(todavia no hay licencias emitidas)");
                return;
            }
            int n = 1;
            foreach (Licencia lic in lista)
            {
                Console.WriteLine($"{n}. {lic}");
                n++;
            }
        }

        public static void RevocarLicencia(LicenciaServicio servicio)
        {
            string clave = LeerTexto("Ingrese la clave a revocar: ");
            if (servicio.Revocar(clave))
                Console.WriteLine("Licencia revocada. Ya no pasara la validacion.");
            else
                Console.WriteLine("No se encontro una licencia activa con esa clave.");
        }

        // ========== STEAM (API oficial) ==========

        public static async Task InfoJuego(SteamServicio steam)
        {
            string appId = LeerTexto("AppID del juego (ej: 220 = Half-Life 2): ");
            Console.WriteLine("\n===== INFO DEL JUEGO (Steam) =====");
            try
            {
                Console.WriteLine(await steam.InfoJuegoAsync(appId));
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine("No se pudo conectar con Steam: " + ex.Message);
            }
        }

        // Pide y guarda credenciales si todavia no estan cargadas.
        private static bool AsegurarCredenciales(SteamServicio steam)
        {
            if (steam.TieneCredenciales) return true;

            Console.WriteLine("\nEsta opcion usa tu cuenta. Necesitas:");
            Console.WriteLine(" - API key gratuita: https://steamcommunity.com/dev/apikey");
            Console.WriteLine(" - Tu SteamID64 (17 digitos): https://steamid.io");
            string key = LeerTexto("API key: ");
            string id = LeerTexto("SteamID64: ");
            steam.GuardarConfig(key, id);
            Console.WriteLine("Credenciales guardadas (steam_config.txt, fuera del repositorio).");
            return true;
        }

        public static async Task MiBiblioteca(SteamServicio steam)
        {
            if (!AsegurarCredenciales(steam)) return;
            Console.WriteLine("\n===== MI BIBLIOTECA (Steam) =====");
            try
            {
                Console.WriteLine(await steam.MiBibliotecaAsync());
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine("No se pudo conectar con Steam: " + ex.Message);
            }
        }

        public static async Task GuardarBiblioteca(SteamServicio steam)
        {
            if (!AsegurarCredenciales(steam)) return;
            const string ruta = "biblioteca_steam.csv";
            Console.WriteLine("\n===== GUARDAR BIBLIOTECA (Steam) =====");
            try
            {
                int cantidad = await steam.GuardarBibliotecaAsync(ruta);
                if (cantidad == 0)
                    Console.WriteLine("No se guardaron juegos (perfil privado o credenciales incorrectas).");
                else
                    Console.WriteLine($"Guardados {cantidad} juegos en '{ruta}'.");
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine("No se pudo conectar con Steam: " + ex.Message);
            }
        }

        public static async Task MiPerfil(SteamServicio steam)
        {
            if (!AsegurarCredenciales(steam)) return;
            Console.WriteLine("\n===== MI PERFIL (Steam) =====");
            try
            {
                Console.WriteLine(await steam.MiPerfilAsync());
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine("No se pudo conectar con Steam: " + ex.Message);
            }
        }

        // ---------- Bucle principal ----------

        public static async Task Operar()
        {
            LicenciaServicio licencias = new LicenciaServicio();
            SteamServicio steam = new SteamServicio();
            int opcion = 0;

            do
            {
                Console.WriteLine("\n===== PLATAFORMA STEAM =====");
                Console.WriteLine("--- Licencias (local, firma digital) ---");
                Console.WriteLine("1. Generar licencia");
                Console.WriteLine("2. Validar licencia");
                Console.WriteLine("3. Listar licencias");
                Console.WriteLine("4. Revocar licencia");
                Console.WriteLine("--- Steam (API oficial) ---");
                Console.WriteLine("5. Ver info y precio de un juego     (publico)");
                Console.WriteLine("6. Ver mi biblioteca de juegos       (mi cuenta)");
                Console.WriteLine("7. Guardar mi biblioteca en archivo  (mi cuenta)");
                Console.WriteLine("8. Ver mi perfil de Steam            (mi cuenta)");
                Console.WriteLine("9. Salir");
                Console.Write("Seleccione opcion: ");

                if (int.TryParse(Console.ReadLine(), out opcion))
                {
                    switch (opcion)
                    {
                        case 1: GenerarLicencia(licencias); break;
                        case 2: ValidarLicencia(licencias); break;
                        case 3: ListarLicencias(licencias); break;
                        case 4: RevocarLicencia(licencias); break;
                        case 5: await InfoJuego(steam); break;
                        case 6: await MiBiblioteca(steam); break;
                        case 7: await GuardarBiblioteca(steam); break;
                        case 8: await MiPerfil(steam); break;
                        case 9: Console.WriteLine("Cerrando el sistema..."); break;
                        default: Console.WriteLine("Opcion no valida."); break;
                    }
                }
            } while (opcion != 9);
        }
    }
}
