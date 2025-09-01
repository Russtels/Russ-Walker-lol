#region Imports de Sistema
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Threading.Tasks;
using Hazdryx.Drawing;
#endregion

namespace refactor
{
    public class AntiCC
    {


        #region Lógica Principal (Bucles Lento y Rápido)
        /// <summary>
        /// BUCLE LENTO: Comprueba si Cleanse o Cimitarra están disponibles.
        /// Se ejecuta en un hilo secundario para eficiencia.
        /// </summary>
        public static void UpdateAvailability(GameState gameState)
        {
            // Comprueba si Cleanse está listo en alguno de los dos slots
            gameState.IsCleanseReady = PixelSearch(Values.Summoner1Area, Values.CleansePattern) || PixelSearch(Values.Summoner2Area, Values.CleansePattern);

            // Comprueba si la Cimitarra está lista en alguno de los slots de objetos
            gameState.IsMercurialReady = false;
            foreach (var slot in Values.ItemSlots)
            {
                if (PixelSearch(slot.Value, Values.MercurialPattern))
                {
                    gameState.IsMercurialReady = true;
                    break;
                }
            }

            // Actualiza el estado para que el overlay lo pueda leer
            GameState.Current.IsCleanseReady = gameState.IsCleanseReady;
            GameState.Current.IsMercurialReady = gameState.IsMercurialReady;
        }

        /// <summary>
        /// BUCLE RÁPIDO: Comprueba si el jugador está stunneado y reacciona al instante.
        /// Se ejecuta en el hilo principal para mínima latencia.
        /// </summary>
        /// <returns>Devuelve TRUE si se realizó una acción (ej. usar Cleanse).</returns>
        public static bool CheckForStunAndReact(GameState gameState)
        {
            bool isCurrentlyStunned = PixelSearch(Values.StunArea, Values.StunPattern);
            gameState.IsStunned = isCurrentlyStunned;
            GameState.Current.IsStunned = isCurrentlyStunned;

            // Reacciona solo en el fotograma exacto en que el estado cambia de NO stun a SÍ stun
            if (isCurrentlyStunned && !Values.wasStunnedLastTick)
            {
                bool canAct = Environment.TickCount > Values._lastActionTimestamp + Values.ActionCooldownMs;
                if (canAct)
                {
                    // Prioridad 1: Cimitarra
                    if (gameState.IsMercurialReady)
                    {
                        // Vuelve a buscar la ranura exacta del objeto antes de usarlo
                        foreach (var slot in Values.ItemSlots)
                        {
                            if (PixelSearch(slot.Value, Values.MercurialPattern))
                            {
                                Console.WriteLine($"[ANTI-CC] REACCIÓN INSTANTÁNEA. USANDO CIMITARRA (Slot {slot.Key})...");
                                InputSimulator.PressItemKey(slot.Key);
                                Values._lastActionTimestamp = Environment.TickCount;
                                Values.wasStunnedLastTick = isCurrentlyStunned;
                                return true; // Acción realizada
                            }
                        }
                    }
                    // Prioridad 2: Cleanse
                    else if (gameState.IsCleanseReady)
                    {
                        Console.WriteLine("[ANTI-CC] REACCIÓN INSTANTÁNEA. USANDO CLEANSE...");
                        InputSimulator.SendKeyDown(InputSimulator.ScanCodeShort.KEY_D);
                        InputSimulator.SendKeyUp(InputSimulator.ScanCodeShort.KEY_D);
                        Values._lastActionTimestamp = Environment.TickCount;
                        Values.wasStunnedLastTick = isCurrentlyStunned;
                        return true; // Acción realizada
                    }
                }
            }

            Values.wasStunnedLastTick = isCurrentlyStunned;
            return false; // No se realizó ninguna acción
        }
        #endregion

        #region Motor de Búsqueda de Píxeles
        /// <summary>
        /// Motor de búsqueda de patrones de píxeles, basado en la lógica de ScreenCapture.cs.
        /// </summary>
        /// <returns>Devuelve TRUE si encuentra el patrón en el área especificada.</returns>
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
        #endregion
    }
}