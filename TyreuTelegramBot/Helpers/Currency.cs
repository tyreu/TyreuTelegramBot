using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using System;
using System.Globalization;
using TyreuTelegramBot.Enums;

namespace TyreuTelegramBot.Helpers
{
    public static class Currency
    {
        public static string? GetRate(CurrencyEnum currency)
        {
            string currencyName = Enum.GetName(typeof(CurrencyEnum), currency);
            string Url = BotData.CurrencyRateUrl(currencyName);

            HtmlWeb web = new();
            HtmlNode tableCell = web.Load(Url).QuerySelector("div[type='nbu']");
            var commaCulture = new CultureInfo("en") { NumberFormat = { NumberDecimalSeparator = "," } };
            return tableCell is null ? "Error." : $"Курс {currencyName}: {double.Parse(tableCell.InnerText, NumberStyles.AllowDecimalPoint, commaCulture):N2}";
        }
        public static CurrencyEnum? TryParseStringToCurrency(string currencyName)
        {
            if (currencyName is { Length: 3 } && currencyName == currencyName.ToUpper())
            {
                foreach (var value in Enum.GetValues(typeof(CurrencyEnum)))
                    if ($"{value}" == currencyName)
                        return (CurrencyEnum)value;
            }
            return null;
        }
    }
}
