using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TyreuTelegramBot.Enums;
using TyreuTelegramBot.Helpers;
using TyreuTelegramBot.Services;

namespace TyreuTelegramBot
{
    public partial class TyreuBot
    {

        private readonly TelegramBotClient Bot = new TelegramBotClient(BotData.Token);

        private readonly UpdateType[] updateTypes = { UpdateType.Message, UpdateType.CallbackQuery };
        private Command CurrentCommand { get; set; } = Command.Default;

        private Zipper Zipper { get; set; }
        private ChatGptService ChatGptService { get; set; }
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
                CurrencyEnum? currency = Currency.TryParseStringToCurrency(ev.CallbackQuery.Data);
                if (currency is not null)
                {
                    await Bot.SendTextMessageAsync(message.Chat.Id, Currency.GetRate(currency.Value));
                }
                if (currency is null)
                {
                    Task task = ev.CallbackQuery.Data switch
                    {
                        "StopZip" when CurrentCommand == Command.Zip => Zipper?.CreateAndSendZip(),
                        _ => Bot.SendTextMessageAsync(message.Chat.Id, "Неизвестная команда.")
                    };
                    await task;
                }
                CurrentCommand = Command.Default;
            }
            catch (MessageIsNotModifiedException ex)
            {
                Console.WriteLine(ex.Message);
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
            Console.WriteLine(Logger.Log($"{e.Message.Chat.FirstName} {e.Message.Chat.LastName} ({e.Message.Chat.Id}) wrote: \"{e.Message.Text}\" on {e.Message.Date}"));

            if (e.Message.Type == MessageType.Text && e.Message.Text.StartsWith("/"))//если сообщение является текстом и начинается с "/", то обрабатываем как команду
            {
                await HandleCommand(e.Message.Chat.Id, e.Message.Text);
                return;
            }

            Task task = CurrentCommand switch
            {
                //если на данный момент команда не выполняется и сообщение текстовое
                Command.Default when e.Message.Type == MessageType.Text
                    => HandleCommand(e.Message.Chat.Id, e.Message.Text),//обрабатываем команду
                //если сейчас выполняется команда /getRate
                Command.GetRate
                    => Task.CompletedTask,
                //если сейчас выполняется /zip и сообщение не текстовое
                Command.Zip when e.Message.Type != MessageType.Text
                    => (Zipper ??= new Zipper(Bot, e.Message.Chat)).DownloadFromMessage(e.Message),//скачиваем вложения
                Command.Gpt
                    => (ChatGptService ??= new ChatGptService()).SendMessageGPT(Bot, e.Message.Chat.Id, e.Message.Text),
                _ => Task.CompletedTask
            };
            await task;
        }

        /// <summary>
        /// Обработчик команд бота
        /// </summary>
        /// <param name="chatId">ID чата</param>
        /// <param name="command">Наименование команды</param>
        private async Task HandleCommand(long chatId, string command)
        {
            var defaultLambda = () => { CurrentCommand = Command.Default; return Task.CompletedTask; };
            Task task = command switch
            {
                "/getrate" => GetRate(chatId),
                "/zip" => Zip(chatId),
                "/gpt" => Gpt(chatId),
                _ => defaultLambda()
            };
            await task;
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
