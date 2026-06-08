namespace refactor
{
    public static class Logger
    {
        private static readonly object _lock = new object();

        public static void Info(string message)    => Write(message, ConsoleColor.White);
        public static void Success(string message) => Write(message, ConsoleColor.Green);
        public static void Warning(string message) => Write(message, ConsoleColor.Yellow);
        public static void Error(string message)   => Write(message, ConsoleColor.Red);
        public static void Action(string message)  => Write(message, ConsoleColor.Cyan);
        public static void Debug(string message)   { if (Values.DebugEnabled) Write(message, ConsoleColor.DarkGray); }

        private static void Write(string message, ConsoleColor color)
        {
            lock (_lock)
            {
                Console.ForegroundColor = color;
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {message}");
                Console.ResetColor();
            }
        }
    }
}
