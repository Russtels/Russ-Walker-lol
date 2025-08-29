using GameOverlay.Drawing;
using GameOverlay.Windows;
using System.Runtime.InteropServices;
using SolidBrush = GameOverlay.Drawing.SolidBrush;
using System.Windows.Forms;

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
        private GameOverlay.Drawing.Font _font = null!;
        private GameOverlay.Drawing.Font _logoFont = null!;

        // Drawings del Anti CC
        private SolidBrush _antiCCAreaBrush = null!;

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

            _antiCCAreaBrush = gfx.CreateSolidBrush(255, 0, 0, 0); // Rojo con 80 de opacidad

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

                //anti cc area
                if (Values.ShowAntiCCAreaGuide)
                {
                    // Dimensiones que pediste
                    float left = 900;
                    float top = 990;
                    float right = 1172; 
                    float bottom = 1064; 

                    var areaRect = new GameOverlay.Drawing.Rectangle(right, bottom, left, top);
                    // Dibuja el relleno semitransparente
                    gfx.FillRectangle(_antiCCAreaBrush, areaRect);

                    // Dibuja un borde blanco para que sea más visible
                    gfx.DrawRectangle(_infoBrush, areaRect, 2);

                    // Añade un texto para identificar el área
                    gfx.DrawTextWithBackground(_font, _fontBrush, _logoBrush, right + 5, bottom + 5, $"Área de Detección Anti-CC ({left}x{top})");
                }

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