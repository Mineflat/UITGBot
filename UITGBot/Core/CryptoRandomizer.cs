using Microsoft.AspNetCore.Mvc.TagHelpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using UITGBot.Logging;
using UITGBot.TGBot;

namespace UITGBot.Core
{
    internal class CryptoRandomizer
    {
        /// <summary>
        /// Функция для получения произвольного числового значения на основе криптографически-стойкой функции
        /// </summary>
        /// <param name="minValue">Минимальное значение числа</param>
        /// <param name="maxValue">Максимальное значение числа</param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static int GetRandom(int minValue, int maxValue)
        {
            if (minValue > maxValue)
                throw new ArgumentOutOfRangeException(nameof(minValue), "Минимальное значение должно быть меньше или равно максимальному значению!");

            if (minValue == maxValue)
                return minValue; // Диапазон содержит одно значение

            // Разница между maxValue и minValue
            long range = (long)maxValue - minValue + 1;

            // Байтовый массив для хранения случайных данных
            byte[] randomBytes = new byte[4]; // Int32 занимает 4 байта
            using (var rng = RandomNumberGenerator.Create())
            {
                int randomValue;
                do
                {
                    rng.GetBytes(randomBytes); // Заполняем массив случайными байтами
                    randomValue = BitConverter.ToInt32(randomBytes, 0) & int.MaxValue; // Преобразуем в положительное число
                }
                while (randomValue >= range * (int.MaxValue / range)); // Исключаем сдвиги диапазона

                return (int)(randomValue % range + minValue); // Преобразуем в диапазон
            }
        }
        /// <summary>
        /// Пройдется по директории и всем вложенным в нее директориям. 
        /// Вытащит 1 конкретный произвольный путь до файла по его маске
        /// </summary>
        /// <param name="dirPath">Директория, с которой начинать поиск</param>
        /// <param name="extentions">Расширения файлов</param>
        public static (bool success, string errorMessage) GetRandomFileInPath(string dirPath, string[] extentions)
        {
            if (extentions.Length == 0) return (false, "Не удалось выбрать произвольный файл: пустой массив расширений файлов");
            if (!Directory.Exists(dirPath)) return (false, "Не удалось выбрать произвольный файл: директория не сущесутвует");
            try
            {
                var selectedFiles = extentions
                    .SelectMany(ext => Directory.GetFiles(dirPath, ext, SearchOption.AllDirectories))
                    .ToArray();
                if (selectedFiles.Length == 0)
                    return (false, $"Не удалось выбрать произвольный файл: " +
                        $"в директории {dirPath} нет ни одного файла с указанными " +
                        $"расширениями {extentions.Length}");
                return (true, selectedFiles[GetRandom(0, selectedFiles.Length - 1)]);
            }
            catch (Exception directoryLookupException)
            {
                return (false, $"Не удалось выбрать произвольный файл: {directoryLookupException.Message}");
            }
        }
        /// <summary>
        /// Вернет 1 элемент из произвольного JSON-массива строк. Читает массив из файла на диске.
        /// </summary>
        /// <param name="fullPath">Путь к JSON-файлу с заготовленным текстом</param>
        /*
            public static (bool success, string errorMessage) GetRandomText(string fullPath)
            {
                if (!System.IO.File.Exists(fullPath)) return (false, $"Не удалось выбрать произвольную строку в файле {fullPath}: файл не сущесутвует");
                try
                {
                    string fullText = System.IO.File.ReadAllText(fullPath);
                    List<string>? lines = System.Text.Json.JsonSerializer.Deserialize<List<string>>(fullText);
                    if (lines == null || lines.Count == 0) return (false, $"Не удалось выбрать произвольную строку в файле {fullPath}: файл пуст");
                    return (true, lines[GetRandom(0, lines.Count - 1)]);
                }
                catch (Exception directoryLookupException)
                {
                    return (false, $"Не удалось выбрать произвольную строку в файле {fullPath}: {directoryLookupException.Message}");
                }
            }
        */

