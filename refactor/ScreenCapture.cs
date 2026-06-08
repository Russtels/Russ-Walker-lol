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
            Rectangle searchRect = Screen.PrimaryScreen!.Bounds;
            List<Point> allEnemies = await Task.Run(() =>
                PixelSearchEnemies(searchRect, Values.EnemyPix, Values.EnemyPix1, Values.EnemyPixBS, Values.EnemyPixBS1));

            if (allEnemies.Count == 0) return Point.Empty;
            return allEnemies.OrderBy(p => SquareDistance(System.Windows.Forms.Cursor.Position, p)).First();
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

        public static List<Point> PixelSearchEnemies(Rectangle rect, Color c0, Color c1, Color bs0, Color bs1)
        {
            int offsetX = 65, offsetY = 95;
            if (Screen.PrimaryScreen!.Bounds.Width != 1920 || Screen.PrimaryScreen!.Bounds.Height != 1080)
            {
                double xr = Screen.PrimaryScreen.Bounds.Width  / 1920.0;
                double yr = Screen.PrimaryScreen.Bounds.Height / 1080.0;
                rect    = new Rectangle((int)(rect.X * xr), (int)(rect.Y * yr), (int)(rect.Width * xr), (int)(rect.Height * yr));
                offsetX = (int)(offsetX * xr);
                offsetY = (int)(offsetY * yr);
            }

            int v0 = c0.ToArgb(),  v1 = c1.ToArgb();
            int vb0 = bs0.ToArgb(), vb1 = bs1.ToArgb();

            var points  = new List<Point>();
            var lockObj = new object();

            using var bmp = new Bitmap(rect.Width, rect.Height, PixelFormat.Format32bppRgb);
            using (var g = Graphics.FromImage(bmp))
                g.CopyFromScreen(rect.X, rect.Y, 0, 0, rect.Size, CopyPixelOperation.SourceCopy);

            using var fast = new FastBitmap(bmp);
            Parallel.For(0, fast.Length - 1, i =>
            {
                int cur  = fast.GetI(i);
                int next = fast.GetI(i + 1);
                if ((cur == v0 && next == v1) || (cur == vb0 && next == vb1))
                {
                    int x = i % fast.Width;
                    int y = i / fast.Width;
                    if (InCircle(x, y, rect))
                        lock (lockObj) { points.Add(new Point(x + rect.X + offsetX, y + rect.Y + offsetY)); }
                }
            });

            return points;
        }

        private static int SquareDistance(Point a, Point b)
        {
            int dx = a.X - b.X, dy = a.Y - b.Y;
            return dx * dx + dy * dy;
        }

        public static bool InCircle(int x, int y, Rectangle rect)
        {
            if (rect.Height == 0) return false;
            double ratio = (double)rect.Width / rect.Height;
            double r  = rect.Height / 2.0;
            double cy = rect.Height / 2.0 - y;
            double cx = (rect.Width  / 2.0 - x) / ratio;
            return cx * cx + cy * cy <= r * r;
        }
    }
}
