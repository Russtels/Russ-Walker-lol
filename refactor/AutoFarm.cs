using System.Drawing;
using System.Windows.Forms;

namespace refactor
{
    public static class AutoFarm
    {
        // +4px X / +10px Y offset to click the minion body rather than its health bar pixel
        private const int OffsetX = 4;
        private const int OffsetY = 10;

        public static async Task ExecuteLastHitLogic(GameState gameState)
        {
            if (SpecialFunctions.CanAttack(gameState.AttackSpeed))
            {
                Point minionPos = await FindLastHittableMinion(gameState.AttackRange);
                if (minionPos != Point.Empty)
                {
                    Point originalMouse = Cursor.Position;
                    SpecialFunctions.ClickAt(minionPos);
                    await Task.Delay(25);
                    SpecialFunctions.SetCursorPos(originalMouse.X, originalMouse.Y);

                    int windupDelay = SpecialFunctions.GetAttackWindup(gameState.AttackSpeed);
                    SpecialFunctions.AAtick = Environment.TickCount;
                    SpecialFunctions.MoveCT = Environment.TickCount + windupDelay + Values.PingBufferMilliseconds;
                }
                else
                {
                    SpecialFunctions.Click();
                }
            }
            else if (SpecialFunctions.CanMove())
            {
                SpecialFunctions.Click();
                SpecialFunctions.MoveCT = Environment.TickCount + new Random().Next(60, 90);
            }
        }

        private static async Task<Point> FindLastHittableMinion(float attackRange)
        {
            int pixelRadius = ScreenCapture.RangeToPixelRadius(attackRange);
            int cx = Screen.PrimaryScreen!.Bounds.Width  / 2;
            int cy = Screen.PrimaryScreen!.Bounds.Height / 2;
            var searchArea = new Rectangle(cx - pixelRadius, cy - pixelRadius, pixelRadius * 2, pixelRadius * 2);

            Point found = await Task.Run(() => ScreenCapture.FindFirstPattern(searchArea, Values.LastHitPattern));
            if (found == Point.Empty) return Point.Empty;
            return new Point(found.X + OffsetX, found.Y + OffsetY);
        }
    }
}
