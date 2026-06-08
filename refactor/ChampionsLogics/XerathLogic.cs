using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using GfxGraphics = GameOverlay.Drawing.Graphics;

namespace refactor
{
    public class XerathLogic : IChampionLogic
    {
        #region State
        private bool _isChargingQ = false;
        private bool _isUltimateActive = false;
        private readonly Stopwatch _qChargeTimer = new Stopwatch();
        private readonly Stopwatch _rShotTimer   = new Stopwatch();

        private GameOverlay.Drawing.Font?       _font;
        private GameOverlay.Drawing.SolidBrush? _fontBrush;
        private GameOverlay.Drawing.SolidBrush? _logoBrush;
        #endregion

        #region Config
        private static readonly System.Drawing.Rectangle Q_Area = new(804, 991, 45, 38);
        private static readonly System.Drawing.Rectangle W_Area = new(851, 992, 42, 38);
        private static readonly System.Drawing.Rectangle E_Area = new(894, 989, 42, 38);
        private static readonly System.Drawing.Rectangle R_Area = new(937, 990, 43, 38);

        private static readonly System.Drawing.Color[] QReady = { System.Drawing.Color.FromArgb(57,  159, 251), System.Drawing.Color.FromArgb(93,  208, 255) };
        private static readonly System.Drawing.Color[] WReady = { System.Drawing.Color.FromArgb(22,  61,  188), System.Drawing.Color.FromArgb(82,  198, 247) };
        private static readonly System.Drawing.Color[] EReady = { System.Drawing.Color.FromArgb(0,   86,  217), System.Drawing.Color.FromArgb(0,   78,  213) };
        private static readonly System.Drawing.Color[] RReady = { System.Drawing.Color.FromArgb(90,  247, 222), System.Drawing.Color.FromArgb(181, 253, 245) };

        private const float Q_MIN_RANGE            = 800f;
        private const float Q_MAX_RANGE            = 1450f;
        private const float Q_MAX_CHARGE_SECONDS   = 1.75f;
        private const float Q_RANGE_BUFFER         = 400f;
        private const float W_RANGE                = 1000f;
        private const float E_RANGE                = 1125f;
        private const float R_TARGETING_RANGE      = 5000f;
        private const int   R_SHOT_COOLDOWN_MS     = 500;
        #endregion

        public string ChampionName => "Xerath";

        public async Task ExecuteCombo(GameState gameState)
        {
            bool isRPressed = (Program.GetAsyncKeyState(Keys.R) & 0x8000) != 0;
            if (isRPressed && IsRReady())
            {
                _isUltimateActive = true;
                Logger.Action("[Xerath] Ultimate mode activated");
                await Task.Delay(200);
            }

            if (_isUltimateActive)
            {
                await ExecuteAimAssistR(gameState);
                return;
            }

            if (_isChargingQ)
            {
                await ManageQCharge();
                return;
            }

            System.Drawing.Point enemyTarget = await ScreenCapture.GetEnemyPosition(Q_MAX_RANGE);
            if (enemyTarget == System.Drawing.Point.Empty)
            {
                await Program.OrbwalkEnemyAsync();
                return;
            }

            float dist = SpecialFunctions.DistanceToPlayer(enemyTarget);

            if (IsQReady() && dist <= Q_MAX_RANGE)
            {
                StartChargingQ();
                return;
            }
            if (IsEReady() && dist <= E_RANGE)
            {
                Logger.Debug("[Xerath] Casting E");
                SpecialFunctions.SetCursorPos(enemyTarget.X, enemyTarget.Y);
                InputSimulator.PressKey(InputSimulator.ScanCodeShort.KEY_E);
                return;
            }
            if (IsWReady() && dist <= W_RANGE)
            {
                Logger.Debug("[Xerath] Casting W");
                SpecialFunctions.SetCursorPos(enemyTarget.X, enemyTarget.Y);
                InputSimulator.PressKey(InputSimulator.ScanCodeShort.KEY_W);
                return;
            }

            await Program.OrbwalkEnemyAsync();
        }

