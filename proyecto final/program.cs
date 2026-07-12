using System;
using System.Collections.Generic;

namespace Steam
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Operar();
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

        // ---------- Opciones del menu ----------

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

        // ---------- Bucle principal ----------

        public static void Operar()
        {
            LicenciaServicio servicio = new LicenciaServicio();
            int opcion = 0;

            do
            {
                Console.WriteLine("\n===== GENERADOR DE LICENCIAS - STEAM =====");
                Console.WriteLine("1. Generar nueva licencia");
                Console.WriteLine("2. Validar una licencia");
                Console.WriteLine("3. Listar licencias emitidas");
                Console.WriteLine("4. Revocar una licencia");
                Console.WriteLine("5. Salir");
                Console.Write("Seleccione opcion: ");

                if (int.TryParse(Console.ReadLine(), out opcion))
                {
                    switch (opcion)
                    {
                        case 1:
                            GenerarLicencia(servicio);
                            break;
                        case 2:
                            ValidarLicencia(servicio);
                            break;
                        case 3:
                            ListarLicencias(servicio);
                            break;
                        case 4:
                            RevocarLicencia(servicio);
                            break;
                        case 5:
                            Console.WriteLine("Cerrando el sistema...");
                            break;
                        default:
                            Console.WriteLine("Opcion no valida.");
                            break;
                    }
                }
            } while (opcion != 5);
        }
    }
}
