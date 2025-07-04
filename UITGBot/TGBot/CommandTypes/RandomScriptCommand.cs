using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using UITGBot.Core;
using UITGBot.Logging;

namespace UITGBot.TGBot.CommandTypes
{
    internal class RandomScriptCommand : ScriptCommand
    {
        /// <summary>
        /// Путь к ДИРЕКТОРИИ со скриптами на диске, один из которых который будет запущен по команде (слуйчайно)
        /// </summary>
        public required string ScriptDirectory { get; set; } = string.Empty;
        /// <summary>
        /// Функция верификации правильности заполнения команды
        /// </summary>
        /// <returns>Успешность верификации команды</returns>
        public override bool Verify()
        {
            if (!base.Verify()) return false;
            if (!Directory.Exists(ScriptDirectory))
            {
                UILogger.AddLog($"Команда {Name} не может быть применена, т.к. директория \"{ScriptDirectory}\" не существует. Команда отключена", "ERROR");
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
                UILogger.AddLog($"Команда {Name}: Получено неверное обновление: update.Message пуст (я хз как ты получил это сообщение, телеге видимо плохо)", "ERROR");
                return;
            }
            List<string> scripts = GetExecutableFiles(ScriptDirectory);
            if (!scripts.Any()) { UILogger.AddLog($"Команда {Name} не смогла найти ни 1 исполняемого файла в директории \"{ScriptDirectory}\"", "ERROR"); return; }
            string selectedPath = scripts[CryptoRandomizer.GetRandom(0, scripts.Count - 1)];
            ScriptCommand c = new()
            {
                FilePath = selectedPath,
                ErrorReplyPath = this.ErrorReplyPath,
                SuccessReplyPath = this.ErrorReplyPath,
                SendTextOutputToChat = this.SendTextOutputToChat,
                UserIDs = this.UserIDs,
                FixedErrorReply = this.FixedErrorReply,
                FixedReply = this.FixedReply,
                IsPublic = this.IsPublic,
                ReplyPrivateMessages = this.ReplyPrivateMessages,
                Timeout = this.Timeout,
                ScriptArgs = this.ScriptArgs,
                Description = this.Description,
                CommandType = "script",
                Enabled = this.Enabled,
                Name = this.Name,
                RunAfter = this.RunAfter
            };
            if (c.Verify())
            {
                await c.ExecuteCommand(client, update, token);
                UILogger.AddLog($"Команда \"{Name}\" успешно завершена");
            }
            else
            {
                UILogger.AddLog($"Не удалось выполнить команду \"{Name}\": ошибка верификации", "ERROR");
            }
        }
        static List<string> GetExecutableFiles(string directory)
        {
            if (!Directory.Exists(directory)) return new List<string>();
            List<string> executableFiles = new List<string>();
            foreach (var file in Directory.GetFiles(directory, "*", SearchOption.AllDirectories))
            {
                if (IsExecutable(file))
                {
                    executableFiles.Add(file);
                }
            }
            return executableFiles;
        }

        static bool IsExecutable(string filePath)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "stat",
                    Arguments = $"-c %A \"{filePath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            string output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();

            return output.Length > 2 && (output[2] == 'x' || output[5] == 'x' || output[8] == 'x');
        }
    }
}
