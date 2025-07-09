using Microsoft.AspNetCore.Server.HttpSys;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using UITGBot.Logging;

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



        public ChatActivity(Telegram.Bot.Types.Chat chat)
        {
            CurrentChat = chat;
            chatTitle = CurrentChat.Title ?? CurrentChat.Username ?? "(неизвестно)";
            chatUniqID = Guid.NewGuid();
            UILogger.AddLog($"Кажется, я изучил новый чат, в который я могу писать: {chat.Id} (@{chat.Username}, {chat.FirstName} {chat.LastName})", "DEBUG");
        }
        public void UpdateChatStory(Telegram.Bot.Types.Message message)
        {
            // Проверяем, что сообщение не является дубликатом (его уникальный ID не существут в списке)
            if (ChatStory.FirstOrDefault(x => x.Id == message.Id) == null)
            {
                ChatStory.Add(message);
                // 3) Рассылаем всем подписчикам:
                MessageReceived?.Invoke(message);
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
    }
}
