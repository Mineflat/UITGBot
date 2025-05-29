using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using Telegram.Bot;
using Telegram.Bot.Types;
using UITGBot.Core;

namespace UITGBot.TGBot.CommandTypes
{
    internal class RemoteFileCommand : BotCommand
    {
        /// <summary>
        /// Путь к ДИРЕКТОРИИ, в которую необходимо сохранить файл
        /// </summary>
        public required string DirPath { get; set; }

        //public static HashSet<string> existingFileHashes { get; protected set; } = new HashSet<string>();
        /// <summary>
        /// Функция верификации правильности заполнения команды
        /// </summary>
        /// <returns>Успешность верификации команды</returns>
        public override bool Verify()
        {
            if (!base.Verify()) return false;
            if (!Directory.Exists(DirPath))
            {
                Storage.Logger?.Logger.Error($"Команда {Name} не может быть применена, т.к. директория \"{DirPath}\" не существует. Команда отключена");
                return false;
            }
            //Task.Run(async () =>
            //{
            //    existingFileHashes = await LoadExistingFileHashes().ConfigureAwait(true);
            //});
            return true;
        }
        /// <summary>
        /// Функция для вызова выбранной команды
        /// </summary>
        /// <returns>Кортеж: true - если инициализация прошла успешно, *string - сообщение об ошибке</returns>
        public override async Task ExecuteCommand(ITelegramBotClient client, Telegram.Bot.Types.Update update, CancellationToken token)
        {
            if (update.Type != Telegram.Bot.Types.Enums.UpdateType.Message || update.Message == null)
            {
                Storage.Logger?.Logger.Warning($"Команде \"{Name}\" не было передано никакого файла, поэтому она читается выполненной по-умолчанию");
                return;
            }
            // return (true, $"Команде \"{Name}\" не было передано никакого файла, поэтому она читается выполненной по-умолчанию", false);
            string? fileId = null;
            string? fileName = null;
            try
            {
                if (update.Message.Document != null) // Документ
                {
                    fileId = update.Message.Document.FileId;
                    //fileName = $"{update.Message.Chat.Id}";
                    fileName = $"{update.Message.From?.Id}_{update.Message.Chat.Id}" +
                        $"_{DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")}_{update.Message.Document.FileName}";
                }
                else if (update.Message.Photo != null && update.Message.Photo.Length > 0) // Фото
                {
                    Console.WriteLine($"Длина вложения: {update.Message.Photo.Length}");
                    bool downloadSuccess = true;
                    foreach (var photo in update.Message.Photo)
                    {
                        fileId = photo.FileId; // Берём фото с максимальным размером
                        //fileName = $"{update.Message.Chat.Id}.jpg";
                        fileName = $"{update.Message.From?.Id}_{update.Message.Chat.Id}" +
                            $"_{DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")}_{Guid.NewGuid()}.jpg"; // Генерируем имя
                        Storage.Logger?.Logger.Information($"Получен файл {fileName}");
                        string buffer = $"{DirPath}/{update.Message.Chat.Id}";
                        if (!Directory.Exists(buffer)) Directory.CreateDirectory(buffer);
                        string buffer2 = Path.Combine(buffer, fileName);
                        var downloadResult0 = await DownloadFileAsync(client, fileId, buffer2);
                        if (!downloadResult0.success)
                        {
                            downloadSuccess = false;
                            Storage.Logger?.Logger.Warning($"{downloadResult0.errorMessage}");
                        }
                        else
                        {
                            Storage.Logger?.Logger.Warning($"Успешно сохранен файл \"{fileName}\"");
                        }
                        //return (downloadResult0.success, downloadResult0.errorMessage, false);
                    }
                    string replyText = CryptoRandomizer.GetRandomReply(this, downloadSuccess);
                    await BotCommand.SendMessage(replyText, this.ReplyPrivateMessages, client, update, token);
                    return;
                }
                else if (update.Message.Audio != null) // Аудио
                {
                    fileId = update.Message.Audio.FileId;
                    //fileName = $"{update.Message.Chat.Id}.mp3";
                    fileName = update.Message.Audio.FileName ?? $"{update.Message.From?.Id}_{update.Message.Chat.Id}" +
                        $"_{DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")}_{Guid.NewGuid()}.mp3";
                }
                else if (update.Message.Video != null) // Видео
                {
                    fileId = update.Message.Video.FileId;
                    //fileName = $"{update.Message.Chat.Id}.mp4";
                    fileName = update.Message.Video.FileName ?? $"{update.Message.From?.Id}" +
                        $"_{update.Message.Chat.Id}_{DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")}_{Guid.NewGuid()}.mp4";
                }
                else if (update.Message.Voice != null) // Голосовое сообщение
                {
                    fileId = update.Message.Voice.FileId;
                    //fileName = $"{update.Message.Chat.Id}.ogg";
                    fileName = $"{update.Message.From?.Id}_{update.Message.Chat.Id}" +
                        $"_{DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")}_{Guid.NewGuid()}.ogg";
                }
                if (fileId != null && fileName != null)
                {
                    string savePath = $"{DirPath}/{update.Message.Chat.Id}";
                    if (!Directory.Exists(savePath)) Directory.CreateDirectory(savePath);
                    string filePath = Path.Combine(savePath, fileName);
                    var downloadResult = await DownloadFileAsync(client, fileId, filePath);
                    Storage.Logger?.Logger.Warning(downloadResult.errorMessage);
                    string replyText = CryptoRandomizer.GetRandomReply(this, downloadResult.success);
                    await BotCommand.SendMessage(replyText, this.ReplyPrivateMessages, client, update, token);
                    return;
                    //return (downloadResult.success, downloadResult.errorMessage, true);
                }
            }
            catch (Exception e)
            {
                //Storage.Logger?.Logger.Error($"Произошла ошибка при сохранении файла:\n{e.Message}");
                //this.Enabled = false;
                await BotCommand.SendMessage($"Произошла ошибка при сохранении файла:\n```\n{e.Message}\n```\n", this.ReplyPrivateMessages, client, update, token);
                return;
                //return (false, $"Произошла ошибка при сохранении файла:\n```\n{e.Message}\n```\n", false);
            }
            await BotCommand.SendMessage($"Ну и что мне сохранять? Хоть бы вложение цепанул...", this.ReplyPrivateMessages, client, update, token);
            //return (true, $"Ну и что мне сохранять? Хоть бы вложение цепанул...", true);
        }
        /// <summary>
        /// Фнкция для загрузки выбранного файла
        /// </summary>
        /// <param name="client">Клиент Телеграмм-бота</param>
        /// <param name="fileId">ID файла для загрузки</param>
        /// <param name="filePath">Полный путь к загружаемоу файлу на ФС</param>
        private async Task<(bool success, string errorMessage)> DownloadFileAsync(ITelegramBotClient client, string fileId, string filePath)
        {
            try
            {
                Storage.Logger?.Logger.Information($"Загрузка файла {filePath}");
                if (System.IO.File.Exists(filePath)) return (false, $"Не удалось загрузить файл \"{filePath}\": файл уже существует");
                var file = await client.GetFile(fileId);
                if (file.FilePath == null)
                {
                    return (false, $"Не удалось загрузить файл: пустая ссылка со стороны Телеграмм");
                }
                using (var saveStream = new FileStream(filePath, FileMode.Create))
                {
                    await client.GetInfoAndDownloadFile(file.FileId, saveStream);
                }
                Storage.Logger?.Logger.Information($"Проверка хеша дубликата файла {filePath}...");
                //bool fileExist = FileExists(filePath);
                //if (fileExist)
                //{
                //    File.Delete(filePath);
                //    return (false, $"Не удалось загрузить файл \"{filePath}\": файл уже существует");
                //}
                //Storage.Logger?.Logger.Information($"Файл {filePath} уникален, поэтому сохранен");
                Storage.Logger?.Logger.Information($"Файл {filePath} сохранен");
                return (true, $"Успешно загружен файл \n```\n{filePath}\n```");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
                return (false, $"Не удалось загрузить файл \"{filePath}\":\n\t{e.Message}");
            }
        }
        private bool FileExists(string filepath)
        {
            string? containingDirectory = Path.GetDirectoryName(filepath);
            if(string.IsNullOrEmpty(containingDirectory)) return false;
            Console.WriteLine($"\nЦелевая директория проверки файла: {containingDirectory}\n"); 
            string downloadHash = ComputeFileHash(filepath);
            foreach (var file in Directory.GetFiles(containingDirectory))
            {
                string fileHash = ComputeFileHash(file);
                Storage.Logger?.Logger.Information($"Проверка хеша {fileHash}/{downloadHash} ({filepath} => {file})");
                if (downloadHash == fileHash) return true;
            }
            return false;
        }
        private async Task<HashSet<string>> LoadExistingFileHashes()
        {
            Storage.Logger?.Logger.Information($"Запуск вычисления хешей для директории {DirPath} (команда {Name})");

            var hashes = new HashSet<string>();
            var tasks = new List<Task<string>>();
            //string[] files = Directory.GetFiles(DirPath, "*", searchOption: SearchOption.AllDirectories);
            string[] files = Directory.GetFiles(DirPath);
            foreach (var file in files)
            {
                Storage.Logger?.Logger.Debug($"Высчитываетсмя хеш для файла: {file}");
                tasks.Add(Task.Run(() => ComputeFileHash(file)));
                Task.Delay(20).Wait();
                //string hash = ComputeFileHash(file);
                //hashes.Add(hash);
                //Storage.Logger?.Logger.Debug($"Успешно добавлен новый хеш файла: {hash}");
            }
            var hasheCalculationsProccesses = await Task.WhenAll(tasks);
            foreach (var hashTask in hasheCalculationsProccesses)
            {
                hashes.Add(hashTask);
            }
            Storage.Logger?.Logger.Information($"Хеши для для директории {DirPath} успешно вычислены (команда {Name})");
            return hashes;
        }

        public static string ComputeFileHash(string filePath)
        {
            using (var sha256 = SHA256.Create())
            using (var stream = File.OpenRead(filePath))
            {
                byte[] hashBytes = sha256.ComputeHash(stream);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }
    }
}
