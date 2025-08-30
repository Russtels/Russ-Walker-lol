using System.Drawing;
using System.Drawing.Imaging;
using Hazdryx.Drawing;

namespace refactor
{
    public class AntiCC
    {
        // === ÁREAS DE DETECCIÓN (CON SLOT 3 CORREGIDO) ===
        private static readonly Rectangle StunArea = new Rectangle(1169, 1023, 30, 23);
        private static readonly Rectangle Summoner1Area = new Rectangle(984, 991, 36, 34);
        private static readonly Rectangle Summoner2Area = new Rectangle(1021, 991, 36, 34);
        internal static readonly Dictionary<int, Rectangle> ItemSlots = new Dictionary<int, Rectangle>
        {
            { 1, new Rectangle(1070, 990, 30, 30) },
            { 2, new Rectangle(1100, 990, 30, 30) },
            { 3, new Rectangle(1134, 990, 30, 30) }, // <-- COORDENADA CORREGIDA
            { 5, new Rectangle(1070, 1021, 30, 30) },
            { 6, new Rectangle(1100, 1021, 30, 30) },
            { 7, new Rectangle(1135, 1021, 30, 30) }
        };

        // === PATRONES DE PÍXELES ===
        private static readonly Color[] StunPattern = { Values.StunCheckPix1};
        private static readonly Color[] CleansePattern = { Values.CleanseCheckPix1};
        private static readonly Color[] MercurialPattern = { Values.MercurialCheckPix1};

        // === CONTROL DE ESTADO Y TIEMPO ===
        private static bool wasStunnedLastTick = false;
        private static int _lastActionTimestamp = 0;
        private const int ActionCooldownMs = 2000;

        // BUCLE LENTO: Se ejecuta en un hilo secundario para comprobar la disponibilidad
        public static void UpdateAvailability(GameState gameState)
        {
            gameState.IsCleanseReady = PixelSearch(Summoner1Area, CleansePattern) || PixelSearch(Summoner2Area, CleansePattern);

            gameState.IsMercurialReady = false;
            int mercurialSlot = -1;
            foreach (var slot in ItemSlots)
            {
                if (PixelSearch(slot.Value, MercurialPattern))
                {
                    gameState.IsMercurialReady = true;
                    mercurialSlot = slot.Key;
                    break;
                }
            }

            // Actualiza el overlay
            GameState.Current.IsCleanseReady = gameState.IsCleanseReady;
            GameState.Current.IsMercurialReady = gameState.IsMercurialReady;
        }

        // BUCLE RÁPIDO: Se ejecuta en el hilo principal para una reacción instantánea
        public static void CheckForStunAndReact(GameState gameState)
        {
            bool isCurrentlyStunned = PixelSearch(StunArea, StunPattern);
            gameState.IsStunned = isCurrentlyStunned;
            GameState.Current.IsStunned = isCurrentlyStunned;

            // La condición clave: reacciona solo en el fotograma exacto en que el estado cambia de NO stun a SÍ stun
            if (isCurrentlyStunned && !wasStunnedLastTick)
            {
                bool canAct = Environment.TickCount > _lastActionTimestamp + ActionCooldownMs;
                if (canAct)
                {
                    // Usa la información de disponibilidad que el otro hilo ya preparó
                    if (gameState.IsMercurialReady)
                    {
                        // La ranura del objeto no está disponible en este hilo rápido, así que necesitamos encontrarla de nuevo
                        // Esto es un pequeño coste aceptable por la reacción instantánea.
                        foreach (var slot in ItemSlots)
                        {
                            if (PixelSearch(slot.Value, MercurialPattern))
                            {
                                Console.WriteLine($"[ANTI-CC] REACCIÓN INSTANTÁNEA. USANDO CIMITARRA (Slot {slot.Key})...");
                                InputSimulator.PressItemKey(slot.Key);
                                _lastActionTimestamp = Environment.TickCount;
                                break;
                            }
                        }
                    }
                    else if (gameState.IsCleanseReady)
                    {
                        Console.WriteLine("[ANTI-CC] REACCIÓN INSTANTÁNEA. USANDO CLEANSE...");
                        InputSimulator.SendKeyDown(InputSimulator.ScanCodeShort.KEY_D);
                        InputSimulator.SendKeyUp(InputSimulator.ScanCodeShort.KEY_D);
                        _lastActionTimestamp = Environment.TickCount;
                    }
                }
            }

            wasStunnedLastTick = isCurrentlyStunned;
        }

        private static bool PixelSearch(Rectangle searchArea, Color[] pattern)
        {
            if (searchArea.Width <= 0 || searchArea.Height <= 0) return false;
            bool found = false;
            try
            {
                using (Bitmap bmp = new Bitmap(searchArea.Width, searchArea.Height, PixelFormat.Format32bppRgb))
                {
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        g.CopyFromScreen(searchArea.Location, Point.Empty, searchArea.Size, CopyPixelOperation.SourceCopy);
                    }
                    using (FastBitmap fastBmp = new FastBitmap(bmp))
                    {
                        int[] patternArgb = pattern.Select(c => c.ToArgb()).ToArray();
                        int patternLength = pattern.Length;
                        Parallel.For(0, fastBmp.Length - patternLength, (i, loopState) =>
                        {
                            if (fastBmp.GetI(i) == patternArgb[0])
                            {
                                bool match = true;
                                for (int j = 1; j < patternLength; j++)
                                {
                                    if (fastBmp.GetI(i + j) != patternArgb[j])
                                    {
                                        match = false;
                                        break;
                                    }
                                }
                                if (match)
                                {
                                    found = true;
                                    loopState.Stop();
                                }
                            }
                        });
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine($"Error en PixelSearch de AntiCC: {ex.Message}"); }
            return found;
        }
    }
}