using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using UITGBot.Core;

namespace UITGBot.TGBot.CommandTypes
{
    internal class FileCommand : BotCommand
    {
        /// <summary>
        /// Путь к файлу, который необходимо отправить в чат
        /// </summary>
        public required string FilePath { get; set; }
        /// <summary>
        /// Функция верификации правильности заполнения команды
        /// </summary>
        /// <returns>Успешность верификации команды</returns>
        public override bool Verify()
        {
            if (!base.Verify()) return false;
            if (!File.Exists(FilePath))
            {
                Storage.Logger?.Logger.Error($"Команда {Name} не может быть применена, т.к. файл \"{FilePath}\" не существует. Команда отключена");
                return false;
            }
            return true;
        }
        /// <summary>
        /// Функция для вызова выбранной команды
        /// </summary>
        /// <returns>Кортеж: true - если инициализация прошла успешно, *string - сообщение об ошибке</returns>
        public override async Task ExecuteCommand(ITelegramBotClient client, Update update, CancellationToken token)
        {

            if (update.Message == null)
            {
                Storage.Logger?.Logger.Error($"Получено неверное обновление: update.Message пуст (я хз как ты получил это сообщение, телеге видимо плохо)");
                return;
            }
            try
            {
                if (!File.Exists(FilePath))
                {
                    Storage.Logger?.Logger.Error($"Ошибка выполнения команды {this.Name}: файл не существует: \"{FilePath}\"");
                    return;
                }
                await using var stream = new FileStream(FilePath, FileMode.Open, FileAccess.Read);
                var inputFile = new InputFileStream(stream, Path.GetFileName(FilePath));
                string replyText = (string.IsNullOrEmpty(FixedReply) ? CryptoRandomizer.GetRandomReply(this, true) : FixedReply);
                if (this.ReplyPrivateMessages && update.Message.From != null)
                {
                    await client.SendDocument(
                        chatId: update.Message.From.Id,
                        document: inputFile,
                        caption: replyText,
                        cancellationToken: token
                    );
                    Storage.Logger?.Logger.Information($"Успешно отправлен файл \"{FilePath}\" в чат {update.Message.Chat.Id} (личные сообщения)");
                    return;
                }
                await client.SendDocument(
                    chatId: update.Message.Chat.Id,
                    document: inputFile,
                    caption: replyText,
                    replyParameters: update.Message.MessageId, // Делаем сообщение ответным
                    cancellationToken: token
                );
                Storage.Logger?.Logger.Information($"Успешно отправлен файл \"{FilePath}\" в чат {update.Message.Chat.Id} (публичный чат)");
            }
            catch (Exception e)
            {
                //this.Enabled = false;
                await BotCommand.SendMessage($"Команда {this.Name} не может быть выполнена:\n{e.Message}", this.ReplyPrivateMessages, client, update, token);
            }
        }
    }
}
