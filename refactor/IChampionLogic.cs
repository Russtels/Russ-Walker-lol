using GameOverlay.Drawing;

namespace refactor
{
    /// <summary>
    /// Define el contrato que toda lógica de campeón específica debe seguir.
    /// </summary>
    public interface IChampionLogic
    {
        /// <summary>
        /// El nombre del campeón al que se aplica esta lógica.
        /// </summary>
        string ChampionName { get; }

        /// <summary>
        /// Ejecuta la secuencia de combo principal (habilidades y autoataques).
        /// </summary>
        void ExecuteCombo(GameState gameState);

        /// <summary>
        /// Dibuja las ayudas visuales específicas de este campeón en el overlay.
        /// </summary>
        void DrawSpells(GameOverlay.Drawing.Graphics gfx);


        void ExecuteAimAssistR(GameState gameState);

    }
}