#region Imports de Sistema
using System;
using System.Drawing;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
#endregion

namespace refactor
{
    internal static class Program
    {
        #region P/Invoke (Interacción con el Sistema Operativo)
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AllocConsole();

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(Keys vKey);
        #endregion

        #region Configuración Principal y Variables Globales
        private static readonly ApiClient _apiClient = new ApiClient();
        private static readonly GameState _gameState = new GameState();
        private static readonly Random _random = new Random();

        // Variables de estado para los toggles (teclas de activación)
        private static bool _showAttackRangePressed = false;
        private static bool _attackChampionsOnlyPressed = false;
        #endregion

        #region Punto de Entrada y Bucles de Ejecución
        [STAThread]
        static async Task Main()
        {
            AllocConsole();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Inicia los hilos secundarios para tareas en segundo plano
            new Thread(() => new Drawings().Run()) { IsBackground = true, Name = "DrawingsThread" }.Start();
            new Thread(() => AvailabilityCheckLoop()) { IsBackground = true, Name = "AntiCCAvailabilityThread" }.Start();

            // Bucle principal de ALTA FRECUENCIA
            while (true)
            {
                if (SpecialFunctions.IsTargetProcessFocused("League of Legends"))
                {
                    await UpdateGameState();
                    if (_gameState.IsApiAvailable)
                    {
                        // Lógica de Prioridad: Anti-CC > Orbwalker
                        bool actionTakenByAntiCC = AntiCC.CheckForStunAndReact(_gameState);
                        if (!actionTakenByAntiCC)
                        {
                            HandleUserInputLogic();
                        }
                    }
                }
                else
                {
                    await Task.Delay(100); // Pausa si el juego no está en foco
                }
                await Task.Delay(1); // Pausa mínima para ceder CPU
            }
        }

        /// <summary>
        /// Bucle de BAJA FRECUENCIA: Se ejecuta en un hilo secundario para comprobar
        /// la disponibilidad de hechizos/objetos de forma eficiente.
        /// </summary>
        private static void AvailabilityCheckLoop()
        {
            while (true)
            {
                if (SpecialFunctions.IsTargetProcessFocused("League of Legends") && _gameState.IsApiAvailable)
                {
                    AntiCC.UpdateAvailability(_gameState);
                }
                Thread.Sleep(250); // Comprueba 4 veces por segundo
            }
        }

        /// <summary>
        /// Actualiza el estado del juego (AS, Rango, etc.) desde la API del cliente.
        /// </summary>
        private static async Task UpdateGameState()
        {
            await _apiClient.FetchAllGameDataAsync(_gameState);
            if (_gameState.IsApiAvailable)
            {
                Values.UpdateChampionWindup(_gameState.ChampionName);
            }
        }
        #endregion

        #region Lógica del Orbwalker y auto farm
        /// <summary>
        /// Gestiona la entrada del usuario para activar el Orbwalker o el Auto-Farm.
        /// Le da prioridad al Orbwalker (Space) sobre el Auto-Farm (V).
        /// </summary>
        private static void HandleUserInputLogic()
        {
            // Gestiona los toggles de opciones (C, Botón central, etc.)
            HandleKeyToggle(Values.ShowAttackRange, ref _showAttackRangePressed, InputSimulator.ScanCodeShort.KEY_C);
            HandleKeyToggle(Values.AttackChampionOnly, ref _attackChampionsOnlyPressed, middleMouse: true);

            // Revisa el estado de las teclas de acción principal
            bool isOrbwalkPressed = (GetAsyncKeyState(Keys.Space) & 0x8000) != 0;
            bool isFarmPressed = (GetAsyncKeyState(Keys.V) & 0x8000) != 0;
            bool isF1Pressed = (GetAsyncKeyState(Keys.F1) & 0x8000) != 0;


            // Lógica de prioridad: Orbwalker > Auto-Farm
            if (isOrbwalkPressed)
            {
                OrbwalkEnemyAsync().Wait();
            }
            else if (isFarmPressed && Values.AutoFarmEnabled)
            {
                AutoFarm.ExecuteLastHitLogic(_gameState).Wait();
            }
            // DEBUG: Lógica para la tecla F1
            if (isF1Pressed && !Values.apiLogDebugEnable)
            {
                // Llama a un nuevo método para mostrar los datos de la API.
                // Usamos .Wait() para asegurar que la tarea asíncrona se complete.
                ShowApiLogAsync().Wait();
                Values.apiLogDebugEnable = true; // Marca la tecla como presionada
            }
            else if (!isF1Pressed && Values.apiLogDebugEnable)
            {
                Values.apiLogDebugEnable = false; // Resetea el estado cuando se suelta la tecla
            }
        }
        /// <summary>
        /// Lógica principal del Orbwalker: decide si atacar o moverse.
        /// </summary>
        /// 

