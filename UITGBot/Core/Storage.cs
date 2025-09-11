using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using UITGBot.Core.GroupMapping;
using UITGBot.Core.Messaging;
using UITGBot.Logging;
using UITGBot.TGBot;

namespace UITGBot.Core
{
    internal class Storage
    {
#pragma warning disable CS8618 // Поле, не допускающее значения NULL, должно содержать значение, отличное от NULL, при выходе из конструктора. Рассмотрите возможность добавления модификатора "required" или объявления значения, допускающего значение NULL.
        public static Preferences SystemSettings { get; set; }
#pragma warning restore CS8618 // Поле, не допускающее значения NULL, должно содержать значение, отличное от NULL, при выходе из конструктора. Рассмотрите возможность добавления модификатора "required" или объявления значения, допускающего значение NULL.
        public static LogProvider? Logger { get; set; }
        public static string PlaintextTelegramBotToken { get; set; } = string.Empty;
        //public static string PlaintextConnectionString { get; set; } = string.Empty;
        public static ReceiverOptions? _receiverOptions { get; set; }
        public static CancellationTokenSource? _cts { get; set; }
        public static string _configurationPath { get; set; } = string.Empty;
        public static Cryptor? SystemCriptor { get; set; }
        public static TGBotClient? botClient { get; set; }
        public static List<BotCommand> BotCommands { get; set; } = new List<BotCommand>();
        public static List<string> SYSTEM_COMMANDS { get; private set; } = new List<string>
        {
            "перезапуск", // Перезапустит бота
            "конфиги", // Покажет пути из конфигов, которые он использует
            "стата", // Покажет статистику формата:
                     // Сообщений получено: Х
                     // Сообщений отправлено: Х
                     // Сообщений из них команд: Х
                     // доступных команд: Х
            "кто в ролях?" // Покажет, кто у какой команде имеет доступ
        };
        public static List<string> LogBuffer { get; set; } = new List<string>();
        public static bool SetupOK { get; set; } = false;
        public static StatsObject Statisticks { get; set; } = new StatsObject();
        public static List<ChatActivity> CurrenetChats { get; set; } = new List<ChatActivity>();
        /// <summary>
        /// Список групп пользователей, которые создал администратор
        /// </summary>
        public static List<BotGroup> InternalGroups { get; set; } = new List<BotGroup>();
    }
}
