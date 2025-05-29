using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace UITGBot.Core
{
    internal class Preferences
    {
        #region Телеграмм-бот
        /// <summary>
        /// Токен бота. Чтобы вызвать команду бота из чата, сообщение должно начинаться с этой строки
        /// </summary>
        [JsonRequired]
        public required string BOT_INIT_TOKEN { get; set; }
        /// <summary>
        /// Зашифрованный токен бота
        /// </summary>
        [JsonRequired]
        public required string BOT_SECRET { get; set; } = string.Empty;
        /// <summary>
        /// ID пользователя-администратора. Только он может вызвать системные команды
        /// </summary>
        [JsonRequired]
        public required long ROOT_ID { get; set; } = 0;
        /// <summary>
        /// Путь к JSON-файлу, описывающему роли. В нему казываются числовые ID пользователей в телеграм и название ролей
        /// </summary>
        //[JsonRequired]
        //public required string RolesPath { get; set; } = string.Empty;
        /// <summary>
        /// Путь к JSON-файлу, описывающему действия бота
        /// </summary>
        [JsonRequired]
        public required string ActionsPath { get; set; } = string.Empty;
        /// <summary>
        /// Разрешить использование бота пользователями без назначенных ролей. В таком случае у действия должна быть прописана роль "Public"
        /// </summary>
        public bool AllowUnsafeUsers { get; set; } = false;
        #endregion

        #region Логирование
        /// <summary>
        /// Путь к конкретному файлу, в который должны быть записаны логи приложения
        /// </summary>
        public string LogDirectory { get; set; } = $"{AppDomain.CurrentDomain.BaseDirectory}";
        /// <summary>
        /// Предельный размер 1 файла лога в Мегабайтах
        /// </summary>
        public int LogFileSizeLimitMB { get; set; } = 100;
        /// <summary>
        /// Количество файлов, сохраняемое после ротации
        /// </summary>
        public int LogRetainedFileCountLimit { get; set; } = 7;
        #endregion

        #region СУБД
        /// <summary>
        /// Зашифрованная строка подключения к СУБД
        /// </summary>
        //[JsonRequired] 
        //public string DB_SECRET { get; set; } = string.Empty;

        #endregion

        #region Ролевка и ответы пользователям
        /// <summary>
        /// Используется для ПРОИЗВОЛЬНОЙ подписи сообщения при УСПЕШНОМ выполнении этой команды
        /// </summary>
        [JsonRequired]
        public required string SuccessReplyPath { get; set; }
        /// <summary>
        /// Используется для ПРОИЗВОЛЬНОЙ подписи сообщения при НЕУДАЧНОМ выполнении этой команды
        /// </summary>
        [JsonRequired]
        public required string ErrorReplyPath { get; set; }
        #endregion
        #region
        #endregion
    }
}
