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
                            HandleOrbwalkingLogic();
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

        #region Lógica del Orbwalker
        /// <summary>
        /// Gestiona la entrada del usuario (barra espaciadora) para activar el orbwalking.
        /// </summary>
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

        /// <summary>
        /// Lógica principal del Orbwalker: decide si atacar o moverse.
        /// </summary>
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
        #endregion
    }
}