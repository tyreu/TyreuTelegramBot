using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using System;

namespace TyreuTelegramBot
{
    public enum CurrencyEnum
    {
        USD,
        EUR
    }
    public class Currency
    {
        public string GetRate(CurrencyEnum currency)
        {
            string currencyName = Enum.GetName(typeof(CurrencyEnum), currency);
            string Url = BotData.CurrencyRateUrl(currencyName);

            HtmlWeb web = new HtmlWeb();
            HtmlNode tableCell = web.Load(Url).QuerySelector("td[data-title$=НБУ]");
            var values = tableCell.InnerText.Split('\n');
            return $"Курс: {double.Parse(values[1]):N2}\nРазница с прошлым значением: {values[2][0..^4]}";
        }
    }
}
