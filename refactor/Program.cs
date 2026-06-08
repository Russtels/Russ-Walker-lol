using System.Drawing;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace refactor
{
    internal static class Program
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AllocConsole();

        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(Keys vKey);

        private static readonly ApiClient _apiClient  = new ApiClient();
        private static readonly GameState _gameState   = new GameState();
        private static readonly Random    _random      = new Random();

        private static bool _showAttackRangePressed    = false;
        private static bool _attackChampionsOnlyPressed = false;
        private static bool _debugDrawPressed           = false;

        [STAThread]
        static async Task Main()
        {
            AllocConsole();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            ChampionManager.Initialize();

            Logger.Success("=== Russ Walker started ===");
            Logger.Info("Space = Orbwalk | V = Auto Farm | F1 = API dump | C = toggle range");

            new Thread(() => new Drawings().Run()) { IsBackground = true, Name = "DrawingsThread" }.Start();
            new Thread(AvailabilityCheckLoop)       { IsBackground = true, Name = "AvailabilityThread" }.Start();

            while (true)
            {
                if (SpecialFunctions.IsTargetProcessFocused("League of Legends"))
                {
                    await UpdateGameState();
                    if (_gameState.IsApiAvailable)
                    {
                        bool antiCCActed = AntiCC.CheckForStunAndReact(_gameState);
                        if (!antiCCActed)
                            await HandleUserInputLogic();
                    }
                }
                else
                {
                    await Task.Delay(100);
                }
                await Task.Delay(1);
            }
        }

        private static void AvailabilityCheckLoop()
        {
            while (true)
            {
                if (SpecialFunctions.IsTargetProcessFocused("League of Legends") && _gameState.IsApiAvailable)
                    AntiCC.UpdateAvailability(_gameState);
                Thread.Sleep(250);
            }
        }

        private static async Task UpdateGameState()
        {
            await _apiClient.FetchAllGameDataAsync(_gameState);
            if (_gameState.IsApiAvailable)
            {
                Values.UpdateChampionWindup(_gameState.ChampionName);
                ChampionManager.Update(_gameState.ChampionName);
            }
        }

        private static async Task HandleUserInputLogic()
        {
            bool isActionPressed = (GetAsyncKeyState(Keys.Space) & 0x8000) != 0;
            bool isFarmPressed   = (GetAsyncKeyState(Keys.V)     & 0x8000) != 0;
            bool isF1Pressed     = (GetAsyncKeyState(Keys.F1)    & 0x8000) != 0;
            bool isF2Pressed     = (GetAsyncKeyState(Keys.F2)    & 0x8000) != 0;

            HandleKeyToggle(Values.ShowAttackRange,   ref _showAttackRangePressed,     InputSimulator.ScanCodeShort.KEY_C);
            HandleKeyToggle(Values.AttackChampionOnly, ref _attackChampionsOnlyPressed, middleMouse: true);

            if (isActionPressed)
            {
                if (ChampionManager.CurrentChampionLogic != null)
                    await ChampionManager.CurrentChampionLogic.ExecuteCombo(_gameState);
                else
                    await OrbwalkEnemyAsync();
            }
            else if (isFarmPressed && Values.AutoFarmEnabled)
            {
                await AutoFarm.ExecuteLastHitLogic(_gameState);
            }

            if (isF1Pressed && !Values.apiLogDebugEnable)
            {
                await ShowApiDump();
                Values.apiLogDebugEnable = true;
            }
            else if (!isF1Pressed && Values.apiLogDebugEnable)
            {
                Values.apiLogDebugEnable = false;
            }

            // F2: toggle debug draw overlay (pixel detection areas)
            if (isF2Pressed && !_debugDrawPressed)
            {
                Values.DebugDrawEnabled = !Values.DebugDrawEnabled;
                Logger.Info($"[Debug] Draw overlay: {(Values.DebugDrawEnabled ? "ON" : "OFF")}");
                _debugDrawPressed = true;
            }
            else if (!isF2Pressed)
            {
                _debugDrawPressed = false;
            }
        }

        public static async Task OrbwalkEnemyAsync()
        {
            if (SpecialFunctions.CanAttack(_gameState.AttackSpeed))
            {
                Point enemyPos = await ScreenCapture.GetEnemyPosition(_gameState.AttackRange);
                if (enemyPos != Point.Empty && !_gameState.IsDead)
                {
                    Point kitePos = Cursor.Position;
                    SpecialFunctions.ClickAt(enemyPos);
                    await Task.Delay(25);
                    SpecialFunctions.SetCursorPos(kitePos.X, kitePos.Y);

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

        private static void HandleKeyToggle(bool condition, ref bool pressed,
            InputSimulator.ScanCodeShort scanCode = InputSimulator.ScanCodeShort.LBUTTON,
            bool middleMouse = false)
        {
            if (condition && !pressed)
            {
                if (middleMouse) InputSimulator.SendMiddleMouseDown();
                else             InputSimulator.SendKeyDown(scanCode);
                pressed = true;
            }
            else if (!condition && pressed)
            {
                if (middleMouse) InputSimulator.SendMiddleMouseUp();
                else             InputSimulator.SendKeyUp(scanCode);
                pressed = false;
            }
        }

        private static async Task ShowApiDump()
        {
            Console.Clear();
            Logger.Info("======================== API DUMP (F1) ========================");
            string data = await _apiClient.GetRawActivePlayerDataAsync();
            Logger.Info(data);
            Logger.Info("===============================================================");
        }
    }
}
