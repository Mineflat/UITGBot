using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Telegram.Bots.Types;

namespace UITGBot.Logging
{
    internal class UpdateHandleResult
    {
        public string ErrorMessage { get; set; } = string.Empty;
        public string ReplyMessage { get; set; } = string.Empty;
        public bool HasErrors = false;
        public UpdateHandleResult(string errorMessage, string replyMessage, bool hasErros = false, TGBot.BotCommand? selectedCommand = null)
        {
            ErrorMessage = errorMessage;
            HasErrors = hasErros;
            SelectedCommand = selectedCommand;
            ReplyMessage = replyMessage;
        }
        public TGBot.BotCommand? SelectedCommand { get; set; } = null;
    }
}