        /// <summary>
        /// Выбирает произвольный ответ для отправки в чат вместе с командой
        /// </summary>
        /// <param name="cmd">Конечная команда</param>
        /// <param name="success">Определяет формат ответа: негативная (команда не выполнена) или позитивный (команда выполнена)</param>
        /// <returns></returns>
        public static string GetRandomReply(BotCommand cmd, bool success = true)
        {
            // Код чата ГПТ
            string? fixedReply = success ? cmd.FixedReply : cmd.FixedErrorReply;
            if (!string.IsNullOrEmpty(fixedReply))
                return fixedReply;
            string? replyPath = GetExistingFilePath(
                success ? cmd.SuccessReplyPath : cmd.ErrorReplyPath,
                success ? Storage.SystemSettings.SuccessReplyPath : Storage.SystemSettings.ErrorReplyPath
                );

            if (replyPath != null)
            {
                try
                {
                    string text = File.ReadAllText(replyPath);
                    List<string>? variations = JsonConvert.DeserializeObject<List<string>>(text);
                    //if (variations is { Count: > 0 }) variations = variations.Where(v => !string.IsNullOrWhiteSpace(v)).ToList();
                    if (variations?.Count > 0) // Проверяем, остались ли варианты после фильтрации
                        return variations[GetRandom(0, variations.Count - 1)];
                }
                catch (Exception ex)
                {
                    UILogger.AddLog($"Ошибка при загрузке файла {replyPath}: {ex.Message}", "ERROR");
                }
            }

            string errorMsg = $"Не удалось найти ни одного словаря или фиксированной строки ответа для команды `{cmd.Name}`, поэтому команда будет отключена\n";
            UILogger.AddLog(errorMsg, "ERROR");
            cmd.Enabled = false;
            return errorMsg;

            // Мой код... Зато честно
            /*
            string filepath = string.Empty;
            switch (success)
            {
                case false:
                    if (!string.IsNullOrEmpty(cmd.FixedErrorReply)) return cmd.FixedErrorReply;
                    if (File.Exists(cmd.ErrorReplyPath)) filepath = cmd.ErrorReplyPath;
                    if (File.Exists(Storage.SystemSettings.ErrorReplyPath)) filepath = Storage.SystemSettings.ErrorReplyPath;
                    if (!string.IsNullOrEmpty(filepath))
                    {
                        string text = File.ReadAllText(filepath);
                        List<string>? variations = JsonConvert.DeserializeObject<List<string>>(text);
                        if (variations != null && variations.Count > 0)
                            return variations[GetRandom(0, variations.Count - 1)];
                    }
                    Storage.Logger?.Logger.Error($"Не удалось найти ни одного словаря или фиксированной строки ответа для команды {cmd.Name}. Команда будет отключена");
                    cmd.Enabled = false;
                    return $"Не удалось найти ни одного словаря или фиксированной строки ответа для команды {cmd.Name}. Команда будет отключена"; 
                case true:
                    if (!string.IsNullOrEmpty(cmd.FixedReply)) return cmd.FixedReply;
                    if (File.Exists(cmd.SuccessReplyPath)) filepath = cmd.SuccessReplyPath;
                    if (File.Exists(Storage.SystemSettings.SuccessReplyPath)) filepath = Storage.SystemSettings.SuccessReplyPath;
                    if (!string.IsNullOrEmpty(filepath))
                    {
                        string text = File.ReadAllText(filepath);
                        List<string>? variations = JsonConvert.DeserializeObject<List<string>>(text);
                        if (variations != null && variations.Count > 0)
                            return variations[GetRandom(0, variations.Count - 1)];
                    }
                    Storage.Logger?.Logger.Error($"Не удалось найти ни одного словаря или фиксированной строки ответа для команды {cmd.Name}. Команда будет отключена");
                    cmd.Enabled = false;
                    return $"Не удалось найти ни одного словаря или фиксированной строки ответа для команды {cmd.Name}. Команда будет отключена";
                }
             */
        }
        /// <summary>
        /// Возвращает путь к первому существующему файлу в ФС
        /// </summary>
        /// <param name="paths">Список путей в файловой системе</param>
        /// <returns>Путь к первому существующему файлу в ФС</returns>
        private static string? GetExistingFilePath(params string?[] paths)
        {
            return paths.FirstOrDefault(path => !string.IsNullOrEmpty(path) && File.Exists(path));
        }
    }
}
