using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TyreuTelegramBot.DTO.ChatGPT
{
    internal class Request
    {
        [JsonPropertyName("model")]
        public string ModelId { get; set; } = "";
        [JsonPropertyName("messages")]
        public List<Message> Messages { get; set; } = new();
    }
}
