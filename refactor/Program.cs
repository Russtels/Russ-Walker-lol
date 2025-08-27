using System.Runtime.InteropServices;

namespace refactor
{
    internal static class Program
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AllocConsole();

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(Keys vKey);

        // Instancia única para la API y el estado del juego
        private static readonly ApiClient _apiClient = new ApiClient();
        private static readonly GameState _gameState = new GameState();
        private static readonly Random _random = new Random();

        // Variables de estado para las teclas
        private static bool _showAttackRangePressed = false;
        private static bool _attackChampionsOnlyPressed = false;

        [STAThread]
        static async Task Main()
        {
            AllocConsole();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Iniciar hilos de UI (Consola y Overlay)
            //new Thread(() => CNSL.LobbyShow()) { IsBackground = true }.Start();
            new Thread(() => new Drawings().Run()) { IsBackground = true }.Start();

            // Bucle principal del Orbwalker
            while (true)
            {
                if (SpecialFunctions.IsTargetProcessFocused("League of Legends"))
                {
                    // 1. Actualizar el estado del juego una vez por ciclo
                    await UpdateGameState();

                    // 2. Ejecutar lógica solo si el jugador está vivo y la API responde
                    if (_gameState.IsApiAvailable && !_gameState.IsDead)
                    {
                        HandleOrbwalkingLogic();
                    }
                }
                // Pausa muy corta para no consumir el 100% del CPU
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

            if (isSpacePressed && SpecialFunctions.AAtick < Environment.TickCount)
            {
                OrbwalkEnemyAsync().GetAwaiter().GetResult(); // Usar .Wait() o .GetAwaiter() si el contexto lo requiere
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
            // Solo se ejecuta si el ataque está listo
            if (await SpecialFunctions.CanAttack(_gameState.AttackSpeed))
            {
                Point enemyPosition = await ScreenCapture.GetEnemyPosition(_gameState.AttackRange);

                // FASE DE ATAQUE Y KITEO
                if (enemyPosition != Point.Empty)
                {
                    Point originalMousePosition = Cursor.Position;

                    // 1. Atacar al enemigo
                    SpecialFunctions.ClickAt(enemyPosition);

                    // 2. Establecer temporizadores
                    int windupDelay = SpecialFunctions.GetAttackWindup(_gameState.AttackSpeed);
                    SpecialFunctions.AAtick = Environment.TickCount;
                    SpecialFunctions.MoveCT = Environment.TickCount + windupDelay;

                    // 3. Esperar el tiempo de windup
                    await Task.Delay(windupDelay + Values.PingBufferMilliseconds);

                    // 4. Volver a la posición original del cursor y hacer clic para moverse (Kiteo)
                    SpecialFunctions.SetCursorPos(originalMousePosition.X, originalMousePosition.Y);
                    SpecialFunctions.Click();

                    // Pausa adicional opcional para campeones de baja velocidad de ataque
                    if (_gameState.AttackSpeed < 1.75)
                    {
                        await Task.Delay(Values.SleepOnLowAS);
                    }
                }
                // Si no hay enemigos, atacar-moverse hacia el cursor
                else
                {
                    SpecialFunctions.Click();
                    SpecialFunctions.AAtick = Environment.TickCount;
                }
            }
            // FASE DE MOVIMIENTO (mientras el ataque está en enfriamiento)
            else if (SpecialFunctions.CanMove())
            {
                SpecialFunctions.Click();
                SpecialFunctions.MoveCT = Environment.TickCount + _random.Next(50, 80);
            }
        }
    }
}