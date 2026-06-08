using GfxGraphics = GameOverlay.Drawing.Graphics;

namespace refactor
{
    public interface IChampionLogic
    {
        string ChampionName { get; }
        Task ExecuteCombo(GameState gameState);
        Task ExecuteAimAssistR(GameState gameState);
        void DrawSpells(GfxGraphics gfx);
        void DrawDebugAreas(GfxGraphics gfx);
    }
}
