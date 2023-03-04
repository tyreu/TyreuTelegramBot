using System.Collections.Generic;

namespace TyreuTelegramBot.Helpers
{
    public static class Logger 
    {
        public static List<string> LogHistory { get; } = new List<string>();

        public static string Log(string message)
        {
            LogHistory.Add(message);
            return message;
        }

        public static void ClearLogs() => LogHistory.Clear();
    }
}
