
using System;

namespace Sube
{
    internal class Tarjeta
    {

        public string IdTarjeta { get; set; }
        public string Pasajero { get; set; }
        public decimal Saldo { get; set; }
        public int ViajesRealizados { get; private set; }


        public Tarjeta(string idTarjeta, string pasajero, decimal saldo)
        {
            IdTarjeta = idTarjeta;
            Pasajero = pasajero;
            Saldo = saldo;
            ViajesRealizados = 0;
        }


        public bool PagarViaje()
        {
            if (Saldo - 100 < -200) return false;
            Saldo -= 100;
            ViajesRealizados++;
            return true;
        }

        public void CargarSaldo(decimal monto)
        {
            if (monto > 0) Saldo += monto;
        }


        public override string ToString()
        {
            return $"ID: {IdTarjeta} | {Pasajero} | Saldo: ${Saldo} | Viajes: {ViajesRealizados}";
        }
    }
}