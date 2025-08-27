using Microsoft.AspNetCore.Server.HttpSys;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UITGBot.Logging;
using Spectre.Console;
using System.Globalization;

namespace UITGBot.Core.Messaging
{
    public class ChatActivity
    {
        /// <summary>
        /// Эвент, который срабатывает каждый раз, когда в чат кто-то пишет (текстом)
        /// </summary>
        public event Action<Telegram.Bot.Types.Message>? MessageReceived;
        /// <summary>
        /// Уникальный ID чата
        /// </summary>
        public Guid chatUniqID { get; set; }
        /// <summary>
        /// Заголовок чата, который виден в админ-панели
        /// </summary>
        public string chatTitle { get; set; } = "no-title";
        /// <summary>
        /// Ссылка на чат в Телеграм
        /// </summary>
        public Telegram.Bot.Types.Chat CurrentChat { get; set; } = new Telegram.Bot.Types.Chat();
        /// <summary>
        /// Список пользователей в этом чате
        /// </summary>
        public List<Telegram.Bot.Types.User> Users { get; set; } = new List<Telegram.Bot.Types.User>();
        /// <summary>
        /// История чата
        /// </summary>
        public List<Telegram.Bot.Types.Message> ChatStory { get; set; } = new List<Telegram.Bot.Types.Message>();
        /// <summary>
        /// Определяет, через сколько сообщений будет произведена попытка синхронизации версий чата с тем, что на самом деле есть в CSV-файле
        /// </summary>
        public short _chatFileSyncCounter { get; private set; } = 0; // Я ставлю в 0, чтобы при первом же сообщении в чат производилась попытка синхронизации истории чата из файла 

        public ChatActivity(Telegram.Bot.Types.Chat chat)
        {
            CurrentChat = chat;
            chatTitle = CurrentChat.Title ?? CurrentChat.Username ?? "(неизвестно)";
            chatUniqID = Guid.NewGuid();
            UILogger.AddLog($"Кажется, я изучил новый чат, в который я могу писать: {chat.Id} (@{chat.Username}, {chat.FirstName} {chat.LastName})", "DEBUG");
        }
        /// <summary>
        /// Обновляет историю сообщений в чате (при получении нового сообщения)
        /// </summary>
        /// <param name="message">Целевое сообщенние в чате</param>
        public void UpdateChatStory(Telegram.Bot.Types.Message message)
        {
            // Проверяем, что сообщение не является дубликатом (его уникальный ID не существут в списке)
            if (ChatStory.FirstOrDefault(x => x.Id == message.Id) == null)
            {
                ChatStory.Add(message);
                // 3) Рассылаем всем подписчикам:
                MessageReceived?.Invoke(message);
                WriteChatStory(message);
                if (_chatFileSyncCounter > 0) _chatFileSyncCounter--;
                else
                {
                    TrySyncChatStoryFromFile();
                    _chatFileSyncCounter = 64; // Каждые 64 сообщения бот попытается синхронизироваться с файлом
                }
            }
            // Добавление информации о пользователе, от которого пришло сообщение
            if (message.From == null) return;
            if (Users.FirstOrDefault(x => x.Id == message.From.Id) == null)
            {
                Users.Add(message.From);
                Storage.Statisticks.botUsersKnown++;
                UILogger.AddLog($"Кажется, я знаю нового пользователя: {message.From.Id} (@{message.From.Username}, {message.From.FirstName} {message.From.LastName})", "DEBUG");
            }
        }

        /// <summary>
        /// Обновляет историю записей в сообщение в CSV-файле
        /// </summary>
        /// <param name="message">Целевое сообщение</param>
        public async void WriteChatStory(Telegram.Bot.Types.Message message)
        {
            if (!Storage.SystemSettings.StoreChatActivity) return;
            string targetStorageDir = Path.Combine(Storage.SystemSettings.ChatActivityStoragePath, $"{chatTitle.Replace(" ", string.Empty).Trim()}");
            string filePath = Path.Combine(targetStorageDir, $"{chatTitle.Replace(" ", string.Empty).Trim()}.msgsdb.csv");
            try
            {
                // Создание директории для этого чата, если она не существует
                if (!Directory.Exists(targetStorageDir))
                {
                    Directory.CreateDirectory(targetStorageDir);
                    UILogger.AddLog($"Создана новая директория для чата \"{chatTitle}\": {targetStorageDir}", "DEBUG");
                }
                string header = string.Empty;
                string content = string.Empty;
                foreach (FieldInfo field in typeof(Telegram.Bot.Types.Message).GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static))
                {
                    header += $"{GetStringBetweenCharacters(field.Name, '<', '>')};";
                    object? c = field.GetValue(message);
                    if (c != null)
                    {
                        content += c.ToString() + ';';
                        continue;
                    }
                    else content += "NULL;";
                }
                if (!File.Exists(filePath))
                {
                    // Write header to a file
                    await File.AppendAllTextAsync(filePath, header.Remove(header.Length - 1) + Environment.NewLine);
                }
                // Append record to the file
                await File.AppendAllTextAsync(filePath, content.Remove(content.Length - 1) + Environment.NewLine);
            }
            catch (Exception directoryWriteException)
            {
                UILogger.AddLog($"Ошибка при записи истории чата \"{chatTitle}\": {directoryWriteException.Message}", "ERROR");
            }
        }

        public async void TrySyncChatStoryFromFile()
        {
            string targetStorageDir = Path.Combine(Storage.SystemSettings.ChatActivityStoragePath, $"{chatTitle.Replace(" ", "_").Trim()}");
            string filePath = Path.Combine(targetStorageDir, $"{chatTitle.Replace(" ", "_").Trim()}.msgsdb.csv");
            if (!File.Exists(filePath)) { UILogger.AddLog($"Не удалось найти историю для чата \"{chatTitle}\". Вероятно, она не велась ранее", "DEBUG"); ; return; }

            // Тут должен быть ебический парсер, который:
            // 1. Читает файл с историей (если он есть)
            // 2. Парсит CSV-заголовки и содержимое файла в объект типа List<Telegram.Bot.Types.Message>
            // 3. Обновляет переменную ChatStory новыми данными из CSV-файла
            // 4. Желательно:
            // Каким-то образом делает так, чтобы история чата была проверена и не изменялась.
            // Потому что при изменении CSV-файла, история чата будет не реальной, а той, которая была оттуда подгружена 
            // 5. Логирует все действия в формате DEBUG
        }
        /// <summary>
        /// Выполняет обрезку строки между двумя символами
        /// </summary>
        /// <param name="input">Целевая строка</param>
        /// <param name="charFrom">Символ, с которого начинать обрезку</param>
        /// <param name="charTo">Символ, на которым заканчивать обрезку</param>
        /// <returns>Строка между двумя символами</returns>
        public static string GetStringBetweenCharacters(string input, char charFrom, char charTo)
        {
            int posFrom = input.IndexOf(charFrom);
            if (posFrom != -1) //if found char
            {
                int posTo = input.IndexOf(charTo, posFrom + 1);
                if (posTo != -1) //if found char
                {
                    return input.Substring(posFrom + 1, posTo - posFrom - 1);
                }
            }
            return string.Empty;
        }
    }
}
