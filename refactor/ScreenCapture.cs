using Hazdryx.Drawing;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Windows.Forms;

namespace refactor
{
    class ScreenCapture
    {
        // =================================================================
        // INICIO: NUEVO MÉTODO GENÉRICO Y REUTILIZABLE
        // =================================================================
        /// <summary>
        /// Busca la primera aparición de un patrón de colores horizontal en un área de la pantalla.
        /// </summary>
        public static Point FindFirstPattern(Rectangle searchArea, Color[] pattern)
        {
            Point foundPoint = Point.Empty;
            if (searchArea.Width <= 0 || searchArea.Height <= 0) return foundPoint;

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
                        int[] patternArgb = pattern.Select(c => c.ToArgb() & 0x00FFFFFF).ToArray();
                        int patternLength = pattern.Length;

                        Parallel.For(0, fastBmp.Length - patternLength, (i, loopState) =>
                        {
                            if (foundPoint != Point.Empty) loopState.Stop();

                            if ((fastBmp.GetI(i) & 0x00FFFFFF) == patternArgb[0])
                            {
                                bool match = true;
                                for (int j = 1; j < patternLength; j++)
                                {
                                    if ((fastBmp.GetI(i + j) & 0x00FFFFFF) != patternArgb[j])
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
                                    loopState.Stop();
                                }
                            }
                        });
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine($"Error en FindFirstPattern: {ex.Message}"); }

            return foundPoint;
        }
        // =================================================================
        // FIN: NUEVO MÉTODO GENÉRICO
        // =================================================================


        // --- El resto del código para la detección de enemigos no cambia ---

        public static async Task<Point> GetEnemyPosition(float attackRange)
        {
            if (attackRange <= 0) return Point.Empty;
            Rectangle rect = CalculateRectangle(attackRange);
            return await Task.Run(() => FindClosestEnemy(rect));
        }

        private static Rectangle CalculateRectangle(double attackRange)
        {
            const double pixelsPerUnit = 0.8;
            int pixelRadius = (int)(attackRange * pixelsPerUnit) + 50;
            int centerX = Screen.PrimaryScreen!.Bounds.Width / 2;
            int centerY = Screen.PrimaryScreen!.Bounds.Height / 2;
            return new Rectangle(centerX - pixelRadius, centerY - pixelRadius, pixelRadius * 2, pixelRadius * 2);
        }

        private static Point FindClosestEnemy(Rectangle rect)
        {
            if (rect.IsEmpty) return Point.Empty;

            Point playerPos = new Point(Screen.PrimaryScreen!.Bounds.Width / 2, Screen.PrimaryScreen!.Bounds.Height / 2);
            List<Point> points = PixelSearchEnemies(rect, Values.EnemyPix, Values.EnemyPix1, Values.EnemyPixBS, Values.EnemyPixBS1);

            if (points.Count > 0)
            {
                Point closestEnemy = points.OrderBy(p => SquareDistance(playerPos, p)).First();
                return closestEnemy;
            }

            return Point.Empty;
        }

        private static int SquareDistance(Point p1, Point p2)
        {
            int dx = p1.X - p2.X;
            int dy = p1.Y - p2.Y;
            return dx * dx + dy * dy;
        }

        public static List<Point> PixelSearchEnemies(Rectangle rect, Color PixelColor, Color PixelColor1, Color PixelColorBS, Color PixelColorBS1)
        {
            // ... esta función permanece sin cambios, ya que es específica para enemigos ...
            int offsetX = 65, offsetY = 95;
            if (Screen.PrimaryScreen!.Bounds.Width != 1920 || Screen.PrimaryScreen!.Bounds.Height != 1080)
            {
                double XRatio = (double)Screen.PrimaryScreen.Bounds.Width / 1920.0;
                double YRatio = (double)Screen.PrimaryScreen.Bounds.Height / 1080.0;
                rect.X = (int)(rect.X * XRatio); rect.Y = (int)(rect.Y * YRatio);
                rect.Width = (int)(rect.Width * XRatio); rect.Height = (int)(rect.Height * YRatio);
                offsetX = (int)(offsetX * XRatio); offsetY = (int)(offsetY * YRatio);
            }
            int searchvalueNormal = PixelColor.ToArgb();
            int searchvalueNormal1 = PixelColor1.ToArgb();
            int searchvalueBS = PixelColorBS.ToArgb();
            int searchvalueBS1 = PixelColorBS1.ToArgb();
            List<Point> Points = new List<Point>();
            object lockObj = new object();
            Bitmap BMP = new Bitmap(rect.Width, rect.Height, PixelFormat.Format32bppRgb);
            using (Graphics GFX = Graphics.FromImage(BMP))
            {
                GFX.CopyFromScreen(rect.X, rect.Y, 0, 0, rect.Size, CopyPixelOperation.SourceCopy);
            }
            using (FastBitmap bitmap = new FastBitmap(BMP))
            {
                Parallel.For(0, bitmap.Length - 1, i =>
                {
                    int currentPixel = bitmap.GetI(i);
                    int nextPixel = bitmap.GetI(i + 1);
                    if ((currentPixel == searchvalueNormal && nextPixel == searchvalueNormal1) ||
                        (currentPixel == searchvalueBS && nextPixel == searchvalueBS1))
                    {
                        AddPointToList(i, bitmap, rect, offsetX, offsetY, lockObj, Points);
                    }
                });
            }
            return Points;
        }

        private static void AddPointToList(int index, FastBitmap bitmap, Rectangle rect, int offsetX, int offsetY, object lockObj, List<Point> Points)
        {
            int x = index % bitmap.Width;
            int y = index / bitmap.Width;
            if (InCircle(x, y, rect))
            {
                lock (lockObj)
                {
                    Points.Add(new Point(x + rect.X + offsetX, y + rect.Y + offsetY));
                }
            }
        }

        public static bool InCircle(int X, int Y, Rectangle rect)
        {
            if (rect.Height == 0) return false;
            double ratio = (double)rect.Width / rect.Height;
            double r = rect.Height / 2.0;
            double y = rect.Height / 2.0 - Y;
            double x = (rect.Width / 2.0 - X) / ratio;
            return x * x + y * y <= r * r;
        }
    }
}