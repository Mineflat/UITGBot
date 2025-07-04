using Microsoft.AspNetCore.Components.Forms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bots.Types;
using UITGBot.Core;
using UITGBot.Logging;

namespace UITGBot.TGBot.CommandTypes
{
    internal class RandomImageCommand : BotCommand
    {
        /// <summary>
        /// Путь к ДИРЕКТОРИИ, из которой необходимо отправить произвольное изображение
        /// </summary>
        public required string DirPath { get; set; }
        /// <summary>
        /// Список разрешений файлов, которые можно отправлять используя эту команду
        /// </summary>
        public string[] Extentions { get; set; } = ["*.png", "*.jpeg", "*.jpg"];
        private List<string> ListedFiles { get; set; } = new List<string>();
        private const int IntervalMinutes = 15; // Интервал обновления в минутах
        private readonly CancellationTokenSource cts = new(); // Для отмены задачи

        /// <summary>
        /// Функция верификации правильности заполнения команды
        /// </summary>
        /// <returns>Успешность верификации команды</returns>
        public override bool Verify()
        {
            if (!base.Verify()) return false;
            if (!Directory.Exists(DirPath))
            {
                UILogger.AddLog($"Команда {Name} не может быть применена, т.к. директория \"{DirPath}\" не существует. Команда отключена", "ERROR");
                return false;
            }
            UILogger.AddLog($"[Команда][{Name}]: Запуск первичной синхронизации директории: {DirPath}");
            Task.Run(async () => await StartFileMonitorAsync(cts.Token));
            UILogger.AddLog($"[Команда][{Name}]: Завершила первичную инициализацию");
            return true;
        }

