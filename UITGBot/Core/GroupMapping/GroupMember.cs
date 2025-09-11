using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UITGBot.Core.GroupMapping
{
    public class GroupMember
    {
        /// <summary>
        /// Числовой идентификатор пользователя в Telegram 
        /// </summary>
        public float UserID { get; set; }
        /// <summary>
        /// Имя пользователя в телеграм (@username)
        /// </summary>
        public string Username { get; set; } = "Hidden user";

        #region Статистика по этому пользователю
        //возможно, будет добавлено в будущих релизах
        ///// <summary>
        ///// Количество сообщений, которое пользователь отправил в чат
        ///// </summary>
        //public ulong MessagesCount { get; protected set; } = 0;
        #endregion


    }
}
