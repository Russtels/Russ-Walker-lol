using System;
using System.Drawing;
using System.Diagnostics;
using GameOverlay.Drawing;
using System.Windows.Forms;

namespace refactor
{
    /// <summary>
    /// Implementación de la lógica de combate específica para Xerath.
    /// </summary>
    public class XerathLogic : IChampionLogic
    {
        #region Configuración y Estado de Habilidades
        // --- Estado Interno ---
        private bool _isChargingQ = false;
        private bool _isUltimateActive = false;
        private readonly Stopwatch _qChargeTimer = new Stopwatch();
        private readonly Stopwatch _rShotTimer = new Stopwatch();

        // --- Recursos de Dibujo (para el texto del modo R) ---
        private GameOverlay.Drawing.Font? _font;
        private GameOverlay.Drawing.SolidBrush? _fontBrush;
        private GameOverlay.Drawing.SolidBrush? _logoBrush;

        // --- Configuración de Habilidades ---
        // Se especifica System.Drawing para evitar ambigüedad
        private static readonly System.Drawing.Rectangle Q_Area = new System.Drawing.Rectangle(804, 991, 45, 38);
        private static readonly System.Drawing.Rectangle W_Area = new System.Drawing.Rectangle(851, 992, 42, 38);
        private static readonly System.Drawing.Rectangle E_Area = new System.Drawing.Rectangle(894, 989, 42, 38);
        private static readonly System.Drawing.Rectangle R_Area = new System.Drawing.Rectangle(937, 990, 43, 38);
        private static readonly System.Drawing.Color[] XerathQReadyPattern = { System.Drawing.Color.FromArgb(57, 159, 251), System.Drawing.Color.FromArgb(93, 208, 255) };
        private static readonly System.Drawing.Color[] XerathWReadyPattern = { System.Drawing.Color.FromArgb(22, 61, 188), System.Drawing.Color.FromArgb(82, 198, 247) };
        private static readonly System.Drawing.Color[] XerathEReadyPattern = { System.Drawing.Color.FromArgb(0, 86, 217), System.Drawing.Color.FromArgb(0, 78, 213) };
        private static readonly System.Drawing.Color[] XerathRReadyPattern = { System.Drawing.Color.FromArgb(90, 247, 222), System.Drawing.Color.FromArgb(181, 253, 245) };

        // --- Constantes de Habilidades ---
        private const float Q_MIN_RANGE = 800f, Q_MAX_RANGE = 1450f, Q_MAX_CHARGE_TIME_SECONDS = 1.75f, Q_RANGE_BUFFER = 400f;
        private const float W_RANGE = 1000f, E_RANGE = 1125f, R_TARGETING_RANGE = 5000f;
        private const int R_SHOT_COOLDOWN_MS = 500;
        #endregion

        public string ChampionName => "Xerath";

        #region Lógica Principal del Combo
        /// <summary>
        /// Ejecuta la secuencia de combo para Xerath, gestionando los diferentes estados.
        /// </summary>
        public void ExecuteCombo(GameState gameState)
        {
            bool isRPressed = (Program.GetAsyncKeyState(Keys.R) & 0x8000) != 0;
            if (isRPressed && IsRReady())
            {
                _isUltimateActive = true;
                Console.WriteLine("[Xerath Logic] Modo Ultimate ACTIVADO.");
                System.Threading.Thread.Sleep(200);
            }

            if (_isUltimateActive)
            {
                ExecuteAimAssistR(gameState);
                return;
            }

            if (_isChargingQ)
            {
                ManageQCharge();
                return;
            }

            System.Drawing.Point enemyTarget = ScreenCapture.GetEnemyPosition(Q_MAX_RANGE).Result;
            if (enemyTarget == System.Drawing.Point.Empty)
            {
                Program.OrbwalkEnemyAsync().Wait();
                return;
            }

            float distanceToTarget = SpecialFunctions.DistanceToPlayer(enemyTarget);

            if (IsQReady() && distanceToTarget <= Q_MAX_RANGE)
            {
                StartChargingQ();
                return;
            }

            if (IsEReady() && distanceToTarget <= E_RANGE)
            {
                Console.WriteLine("[Xerath Logic] Lanzando E...");
                SpecialFunctions.SetCursorPos(enemyTarget.X, enemyTarget.Y);
                InputSimulator.PressKey(InputSimulator.ScanCodeShort.KEY_E);
                return;
            }

            if (IsWReady() && distanceToTarget <= W_RANGE)
            {
                Console.WriteLine("[Xerath Logic] Lanzando W...");
                SpecialFunctions.SetCursorPos(enemyTarget.X, enemyTarget.Y);
                InputSimulator.PressKey(InputSimulator.ScanCodeShort.KEY_W);
                return;
            }

            Program.OrbwalkEnemyAsync().Wait();
        }

