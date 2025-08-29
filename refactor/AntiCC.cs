using System.Drawing;

namespace refactor
{
    public class AntiCC
    {
        // Coordenadas del área de búsqueda
        private static readonly Rectangle SearchArea = new Rectangle(900, 990, 272, 74);

        // Patrón de Stun
        private static readonly Color[,] StunPattern = new Color[8, 8]
        {
};

        public static void CheckForStun()
        {
            // Llama a nuestra nueva herramienta reutilizable
            Point stunFoundAt = ScreenScanner.FindFirstOccurrence(SearchArea, StunPattern);

            // Si se encontró, avisa en la consola
            if (stunFoundAt != Point.Empty)
            {
                Console.WriteLine($"[ANTI-CC] STUN DETECTADO");
            }
        }
    }
}