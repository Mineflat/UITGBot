using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bots.Types;
using UITGBot.Core;
using UITGBot.Logging;

namespace UITGBot.TGBot.CommandTypes
{
    internal class ScriptCommand : BotCommand
    {
        /// <summary>
        /// Путь к конкретному скрипту на диске, который будет запущен по команде
        /// </summary>
        public required string FilePath { get; set; } = string.Empty;
        /// <summary>
        /// Аргументы, которые будут переданы скрипту. Если заполнено, то AllowArgsFromChat будет игнорироватся
        /// </summary>
        public string? ScriptArgs { get; set; }
        /// <summary>
        /// Разрешает пользователю передавать аргументы в функцию прямо из чата
        /// </summary>
        //public bool AllowArgsFromChat { get; set; } = false;
        /// <summary>
        /// Требует передачи аргументов скрипту. Если аргументов переданно не будет, команда не будет выполнена
        /// </summary>
        //public bool ForceArguments { get; set; } = false;
        /// <summary>
        /// Разрешает отправлять текстовый вывод скрипта в чат
        /// </summary>
        public bool SendTextOutputToChat { get; set; } = true;
        /// <summary>
        /// Позволяет установить скрипту таймаут выполнения в N секунд. 
        /// Если равен 0 или отсутствует, то время выполнения скрипта не ограничено 
        /// </summary>
        public int Timeout { get; set; } = 0;

        /// <summary>
        /// Функция верификации правильности заполнения команды
        /// </summary>
        /// <returns>Успешность верификации команды</returns>
        public override bool Verify()
        {
            if (!base.Verify()) return false;
            if (!System.IO.File.Exists(FilePath))
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
        public override async Task ExecuteCommand(ITelegramBotClient client, Telegram.Bot.Types.Update update, CancellationToken token)
        {
            try
            {
                // Отписаться в чат, что задача успешно поставлена на выполнение
                await BotCommand.SendMessage($"Задача успешно поставлена на выполнение. Таймаут выполнения для этой задачи: {(Timeout <= 0 ? Timeout : "не установлен")}", this.ReplyPrivateMessages, client, update, token);
                var outputBuilder = new StringBuilder();
                var process = new Process()
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = FilePath,
                        Arguments = ScriptArgs,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                    }
                };

                process.OutputDataReceived += (sender, e) => { if (!string.IsNullOrEmpty(e.Data)) outputBuilder.AppendLine(e.Data); };
                process.ErrorDataReceived += (sender, e) => { if (!string.IsNullOrEmpty(e.Data)) outputBuilder.AppendLine(e.Data); };
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                if (Timeout > 0)
                {
                    if (!process.WaitForExit(Timeout * 1000))
                    {
                        process.Kill();
                        outputBuilder.AppendLine("Команда завершена принудительно (таймаут)");
                    }
                }
                else
                {
                    process.WaitForExit();
                }
                string result = outputBuilder.Length > 0 ? outputBuilder.ToString().Trim() : "Команда завершена, но ничего не выводила на экран";

                // Ограничение в 4096 символов
                if (result.Length > 4096)
                    result = result.Substring(0, 4090) + "...";

                var text_panel = new Panel($"[wheat1]{result}[/]");
                text_panel.Header = new PanelHeader($"|     [grey100]Результат выполнения скрипта [/][lightgreen]{(string.IsNullOrEmpty(update.Message?.Text) ? "(пустое сообщение)" : update.Message.Text)}[/]     |");
                text_panel.Padding = new Padding(2, 0);
                //text_panel.PadRight(Console.BufferWidth / 2);
                //text_panel.Width = Console.BufferWidth / 2;
                text_panel.BorderColor(color: Spectre.Console.Color.DarkSlateGray1);
                AnsiConsole.Write(text_panel);

                // Отправка результата в чат
                if (SendTextOutputToChat)
                {
                    await BotCommand.SendMessage($"{result}", this.ReplyPrivateMessages, client, update, token);
                }
                else
                {
                    await BotCommand.SendMessage($"Выполнение команды завершено", this.ReplyPrivateMessages, client, update, token);
                }

                // Выполнение каскадной команды
                if (process.ExitCode != 0) return;
                if (RunAfter == null) return;
                if (RunAfter.Enabled)
                {
                    UILogger.AddLog($"Выполнение КАСКАДНОЙ команды: {Name} => {RunAfter.Name}");
                    await RunAfter.ExecuteCommand(client, update, token);
                }
            }
            catch (Exception e)
            {
                //this.Enabled = false;
                await BotCommand.SendMessage($"Команда {this.Name} не может быть выполнена:\n{e.Message}", this.ReplyPrivateMessages, client, update, token);
            }
        }

