using System;
using System.Collections.Generic;
using System.Text;

namespace TyreuTelegramBot
{
    public static class BotData
    {
        public const string Token = "383335729:AAEQ-wlWI0d_Zlm8Lrn7n4c2_gfXhvIjSE4";
        public static string CurrencyRateUrl(string currencyName) => $"https://minfin.com.ua/ua/currency/nbu/{currencyName}/";

    }
}