        private async Task StartFileMonitorAsync(CancellationToken token)
        {
            UILogger.AddLog($"Команда {Name} установила таймер обновления директории через {IntervalMinutes} минут");

            // Запускаем задачу на обновление списка файлов
            var fileMonitorTask = Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    await UpdateFileListAsync(); // Обновляем список файлов

                    if (ListedFiles.Any()) // Если в списке есть файлы, сразу выполняем
                    {
                        UILogger.AddLog($"Файлы готовы, список не пуст: {ListedFiles.Count}");
                    }
                    await Task.Delay(TimeSpan.FromMinutes(IntervalMinutes), token); // Ждем 15 минут
                }
            }, token);

            // Немедленно проверяем и запускаем отправку файла, если список не пуст
            if (ListedFiles.Any())
            {
                UILogger.AddLog($"Файлы уже доступны, можно начать отправку: {ListedFiles.Count}.");
            }
            else
            {
                UILogger.AddLog("Список файлов еще пуст.", "WARNING");
            }
            UILogger.AddLog("Таймер запущен.");
        }


        private async Task UpdateFileListAsync()
        {
            try
            {
                UILogger.AddLog($"Обновление списка файлов: {DateTime.Now}", "DEBUG");

                // Собираем все файлы в список асинхронно
                var newFiles = await GetFilesAsync(DirPath, Extentions);

                // Добавляем новые файлы в текущий список
                lock (ListedFiles) // Потокобезопасно обновляем список
                {
                    foreach (var newFile in newFiles)
                    {
                        // Добавляем файл в список, если его нет
                        if (!ListedFiles.Contains(newFile))
                        {
                            ListedFiles.Add(newFile);
                        }
                    }
                }

                UILogger.AddLog($"Добавлено файлов: {newFiles.Count()}, общее количество: {ListedFiles.Count}");
            }
            catch (Exception ex)
            {
                UILogger.AddLog($"Ошибка при обновлении списка файлов: {ex.Message}", "ERROR");
            }
        }

        private async Task<List<string>> GetFilesAsync(string path, string[] extensions)
        {
            UILogger.AddLog($"Проверка директории: {path}, Маски: {string.Join(", ", extensions)}");

            var files = new List<string>();

            // Перебор всех масок из массива Extentions
            foreach (var ext in extensions)
            {
                Console.WriteLine($"Поиск файлов с маской: {ext}");

                // Используем Directory.EnumerateFiles, чтобы получить список файлов
                foreach (var file in Directory.EnumerateFiles(path, ext, SearchOption.AllDirectories))
                {
                    //Console.WriteLine($"Найден файл: {file}");
                    files.Add(file); // Добавляем файл в список
                }
            }

            return files;
        }
        /// <summary>
        /// Функция для вызова выбранной команды
        /// </summary>
        /// <returns>Кортеж: true - если инициализация прошла успешно, *string - сообщение об ошибке</returns>
        public override async Task ExecuteCommand(ITelegramBotClient client, Telegram.Bot.Types.Update update, CancellationToken token)
        {
            if (update.Message == null)
            {
                UILogger.AddLog($"Получено неверное обновление: update.Message пуст (я хз как ты получил это сообщение, телеге видимо плохо)", "ERROR");
                return;
            }
            try
            {
                if (ListedFiles.Count == 0)
                {
                    if (this.ReplyPrivateMessages && update.Message.From != null)
                        await client.SendMessage(
                            chatId: update.Message.From.Id,
                            text: "Увы, картинки закончились (или не начинались)",
                            //replyParameters: update.Message.MessageId, // Делаем сообщение ответным
                            cancellationToken: token
                        );
                    return;
                }

                //var fileSelectionResult = CryptoRandomizer.GetRandomFileInPath(DirPath, Extentions);
                //if (!fileSelectionResult.success)
                //{
                //    Storage.Logger?.Logger.Error($"Ошибка выполнения команды {this.Name}: {fileSelectionResult.errorMessage}");
                //    return;
                //}
                //string filePath = fileSelectionResult.errorMessage;
                UILogger.AddLog($"Количество файлов в списке: {ListedFiles.Count}");
                string filePath = ListedFiles[CryptoRandomizer.GetRandom(0, ListedFiles.Count - 1)];
                if (!System.IO.File.Exists(filePath))
                {
                    UILogger.AddLog($"Ошибка выполнения команды {this.Name}: Файл \"{filePath}\" не существует", "ERROR");
                    await ExecuteCommand(client, update, token).ConfigureAwait(true);
                    return;
                }
                await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                var inputFile = new InputFileStream(stream, Path.GetFileName(filePath));
                string replyText = CryptoRandomizer.GetRandomReply(this, true);
                if (this.ReplyPrivateMessages && update.Message.From != null)
                {
                    await client.SendPhoto(
                        chatId: update.Message.From.Id,
                        photo: inputFile,
                        caption: replyText,
                        //replyParameters: update.Message.MessageId, // Делаем сообщение ответным
                        cancellationToken: token
                    );
                    UILogger.AddLog($"Успешно отправлена картинка \"{filePath}\" в чат {update.Message.Chat.Id} (личные сообщения)");
                    return;
                }
                await client.SendPhoto(
                    chatId: update.Message.Chat.Id,
                    photo: inputFile,
                    caption: replyText,
                    replyParameters: update.Message.MessageId, // Делаем сообщение ответным
                    cancellationToken: token
                );
                UILogger.AddLog($"Успешно отправлена картинка \"{filePath}\" в чат {update.Message.Chat.Id} (публичный чат)");

            }
            catch (Exception e)
            {
                //this.Enabled = false;
                await BotCommand.SendMessage($"Команда {this.Name} не может быть выполнена:\n{e.Message}", this.ReplyPrivateMessages, client, update, token);
            }
            // Выполнение каскадной команды
            if (RunAfter != null)
            {
                if (RunAfter.Enabled)
                {
                    UILogger.AddLog($"Выполнение КАСКАДНОЙ команды: {Name} => {RunAfter.Name}");
                    await RunAfter.ExecuteCommand(client, update, token);
                }
            }
        }
    }
}
