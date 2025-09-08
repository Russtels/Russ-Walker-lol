using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;

namespace refactor
{
    class SpecialFunctions
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern int GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

        [DllImport("user32.dll")]
        public static extern bool SetCursorPos(int X, int Y);

        public static int AAtick;
        public static int MoveCT;

        public static bool IsTargetProcessFocused(string processName)
        {
            try
            {
                IntPtr activeWindowHandle = GetForegroundWindow();
                GetWindowThreadProcessId(activeWindowHandle, out int activeProcId);
                Process activeProcess = Process.GetProcessById(activeProcId);
                return activeProcess.ProcessName.Equals(processName, StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        }

        // =================================================================
        // RESTAURADO: Ataque con Clic Derecho Directo para garantizar el objetivo
        // =================================================================
        public static void ClickAt(Point location)
        {
            SetCursorPos(location.X, location.Y);
            InputSimulator.SendRightMouseDown();
            InputSimulator.SendRightMouseUp();
        }

        // MÉTODO DE MOVIMIENTO: También es un Clic Derecho
        public static void Click()
        {
            InputSimulator.SendRightMouseDown();
            InputSimulator.SendRightMouseUp();
        }

        public static int GetAttackWindup(float attackSpeed)
        {
            if (attackSpeed <= 0) return 0;
            return (int)(((1 / attackSpeed) * 1000) * (Values.Windup / 100f));
        }

        public static int GetAttackDelay(float attackSpeed)
        {
            if (attackSpeed <= 0) return int.MaxValue;
            return (int)(1000.0f / attackSpeed);
        }

        public static bool CanAttack(float attackSpeed)
        {
            int attackDelay = GetAttackDelay(attackSpeed);
            return AAtick + attackDelay < Environment.TickCount;
        }

        public static bool CanMove()
        {
            if (GameState.Current.ChampionName == "Kalista") return true;
            return MoveCT <= Environment.TickCount;
        }

        public static float DistanceToPlayer(Point target)
        {
            // Obtiene la posición del jugador (centro de la pantalla)
            int playerX = Screen.PrimaryScreen!.Bounds.Width / 2;
            int playerY = Screen.PrimaryScreen!.Bounds.Height / 2;

            // Calcula la diferencia en los ejes X e Y
            float dx = target.X - playerX;
            float dy = target.Y - playerY;

            // Aplica el teorema de Pitágoras para obtener la distancia
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

    }
}