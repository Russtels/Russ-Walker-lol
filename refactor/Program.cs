using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Threading;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using refactor;

namespace refactor
{
    internal static class Program
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AllocConsole();

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(Keys vKey);

        private static readonly ApiClient _apiClient = new ApiClient();
        private static readonly GameState _gameState = new GameState();
        private static readonly Random _random = new Random();
        private static bool _showAttackRangePressed = false;
        private static bool _attackChampionsOnlyPressed = false;

        [STAThread]
        static async Task Main()
        {
            AllocConsole();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            new Thread(() => new Drawings().Run()) { IsBackground = true }.Start();

            while (true)
            {
                if (SpecialFunctions.IsTargetProcessFocused("League of Legends"))
                {
                    await UpdateGameState();
                    if (_gameState.IsApiAvailable)
                    {
                        HandleOrbwalkingLogic();
                    }
                }
                else
                {
                    await Task.Delay(100);
                }
                await Task.Delay(1);
            }
        }

        private static async Task UpdateGameState()
        {
            await _apiClient.FetchAllGameDataAsync(_gameState);
            if (_gameState.IsApiAvailable)
            {
                Values.UpdateChampionWindup(_gameState.ChampionName);
            }
        }

        private static void HandleOrbwalkingLogic()
        {
            bool isSpacePressed = (GetAsyncKeyState(Keys.Space) & 0x8000) != 0;

            HandleKeyToggle(Values.ShowAttackRange, ref _showAttackRangePressed, InputSimulator.ScanCodeShort.KEY_C);
            HandleKeyToggle(Values.AttackChampionOnly, ref _attackChampionsOnlyPressed, middleMouse: true);

            if (isSpacePressed)
            {
                OrbwalkEnemyAsync().Wait();
            }
        }

        private static void HandleKeyToggle(bool condition, ref bool keyPressedState, InputSimulator.ScanCodeShort scanCode = InputSimulator.ScanCodeShort.LBUTTON, bool middleMouse = false)
        {
            if (condition && !keyPressedState)
            {
                if (middleMouse) InputSimulator.SendMiddleMouseDown();
                else InputSimulator.SendKeyDown(scanCode);
                keyPressedState = true;
            }
            else if (!condition && keyPressedState)
            {
                if (middleMouse) InputSimulator.SendMiddleMouseUp();
                else InputSimulator.SendKeyUp(scanCode);
                keyPressedState = false;
            }
        }

        // =================================================================
        // LÓGICA DE KकुमारO ORIGINAL RESTAURADA Y PERFECCIONADA
        // =================================================================
        private static async Task OrbwalkEnemyAsync()
        {
            // FASE DE ATAQUE
            if (SpecialFunctions.CanAttack(_gameState.AttackSpeed))
            {
                Point enemyPosition = await ScreenCapture.GetEnemyPosition(_gameState.AttackRange);

                if (enemyPosition != Point.Empty && !_gameState.IsDead)
                {
                    // 1. Guarda la posición del cursor para el kiteo.
                    Point kitePosition = Cursor.Position;

                    // 2. Ejecuta el ATAQUE DIRECTO sobre el enemigo.
                    SpecialFunctions.ClickAt(enemyPosition);

                    // =================================================================
                    // INICIO DE LA CORRECCIÓN CRÍTICA
                    // Añadimos una pequeña pausa para darle tiempo al juego a procesar el clic
                    // mientras el cursor AÚN está sobre el enemigo.
                    // =================================================================
                    await Task.Delay(25);
                    // =================================================================
                    // FIN DE LA CORRECCIÓN
                    // =================================================================

                    // 3. Restaura la posición del cursor a donde el usuario estaba apuntando.
                    SpecialFunctions.SetCursorPos(kitePosition.X, kitePosition.Y);

                    // 4. Establece los temporizadores para el siguiente ataque y para el movimiento.
                    int windupDelay = SpecialFunctions.GetAttackWindup(_gameState.AttackSpeed);
                    SpecialFunctions.AAtick = Environment.TickCount;
                    SpecialFunctions.MoveCT = Environment.TickCount + windupDelay + Values.PingBufferMilliseconds;
                }
                else
                {
                    // Si no hay enemigos, moverse hacia el cursor.
                    SpecialFunctions.Click();
                }
            }
            // FASE DE MOVIMIENTO (KITEO)
            else if (SpecialFunctions.CanMove())
            {
                // Este clic se ejecuta después del windup, en la dirección que el usuario
                // eligió (porque ya restauramos la posición del cursor).
                SpecialFunctions.Click();
                SpecialFunctions.MoveCT = Environment.TickCount + _random.Next(40, 60);
            }
        }
    }
}