
using System;
using System.Collections.Generic;

namespace Sube
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Operacion();
        }


        public static string LeerTexto(string mensaje)
        {
            string valor;
            do
            {
                Console.Write(mensaje);
                valor = Console.ReadLine() ?? "";
            } while (string.IsNullOrWhiteSpace(valor));
            return valor;
        }

        public static decimal LeerDecimalPositivo(string mensaje)
        {
            decimal valor;
            do
            {
                Console.Write(mensaje);
            } while (!decimal.TryParse(Console.ReadLine(), out valor) || valor <= 0);
            return valor;
        }


        public static void EmitirTarjeta(TarjetaServicio servicio)
        {
            string id;
            do
            {
                id = LeerTexto("ID de la tarjeta: ");
                if (servicio.ExisteId(id))
                    Console.WriteLine("Ese ID ya existe. Ingrese otro.");
            } while (servicio.ExisteId(id));

            string pasajero = LeerTexto("Nombre del pasajero: ");
            decimal saldo = LeerDecimalPositivo("Saldo inicial: ");

            Tarjeta nueva = new Tarjeta(id, pasajero, saldo);
            servicio.Guardar(nueva);
            Console.WriteLine("Tarjeta emitida correctamente.");
        }


        public static void VerSaldo(TarjetaServicio servicio)
        {
            string id = LeerTexto("ID de la tarjeta: ");
            Tarjeta? t = servicio.Buscar(id);
            if (t == null)
            {
                Console.WriteLine("No se encontro esa tarjeta.");
                return;
            }
            Console.WriteLine("\n===== DATOS DE LA TARJETA =====");
            Console.WriteLine(t.ToString());
        }


        public static void RegistrarViaje(TarjetaServicio servicio)
        {
            string id = LeerTexto("ID de la tarjeta: ");
            Tarjeta? t = servicio.Buscar(id);
            if (t == null)
            {
                Console.WriteLine("No se encontro esa tarjeta.");
                return;
            }
            if (t.PagarViaje())
            {
                servicio.Actualizar(t);
                Console.WriteLine($"Viaje registrado. Saldo restante: ${t.Saldo}");
            }
            else
            {
                Console.WriteLine("Saldo insuficiente. El saldo no puede bajar de -$200.");
            }
        }


        public static void RecargarSaldo(TarjetaServicio servicio)
        {
            string id = LeerTexto("ID de la tarjeta: ");
            Tarjeta? t = servicio.Buscar(id);
            if (t == null)
            {
                Console.WriteLine("No se encontro esa tarjeta.");
                return;
            }
            decimal monto = LeerDecimalPositivo("Monto a recargar: ");
            t.CargarSaldo(monto);
            servicio.Actualizar(t);
            Console.WriteLine($"Recarga exitosa. Nuevo saldo: ${t.Saldo}");
        }


        public static void Operacion()
        {
            TarjetaServicio servicio = new TarjetaServicio();
            int opcion = 0;

            do
            {
                Console.WriteLine("\n===== SISTEMA SUBE =====");
                Console.WriteLine("1. Emitir nueva tarjeta");
                Console.WriteLine("2. Ver saldo y estadisticas");
                Console.WriteLine("3. Registrar viaje en colectivo");
                Console.WriteLine("4. Recargar dinero en efectivo");
                Console.WriteLine("5. Salir");
                Console.Write("Seleccione opcion: ");

                if (int.TryParse(Console.ReadLine(), out opcion))
                {
                    switch (opcion)
                    {
                        case 1:
                            EmitirTarjeta(servicio);
                            break;
                        case 2:
                            VerSaldo(servicio);
                            break;
                        case 3:
                            RegistrarViaje(servicio);
                            break;
                        case 4:
                            RecargarSaldo(servicio);
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