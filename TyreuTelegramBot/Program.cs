using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace TyreuTelegramBot
{
    public class TyreuBot
    {
        enum Command
        {
            Default,
            GetRate,
            Zip
        }

        private readonly TelegramBotClient Bot = new TelegramBotClient(BotData.Token);
        private Chat Chat { get; set; }
        private readonly UpdateType[] updateTypes = { UpdateType.Message, UpdateType.CallbackQuery };
        private Command CurrentCommand { get; set; } = Command.Default;

        private Zip Zip { get; set; }
        private Currency Currency { get; set; }
        private Dictionary<long, int> userMessages = new Dictionary<long, int>();

        public TyreuBot()
        {
            Bot.OnMessage += Bot_OnMessage;
            Bot.OnCallbackQuery += Bot_OnCallbackQuery;
            Bot.StartReceiving(updateTypes);
        }

        /// <summary>
        /// Обработчик CallbackQuery Data
        /// </summary>
        private async void Bot_OnCallbackQuery(object sender, CallbackQueryEventArgs ev)
        {
            var message = ev.CallbackQuery.Message;
            Console.WriteLine(Logger.Log($"{message.Chat.FirstName} {message.Chat.LastName} ({message.Chat.Id}) chose: \"{ev.CallbackQuery.Data}\" on {message.Date}"));
            try
            {
                switch (ev.CallbackQuery.Data)
                {
                    case "USD" when !userMessages.ContainsKey(message.Chat.Id):
                        var messageId = (await Bot.SendTextMessageAsync(message.Chat.Id, Currency.GetRate(CurrencyEnum.USD))).MessageId;
                        userMessages.Add(message.Chat.Id, messageId);
                        CurrentCommand = Command.Default;
                        break;
                    case "USD" when userMessages[message.Chat.Id] != -1:
                        await Bot.EditMessageTextAsync(message.Chat.Id, userMessages[message.Chat.Id], Currency.GetRate(CurrencyEnum.USD));
                        CurrentCommand = Command.Default;
                        break;
                    case "EUR" when !userMessages.ContainsKey(message.Chat.Id):
                        messageId = (await Bot.SendTextMessageAsync(message.Chat.Id, Currency.GetRate(CurrencyEnum.EUR))).MessageId;
                        userMessages.Add(message.Chat.Id, messageId);
                        CurrentCommand = Command.Default;
                        break;
                    case "EUR" when userMessages[message.Chat.Id] != -1:
                        await Bot.EditMessageTextAsync(message.Chat.Id, userMessages[message.Chat.Id], Currency.GetRate(CurrencyEnum.EUR));
                        CurrentCommand = Command.Default;
                        break;
                    case "StopZip" when CurrentCommand == Command.Zip:
                        Zip?.CreateAndSendZip();
                        CurrentCommand = Command.Default;
                        break;
                    default:
                        CurrentCommand = Command.Default;
                        break;
                }
            }
            catch (MessageIsNotModifiedException ex)
            {
                await Bot.AnswerCallbackQueryAsync(ev.CallbackQuery.Id, $"Вы уже выбрали {ev.CallbackQuery.Data}!");
            }
            catch (InvalidParameterException ex)
            {
                Console.WriteLine(ex.Message);
            }

            await Bot.AnswerCallbackQueryAsync(ev.CallbackQuery.Id); // отсылаем пустое, чтобы убрать "часики" на кнопке
        }

        /// <summary>
        /// Обрабатывает полученные сообщения
        /// </summary>
        private async void Bot_OnMessage(object sender, MessageEventArgs e)
        {
            var message = e.Message;
            Chat = message.Chat;
            Console.WriteLine(Logger.Log($"{message.Chat.FirstName} {message.Chat.LastName} ({message.Chat.Id}) wrote: \"{message.Text}\" on {message.Date}"));

            if (message.Type == MessageType.Text && message.Text.StartsWith("/"))//если сообщение является текстом и начинается с "/", то обрабатываем как команду
            {
                HandleCommand(message.Chat.Id, message.Text);
                return;
            }

            switch (CurrentCommand)
            {
                case Command.Default when message.Type == MessageType.Text://если на данный момент команда не выполняется и сообщение текстовое
                    await HandleCommand(message.Chat.Id, message.Text);//обрабатываем команду
                    break;
                case Command.GetRate://если сейчас выполняется команда /getRate
                    break;
                case Command.Zip when message.Type != MessageType.Text://если сейчас выполняется /zip и сообщение не текстовое
                    await Zip.DownloadFromMessage(message);//скачиваем вложения
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Обработчик команд бота
        /// </summary>
        /// <param name="chatId">ID чата</param>
        /// <param name="command">Наименование команды</param>
        private async Task HandleCommand(long chatId, string command)
        {
            switch (command)
            {
                case "/getrate":
                    CurrentCommand = Command.GetRate;
                    Currency ??= new Currency();
                    var buttons = new List<InlineKeyboardButton>();
                    foreach (var value in Enum.GetValues(typeof(CurrencyEnum)))
                        buttons.Add(InlineKeyboardButton.WithCallbackData($"{value}"));

                    await Bot.SendTextMessageAsync(chatId, "Выберите валюту", ParseMode.Default, false, false, 0, new InlineKeyboardMarkup(buttons));
                    break;
                case "/zip":
                    CurrentCommand = Command.Zip;
                    Zip ??= new Zip(Bot, Chat);
                    await Bot.SendTextMessageAsync(chatId, "Пришлите/перешлите мне файлы и я отправлю Вам архив.", replyMarkup: new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("Остановить загрузку и создать архив", "StopZip")));
                    break;
                default:
                    CurrentCommand = Command.Default;
                    break;
            }
        }
    }
    class Program
    {
        static void Main()
        {
            new TyreuBot();
            Console.ReadKey();
        }
    }
}
