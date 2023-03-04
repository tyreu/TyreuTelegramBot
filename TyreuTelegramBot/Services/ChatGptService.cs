using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Telegram.Bot;
using TyreuTelegramBot.DTO.ChatGPT;

namespace TyreuTelegramBot.Services
{
    internal class ChatGptService
    {
        // адрес api для взаимодействия с чат-ботом
        public const string Endpoint = "https://api.openai.com/v1/chat/completions";
        // набор соообщений диалога с чат-ботом
        public List<Message> Messages { get; set; } = new List<Message>();

        public async Task SendMessageGPT(TelegramBotClient bot, long chatId, string content)
        {
            var httpClient = new HttpClient();
            var сhatGptService = new ChatGptService();

            // формируем отправляемое сообщение
            var message = new Message() { Role = "user", Content = content };

            // добавляем сообщение в список сообщений
            сhatGptService.Messages.Add(message);

            // формируем отправляемые данные
            var requestData = new Request()
            {
                ModelId = "gpt-3.5-turbo",
                Messages = сhatGptService.Messages
            };

            // устанавливаем отправляемый в запросе токен
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {BotData.ChatGPTToken}");

            // отправляем запрос
            using var response = await httpClient.PostAsJsonAsync(Endpoint, requestData);

            // если произошла ошибка, выводим сообщение об ошибке на консоль
            if (!response.IsSuccessStatusCode)
            {
                await bot.SendTextMessageAsync(chatId, $"{(int)response.StatusCode} {response.StatusCode}");
                return;
            }
            // получаем данные ответа
            ResponseData? responseData = await response.Content.ReadFromJsonAsync<ResponseData>();

            var choices = responseData?.Choices ?? new List<Choice>();
            if (choices is { Count: 0 })
            {
                await bot.SendTextMessageAsync(chatId, "No choices were returned by the API. Try again.");
            }
            var responseMessage = choices[0].Message;
            // добавляем полученное сообщение в список сообщений
            сhatGptService.Messages.Add(responseMessage);
            var responseText = responseMessage.Content.Trim();
            await bot.SendTextMessageAsync(chatId, $"{responseText}");
        }

    }
}