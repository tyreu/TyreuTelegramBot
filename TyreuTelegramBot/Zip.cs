using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InputFiles;

namespace TyreuTelegramBot
{
    public class Zip
    {
        private const string DOWNLOAD_PATH = "..\\Downloaded Files";
        private const string ZIP_PATH = "..\\DownloadedFiles.zip";
        private readonly TelegramBotClient Bot;
        public readonly Chat Chat;

        public List<Message> Messages { get; set; } = new List<Message>();

        public Zip(TelegramBotClient bot, Chat chat)
        {
            Bot = bot;
            Chat = chat;
        }

        public async void CreateAndSendZip()
        {
            if (Directory.Exists(DOWNLOAD_PATH))
            {
                ZipFile.CreateFromDirectory(DOWNLOAD_PATH, ZIP_PATH);
                Directory.Delete(DOWNLOAD_PATH, true);
                using var fs = new FileStream(ZIP_PATH, FileMode.Open);
                await Bot.SendDocumentAsync(new ChatId(Chat.Id), new InputOnlineFile(fs, $"{Chat.Username}_{DateTime.Now:ddMMyyyy}.zip"), $"Архив для {Chat.Username}");
            }
            if (System.IO.File.Exists(ZIP_PATH))
                System.IO.File.Delete(ZIP_PATH);
        }

        public async Task DownloadFromMessage(Message message)
        {
            switch (message.Type)
            {
                case Telegram.Bot.Types.Enums.MessageType.Photo:
                    await DownloadAttachment(message.Photo.Last().FileId);
                    break;
                case Telegram.Bot.Types.Enums.MessageType.Audio:
                    await DownloadAudio(message);
                    break;
                case Telegram.Bot.Types.Enums.MessageType.Video:
                    await DownloadAttachment(message.Video.FileId);
                    break;
                case Telegram.Bot.Types.Enums.MessageType.Voice:
                    await DownloadAttachment(message.Voice.FileId);
                    break;
                case Telegram.Bot.Types.Enums.MessageType.Document:
                    await DownloadAttachment(message.Document.FileId);
                    break;
                case Telegram.Bot.Types.Enums.MessageType.VideoNote:
                    await DownloadAttachment(message.VideoNote.FileId);
                    break;
                default:
                    break;
            }
        }

        private async Task DownloadAttachment(string fileId)
        {
            var attachInfo = await Bot.GetFileAsync(fileId);
            DownloadFile(Bot, attachInfo.FileId, attachInfo.FilePath);
        }

        private async Task DownloadAudio(Message message)
        {
            var audioInfo = await Bot.GetFileAsync(message.Audio.FileId);
            var audioName = $"{message.Audio.Performer} - {message.Audio.Title}.{audioInfo.FilePath.Split('.').Last()}";
            if (message.Audio.Performer == null || message.Audio.Title == null)
                audioName = null;
            DownloadFile(Bot, audioInfo.FileId, audioInfo.FilePath, audioName);
        }

        private async void DownloadFile(TelegramBotClient Bot, string fileId, string filePath, string songName = null)
        {
            try
            {
                if (!Directory.Exists(DOWNLOAD_PATH))
                    Directory.CreateDirectory(DOWNLOAD_PATH);

                var filename = filePath.Split('/')[1];
                using var saveImageStream = new FileStream($"{DOWNLOAD_PATH}/{songName ?? filename}", FileMode.Create);
                await Bot.GetInfoAndDownloadFileAsync(fileId, saveImageStream);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error downloading: " + ex.Message);
            }
        }
    }
}
