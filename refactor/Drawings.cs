using GameOverlay.Windows;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using GfxGraphics  = GameOverlay.Drawing.Graphics;
using GfxBrush    = GameOverlay.Drawing.SolidBrush;
using GfxFont     = GameOverlay.Drawing.Font;

namespace refactor
{
    internal class Drawings : IDisposable
    {
        [DllImport("user32.dll")]
        static extern short GetAsyncKeyState(Keys vKey);

        private readonly GraphicsWindow _window;

        // Brushes
        private GfxBrush _white    = null!;
        private GfxBrush _black    = null!;
        private GfxBrush _cyan     = null!;
        private GfxBrush _yellow   = null!;
        private GfxBrush _red      = null!;
        private GfxBrush _green    = null!;
        private GfxBrush _dimWhite = null!;

        // Range circle brushes
        private GfxBrush _rangeFill    = null!;
        private GfxBrush _rangeOutline = null!;

        // Fonts
        private GfxFont _font     = null!;
        private GfxFont _fontBold = null!;

        public Drawings()
        {
            var gfx = new GfxGraphics
            {
                MeasureFPS              = true,
                PerPrimitiveAntiAliasing = true,
                TextAntiAliasing        = true,
            };

            _window = new GraphicsWindow(
                0, 0,
                Screen.PrimaryScreen!.Bounds.Width,
                Screen.PrimaryScreen!.Bounds.Height,
                gfx)
            {
                FPS       = 60,
                IsTopmost = true,
                IsVisible = true,
            };

            _window.SetupGraphics  += OnSetup;
            _window.DrawGraphics   += OnDraw;
            _window.DestroyGraphics += OnDestroy;
        }

        private void OnSetup(object? sender, GameOverlay.Windows.SetupGraphicsEventArgs e)
        {
            GfxGraphics gfx = e.Graphics;
            _white    = gfx.CreateSolidBrush(255, 255, 255);
            _black    = gfx.CreateSolidBrush(0,   0,   0);
            _cyan     = gfx.CreateSolidBrush(0,   220, 255);
            _yellow   = gfx.CreateSolidBrush(255, 220, 0);
            _red      = gfx.CreateSolidBrush(255, 60,  60);
            _green    = gfx.CreateSolidBrush(80,  255, 120);
            _dimWhite = gfx.CreateSolidBrush(200, 200, 200);

            // Semi-transparent attack range circle
            _rangeFill    = gfx.CreateSolidBrush(0, 200, 255, 12);
            _rangeOutline = gfx.CreateSolidBrush(0, 200, 255, 120);

            _font     = gfx.CreateFont("Consolas", 12);
            _fontBold = gfx.CreateFont("Consolas", 13, true);
        }

        private void OnDraw(object? sender, GameOverlay.Windows.DrawGraphicsEventArgs e)
        {
            GfxGraphics gfx = e.Graphics;
            gfx.ClearScene();

            if (!SpecialFunctions.IsTargetProcessFocused("League of Legends") || !Values.DrawingsEnabled)
                return;

            var state = GameState.Current;
            float cx  = _window.Width  / 2f;
            float cy  = _window.Height / 2f;

            DrawAttackRange(gfx, state, cx, cy);
            DrawStatsPanel(gfx, state);
            DrawOrbwalkerStatus(gfx, cx, cy);

            if (ChampionManager.CurrentChampionLogic != null)
                ChampionManager.CurrentChampionLogic.DrawSpells(gfx);
        }

        private void DrawAttackRange(GfxGraphics gfx, GameState state, float cx, float cy)
        {
            if (!Values.ShowAttackRange || state.AttackRange <= 0) return;

            float radius = state.AttackRange * 0.8f; // same pixels-per-unit as ScreenCapture

            // Filled semi-transparent disc
            gfx.FillCircle(_rangeFill, cx, cy, radius);
            // Solid border
            gfx.DrawCircle(_rangeOutline, cx, cy, radius, 1.5f);

            // Range label just above the circle edge at the top
            string label = $"{state.AttackRange:F0}";
            gfx.DrawText(_fontBold, _rangeOutline, cx - 14, cy - radius - 18, label);
        }

        private void DrawStatsPanel(GfxGraphics gfx, GameState state)
        {
            float panelX = 10;
            float panelY = _window.Height - 160;
            float lineH  = 16f;
            int   line   = 0;

            // Helper: draws a labeled value, coloring value with valueBrush
            void Row(string label, string value, GfxBrush valueBrush)
            {
                float y = panelY + line * lineH;
                gfx.DrawTextWithBackground(_font, _black, _dimWhite, panelX, y, label);
                gfx.DrawText(_font, valueBrush, panelX + 110, y, value);
                line++;
            }

            // Champion & API status
            var apiColor  = state.IsApiAvailable ? _green : _red;
            var apiStatus = state.IsApiAvailable ? "Connected" : "Offline";
            Row("API", apiStatus, apiColor);
            Row("Champion", state.ChampionName, _white);

            // Attack stats
            Row("ATK Speed", $"{state.AttackSpeed:F3}", _white);
            Row("ATK Range", $"{state.AttackRange:F0} px", _white);
            Row("Windup %",  $"{Values.Windup:F2}", _white);
            int windupMs = SpecialFunctions.GetAttackWindup(state.AttackSpeed);
            Row("Windup ms", $"{windupMs}", _white);

            line++; // spacer

            // Anti-CC status
            Row("Stun",     state.IsStunned       ? "DETECTED" : "None",  state.IsStunned       ? _red   : _dimWhite);
            Row("Cleanse",  state.IsCleanseReady   ? "READY"    : "—",     state.IsCleanseReady   ? _green : _dimWhite);
            Row("Mercurial", state.IsMercurialReady ? "READY"   : "—",     state.IsMercurialReady ? _green : _dimWhite);
        }

        private void DrawOrbwalkerStatus(GfxGraphics gfx, float cx, float cy)
        {
            bool orbwalkOn = (GetAsyncKeyState(Keys.Space) & 0x8000) != 0;
            bool farmOn    = (GetAsyncKeyState(Keys.V)     & 0x8000) != 0;

            if (orbwalkOn)
                gfx.DrawTextWithBackground(_fontBold, _black, _cyan, cx - 52, cy - 520, "  ORBWALK ON  ");
            else if (farmOn)
                gfx.DrawTextWithBackground(_fontBold, _black, _yellow, cx - 52, cy - 520, "  AUTO FARM ON  ");
        }

        private void OnDestroy(object? sender, GameOverlay.Windows.DestroyGraphicsEventArgs e)
        {
            _white?.Dispose();    _black?.Dispose();    _cyan?.Dispose();
            _yellow?.Dispose();   _red?.Dispose();      _green?.Dispose();
            _dimWhite?.Dispose(); _rangeFill?.Dispose(); _rangeOutline?.Dispose();
            _font?.Dispose();     _fontBold?.Dispose();
        }

        public void Run()
        {
            _window.Create();
            _window.Join();
        }

        public void Dispose()
        {
            _window?.Dispose();
        }
    }
}
