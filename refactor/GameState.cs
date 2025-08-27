namespace refactor
{
    public class GameState
    {
        public float AttackSpeed { get; set; }
        public float AttackRange { get; set; }
        public string ChampionName { get; set; } = "none";
        public bool IsDead { get; set; } = true;
        public bool IsApiAvailable { get; set; } = false;

        // Propiedad para pasar al overlay
        public static GameState Current { get; } = new GameState();
    }
}