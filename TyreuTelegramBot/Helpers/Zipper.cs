using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;

namespace TyreuTelegramBot.Helpers
{
    public class Zipper
    {
        private const string DOWNLOAD_PATH = "..\\Downloaded Files";
        private const string ZIP_PATH = "..\\DownloadedFiles.zip";
        private readonly TelegramBotClient Bot;
        public readonly Chat Chat;

        public List<Message> Messages { get; set; } = new List<Message>();

        public Zipper(TelegramBotClient bot, Chat chat)
        {
            Bot = bot;
            Chat = chat;
        }

        public async Task CreateAndSendZip()
        {
            if (Directory.Exists(DOWNLOAD_PATH))
            {
                //if (!System.IO.File.Exists(Path.GetFullPath(ZIP_PATH)))
                //{
                //    System.IO.File.Delete(ZIP_PATH);
                //}
                var allFilesAccess = true;
                do
                {
                    allFilesAccess = new DirectoryInfo(DOWNLOAD_PATH).GetFiles().ToList().All(file => !IsFileLocked(file));
                } while (!allFilesAccess);

                if (allFilesAccess)
                {
                    ZipFile.CreateFromDirectory(DOWNLOAD_PATH, ZIP_PATH);
                    Directory.Delete(DOWNLOAD_PATH, true);
                    using var fs = new FileStream(ZIP_PATH, FileMode.Open);
                    await Bot.SendDocumentAsync(new ChatId(Chat.Id), new InputOnlineFile(fs, $"{Chat.Username}_{DateTime.Now:ddMMyyyy}.zip"), $"Архив для {Chat.Username}");
                }
            }
            if (System.IO.File.Exists(ZIP_PATH))
                System.IO.File.Delete(ZIP_PATH);
        }

        protected virtual bool IsFileLocked(FileInfo file)
        {
            try
            {
                using FileStream stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None);
                stream.Close();
            }
            catch (IOException)
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                return true;
            }

            //file is not locked
            return false;
        }

        public async Task DownloadFromMessage(Message message)
        {
            switch (message.Type)
            {
                case MessageType.Photo:
                    await DownloadAttachment(message.Photo.Last().FileId, $"photo_{message.ForwardDate:dd-MM-yy_hh-mm-ss}_{message.GetHashCode()}");
                    break;
                case MessageType.Audio:
                    await DownloadAudio(message);
                    break;
                case MessageType.Video when message.Video.FileSize <= 20971520:
                    await DownloadAttachment(message.Video.FileId, $"video_{message.ForwardDate:dd-MM-yy_hh-mm-ss}_{message.GetHashCode()}");
                    break;
                case MessageType.Voice:
                    await DownloadAttachment(message.Voice.FileId, $"voice_{message.ForwardDate:dd-MM-yy_hh-mm-ss}_{message.GetHashCode()}");
                    break;
                case MessageType.Document:
                    await DownloadAttachment(message.Document.FileId, $"doc_{message.ForwardDate:dd-MM-yy_hh-mm-ss}_{message.GetHashCode()}");
                    break;
                case MessageType.VideoNote:
                    await DownloadAttachment(message.VideoNote.FileId, $"videonote_{message.ForwardDate:dd-MM-yy_hh-mm-ss}_{message.GetHashCode()}");
                    break;
                default:
                    break;
            }
        }

        private async Task DownloadAttachment(string fileId, string filename)
        {
            var attachInfo = await Bot.GetFileAsync(fileId);
            DownloadFile(Bot, attachInfo.FileId, $"{filename}.{attachInfo.FilePath.Split('.').Last()}");
        }

        private async Task DownloadAudio(Message message)
        {
            var audioInfo = await Bot.GetFileAsync(message.Audio.FileId);
            var audioName = $"{message.Audio.Performer} - {message.Audio.Title}.{audioInfo.FilePath.Split('.').Last()}";
            if (message.Audio.Performer == null || message.Audio.Title == null)
                audioName = null;
            DownloadFile(Bot, audioInfo.FileId, audioInfo.FilePath, audioName);
        }

        private async void DownloadFile(TelegramBotClient Bot, string fileId, string filename, string songName = null)
        {
            try
            {
                if (!Directory.Exists(DOWNLOAD_PATH))
                    Directory.CreateDirectory(DOWNLOAD_PATH);

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
