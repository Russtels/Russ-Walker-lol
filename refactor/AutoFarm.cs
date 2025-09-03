using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Hazdryx.Drawing;

namespace refactor
{
    /// <summary>
    /// Gestiona la lógica de auto-farm, buscando y ejecutando el último golpe (last hit) en minions.
    /// </summary>
    public static class AutoFarm
    {
        #region Lógica Principal de Last Hit
        /// <summary>
        /// Método principal que se ejecuta en cada ciclo para decidir si lasthitear o moverse.
        /// </summary>
        public static async Task ExecuteLastHitLogic(GameState gameState)
        {
            // Solo actuar si nuestro autoataque está listo
            if (SpecialFunctions.CanAttack(gameState.AttackSpeed))
            {
                // Buscar un minion que se pueda lasthitear
                Point minionPosition = await FindLastHittableMinion(gameState.AttackRange);

                if (minionPosition != Point.Empty)
                {
                    // Si encontramos un minion, lo atacamos.
                    Point originalMousePosition = Cursor.Position;
                    SpecialFunctions.ClickAt(minionPosition);
                    await Task.Delay(25);
                    SpecialFunctions.SetCursorPos(originalMousePosition.X, originalMousePosition.Y);

                    int windupDelay = SpecialFunctions.GetAttackWindup(gameState.AttackSpeed);
                    SpecialFunctions.AAtick = Environment.TickCount;
                    SpecialFunctions.MoveCT = Environment.TickCount + windupDelay + Values.PingBufferMilliseconds;
                }
                // =================================================================
                // INICIO DE LA CORRECCIÓN: LÓGICA FALTANTE
                // =================================================================
                else
                {
                    // Si NO encontramos un minion, nos movemos hacia el cursor.
                    SpecialFunctions.Click();
                }
                // =================================================================
                // FIN DE LA CORRECCIÓN
                // =================================================================
            }
            // Si no podemos atacar pero sí movernos, seguimos kiteando para mantenernos activos
            else if (SpecialFunctions.CanMove())
            {
                SpecialFunctions.Click();
                SpecialFunctions.MoveCT = Environment.TickCount + new Random().Next(60, 90);
            }
        }
        #endregion

        #region Motor de Búsqueda de Píxeles (Adaptado de ScreenCapture)
        /// <summary>
        /// Escanea el área de rango de ataque en busca de un minion con la firma de "last hit".
        /// </summary>
        private static async Task<Point> FindLastHittableMinion(float attackRange)
        {
            // 1. Define el área de búsqueda basado en el rango de ataque
            const double pixelsPerUnit = 0.8;
            int pixelRadius = (int)(attackRange * pixelsPerUnit) + 50;
            int centerX = Screen.PrimaryScreen!.Bounds.Width / 2;
            int centerY = Screen.PrimaryScreen!.Bounds.Height / 2;
            Rectangle searchArea = new Rectangle(centerX - pixelRadius, centerY - pixelRadius, pixelRadius * 2, pixelRadius * 2);

            // 2. Ejecutar la búsqueda de píxeles en un hilo secundario
            Point foundPatternAt = await Task.Run(() => PixelSearch(searchArea, Values.LastHitPattern));

            // =================================================================
            // INICIO DE LA CORRECCIÓN: APLICAR OFFSET
            // =================================================================
            // Si se encontró un patrón en la barra de vida...
            if (foundPatternAt != Point.Empty)
            {
                // Define el offset para ajustar el clic al cuerpo del minion.
                // Puedes ajustar estos valores para una puntería perfecta.
                int offsetX = 4;
                int offsetY = 10;

                // Aplica el offset a la coordenada encontrada y devuelve el nuevo punto.
                return new Point(foundPatternAt.X + offsetX, foundPatternAt.Y + offsetY);
            }
            // =================================================================
            // FIN DE LA CORRECCIÓN
            // =================================================================

            // Si no se encontró nada, devuelve un punto vacío.
            return Point.Empty;
        }

        /// <summary>
        /// Motor de búsqueda que encuentra la primera aparición de un patrón de píxeles.
        /// Es una réplica de la tecnología de AntiCC y ScreenCapture.
        /// </summary>
        private static Point PixelSearch(Rectangle searchArea, Color[] pattern)
        {
            if (searchArea.Width <= 0 || searchArea.Height <= 0) return Point.Empty;
            Point foundPoint = Point.Empty;
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
                                    int x = i % fastBmp.Width;
                                    int y = i / fastBmp.Width;
                                    foundPoint = new Point(searchArea.X + x, searchArea.Y + y);
                                    loopState.Stop(); // Encontramos uno, paramos de buscar
                                }
                            }
                        });
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine($"Error en PixelSearch de AutoFarm: {ex.Message}"); }
            return foundPoint;
        }
        #endregion
    }
}