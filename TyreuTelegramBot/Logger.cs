using System.Collections.Generic;

namespace TyreuTelegramBot
{
    public static class Logger
    {
        public static List<string> LogHistory { get; set; } = new List<string>();
        public static string Log(string message)
        {
            LogHistory.Add(message);
            return message;
        }
    }
}
