using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TyreuTelegramBot.Enums;
using TyreuTelegramBot.Helpers;
using TyreuTelegramBot.Services;

namespace TyreuTelegramBot
{
    public class TyreuBot
    {

        private readonly TelegramBotClient Bot = new(BotData.Token);
        private Command CurrentCommand { get; set; } = Command.Default;
        private readonly CancellationTokenSource cts = new();

        private Zipper Zipper { get; set; }
        private ChatGptService ChatGptService { get; set; }

        public TyreuBot()
        {
            Bot.GetUpdatesAsync().Wait();
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = new[] { UpdateType.Message, UpdateType.CallbackQuery }
            };
            Bot.StartReceiving(updateHandler: HandleUpdateAsync,
                               pollingErrorHandler: HandlePollingErrorAsync,
                               receiverOptions: receiverOptions,
                               cancellationToken: cts.Token);
        }

        Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => $"{exception}"
            };
            Console.WriteLine(ErrorMessage);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Обработчик CallbackQuery Data
        /// </summary>
        private async void Bot_OnCallbackQuery(CallbackQuery callbackData)
        {
            var message = callbackData.Message;
            Console.WriteLine(Logger.Log($"{message.Chat.FirstName} {message.Chat.LastName} ({message.Chat.Id}) chose: \"{callbackData.Data}\" on {message.Date}"));

            CurrencyEnum? currency = Currency.TryParseStringToCurrency(callbackData.Data);
            if (currency is not null)
            {
                await Bot.SendTextMessageAsync(message.Chat.Id, Currency.GetRate(currency.Value));
            }
            if (currency is null)
            {
                Task task = callbackData.Data switch
                {
                    "StopZip" when CurrentCommand == Command.Zip => Zipper?.CreateAndSendZip(),
                    _ => Bot.SendTextMessageAsync(message.Chat.Id, "Неизвестная команда.")
                };
                await task;
            }
            CurrentCommand = Command.Default;

            await Bot.AnswerCallbackQueryAsync(callbackData.Id); // отсылаем пустое, чтобы убрать "часики" на кнопке
        }

        /// <summary>
        /// Обрабатывает полученные сообщения
        /// </summary>
        async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            Console.WriteLine(Logger.Log($"{update.Message?.Chat.FirstName} {update.Message?.Chat.LastName} ({update.Message?.Chat.Id}) wrote: \"{update.Message?.Text}\" on {update.Message?.Date}"));

            if (update.Type == UpdateType.CallbackQuery)
            {
                Bot_OnCallbackQuery(update.CallbackQuery);
            }

            else if (update.Message is not null)
            {
                if (update.Message.Type == MessageType.Text && update.Message.Text.StartsWith("/"))//если сообщение является текстом и начинается с "/", то обрабатываем как команду
                {
                    await HandleCommand(update.Message.Chat.Id, update.Message.Text);
                    return;
                }

                Task task = CurrentCommand switch
                {
                    //если на данный момент команда не выполняется и сообщение текстовое
                    Command.Default when update.Message.Type == MessageType.Text
                        => HandleCommand(update.Message.Chat.Id, update.Message.Text),//обрабатываем команду
                    //если сейчас выполняется команда /getRate
                    Command.GetRate
                        => Task.CompletedTask,
                    //если сейчас выполняется /zip и сообщение не текстовое
                    Command.Zip when update.Message.Type != MessageType.Text
                        => (Zipper ??= new Zipper(Bot, update.Message.Chat)).DownloadFromMessage(update.Message),//скачиваем вложения
                    Command.Gpt
                        => (ChatGptService ??= new ChatGptService()).SendMessageGPT(Bot, update.Message.Chat.Id, update.Message.Text),
                    _ => Task.CompletedTask
                };
                await task;
            }
        }

        /// <summary>
        /// Обработчик команд бота
        /// </summary>
        /// <param name="chatId">ID чата</param>
        /// <param name="command">Наименование команды</param>
        private async Task HandleCommand(long chatId, string command)
        {
            Task defaultLambda() { CurrentCommand = Command.Default; return Task.CompletedTask; }
            Task task = command switch
            {
                "/getrate" => GetRate(chatId),
                "/zip" => Zip(chatId),
                "/gpt" => Gpt(chatId),
                "/scirocco" => GetSciroccoAvgPrice(chatId),
                _ => defaultLambda()
            };
            await task;
        }

        private async Task GetSciroccoAvgPrice(long chatId)
        {
            CurrentCommand = Command.Scirocco;
            string url = "https://www.otomoto.pl/osobowe/volkswagen/scirocco/od-2012?search%5Bfilter_float_engine_capacity%3Ato%5D=1500&search%5Badvanced_search_expanded%5D=true";
            var avgPrice = new HtmlWeb().Load(url).QuerySelectorAll("div > h3").Average(car => int.Parse(Regex.Replace(car.InnerText, @"\s+", "")));
            await Bot.SendTextMessageAsync(chatId, $"Средняя цена VW Scirocco: {avgPrice:N0} zł");
        }

        private async Task GetRate(long chatId)
        {
            CurrentCommand = Command.GetRate;
            var buttons = new List<InlineKeyboardButton>();//TODO: create extension
            foreach (var value in Enum.GetValues(typeof(CurrencyEnum)))
                buttons.Add(InlineKeyboardButton.WithCallbackData($"{value}"));
            await Bot.SendTextMessageAsync(chatId, "Выберите валюту", replyMarkup: new InlineKeyboardMarkup(buttons));
        }

        private async Task Zip(long chatId)
        {
            CurrentCommand = Command.Zip;
            var stopZipButton = InlineKeyboardButton.WithCallbackData("Остановить загрузку и создать архив", "StopZip");
            await Bot.SendTextMessageAsync(chatId, "Пришлите/перешлите мне файлы и я отправлю Вам архив.", replyMarkup: new InlineKeyboardMarkup(stopZipButton));
        }

        private async Task Gpt(long chatId)
        {
            CurrentCommand = Command.Gpt;
            await Bot.SendTextMessageAsync(chatId, "Отправьте боту Ваш запрос.");
        }

    }
}