        /// <summary>
        /// Procesa las acciones continuas como Orbwalk y Auto-Farm, respetando su prioridad.
        /// </summary>
        private static void ProcessActionKeys()
        {
            bool isOrbwalkPressed = (GetAsyncKeyState(Keys.Space) & 0x8000) != 0;
            bool isFarmPressed = (GetAsyncKeyState(Keys.V) & 0x8000) != 0;

            // La estructura if/else if es la correcta aquí para mantener la prioridad: Orbwalker > Auto-Farm
            if (isOrbwalkPressed)
            {
                OrbwalkEnemyAsync().Wait();
            }
            else if (isFarmPressed && Values.AutoFarmEnabled)
            {
                AutoFarm.ExecuteLastHitLogic(_gameState).Wait();
            }
        }

        /// <summary>
        /// Procesa las teclas de depuración que funcionan como un evento de una sola pulsación.
        /// </summary>
        private static async Task ProcessDebugKeys()
        {
            bool isF1Pressed = (GetAsyncKeyState(Keys.F1) & 0x8000) != 0;

            // La lógica de "toggle" para la tecla F1
            if (isF1Pressed && !Values.apiLogDebugEnable)
            {
                await ShowApiLogAsync();
                Values.apiLogDebugEnable = true; // Marca la tecla como presionada
            }
            else if (!isF1Pressed && Values.apiLogDebugEnable)
            {
                Values.apiLogDebugEnable = false; // Resetea el estado cuando se suelta la tecla
            }
        }


        private static async Task OrbwalkEnemyAsync()
        {
            if (SpecialFunctions.CanAttack(_gameState.AttackSpeed))
            {
                Point enemyPosition = await ScreenCapture.GetEnemyPosition(_gameState.AttackRange);
                if (enemyPosition != Point.Empty && !_gameState.IsDead)
                {
                    // Secuencia de Ataque y Kiteo
                    Point kitePosition = Cursor.Position;
                    SpecialFunctions.ClickAt(enemyPosition);
                    await Task.Delay(25); // Pausa para que el juego registre el clic
                    SpecialFunctions.SetCursorPos(kitePosition.X, kitePosition.Y);

                    // Establece los temporizadores
                    int windupDelay = SpecialFunctions.GetAttackWindup(_gameState.AttackSpeed);
                    SpecialFunctions.AAtick = Environment.TickCount;
                    SpecialFunctions.MoveCT = Environment.TickCount + windupDelay + Values.PingBufferMilliseconds;
                }
                else
                {
                    // Si no hay enemigos, solo moverse
                    SpecialFunctions.Click();
                }
            }
            else if (SpecialFunctions.CanMove())
            {
                // Moverse mientras el autoataque está en enfriamiento
                SpecialFunctions.Click();
                SpecialFunctions.MoveCT = Environment.TickCount + _random.Next(40, 60);
            }
        }

        /// <summary>
        /// Gestiona la pulsación de teclas para activar/desactivar opciones.
        /// </summary>
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

        /// <summary>
        /// Obtiene y muestra los datos JSON sin procesar del endpoint /activeplayer de la API.
        /// </summary>
        private static async Task ShowApiLogAsync()
        {
            Console.Clear(); // Limpia la consola para una mejor visualización
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("======================= LOG DE LA API (F1) =======================");

            // Llama a un método en la clase ApiClient para obtener los datos
            string apiData = await _apiClient.GetRawActivePlayerDataAsync();

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(apiData); // Muestra el JSON en la consola

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("==================================================================");
            Console.ResetColor();
        }

        #endregion
    }
}