        public static async Task ExecuteScript(string scriptPath, string? arguments, int timeoutInSeconds, ITelegramBotClient client, Telegram.Bot.Types.Update update, CancellationToken token, BotCommand targetCommand)
        {
            if (update.Message == null)
            {
                UILogger.AddLog($"Получено неверное обновление: update.Message пуст (я хз как ты получил это сообщение, телеге видимо плохо)", "ERROR");
                return;
            }
            var processStartInfo = new ProcessStartInfo
            {
                FileName = scriptPath,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = new Process { StartInfo = processStartInfo })
            {
                try
                {
                    StringBuilder output = new StringBuilder();
                    StringBuilder error = new StringBuilder();
                    process.OutputDataReceived += (sender, e) =>
                    {
                        if (e.Data != null) output.AppendLine(e.Data);
                    };
                    process.ErrorDataReceived += (sender, e) =>
                    {
                        if (e.Data != null) error.AppendLine(e.Data);
                    };
                    UILogger.AddLog($"Запуск скрипта \"{scriptPath}\"");
                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    string randomReply = CryptoRandomizer.GetRandomReply(targetCommand, true);
                    string replyMessage = randomReply;
                    // Если timeoutInSeconds равен 0, то не задаём таймаут
                    if (timeoutInSeconds == 0)
                    {
                        UILogger.AddLog($"Этот скрипт будет выполняться без таймаута выполнения: \"{scriptPath}\"", "WARNING");
                        await Task.Run(() => process.WaitForExit());
                        UILogger.AddLog($"Скрипт \"{scriptPath}\" закончил свое выполнение");
                        string limitedString = output.ToString().Length > (4096 - randomReply.Length - 10) ? output.ToString()
                            .Substring(0, 4096 - randomReply.Length - 10) : output.ToString();
                        replyMessage = $"{randomReply}:\n```\n{limitedString}\n```";
                    }
                    else
                    {
                        var waitForExitTask = Task.Run(() => process.WaitForExit());
                        if (await Task.WhenAny(Task.Delay(timeoutInSeconds * 1000), waitForExitTask) == waitForExitTask)
                        {
                            UILogger.AddLog($"Скрипт \"{scriptPath}\" закончил свое выполнение");
                            string limitedString = output.ToString().Length > (4096 - randomReply.Length - 10) ? output.ToString()
                                .Substring(0, 4096 - randomReply.Length - 10) : output.ToString();
                            replyMessage = $"{randomReply}:\n```\n{limitedString}\n```";
                        }
                        else
                        {
                            process.Kill(); // Если таймаут истёк, убиваем процесс
                            UILogger.AddLog($"Остановка выполнения скрипта \"{scriptPath}\": скрипт выполняется дольше {timeoutInSeconds} секунд", "WARNING");
                            string userWarning = "Мне пришлось остановить этот скрипт, потому что он выполнялся слишком долго. Однако, возможно, у меня есть часть его вывода:";
                            string limitedString = output.ToString().Length > (4096 - userWarning.Length - 10) ? output.ToString()
                                .Substring(0, 4096 - userWarning.Length - 10) : output.ToString();
                            replyMessage = $"{userWarning}\n```\n{limitedString}\n```";
                            if (targetCommand.ReplyPrivateMessages)
                            {
                                if (update.Message.From == null) return;
                                await client.SendMessage(update.Message.From.Id,
                                    replyMessage,
                                    replyParameters: update.Message.MessageId,
                                    cancellationToken: token,
                                    parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);
                            }
                            else
                            {
                                await client.SendMessage(update.Message.Chat.Id,
                                    replyMessage,
                                    replyParameters: update.Message.MessageId,
                                    cancellationToken: token,
                                    parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);
                            }

                            //string scriptLogs = output.ToString();
                            //string errorLogs =  error.ToString();
                            //string fixedReply = "Этот скрипт был завершен по таймауту";
                            //if ($"{scriptLogs}{errorLogs}".Length < 3000)
                            //{
                            //    fixedReply += ", но мне удалось собрать кое-какие логи:";
                            //}
                            //string limitedString = output.ToString().Length > 4096 - randomReply.Length - 10 ? output.ToString()
                            //    .Substring(0, 4096 - randomReply.Length - 10) : output.ToString();
                            //replyMessage = $"{fixedReply}:\n```\n{limitedString}\n```";

                        }
                    }
                }
                catch (Exception e)
                {
                    UILogger.AddLog($"Ошибка выполнения скрипта \"{scriptPath}\":\n{e.Message}", "ERROR");
                    string limitedString = e.Message.ToString().Length > (4096 - scriptPath.Length - 10) ? e.Message.ToString()
                        .Substring(0, 4096 - $"Ошибка выполнения скрипта \"{scriptPath}\":".Length - 10) : e.Message.ToString();
                    await client.SendMessage(update.Message.Chat.Id,
                        $"Ошибка выполнения скрипта \"{scriptPath}\":\n```\n{limitedString}\n```\n",
                        replyParameters: update.Message.MessageId,
                        cancellationToken: token,
                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);
                }
            }
        }

        //public static async Task<string> ExecuteBashCommand(string command, string args, int timeout)
        //{
        //    var process = new Process
        //    {
        //        StartInfo = new ProcessStartInfo
        //        {
        //            FileName = "/bin/bash",
        //            Arguments = $"-c \"{command} {args}\"",
        //            RedirectStandardOutput = true,
        //            RedirectStandardError = true,
        //            UseShellExecute = false,
        //            CreateNoWindow = true
        //        }
        //    };

        //    var outputBuilder = new StringBuilder();
        //    var errorBuilder = new StringBuilder();

        //    process.OutputDataReceived += (sender, e) => { if (e.Data != null) outputBuilder.AppendLine(e.Data); };
        //    process.ErrorDataReceived += (sender, e) => { if (e.Data != null) errorBuilder.AppendLine(e.Data); };

        //    process.Start();
        //    process.BeginOutputReadLine();
        //    process.BeginErrorReadLine();

        //    bool finished = await Task.Run(() => process.WaitForExit(timeout));

        //    if (!finished)
        //    {
        //        process.Kill();
        //        return $"Process timed out after {timeout}ms.\nOutput:\n{outputBuilder}\nError:\n{errorBuilder}";
        //    }

        //    return outputBuilder.ToString() + errorBuilder.ToString();
        //}
    }
}