        public async Task ExecuteAimAssistR(GameState gameState)
        {
            if (!_rShotTimer.IsRunning || _rShotTimer.ElapsedMilliseconds > R_SHOT_COOLDOWN_MS)
            {
                System.Drawing.Point target = await ScreenCapture.GetEnemyPositionClosestToCursor(R_TARGETING_RANGE);
                if (target != System.Drawing.Point.Empty)
                {
                    Logger.Debug("[Xerath] R: firing shot");
                    SpecialFunctions.SetCursorPos(target.X, target.Y);
                    InputSimulator.PressKey(InputSimulator.ScanCodeShort.KEY_R);
                    _rShotTimer.Restart();
                }
            }
            await Task.CompletedTask;
        }

        private bool IsQReady() => ScreenCapture.IsAbilityReady(Q_Area, QReady);
        private bool IsWReady() => ScreenCapture.IsAbilityReady(W_Area, WReady);
        private bool IsEReady() => ScreenCapture.IsAbilityReady(E_Area, EReady);
        private bool IsRReady() => ScreenCapture.IsAbilityReady(R_Area, RReady);

        private void StartChargingQ()
        {
            Logger.Debug("[Xerath] Q charge started");
            InputSimulator.SendKeyDown(InputSimulator.ScanCodeShort.KEY_Q);
            _qChargeTimer.Restart();
            _isChargingQ = true;
        }

        private void ReleaseQ(System.Drawing.Point target)
        {
            Logger.Debug("[Xerath] Q released");
            if (target != System.Drawing.Point.Empty)
                SpecialFunctions.SetCursorPos(target.X, target.Y);
            InputSimulator.SendKeyUp(InputSimulator.ScanCodeShort.KEY_Q);
            _qChargeTimer.Stop();
            _isChargingQ = false;
        }

        private async Task ManageQCharge()
        {
            float currentRange = GetCurrentQRange();
            System.Drawing.Point target = await ScreenCapture.GetEnemyPosition(currentRange - Q_RANGE_BUFFER);
            if (target != System.Drawing.Point.Empty || _qChargeTimer.Elapsed.TotalSeconds >= Q_MAX_CHARGE_SECONDS)
                ReleaseQ(target);
        }

        private float GetCurrentQRange()
        {
            if (!_qChargeTimer.IsRunning) return Q_MIN_RANGE;
            float progress = Math.Min((float)_qChargeTimer.Elapsed.TotalSeconds, Q_MAX_CHARGE_SECONDS) / Q_MAX_CHARGE_SECONDS;
            return Q_MIN_RANGE + (Q_MAX_RANGE - Q_MIN_RANGE) * progress;
        }

        public void DrawSpells(GfxGraphics gfx)
        {
            if (_font == null)
            {
                _font      = gfx.CreateFont("Arial", 12);
                _fontBrush = gfx.CreateSolidBrush(0, 0, 0);
                _logoBrush = gfx.CreateSolidBrush(255, 220, 0);
            }

            float cx = gfx.Width  / 2f;
            float cy = gfx.Height / 2f;

            // Q range circle (color indicates readiness)
            using (var qBrush = IsQReady() ? gfx.CreateSolidBrush(0, 200, 255, 60) : gfx.CreateSolidBrush(200, 50, 50, 40))
                gfx.DrawCircle(qBrush, cx, cy, GetCurrentQRange(), 2);

            using (var wBrush = IsWReady() ? gfx.CreateSolidBrush(0, 200, 255, 30) : gfx.CreateSolidBrush(200, 50, 50, 25))
                gfx.DrawCircle(wBrush, cx, cy, W_RANGE, 1);

            if (_isUltimateActive && _font != null && _fontBrush != null && _logoBrush != null)
                gfx.DrawTextWithBackground(_font, _fontBrush, _logoBrush, cx - 80, cy - 40, "[ ULTIMATE MODE ]");
        }
    }
}
