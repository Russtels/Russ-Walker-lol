using System.Drawing;

namespace refactor
{
    class Values
    {
        // Configuraciones y Toggles
        public static bool ShowAttackRange { get; set; } = false;
        public static bool AttackChampionOnly { get; set; } = false;
        public static bool DrawingsEnabled { get; set; } = true;
        public static int SleepOnLowAS { get; set; } = 100;

        // Propiedades de Windup

        // Buffer de latencia en ms (ping / LAG)
        public const int PingBufferMilliseconds = 65; // Búfer de 65ms para la latencia
        public static float Windup { get; private set; } = 15.0f; // Windup porcentual

        // Constantes de píxeles
        public static readonly Color EnemyPix = Color.FromArgb(52, 3, 0);
        public static readonly Color EnemyPix1 = Color.FromArgb(53, 3, 0);
        public static readonly Color EnemyPixBS = Color.FromArgb(148, 81, 165);
        public static readonly Color EnemyPixBS1 = Color.FromArgb(82, 40, 90);

        private static readonly Dictionary<string, float> ChampionWindups = new Dictionary<string, float>
        {
            {"Ashe", 21.93f}, {"Caitlyn", 17.708f}, {"Corki", 10.00f}, {"Draven", 15.614f},
            {"Ezreal", 18.839f}, {"Jinx", 16.875f}, {"Kaisa", 16.108f}, {"Kalista", 36.000f},
            {"Kayle", 19.355f}, {"Kindred", 17.544f}, {"Kogmaw", 16.622f}, {"Lucian", 15.00f},
            {"MissFortune", 14.801f}, {"Quinn", 17.544f}, {"Samira", 15.00f}, {"Senna", 31.25f},
            {"Sivir", 12.00f}, {"Tristana", 14.801f}, {"Twitch", 30.192f}, {"Varus", 17.544f},
            {"Vayne", 17.544f}, {"Xayah", 17.687f}
        };
        private const float Other_wu = 15.0f;

        public static void UpdateChampionWindup(string championName)
        {
            if (string.IsNullOrEmpty(championName)) return;

            if (ChampionWindups.TryGetValue(championName, out float specificWindup))
            {
                Windup = specificWindup;
            }
            else
            {
                Windup = Other_wu;
            }
        }
    }
}