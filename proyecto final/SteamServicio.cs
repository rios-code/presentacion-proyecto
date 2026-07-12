using System;
using System.IO;
using System.Text;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Steam
{
    // Cliente de la API OFICIAL de Steam (Valve). Solo LEE informacion:
    //  - Publico  : detalles y precio de cualquier juego de la tienda (no requiere clave).
    //  - Personal : tu biblioteca y tu perfil (requiere tu API key gratuita + tu SteamID64).
    // Nunca activa, compra ni genera juegos: eso no es posible por esta via.
    internal class SteamServicio
    {
        // key|steamid  -> archivo local, excluido del repositorio.
        private const string RUTA_CONFIG = "steam_config.txt";

        private static readonly HttpClient _http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(20)
        };

        public string? ApiKey { get; private set; }
        public string? SteamId { get; private set; }

        public bool TieneCredenciales =>
            !string.IsNullOrWhiteSpace(ApiKey) && !string.IsNullOrWhiteSpace(SteamId);

        public SteamServicio()
        {
            CargarConfig();
        }

        // ---------- Configuracion (credenciales personales) ----------

        private void CargarConfig()
        {
            if (!File.Exists(RUTA_CONFIG)) return;
            string[] p = File.ReadAllText(RUTA_CONFIG).Trim().Split('|');
            if (p.Length == 2)
            {
                ApiKey = p[0];
                SteamId = p[1];
            }
        }

        public void GuardarConfig(string apiKey, string steamId)
        {
            ApiKey = apiKey.Trim();
            SteamId = steamId.Trim();
            File.WriteAllText(RUTA_CONFIG, $"{ApiKey}|{SteamId}");
        }

        // ---------- Publico: info y precio de un juego (sin clave) ----------

        public async Task<string> InfoJuegoAsync(string appId)
        {
            string url = $"https://store.steampowered.com/api/appdetails?appids={appId}&cc=ar&l=spanish";
            using JsonDocument doc = await ObtenerJsonAsync(url);

            if (!doc.RootElement.TryGetProperty(appId, out JsonElement entrada) ||
                !entrada.TryGetProperty("success", out JsonElement ok) || !ok.GetBoolean())
                return "No se encontro un juego con ese AppID.";

            JsonElement data = entrada.GetProperty("data");
            string nombre = Texto(data, "name", "(sin nombre)");
            string tipo = Texto(data, "type", "");

            string precio;
            if (data.TryGetProperty("is_free", out JsonElement free) && free.GetBoolean())
                precio = "Gratis";
            else if (data.TryGetProperty("price_overview", out JsonElement po))
                precio = Texto(po, "final_formatted", "(sin precio)");
            else
                precio = "(precio no disponible)";

            string desc = Texto(data, "short_description", "");

            return $"Juego  : {nombre}\n" +
                   $"Tipo   : {tipo}\n" +
                   $"Precio : {precio}\n" +
                   $"Info   : {desc}";
        }

        // ---------- Personal: biblioteca de juegos (requiere credenciales) ----------

        // Descarga la biblioteca como lista de objetos (reutilizado por ver y guardar).
        public async Task<List<JuegoSteam>> ObtenerBibliotecaAsync()
        {
            string url = "https://api.steampowered.com/IPlayerService/GetOwnedGames/v1/" +
                         $"?key={ApiKey}&steamid={SteamId}&include_appinfo=1" +
                         "&include_played_free_games=1&format=json";
            using JsonDocument doc = await ObtenerJsonAsync(url);

            List<JuegoSteam> lista = new List<JuegoSteam>();
            JsonElement response = doc.RootElement.GetProperty("response");
            if (!response.TryGetProperty("games", out JsonElement juegos))
                return lista; // vacia: perfil privado, sin juegos o credenciales incorrectas

            foreach (JsonElement j in juegos.EnumerateArray())
            {
                int appId = j.TryGetProperty("appid", out JsonElement a) ? a.GetInt32() : 0;
                string nombre = Texto(j, "name", "?");
                int minutos = j.TryGetProperty("playtime_forever", out JsonElement pt) ? pt.GetInt32() : 0;
                lista.Add(new JuegoSteam(appId, nombre, minutos));
            }
            return lista;
        }

        public async Task<string> MiBibliotecaAsync()
        {
            List<JuegoSteam> lista = await ObtenerBibliotecaAsync();
            if (lista.Count == 0)
                return "No se pudo leer la biblioteca. Revisa que el perfil sea publico y que la clave/SteamID sean correctos.";

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Juegos en tu cuenta: {lista.Count}\n");
            int n = 1;
            int totalHoras = 0;
            foreach (JuegoSteam j in lista)
            {
                sb.AppendLine($"{n,3}. {j.Nombre}  ({j.HorasJugadas} h jugadas)");
                totalHoras += j.HorasJugadas;
                n++;
            }
            sb.AppendLine($"\nTotal jugado: {totalHoras} horas.");
            return sb.ToString();
        }

        // Guarda la biblioteca en un archivo CSV (se abre en Excel/LibreOffice).
        // Devuelve la cantidad de juegos guardados.
        public async Task<int> GuardarBibliotecaAsync(string ruta)
        {
            List<JuegoSteam> lista = await ObtenerBibliotecaAsync();

            List<string> lineas = new List<string> { "AppID,Nombre,HorasJugadas" };
            foreach (JuegoSteam j in lista)
            {
                // Comillas dobles para nombres con comas; se escapan segun formato CSV.
                string nombre = "\"" + j.Nombre.Replace("\"", "\"\"") + "\"";
                lineas.Add($"{j.AppId},{nombre},{j.HorasJugadas}");
            }

            File.WriteAllLines(ruta, lineas);
            return lista.Count;
        }

        // ---------- Personal: perfil (requiere credenciales) ----------

        public async Task<string> MiPerfilAsync()
        {
            string url = "https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v2/" +
                         $"?key={ApiKey}&steamids={SteamId}";
            using JsonDocument doc = await ObtenerJsonAsync(url);

            JsonElement players = doc.RootElement.GetProperty("response").GetProperty("players");
            if (players.GetArrayLength() == 0)
                return "No se encontro el perfil. Revisa el SteamID o la clave.";

            JsonElement p = players[0];
            string nombre = Texto(p, "personaname", "?");
            int estadoNum = p.TryGetProperty("personastate", out JsonElement ps) ? ps.GetInt32() : 0;
            string estado = estadoNum switch
            {
                1 => "Conectado",
                2 => "Ocupado",
                3 => "Ausente",
                4 => "Descansando",
                _ => "Desconectado"
            };
            string perfilUrl = Texto(p, "profileurl", "");

            return $"Perfil : {nombre}\n" +
                   $"Estado : {estado}\n" +
                   $"URL    : {perfilUrl}";
        }

        // ---------- Auxiliares ----------

        private static async Task<JsonDocument> ObtenerJsonAsync(string url)
        {
            HttpResponseMessage resp = await _http.GetAsync(url);
            resp.EnsureSuccessStatusCode();
            string cuerpo = await resp.Content.ReadAsStringAsync();
            return JsonDocument.Parse(cuerpo);
        }

        private static string Texto(JsonElement obj, string propiedad, string porDefecto)
        {
            return obj.TryGetProperty(propiedad, out JsonElement v) && v.ValueKind == JsonValueKind.String
                ? v.GetString() ?? porDefecto
                : porDefecto;
        }
    }
}
