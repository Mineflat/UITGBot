using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UITGBot.Core;

namespace UITGBot.Logging
{
    internal static class UILogger
    {
        private static bool _WriteLogsToConsole = false;
        public static void InitUILogger(bool writeConsole = false)
        {
            _WriteLogsToConsole = writeConsole;
            Storage.Logger = new LogProvider(writeConsole);
            if (writeConsole) AddLog("Система логирования не меняла режим изменения логов. Сообщения дублируются в консоль", "DEBUG");
            else AddLog("Система логирования перешла в тихий режим (только файл и память)", "DEBUG");
        }
        public static void AddLog(string message, string severity = "INFORMATION")
        {
            severity = severity.ToUpper();
            //string logString = $"{severity} {message.Replace("[", "[[").Replace("]", "]]")}";
            string logString = $"{severity} {message}";
            logString = Markup.Escape(logString);
            switch (severity.Trim().ToUpper())
            {
                case "INFORMATION":
                    logString = logString.Replace($"{severity}", $"[yellow][[{severity}]][[{DateTime.Now.ToString("dd.MM.yyyy hh:mm:ss")}]]:[/]");
                    Storage.Logger?.Logger.Information(message);
                    Storage.LogBuffer.Add(logString); 
                    break;
                case "WARNING":
                    logString = logString.Replace($"{severity}", $"[darkorange][[{severity}]][[{DateTime.Now.ToString("dd.MM.yyyy hh:mm:ss")}]]:[/]");
                    Storage.Logger?.Logger.Warning(message);
                    Storage.LogBuffer.Add(logString); 
                    break;
                case "ERROR":
                    logString = logString.Replace($"{severity}", $"[red1][[{severity}]][[{DateTime.Now.ToString("dd.MM.yyyy hh:mm:ss")}]]:[/]");
                    Storage.Logger?.Logger.Error(message);
                    Storage.LogBuffer.Add(logString); 
                    break;
                case "FATAL":
                    logString = logString.Replace($"{severity}", $"[darkred][[{severity}]][[{DateTime.Now.ToString("dd.MM.yyyy hh:mm:ss")}]]:[/]");
                    Storage.Logger?.Logger.Fatal(message);
                    Program.OnPanic($"Критическая ошибка: {message}");
                    Storage.LogBuffer.Add(logString); 
                    break;
                case "MESSAGE":
                case "VERBOSE":
                    logString = logString.Replace($"{severity}", $"[skyblue3][[{severity}]][[{DateTime.Now.ToString("dd.MM.yyyy hh:mm:ss")}]]:[/]");
                    Storage.Logger?.Logger.Information($"[{severity}]: {message}");
                    Storage.Logger?.Logger.Verbose(message);
                    Storage.LogBuffer.Add(logString);
                    break;
                case "EXECUTION RESULT":
                    logString = logString.Replace($"{severity}", $"[green1][[{severity}]][[{DateTime.Now.ToString("dd.MM.yyyy hh:mm:ss")}]]:[/]");
                    Storage.Logger?.Logger.Information(message);
                    Storage.LogBuffer.Add(logString); 
                    break;
                default:
                    if (Storage.SystemSettings.DebugMode)
                    {
                        logString = logString.Replace($"{severity}", $"[grey37][[{severity}]][[{DateTime.Now.ToString("dd.MM.yyyy hh:mm:ss")}]]:[/]");
                        Storage.Logger?.Logger.Debug(message);
                        Storage.LogBuffer.Add(logString);
                    }
                    return;
            }
            if (_WriteLogsToConsole) Console.WriteLine(logString);
            if (Storage.SetupOK) Core.UIRenderer.UpdateMainMenu();
            if (Storage.LogBuffer.Count > 250) Storage.LogBuffer = Storage.LogBuffer.TakeLast(250).ToList<string>();
        }

        /// <summary>
        /// Метод для получения какого-то числа логов из буфера
        /// </summary>
        /// <param name="recordCount">Количество получаемых записей</param>
        /// <returns></returns>
        public static List<string> GetLogs(int recordCount = 30)
        {
            var result = new List<string>();
            int usedLines = 0;

            // идём с конца буфера, чтобы взять последние записи
            for (int i = Storage.LogBuffer.Count - 1; i >= 0; i--)
            {
                var entry = Storage.LogBuffer[i];
                // считаем, во сколько физических строк развернётся entry
                int linesInEntry = entry.Count(ch => ch == '\n') + 1;

                // если добавление этого entry превысит лимит — выходим
                if (usedLines + linesInEntry > recordCount)
                    break;

                result.Add(entry);
                usedLines += linesInEntry;
            }

            // сейчас result в обратном порядке — разворачиваем
            result.Reverse();
            return result;
        }
    }
}
