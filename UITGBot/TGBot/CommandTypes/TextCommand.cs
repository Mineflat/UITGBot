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
    internal class TextCommand : BotCommand
    {
        /// <summary>
        /// Путь к файлу, откуда необходимо прочитать текст
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
            try
            {
                string text = await File.ReadAllTextAsync(FilePath, encoding: Encoding.UTF8);
                if (string.IsNullOrEmpty(text)) throw new Exception("В указанном файле отсутствует текст");
                if (text.Length > 4096) throw new Exception("Текст в указанном файле имеет длину более 4096 символов - это ограничение Телеграмм");
                await BotCommand.SendMessage(text, this.ReplyPrivateMessages, client, update, token);
            }
            catch (Exception e)
            {
                //this.Enabled = false;
                await BotCommand.SendMessage($"Команда `{this.Name}` не может быть выполнена:\n{e.Message}", this.ReplyPrivateMessages, client, update, token);
            }
        }
    }
}
