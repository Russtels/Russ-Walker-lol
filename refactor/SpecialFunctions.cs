using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;

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

        [DllImport("user32.dll")]
        private static extern void mouse_event(int dwFlags);

        private const int MOUSEEVENTF_RIGHTDOWN = 0x08;
        private const int MOUSEEVENTF_RIGHTUP = 0x10;

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
            catch
            {
                return false;
            }
        }

        public static void ClickAt(Point location)
        {
            SetCursorPos(location.X, location.Y);
            mouse_event(MOUSEEVENTF_RIGHTDOWN);
            mouse_event(MOUSEEVENTF_RIGHTUP);
        }

        public static void Click()
        {
            mouse_event(MOUSEEVENTF_RIGHTDOWN);
            mouse_event(MOUSEEVENTF_RIGHTUP);
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

        public static async Task<bool> CanAttack(float attackSpeed)
        {
            int attackDelay = GetAttackDelay(attackSpeed);
            return AAtick + attackDelay < Environment.TickCount;
        }

        public static bool CanMove()
        {
            // La lógica de Kalista requiere que siempre pueda moverse después de un ataque.
            if (GameState.Current.ChampionName == "Kalista")
            {
                return true;
            }
            return MoveCT <= Environment.TickCount;
        }
    }
}