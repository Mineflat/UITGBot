using Polly;
using System;
using System.Collections.Generic;
using Spectre.Console;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using UITGBot.Core;
using Telegram.Bots.Types;

namespace UITGBot.TGBot
{
    internal class BotCommand
    {
        /// <summary>
        /// Имя команды, по которому она будет инициирована
        /// </summary>
        [Required]
        public string Name { get; set; } = string.Empty;
        /// <summary>
        /// Описание команды. Нужно только администратору чтобы понимать, что за команда перед ним
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Список ролей, пользователи в которых могут выполнять эту команду
        /// </summary>
        //public List<string> RoleNames { get; set; } = new List<string>();

        /// <summary>
        /// Указывает, что команду может выполнить кто угодно
        /// </summary>
        public bool IsPublic { get; set; } = false;
        /// <summary>
        /// Указывает, доступна ли команда в данный момент
        /// </summary>
        public bool Enabled { get; set; } = false;
        /// <summary>
        /// Тип команды, используется для десериализации
        /// </summary>
        [Required]
        public string CommandType { get; set; } = string.Empty;
        /// <summary>
        /// Список ID пользователей в телеграмм, которые могут выполнить эту команду
        /// </summary>
        public List<long> UserIDs { get; set; } = new List<long>();

        /// <summary>
        /// Используется для ФИКСИРОВАННОЙ подписи сообщения при УСПЕШНОМ выполнении этой команды
        /// </summary>
        public string? FixedReply { get; set; }
        /// <summary>
        /// Используется для ФИКСИРОВАННОЙ подписи сообщения при НЕУДАЧНОМ выполнении этой команды
        /// </summary>
        public string? FixedErrorReply { get; set; }
        /// <summary>
        /// Используется для ПРОИЗВОЛЬНОЙ подписи сообщения при УСПЕШНОМ выполнении этой команды
        /// </summary>
        public string? SuccessReplyPath { get; set; }
        /// <summary>
        /// Используется для ПРОИЗВОЛЬНОЙ подписи сообщения при НЕУДАЧНОМ выполнении этой команды
        /// </summary>
        public string? ErrorReplyPath { get; set; }
        /// <summary>
        /// Указывает, что результат выполнения команды должен был отправлен этому пользователю в личные сообщения
        /// </summary>
        public bool ReplyPrivateMessages { get; set; } = false;
        /// <summary>
        /// Разрешает или запрещает боту выполнять команду, 
        /// если сообщение начинается с команды (ключевого слова), но имеет текст после
        /// </summary>
        public bool IgnoreMessageText { get; set; } = false;
        /// <summary>
        /// Список алиасов для этой команды
        /// Чтобы избежать создания нескольких одинаковых команд, 
        /// каждая команда может иметь Алиас (альтернативное имя), по которой ее можно вызвать 
        /// </summary>
        public List<string> AlternativeNames { get; set; } = new List<string>();   
        /// <summary>
        /// Действие, которое будет вызвано после запуска первой команды
        /// </summary>
        public BotCommand? RunAfter { get; set; } = null;
        /// <summary>
        /// Список ID пользователей в телеграмм, которые точно НЕ могут выполнить эту команду
        /// </summary>
        public List<long> BannedUserIDs { get; set; } = new List<long>();

        /// <summary>
        /// Проверяет команду перед ее добавлением в список доступных действий 
        /// </summary>
        /// <returns>Успешность проверки команды</returns>
        public virtual bool Verify()
        {
            string errorMessage = string.Empty;
            if (string.IsNullOrEmpty(Name)) errorMessage += "Name: Пустое имя команды" + Environment.NewLine;
            if (UserIDs.Count == 0)
            {
                if (!IsPublic)
                {
                    errorMessage += "Команда не может быть непубличной, при этом не имея списка ролей внутри " +
                        "(IsPublic=false, количество UserIDs = 0)" + Environment.NewLine;
                }
            }
            if (string.IsNullOrEmpty(CommandType)) errorMessage += "CommandType: Неверный тип команды" + Environment.NewLine;
            if (string.IsNullOrEmpty(errorMessage)) return true;// && Enabled;
            Storage.Logger?.Logger.Warning($"Команда {(string.IsNullOrEmpty(Name) ? "(пустое имя команды)" : $"{Name}")} не прошла первичную верицикацию: {errorMessage}");
            Console.WriteLine($"Команда {(string.IsNullOrEmpty(Name) ? "(пустое имя команды)" : $"{Name}")} не прошла первичную верицикацию: {errorMessage}");
            Enabled = false;
            return Enabled;
        }

        /// <summary>
        /// Функция для вызова выбранной команды
        /// </summary>
        /// <returns>Кортеж: true - если инициализация прошла успешно, *string - сообщение об ошибке</returns>
        public async virtual Task ExecuteCommand(ITelegramBotClient client, Telegram.Bot.Types.Update update, CancellationToken token)
        {
            await Task.Delay(0);
            throw new Exception($"Ты ебанат вызывать функцию в классе-родителе? ({Name})");
        }
        public static async Task<bool> SendMessage(string message, bool replyPrivateMessages, ITelegramBotClient client, Telegram.Bot.Types.Update update, CancellationToken token)
        {
            var text_panel = new Panel($"[wheat1]{message}[/]");
            text_panel.Header = new PanelHeader($"|     [grey100]Результат выполнения команды [/][lightgreen]{(string.IsNullOrEmpty(update.Message?.Text) ? "(пустое сообщение)" : update.Message.Text)}[/]     |");
            text_panel.Padding = new Padding(2, 0);
            //text_panel.PadRight(Console.BufferWidth / 2);
            //text_panel.Width = Console.BufferWidth / 2;
            text_panel.BorderColor(color: Spectre.Console.Color.LightGreen);
            AnsiConsole.Write(text_panel);
            if (message.Length > 4096)
            {
                Storage.Logger?.Logger.Error($"Произошла ошибка при отправке слишком большого текста ({message.Length})!");
                return false;
            }
            try
            {
                if (update.Message == null || update.Message.From == null) { Storage.Logger?.Logger.Error($"Сообщение не может быть отправлено, т.к. параметр update.Message.From пуст"); ; return false; }
                if (replyPrivateMessages)
                {
                    await client.SendMessage(update.Message.From.Id,
                        message,
                        cancellationToken: token,
                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);
                    Storage.Logger?.Logger.Information($"Сообщение отправлено ПОЛЬЗОВАТЕЛЮ {update.Message.From.Username} ({update.Message.From.Id})");
                }
                else
                {
                    await client.SendMessage(update.Message.Chat.Id,
                        message,
                        replyParameters: update.Message.MessageId,
                        cancellationToken: token,
                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);
                    Storage.Logger?.Logger.Information($"Сообщение отправлено В ПУБЛИЧНЫЙ ЧАТ {update.Message.Chat.Username} ({update.Message.Chat.Id})");
                }
            }
            catch (Exception e)
            {
                Storage.Logger?.Logger.Error($"Произошла ошибка при выполнении команды:\n{e.Message}");
                return false;
            }
            return true;
        }
    }
}
