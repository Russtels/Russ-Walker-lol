using Newtonsoft.Json.Linq;
using System.Net;

namespace refactor
{
    public class ApiClient
    {
        private static readonly HttpClient _httpClient;
        private const string BaseUrl = "https://127.0.0.1:2999/liveclientdata/";

        static ApiClient()
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            _httpClient = new HttpClient(handler)
            {
                DefaultRequestHeaders = { { "User-Agent", "MagicOrbwalker" } }
            };
        }

        public async Task FetchAllGameDataAsync(GameState gameState)
        {
            try
            {
                // Realizar ambas llamadas en paralelo para mayor eficiencia
                Task<string> activePlayerTask = _httpClient.GetStringAsync(BaseUrl + "activeplayer");
                Task<string> playerListTask = _httpClient.GetStringAsync(BaseUrl + "playerlist");

                await Task.WhenAll(activePlayerTask, playerListTask);

                var activePlayerData = JObject.Parse(await activePlayerTask);
                var playerListData = JArray.Parse(await playerListTask);

                // Actualizar el objeto GameState directamente
                var championStats = activePlayerData["championStats"];
                gameState.AttackSpeed = championStats?["attackSpeed"]?.Value<float>() ?? 0;
                gameState.AttackRange = championStats?["attackRange"]?.Value<float>() ?? 0;

                if (playerListData.Count > 0)
                {
                    var mainPlayer = playerListData[0];
                    gameState.ChampionName = mainPlayer["championName"]?.ToString() ?? "none";
                    gameState.IsDead = mainPlayer["isDead"]?.Value<bool>() ?? true;
                }

                gameState.IsApiAvailable = true;

                // Actualizar la instancia estática para el overlay
                GameState.Current.AttackSpeed = gameState.AttackSpeed;
                GameState.Current.AttackRange = gameState.AttackRange;
                GameState.Current.IsDead = gameState.IsDead;
            }
            catch (Exception)
            {
                // Si falla la API, establecer estado no disponible
                gameState.IsApiAvailable = false;
            }
        }

        /// <summary>
        /// Obtiene la respuesta JSON sin procesar del endpoint /activeplayer.
        /// </summary>
        /// <returns>Un string con el JSON formateado o un mensaje de error.</returns>
        public async Task<string> GetRawActivePlayerDataAsync()
        {
            try
            {
                string rawJson = await _httpClient.GetStringAsync(BaseUrl + "activeplayer");
                // Parsea y vuelve a formatear el JSON para que se vea bien indentado en la consola
                return JObject.Parse(rawJson).ToString(Newtonsoft.Json.Formatting.Indented);
            }
            catch (Exception ex)
            {
                return $"No se pudo obtener datos de la API: {ex.Message}";
            }
        }
    }
}