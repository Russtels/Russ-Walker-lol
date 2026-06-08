using Newtonsoft.Json.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

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
                // Only trust certs served from localhost (LoL client uses a self-signed cert on 127.0.0.1)
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
                    message.RequestUri?.Host == "127.0.0.1"
            };
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            _httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(2),
                DefaultRequestHeaders = { { "User-Agent", "RussWalker" } }
            };
        }

        public async Task FetchAllGameDataAsync(GameState gameState)
        {
            try
            {
                Task<string> activePlayerTask = _httpClient.GetStringAsync(BaseUrl + "activeplayer");
                Task<string> playerListTask   = _httpClient.GetStringAsync(BaseUrl + "playerlist");
                await Task.WhenAll(activePlayerTask, playerListTask);

                var activePlayerData = JObject.Parse(activePlayerTask.Result);
                var playerListData   = JArray.Parse(playerListTask.Result);

                var championStats = activePlayerData["championStats"];
                gameState.AttackSpeed = championStats?["attackSpeed"]?.Value<float>() ?? 0;
                gameState.AttackRange = championStats?["attackRange"]?.Value<float>() ?? 0;

                if (playerListData.Count > 0)
                {
                    var mainPlayer = playerListData[0];
                    gameState.ChampionName = mainPlayer["championName"]?.ToString() ?? "none";
                    gameState.IsDead       = mainPlayer["isDead"]?.Value<bool>() ?? true;
                }

                gameState.IsApiAvailable = true;

                // Sync to shared state for drawing thread
                GameState.Current.AttackSpeed  = gameState.AttackSpeed;
                GameState.Current.AttackRange  = gameState.AttackRange;
                GameState.Current.IsDead       = gameState.IsDead;
                GameState.Current.ChampionName = gameState.ChampionName;
                GameState.Current.IsApiAvailable = true;
            }
            catch (Exception ex)
            {
                gameState.IsApiAvailable = false;
                GameState.Current.IsApiAvailable = false;
                Logger.Warning($"[API] Unavailable: {ex.Message}");
            }
        }

        public async Task<string> GetRawActivePlayerDataAsync()
        {
            try
            {
                string rawJson = await _httpClient.GetStringAsync(BaseUrl + "activeplayer");
                return JObject.Parse(rawJson).ToString(Newtonsoft.Json.Formatting.Indented);
            }
            catch (Exception ex)
            {
                return $"Could not reach API: {ex.Message}";
            }
        }
    }
}
