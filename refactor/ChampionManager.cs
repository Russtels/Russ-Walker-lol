using System.Collections.Generic;
using System.Linq;

namespace refactor
{
    /// <summary>
    /// Gestiona y selecciona la lógica de combate específica para el campeón actual.
    /// </summary>
    public static class ChampionManager
    {
        /// <summary>
        /// Almacena una lista de todas las lógicas de campeones disponibles.
        /// </summary>
        private static readonly List<IChampionLogic> _logics = new List<IChampionLogic>();

        /// <summary>
        /// La lógica de campeón activa actualmente. Es 'null' si no hay ninguna para el campeón seleccionado.
        /// </summary>
        public static IChampionLogic? CurrentChampionLogic { get; private set; }

        /// <summary>
        /// Carga todas las lógicas de campeones disponibles al iniciar el programa.
        /// </summary>
        public static void Initialize()
        {
            // Aquí añadiremos todas las nuevas lógicas de campeones que creemos.
            _logics.Add(new XerathLogic());
            // _logics.Add(new OtroCampeonLogic()); // Ejemplo para el futuro
        }

        /// <summary>
        /// Revisa el nombre del campeón actual y activa el módulo de lógica correspondiente.
        /// </summary>
        public static void Update(string currentChampionName)
        {
            // Busca en la lista una lógica que coincida con el nombre del campeón.
            var logic = _logics.FirstOrDefault(l => l.ChampionName.Equals(currentChampionName, StringComparison.OrdinalIgnoreCase));

            // Si la lógica activa no es la correcta, la actualiza.
            if (CurrentChampionLogic != logic)
            {
                CurrentChampionLogic = logic;
                if (CurrentChampionLogic != null)
                {
                    Console.WriteLine($"[ChampionManager] Módulo de lógica activado para: {CurrentChampionLogic.ChampionName}");
                }
            }
        }
    }
}