using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bots;
using Telegram.Bots.Http;
using Telegram.Bots.Types;

using UITGBot.Core;

namespace UITGBot.TGBot.CommandTypes
{
    internal class RandomFileCommand : BotCommand
    {
        /// <summary>
        /// Путь к ДИРЕКТОРИИ, из которой необходимо отправить произвольный файл
        /// </summary>
        public required string DirPath { get; set; }
        /// <summary>
        /// Список разрешений файлов, которые можно отправлять используя эту команду
        /// </summary>
        public string[] Extentions { get; set; } = ["*"];
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
            return true;
        }
        /// <summary>
        /// Функция для вызова выбранной команды
        /// </summary>
        /// <returns>Кортеж: true - если инициализация прошла успешно, *string - сообщение об ошибке</returns>
        public override async Task ExecuteCommand(ITelegramBotClient client, Telegram.Bot.Types.Update update, CancellationToken token)
        {
            if (update.Message == null)
            {
                Storage.Logger?.Logger.Error($"Получено неверное обновление: update.Message пуст (я хз как ты получил это сообщение, телеге видимо плохо)");
                return;
            }
            try
            {
                var fileSelectionResult = CryptoRandomizer.GetRandomFileInPath(DirPath, Extentions);
                if (!fileSelectionResult.success)
                {
                    Storage.Logger?.Logger.Error($"Ошибка выполнения команды {this.Name}: {fileSelectionResult.errorMessage}");
                    return;
                }
                string filePath = fileSelectionResult.errorMessage;
                await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                var inputFile = new InputFileStream(stream, Path.GetFileName(filePath));
                string replyText = (string.IsNullOrEmpty(FixedReply) ? CryptoRandomizer.GetRandomReply(this, true) : FixedReply);
                if (this.ReplyPrivateMessages && update.Message.From != null)
                {
                    await client.SendDocument(
                        chatId: update.Message.From.Id,
                        document: inputFile,
                        caption: replyText,
                        //replyParameters: update.Message.MessageId, // Делаем сообщение ответным
                        cancellationToken: token
                    );
                    Storage.Logger?.Logger.Information($"Успешно отправлен файл \"{filePath}\" в чат {update.Message.Chat.Id} (личные сообщения)");
                    return;
                }
                await client.SendDocument(
                    chatId: update.Message.Chat.Id,
                    document: inputFile,
                    caption: replyText,
                    replyParameters: update.Message.MessageId, // Делаем сообщение ответным
                    cancellationToken: token
                );
                Storage.Logger?.Logger.Information($"Успешно отправлен файл \"{filePath}\" в чат {update.Message.Chat.Id} (публичный чат)");
                // Выполнение каскадной команды
                if (RunAfter != null)
                {
                    if (RunAfter.Enabled)
                    {
                        Storage.Logger?.Logger.Information($"Выполнение КАСКАДНОЙ команды: {Name} => {RunAfter.Name}");
                        await RunAfter.ExecuteCommand(client, update, token);
                    }
                }
            }
            catch (Exception e)
            {
                //this.Enabled = false;
                await BotCommand.SendMessage($"Команда {this.Name} не может быть выполнена:\n{e.Message}", this.ReplyPrivateMessages, client, update, token);
            }
        }
    }
}
