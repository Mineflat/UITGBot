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
            string logString = $"[{severity}][{DateTime.Now.ToString("dd.MM.yyyy hh:mm:ss")}]: {message}";
            Storage.LogBuffer.Add(logString);
            switch (severity.Trim().ToUpper())
            {
                case "INFORMATION":
                    Storage.Logger?.Logger.Information(message);
                    break;
                case "WARNING":
                    Storage.Logger?.Logger.Warning(message);
                    break;
                case "ERROR":
                    Storage.Logger?.Logger.Error(message);
                    break;
                case "FATAL":
                    Storage.Logger?.Logger.Fatal(message);
                    Program.OnPanic();
                    break;
                case "VERBOSE":
                    Storage.Logger?.Logger.Verbose(message);
                    break;
                default:
                    Storage.Logger?.Logger.Debug(message);
                    break;
            }
            if (Storage.LogBuffer.Count > 250) Storage.LogBuffer = Storage.LogBuffer.TakeLast(250).ToList<string>();
        }
        /// <summary>
        /// Метод для получения какого-то числа логов из буфера
        /// </summary>
        /// <param name="recordCount">Количество получаемых записей</param>
        /// <returns></returns>
        public static List<string> GetLogs(int recordCount = 30) => Storage.LogBuffer.TakeLast(recordCount).ToList<string>();
    }
}
