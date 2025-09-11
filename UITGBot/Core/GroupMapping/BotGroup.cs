using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UITGBot.Core.GroupMapping
{
    public class BotGroup
    {
        /// <summary>
        /// Список пользователей, которые входят в эту группу
        /// </summary>
        public List<GroupMember> GroupMembers { get; set; } = new List<GroupMember>();
        /// <summary>
        /// Список команд, которые разрешено выполнять этим пользователям (помимо публичных команд)
        /// </summary>
        public List<string> AllowedCommandNames { get; set; } = new List<string>() { "help", "start" };

    }
}
