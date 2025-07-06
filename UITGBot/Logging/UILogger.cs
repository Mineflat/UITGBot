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
        public static void InitUILogger()
        {
            Storage.Logger = new LogProvider(false);
            Storage.Logger.Logger.Information("Система логирования перешла в тихий режим (только файл и память)");
        }
        public static void AddLog(string message, string severity = "INFORMATION")
        {
            severity = severity.ToUpper();
            //string logString = $"{severity} {message.Replace("[", "[[").Replace("]", "]]")}";
            string logString = $"{severity} {message}";
            switch (severity.Trim().ToUpper())
            {
                case "INFORMATION":
                    logString = logString.Replace($"{severity}", $"[yellow][[{severity}]][[{DateTime.Now.ToString("dd.MM.yyyy hh:mm:ss")}]]:[/]");
                    Storage.Logger?.Logger.Information(message);
                    break;
                case "WARNING":
                    logString = logString.Replace($"{severity}", $"[darkorange][[{severity}]][[{DateTime.Now.ToString("dd.MM.yyyy hh:mm:ss")}]]:[/]");
                    Storage.Logger?.Logger.Warning(message);
                    break;
                case "ERROR":
                    logString = logString.Replace($"{severity}", $"[red1][[{severity}]][[{DateTime.Now.ToString("dd.MM.yyyy hh:mm:ss")}]]:[/]");
                    Storage.Logger?.Logger.Error(message);
                    break;
                case "FATAL":
                    logString = logString.Replace($"{severity}", $"[darkred][[{severity}]][[{DateTime.Now.ToString("dd.MM.yyyy hh:mm:ss")}]]:[/]");
                    Storage.Logger?.Logger.Fatal(message);
                    Program.OnPanic();
                    break;
                case "MESSAGE":
                case "VERBOSE":
                    logString = logString.Replace($"{severity}", $"[skyblue3][[{severity}]][[{DateTime.Now.ToString("dd.MM.yyyy hh:mm:ss")}]]:[/]");
                    Storage.Logger?.Logger.Verbose(message);
                    break;
                default:
                    logString = logString.Replace($"{severity}", $"[grey37][[{severity}]][[{DateTime.Now.ToString("dd.MM.yyyy hh:mm:ss")}]]:[/]");
                    Storage.Logger?.Logger.Debug(message);
                    break;
            }
            Storage.LogBuffer.Add(logString);
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
