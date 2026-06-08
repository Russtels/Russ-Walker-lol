using Hazdryx.Drawing;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Windows.Forms;

namespace refactor
{
    class ScreenCapture
    {
        private const double PixelsPerUnit = 0.8;

        // Returns the screen-space pixel radius for a given in-game attack range.
        public static int RangeToPixelRadius(float attackRange)
            => (int)(attackRange * PixelsPerUnit) + 50;

        /// <summary>
        /// Returns true if the pattern exists anywhere in searchArea.
        /// </summary>
        public static bool ContainsPattern(Rectangle searchArea, Color[] pattern)
            => FindFirstPattern(searchArea, pattern) != Point.Empty;

        /// <summary>
        /// Finds the first occurrence of a horizontal pixel pattern in searchArea.
        /// Returns Point.Empty if not found.
        /// </summary>
        public static Point FindFirstPattern(Rectangle searchArea, Color[] pattern)
        {
            Point foundPoint = Point.Empty;
            if (searchArea.Width <= 0 || searchArea.Height <= 0) return foundPoint;

            try
            {
                using var bmp = new Bitmap(searchArea.Width, searchArea.Height, PixelFormat.Format32bppRgb);
                using (var g = Graphics.FromImage(bmp))
                    g.CopyFromScreen(searchArea.Location, Point.Empty, searchArea.Size, CopyPixelOperation.SourceCopy);

                using var fastBmp = new FastBitmap(bmp);
                int[] patternArgb = pattern.Select(c => c.ToArgb() & 0x00FFFFFF).ToArray();
                int patternLength = patternArgb.Length;

                Parallel.For(0, fastBmp.Length - patternLength, (i, state) =>
                {
                    if (foundPoint != Point.Empty) { state.Stop(); return; }
                    if ((fastBmp.GetI(i) & 0x00FFFFFF) != patternArgb[0]) return;

                    for (int j = 1; j < patternLength; j++)
                    {
                        if ((fastBmp.GetI(i + j) & 0x00FFFFFF) != patternArgb[j]) return;
                    }

                    int x = i % fastBmp.Width;
                    int y = i / fastBmp.Width;
                    foundPoint = new Point(searchArea.X + x, searchArea.Y + y);
                    state.Stop();
                });
            }
            catch (Exception ex) { Logger.Error($"[ScreenCapture] FindFirstPattern: {ex.Message}"); }

            return foundPoint;
        }

        public static async Task<Point> GetEnemyPosition(float attackRange)
        {
            if (attackRange <= 0) return Point.Empty;
            Rectangle rect = BuildSearchRect(attackRange);
            return await Task.Run(() => FindClosestEnemy(rect));
        }

        public static async Task<Point> GetEnemyPositionClosestToCursor(float maxRange)
        {
            if (maxRange <= 0) return Point.Empty;
            Rectangle searchRect = new Rectangle(0, 0, Screen.PrimaryScreen!.Bounds.Width, Screen.PrimaryScreen!.Bounds.Height);
            List<Point> allEnemies = await Task.Run(() =>
                PixelSearchEnemies(searchRect, Values.EnemyPix, Values.EnemyPix1, Values.EnemyPixBS, Values.EnemyPixBS1));
            if (allEnemies.Count == 0) return Point.Empty;
            return allEnemies.OrderBy(p => SquareDistance(Cursor.Position, p)).First();
        }

        public static bool IsAbilityReady(Rectangle abilityArea, Color[] readyPattern)
            => ContainsPattern(abilityArea, readyPattern);

        private static Rectangle BuildSearchRect(float attackRange)
        {
            int pixelRadius = RangeToPixelRadius(attackRange);
            int cx = Screen.PrimaryScreen!.Bounds.Width  / 2;
            int cy = Screen.PrimaryScreen!.Bounds.Height / 2;
            return new Rectangle(cx - pixelRadius, cy - pixelRadius, pixelRadius * 2, pixelRadius * 2);
        }

        private static Point FindClosestEnemy(Rectangle rect)
        {
            if (rect.IsEmpty) return Point.Empty;
            Point playerPos = new Point(Screen.PrimaryScreen!.Bounds.Width / 2, Screen.PrimaryScreen!.Bounds.Height / 2);
            List<Point> points = PixelSearchEnemies(rect, Values.EnemyPix, Values.EnemyPix1, Values.EnemyPixBS, Values.EnemyPixBS1);
            if (points.Count == 0) return Point.Empty;
            return points.OrderBy(p => SquareDistance(playerPos, p)).First();
        }

        public static List<Point> PixelSearchEnemies(Rectangle rect, Color PixelColor, Color PixelColor1, Color PixelColorBS, Color PixelColorBS1)
        {
            int offsetX = 65, offsetY = 95;
            if (Screen.PrimaryScreen!.Bounds.Width != 1920 || Screen.PrimaryScreen!.Bounds.Height != 1080)
            {
                double XRatio = (double)Screen.PrimaryScreen.Bounds.Width / 1920.0;
                double YRatio = (double)Screen.PrimaryScreen.Bounds.Height / 1080.0;
                rect.X = (int)(rect.X * XRatio); rect.Y = (int)(rect.Y * YRatio);
                rect.Width = (int)(rect.Width * XRatio); rect.Height = (int)(rect.Height * YRatio);
                offsetX = (int)(offsetX * XRatio); offsetY = (int)(offsetY * YRatio);
            }
            int searchvalueNormal  = PixelColor.ToArgb();
            int searchvalueNormal1 = PixelColor1.ToArgb();
            int searchvalueBS      = PixelColorBS.ToArgb();
            int searchvalueBS1     = PixelColorBS1.ToArgb();
            List<Point> Points = new List<Point>();
            object lockObj = new object();
            // NOTE: BMP is intentionally not in a using block — FastBitmap holds a reference
            // to the locked bitmap data and must be disposed before BMP is freed.
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
                    int nextPixel    = bitmap.GetI(i + 1);
                    if ((currentPixel == searchvalueNormal && nextPixel == searchvalueNormal1) ||
                        (currentPixel == searchvalueBS     && nextPixel == searchvalueBS1))
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
                lock (lockObj) { Points.Add(new Point(x + rect.X + offsetX, y + rect.Y + offsetY)); }
        }

        private static int SquareDistance(Point a, Point b)
        {
            int dx = a.X - b.X, dy = a.Y - b.Y;
            return dx * dx + dy * dy;
        }

        public static bool InCircle(int X, int Y, Rectangle rect)
        {
            if (rect.Height == 0) return false;
            double ratio = (double)rect.Width / rect.Height;
            double r = rect.Height / 2.0;
            double y = rect.Height / 2.0 - Y;
            double x = (rect.Width  / 2.0 - X) / ratio;
            return x * x + y * y <= r * r;
        }
    }
}
