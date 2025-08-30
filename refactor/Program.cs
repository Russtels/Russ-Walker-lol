using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Threading;
using System.Drawing;
using System.Diagnostics;

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

            // Inicia el hilo de BAJA FRECUENCIA para la disponibilidad de hechizos/objetos
            new Thread(() => AvailabilityCheckLoop()) { IsBackground = true }.Start();

            // Bucle principal de ALTA FRECUENCIA
            while (true)
            {
                if (SpecialFunctions.IsTargetProcessFocused("League of Legends"))
                {
                    await UpdateGameState();
                    if (_gameState.IsApiAvailable)
                    {
                        // =================================================================
                        // INICIO: LÓGICA DE PRIORIDAD
                        // =================================================================

                        // 1. Revisa el Anti-CC primero.
                        bool actionTakenByAntiCC = AntiCC.CheckForStunAndReact(_gameState);

                        // 2. Si el Anti-CC NO tomó ninguna acción, entonces ejecuta el orbwalker.
                        if (!actionTakenByAntiCC)
                        {
                            HandleOrbwalkingLogic();
                        }

                        // =================================================================
                        // FIN: LÓGICA DE PRIORIDAD
                        // =================================================================
                    }
                }
                else
                {
                    await Task.Delay(100);
                }
                await Task.Delay(1);
            }
        }

        // Bucle de BAJA FRECUENCIA
        private static void AvailabilityCheckLoop()
        {
            while (true)
            {
                if (SpecialFunctions.IsTargetProcessFocused("League of Legends") && _gameState.IsApiAvailable)
                {
                    AntiCC.UpdateAvailability(_gameState);
                }
                // Comprueba la disponibilidad cada 250ms, es más que suficiente y muy eficiente.
                Thread.Sleep(250);
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

        private static async Task OrbwalkEnemyAsync()
        {
            if (SpecialFunctions.CanAttack(_gameState.AttackSpeed))
            {
                Point enemyPosition = await ScreenCapture.GetEnemyPosition(_gameState.AttackRange);
                if (enemyPosition != Point.Empty && !_gameState.IsDead)
                {
                    Point kitePosition = Cursor.Position;
                    SpecialFunctions.ClickAt(enemyPosition);
                    await Task.Delay(25);
                    SpecialFunctions.SetCursorPos(kitePosition.X, kitePosition.Y);
                    int windupDelay = SpecialFunctions.GetAttackWindup(_gameState.AttackSpeed);
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
                SpecialFunctions.MoveCT = Environment.TickCount + _random.Next(40, 60);
            }
        }
    }
}