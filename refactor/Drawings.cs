using GameOverlay.Windows;
using System.Runtime.InteropServices;
using SolidBrush = GameOverlay.Drawing.SolidBrush;
using System.Windows.Forms; // AÑADE ESTA LÍNEA


namespace refactor
{
    internal class Drawings : IDisposable
    {
        [DllImport("user32.dll")]
        static extern short GetAsyncKeyState(Keys vKey);

        private readonly GraphicsWindow _graphicsWindow;
        private SolidBrush _orbwalkerActivatedBrush = null!;
        private SolidBrush _fontBrush = null!;
        private SolidBrush _infoBrush = null!;
        private SolidBrush _logoBrush = null!;
        private SolidBrush _antiCCAreaBrush = null!;
        private GameOverlay.Drawing.Font _font = null!;
        private GameOverlay.Drawing.Font _logoFont = null!;

        public Drawings()
        {
            var gfx = new GameOverlay.Drawing.Graphics
            {
                MeasureFPS = true,
                PerPrimitiveAntiAliasing = true,
                TextAntiAliasing = true
            };

            _graphicsWindow = new GraphicsWindow(0, 0, Screen.PrimaryScreen!.Bounds.Width, Screen.PrimaryScreen.Bounds.Height, gfx)
            {
                FPS = 30, // Se puede aumentar ligeramente para mayor fluidez del overlay
                IsTopmost = true,
                IsVisible = true
            };

            _graphicsWindow.DrawGraphics += DrawGraphics;
            _graphicsWindow.SetupGraphics += SetupGraphics;
        }

        private void SetupGraphics(object? sender, SetupGraphicsEventArgs e)
        {
            var gfx = e.Graphics;


            // Brushes
            _orbwalkerActivatedBrush = gfx.CreateSolidBrush(0, 255, 255, 100);
            _infoBrush = gfx.CreateSolidBrush(255, 255, 255);
            _fontBrush = gfx.CreateSolidBrush(0, 0, 0);
            _logoBrush = gfx.CreateSolidBrush(255, 255, 0);
            _antiCCAreaBrush = gfx.CreateSolidBrush(255, 255, 255);


            _logoFont = gfx.CreateFont("Monaco", 15, true);
            _font = gfx.CreateFont("Arial", 12);
        }

        private void DrawGraphics(object? sender, DrawGraphicsEventArgs e)
        {
            var gfx = e.Graphics;
            gfx.ClearScene();

            if (SpecialFunctions.IsTargetProcessFocused("League of Legends") && Values.DrawingsEnabled)
            {
                // Usar la nueva clase GameState para obtener datos de forma centralizada
                var state = GameState.Current;

                // Formatear los valores para una mejor visualización
                gfx.DrawTextWithBackground(_font, _fontBrush, _infoBrush, 570, _graphicsWindow.Height - 16, $"ATK Speed: {state.AttackSpeed:F2}");
                gfx.DrawTextWithBackground(_font, _fontBrush, _infoBrush, 570, _graphicsWindow.Height - 35, $"ATK Range: {state.AttackRange:F0}");
                gfx.DrawTextWithBackground(_font, _fontBrush, _infoBrush, 570, _graphicsWindow.Height - 50, $"Windup %: {Values.Windup:F2}");

                int windupMs = SpecialFunctions.GetAttackWindup(state.AttackSpeed);
                gfx.DrawTextWithBackground(_font, _fontBrush, _infoBrush, 570, _graphicsWindow.Height - 65, $"Windup ms: {windupMs}");

                // Lógica para mostrar el estado del Orbwalker
                if ((GetAsyncKeyState(Keys.Space) & 0x8000) != 0)
                {
                    gfx.DrawTextWithBackground(_font, _fontBrush, _orbwalkerActivatedBrush, (_graphicsWindow.Width / 2f) - 70, _graphicsWindow.Height - 500, "Orbwalker: ON");
                }

                // =================================================================
                // INICIO: ESTADOS DEL ANTI-CC
                // =================================================================
                var stunStatusText = $"Status Stun: {(state.IsStunned ? "SI" : "NO")}";
                var cleanseStatusText = $"Status Cleanse: {(state.IsCleanseReady ? "LISTO" : "NO")}";
                var mercurialStatusText = $"Status Mercurial: {(state.IsMercurialReady ? "LISTO" : "NO")}";

                gfx.DrawTextWithBackground(_font, _fontBrush, state.IsStunned ? _antiCCAreaBrush : _infoBrush, 530, _graphicsWindow.Height - 85, stunStatusText);
                gfx.DrawTextWithBackground(_font, _fontBrush, state.IsCleanseReady ? _orbwalkerActivatedBrush : _infoBrush, 530, _graphicsWindow.Height - 100, cleanseStatusText);
                gfx.DrawTextWithBackground(_font, _fontBrush, state.IsMercurialReady ? _orbwalkerActivatedBrush : _infoBrush, 530, _graphicsWindow.Height - 115, mercurialStatusText);
                // =================================================================
                // FIN: ESTADOS DEL ANTI-CC
                // =================================================================
                ///DEBUG
                foreach (var itemSlot in Values.ItemSlots)
                {
                    // Convierte el Rectangle de System.Drawing al del overlay
                    var rect = new GameOverlay.Drawing.Rectangle(
                        itemSlot.Value.Left,
                        itemSlot.Value.Top,
                        itemSlot.Value.Right,
                        itemSlot.Value.Bottom
                    );

                    // Dibuja un borde de color para que sea visible (usaremos el pincel amarillo)
                    gfx.DrawRectangle(_logoBrush, rect, 2);

                    // Dibuja el número del slot para poder identificarlo
                    gfx.DrawText(_font, _logoBrush, rect.Left, rect.Top - 15, itemSlot.Key.ToString());
                }


            }
        }

        public void Run()
        {
            _graphicsWindow.Create();
            _graphicsWindow.Join();
        }

        public void Dispose()
        {
            _graphicsWindow?.Dispose();
            _orbwalkerActivatedBrush?.Dispose();
            _fontBrush?.Dispose();
            _infoBrush?.Dispose();
            _logoBrush?.Dispose();
            _font?.Dispose();
            _logoFont?.Dispose();
        }
    }
}