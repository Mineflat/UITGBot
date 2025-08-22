using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using UITGBot.Core;
using UITGBot.Logging;

namespace UITGBot.TGBot.CommandTypes
{
    internal class SimpleCommand : BotCommand
    {
        /// <summary>
        /// Текстовое сообщение, которое  необходимо отправить в чат
        /// </summary>
        public required string Message { get; set; }
        /// <summary>
        /// Функция верификации правильности заполнения команды
        /// </summary>
        /// <returns>Успешность верификации команды</returns>
        public override bool Verify()
        {
            if (!base.Verify()) return false;
            if (string.IsNullOrEmpty(Message)) { UILogger.AddLog($"Не удалось верифицировать команду {Name} - пустой параметр Message", "ERROR"); return false; }
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
                if (string.IsNullOrEmpty(Message)) throw new Exception("В этой команде отсутствует текст для отправки");
                if (Message.Length > 4096) throw new Exception("Текст, указанный в этой команде, имеет длину более 4096 символов - это ограничение Телеграмм");
                await BotCommand.SendMessage(Message, this.ReplyPrivateMessages, client, update, token, this.SendMessageAsReply);
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
            catch (Exception e)
            {
                //this.Enabled = false;
                await BotCommand.SendMessage($"Команда `{this.Name}` не может быть выполнена:\n{e.Message}", this.ReplyPrivateMessages, client, update, token, this.SendMessageAsReply);
            }
        }
    }
}
