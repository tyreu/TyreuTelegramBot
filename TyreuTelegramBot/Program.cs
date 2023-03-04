using System;

namespace TyreuTelegramBot
{
    class Program
    {
        static void Main()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            _ = new TyreuBot();
            Console.ReadKey();
        }
    }
}
