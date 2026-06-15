using System.Drawing;
using System.Collections.Generic;

namespace refactor
{
    class Values
    {
        #region Debug
        public static bool DebugEnabled = false;
        public static bool apiLogDebugEnable = false;
        public static bool DebugDrawEnabled = false; // F2: draws all pixel detection areas on screen
        #endregion

        #region Orbwalker
        public static bool DrawingsEnabled { get; set; } = true;
        public static bool ShowAttackRange { get; set; } = true;
        public static bool AttackChampionOnly { get; set; } = false;
        public const int PingBufferMilliseconds = 65;
        #endregion

        #region Auto Farm
        public static bool AutoFarmEnabled { get; set; } = true;
        #endregion

        #region Anti-CC
        // volatile so reads/writes are visible across threads without caching
        public static volatile bool wasStunnedLastTick = false;
        public static volatile int _lastActionTimestamp = 0;
        public const int ActionCooldownMs = 150;

        public static readonly Rectangle StunArea      = new Rectangle(1169, 1023, 30, 23);
        public static readonly Rectangle Summoner1Area = new Rectangle(984,  991,  36, 34);
        public static readonly Rectangle Summoner2Area = new Rectangle(1021, 991,  36, 34);
        public static readonly Dictionary<int, Rectangle> ItemSlots = new Dictionary<int, Rectangle>
        {
            { 1, new Rectangle(1070, 990, 30, 30) },
            { 2, new Rectangle(1100, 990, 30, 30) },
        };

        public static readonly Color[] StunPattern     = { Color.FromArgb(191, 43, 42), Color.FromArgb(188, 40, 40) };
        public static readonly Color[] CleansePattern  = { Color.FromArgb(77,  194, 162), Color.FromArgb(182, 235, 224) };
        public static readonly Color[] MercurialPattern = { Color.FromArgb(239, 255, 57), Color.FromArgb(189, 210, 47) };
        public static readonly Color[] LastHitPattern  = { Color.FromArgb(219, 226, 231), Color.FromArgb(219, 226, 231) };
        #endregion

        #region Enemy Pixel Colors
        public static readonly Color EnemyPix      = Color.FromArgb(52,  3,   0);
        public static readonly Color EnemyPix1     = Color.FromArgb(53,  3,   0);
        public static readonly Color EnemyPixBS    = Color.FromArgb(148, 81,  165);
        public static readonly Color EnemyPixBS1   = Color.FromArgb(82,  40,  90);
        #endregion

        #region Champion Windups
        public static float Windup { get; private set; } = 15.0f;

        private static readonly Dictionary<string, float> ChampionWindups = new Dictionary<string, float>
        { 
            {"Akshan", 13.33f},   {"Aphelios", 15.333f}, {"Ashe", 21.93f},      {"Caitlyn", 17.708f},
            {"Corki", 27.00f},    {"Draven", 15.614f},   {"Ezreal", 18.839f},   {"Graves", 0.5f},
            {"Jayce", 9.5f},      {"Jhin", 15.625f},     {"Jinx", 16.875f},     {"Kaisa", 16.108f},
            {"Kalista", 36.00f},  {"Kayle", 19.355f},    {"Kindred", 17.544f},  {"Kogmaw", 16.622f},
            {"Lucian", 15.00f},   {"MissFortune", 14.801f}, {"Quinn", 17.544f},
            {"Samira", 15.00f},   {"Senna", 31.25f},     {"Sivir", 12.00f},     {"Smolder", 16.622f},
            {"Tristana", 14.80f}, {"Twitch", 20.192f},   {"Varus", 17.544f},    {"Vayne", 17.544f},
            {"Xayah", 17.687f},   {"Zeri", 15.625f},
        };
        private const float DefaultWindup = 15.0f;

        public static void UpdateChampionWindup(string championName)
        {
            if (string.IsNullOrEmpty(championName)) return;
            Windup = ChampionWindups.TryGetValue(championName, out float w) ? w : DefaultWindup;
        }
        #endregion
    }
}
