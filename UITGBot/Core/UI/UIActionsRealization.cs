using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UITGBot.Logging;
using UITGBot.TGBot;

namespace UITGBot.Core.UI
{
    internal static class UIActionsRealization
    {
        public static void SetupActions()
        {

        }
        public static void OpenBotChat()
        {

        }
        public static void RestartBot()
        {
            UILogger.AddLog($"Инициализирован перезапуск бота @{TGBotClient.BotName}", "WARNING");
            Storage.SetupOK = false;
            Storage.botClient?.InitializeBot();
            UILogger.AddLog($"Бот @{TGBotClient.BotName} перезапущен");
        }
    }
}