        /// <summary>
        /// Lógica de apuntado y disparo automático para la R. Debe ser pública para implementar la interfaz.
        /// </summary>
        public void ExecuteAimAssistR(GameState gameState)
        {
            if (!_rShotTimer.IsRunning || _rShotTimer.ElapsedMilliseconds > R_SHOT_COOLDOWN_MS)
            {
                System.Drawing.Point enemyTarget = ScreenCapture.GetEnemyPositionClosestToCursor(R_TARGETING_RANGE).Result;
                if (enemyTarget != System.Drawing.Point.Empty)
                {
                    Console.WriteLine("[Xerath Logic] Modo R: Disparando...");
                    SpecialFunctions.SetCursorPos(enemyTarget.X, enemyTarget.Y);
                    InputSimulator.PressKey(InputSimulator.ScanCodeShort.KEY_R);
                    _rShotTimer.Restart();
                }
            }
        }
        #endregion

        #region Lógica y Cálculos Internos de Habilidades
        private bool IsQReady() => ScreenCapture.IsAbilityReady(Q_Area, XerathQReadyPattern);
        private bool IsWReady() => ScreenCapture.IsAbilityReady(W_Area, XerathWReadyPattern);
        private bool IsEReady() => ScreenCapture.IsAbilityReady(E_Area, XerathEReadyPattern);
        private bool IsRReady() => ScreenCapture.IsAbilityReady(R_Area, XerathRReadyPattern);

        private void StartChargingQ()
        {
            Console.WriteLine("[Xerath Logic] Iniciando carga de Q...");
            InputSimulator.SendKeyDown(InputSimulator.ScanCodeShort.KEY_Q);
            _qChargeTimer.Restart();
            _isChargingQ = true;
        }

        private void ReleaseQ(System.Drawing.Point target)
        {
            Console.WriteLine("[Xerath Logic] Soltando Q!");
            if (target != System.Drawing.Point.Empty)
            {
                SpecialFunctions.SetCursorPos(target.X, target.Y);
            }
            InputSimulator.SendKeyUp(InputSimulator.ScanCodeShort.KEY_Q);
            _qChargeTimer.Stop();
            _isChargingQ = false;
        }

        private void ManageQCharge()
        {
            float currentQRange = GetCurrentQRange();
            System.Drawing.Point enemyTarget = ScreenCapture.GetEnemyPosition(currentQRange - Q_RANGE_BUFFER).Result;
            if (enemyTarget != System.Drawing.Point.Empty || _qChargeTimer.Elapsed.TotalSeconds >= Q_MAX_CHARGE_TIME_SECONDS)
            {
                ReleaseQ(enemyTarget);
            }
        }

        private float GetCurrentQRange()
        {
            if (!_qChargeTimer.IsRunning) return Q_MIN_RANGE;
            float chargeTime = Math.Min((float)_qChargeTimer.Elapsed.TotalSeconds, Q_MAX_CHARGE_TIME_SECONDS);
            float chargeProgress = chargeTime / Q_MAX_CHARGE_TIME_SECONDS;
            float bonusRange = (Q_MAX_RANGE - Q_MIN_RANGE) * chargeProgress;
            return Q_MIN_RANGE + bonusRange;
        }

        private System.Drawing.Point PredictTargetPosition(System.Drawing.Point target, float travelTime)
        {
            return target;
        }
        #endregion

        #region Dibujos del Overlay
        /// <summary>
        /// Dibuja los rangos y estados de las habilidades de Xerath en el overlay.
        /// </summary>
        public void DrawSpells(GameOverlay.Drawing.Graphics gfx)
        {
            // Inicializa los recursos de dibujo la primera vez que se necesitan
            if (_font == null)
            {
                _font = gfx.CreateFont("Arial", 12);
                _fontBrush = gfx.CreateSolidBrush(0, 0, 0);
                _logoBrush = gfx.CreateSolidBrush(255, 255, 0);
            }

            var playerPos = new GameOverlay.Drawing.Point(gfx.Width / 2, gfx.Height / 2);
            using (var qBrush = IsQReady() ? gfx.CreateSolidBrush(0, 255, 255, 30) : gfx.CreateSolidBrush(255, 0, 0, 30))
            {
                gfx.DrawCircle(qBrush, playerPos.X, playerPos.Y, GetCurrentQRange(), 2);
            }
            using (var wBrush = IsWReady() ? gfx.CreateSolidBrush(0, 255, 255, 20) : gfx.CreateSolidBrush(255, 0, 0, 20))
            {
                gfx.DrawCircle(wBrush, playerPos.X, playerPos.Y, W_RANGE, 1);
            }

            if (_isUltimateActive)
            {
                gfx.DrawTextWithBackground(_font, _fontBrush, _logoBrush, playerPos.X - 50, playerPos.Y, "ULTIMATE MODE ACTIVE");
            }
        }
        #endregion
    }
}