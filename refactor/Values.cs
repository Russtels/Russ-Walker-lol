using System.Drawing;
using System.Collections.Generic;

namespace refactor
{
    class Values
    {
        #region Configuración del Orbwalker
        public static bool DrawingsEnabled { get; set; } = true; // Habilitar/Deshabilitar todas las ayudas visuales
        public static bool ShowAttackRange { get; set; } = true; //    Mostrar el rango de ataque
        public static bool AttackChampionOnly { get; set; } = false; // Atacar solo campeones enemigos (need fix)
        public static int SleepOnLowAS { get; set; } = 100;

        public const int PingBufferMilliseconds = 65;// Ajusta este valor según tu ping para mejorar la sincronización
        #endregion

        #region Configuración del Anti-CC & Control de Estado y Tiempo

        public static bool wasStunnedLastTick = false; // Estado del fotograma anterior
        public static int _lastActionTimestamp = 0; // Marca de tiempo de la última acción
        public const int ActionCooldownMs = 150; // Tiempo mínimo entre acciones (ms)
        public static bool ShowAntiCCAreaGuide { get; set; } = false; // Mostrar guía visual del área de detección

        // Coordenadas de Detección
        public static readonly Rectangle StunArea = new Rectangle(1169, 1023, 30, 23);
        public static readonly Rectangle Summoner1Area = new Rectangle(984, 991, 36, 34);
        public static readonly Rectangle Summoner2Area = new Rectangle(1021, 991, 36, 34);
        public static readonly Dictionary<int, Rectangle> ItemSlots = new Dictionary<int, Rectangle>
        {
            { 1, new Rectangle(1070, 990, 30, 30) },
            { 2, new Rectangle(1100, 990, 30, 30) },
            //other slots
            //{ 3, new Rectangle(1134, 990, 30, 30) },
            //{ 5, new Rectangle(1070, 1021, 30, 30) },
            //{ 6, new Rectangle(1100, 1021, 30, 30) },
            //{ 7, new Rectangle(1135, 1021, 30, 30) }
        };

        // Patrones de Píxeles para el Anti-CC
        public static readonly Color[] StunPattern = { Color.FromArgb(101, 29, 29), Color.FromArgb(98, 26, 27) };
        public static readonly Color[] CleansePattern = { Color.FromArgb(77, 194, 162), Color.FromArgb(182, 235, 224) };
        public static readonly Color[] MercurialPattern = { Color.FromArgb(239, 255, 57), Color.FromArgb(189, 210, 47) };
        #endregion

        #region Datos del Juego (Enemigos y Windups)
        // Constantes de píxeles para enemigos
        public static readonly Color EnemyPix = Color.FromArgb(52, 3, 0);
        public static readonly Color EnemyPix1 = Color.FromArgb(53, 3, 0);
        public static readonly Color EnemyPixBS = Color.FromArgb(148, 81, 165);
        public static readonly Color EnemyPixBS1 = Color.FromArgb(82, 40, 90);

        // Windup (se actualiza dinámicamente)
        public static float Windup { get; private set; } = 15.0f;
        private static readonly Dictionary<string, float> ChampionWindups = new Dictionary<string, float>
        {
            {"Akshan", 13.33f}, {"Aphelios", 15.333f}, {"Ashe", 21.93f}, {"Caitlyn", 17.708f},
            {"Corki", 27.00f}, {"Draven", 15.614f}, {"Ezreal", 18.839f}, {"Graves", 0.5f},
            {"Jayce", 9.5f}, {"Jhin", 15.625f}, {"Jinx", 16.875f}, {"Kaisa", 16.108f},
            {"Kalista", 36.00f}, {"Kayle", 19.355f}, {"Kindred", 17.544f}, {"Kogmaw", 16.622f},
            {"Lucian", 15.00f}, {"MissFortune", 14.801f}, {"Quinn", 17.544f},
            {"Samira", 15.00f}, {"Senna", 31.25f}, {"Sivir", 12.00f}, {"Smolder", 16.622f},
            {"Tristana", 14.80f}, {"Twitch", 20.192f}, {"Varus", 17.544f}, {"Vayne", 17.544f},
            {"Xayah", 17.687f}, {"Zeri", 15.625f}
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
        #endregion
    }
}