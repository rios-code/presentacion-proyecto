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

        public async Task<string> MiBibliotecaAsync()
        {
            string url = "https://api.steampowered.com/IPlayerService/GetOwnedGames/v1/" +
                         $"?key={ApiKey}&steamid={SteamId}&include_appinfo=1" +
                         "&include_played_free_games=1&format=json";
            using JsonDocument doc = await ObtenerJsonAsync(url);

            JsonElement response = doc.RootElement.GetProperty("response");
            if (!response.TryGetProperty("games", out JsonElement juegos))
                return "No se pudo leer la biblioteca. Revisa que el perfil sea publico y que la clave/SteamID sean correctos.";

            int total = response.TryGetProperty("game_count", out JsonElement gc) ? gc.GetInt32() : 0;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Juegos en tu cuenta: {total}\n");
            int n = 1;
            foreach (JsonElement j in juegos.EnumerateArray())
            {
                string nombre = Texto(j, "name", "?");
                int minutos = j.TryGetProperty("playtime_forever", out JsonElement pt) ? pt.GetInt32() : 0;
                sb.AppendLine($"{n,3}. {nombre}  ({minutos / 60} h jugadas)");
                n++;
            }
            return sb.ToString();
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
