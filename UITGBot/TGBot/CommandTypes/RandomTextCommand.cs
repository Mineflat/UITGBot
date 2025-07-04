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
    internal class RandomTextCommand : BotCommand
    {
        /// <summary>
        /// Путь к файлу c JSON-массивом, откуда необходимо выбрать произвольную строку 
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
                UILogger.AddLog($"Команда {Name} не может быть применена, т.к. файл \"{FilePath}\" не существует. Команда отключена", "ERROR");
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
                string fullText = await File.ReadAllTextAsync(FilePath);
                List<string>? lines = System.Text.Json.JsonSerializer.Deserialize<List<string>>(fullText);
                if (lines == null || lines.Count == 0) throw new Exception($"Не удалось выбрать произвольную строку в файле {FilePath}: файл пуст");
                string reply = lines[CryptoRandomizer.GetRandom(0, lines.Count - 1)];
                if (string.IsNullOrEmpty(reply)) throw new Exception("В указанном файле отсутствует текст");
                if (reply.Length > 4096) throw new Exception("Текст в указанном файле имеет длину более 4096 символов (ограничение Телеграмм)");
                await BotCommand.SendMessage(reply, this.ReplyPrivateMessages, client, update, token);
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
                await BotCommand.SendMessage($"Команда {this.Name} не может быть выполнена:\n{e.Message}", this.ReplyPrivateMessages, client, update, token);
            }
        }
    }
}
