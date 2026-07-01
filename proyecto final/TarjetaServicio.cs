
using System;
using System.IO;
using System.Collections.Generic;

namespace Sube
{
    internal class TarjetaServicio
    {
        private const string RUTA = "tarjetas.txt";


        public bool ExisteId(string idTarjeta)
        {
            if (!File.Exists(RUTA)) return false;
            foreach (string linea in File.ReadAllLines(RUTA))
            {
                if (string.IsNullOrWhiteSpace(linea)) continue;
                if (linea.Split('|')[0] == idTarjeta) return true;
            }
            return false;
        }


        public bool Guardar(Tarjeta t)
        {
            if (ExisteId(t.IdTarjeta)) return false;
            string linea = $"{t.IdTarjeta}|{t.Pasajero}|{t.Saldo}|{t.ViajesRealizados}";
            File.AppendAllText(RUTA, linea + "\n");
            return true;
        }


        public List<Tarjeta> LeerTodos()
        {
            List<Tarjeta> lista = new List<Tarjeta>();
            if (!File.Exists(RUTA)) return lista;
            foreach (string linea in File.ReadAllLines(RUTA))
            {
                if (string.IsNullOrWhiteSpace(linea)) continue;
                string[] partes = linea.Split('|');
                Tarjeta t = new Tarjeta(partes[0], partes[1], decimal.Parse(partes[2]));
                for (int i = 0; i < int.Parse(partes[3]); i++)
                    t.PagarViaje();
                lista.Add(t);
            }
            return lista;
        }


        public bool Actualizar(Tarjeta t)
        {
            if (!ExisteId(t.IdTarjeta)) return false;
            var lineas = new List<string>(File.ReadAllLines(RUTA));
            for (int i = 0; i < lineas.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(lineas[i])) continue;
                if (lineas[i].Split('|')[0] == t.IdTarjeta)
                {
                    lineas[i] = $"{t.IdTarjeta}|{t.Pasajero}|{t.Saldo}|{t.ViajesRealizados}";
                    break;
                }
            }
            File.WriteAllLines(RUTA, lineas);
            return true;
        }


        public Tarjeta? Buscar(string idTarjeta)
        {
            if (!File.Exists(RUTA)) return null;
            foreach (string linea in File.ReadAllLines(RUTA))
            {
                if (string.IsNullOrWhiteSpace(linea)) continue;
                string[] partes = linea.Split('|');
                if (partes[0] == idTarjeta)
                {
                    Tarjeta t = new Tarjeta(partes[0], partes[1], decimal.Parse(partes[2]));
                    for (int i = 0; i < int.Parse(partes[3]); i++)
                        t.PagarViaje();
                    return t;
                }
            }
            return null;
        }
    }
}