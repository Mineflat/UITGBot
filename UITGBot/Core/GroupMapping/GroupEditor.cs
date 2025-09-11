using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UITGBot.Core.GroupMapping
{
    internal class GroupEditor
    {
        public static void Edit()
        {
            if (string.IsNullOrEmpty(Storage.SystemSettings.GroupConfigurationFilePath) || !File.Exists(Storage.SystemSettings.GroupConfigurationFilePath))
            {
                string warningMessage = "Вы не указали путь к группам в конфигурационном файле " +
                    "(параметр [underline]GroupConfigurationFilePath[/]), " +
                    "поэтому я не могу дать вам его отредактировать сейчас.\n" +
                    "Однако, я могу создать его для вас. Что вы предпочтете?";
                Console.Clear();
                var confirmation = AnsiConsole.Prompt(
                new ConfirmationPrompt(warningMessage));
                // Echo the confirmation back to the terminal
                Console.WriteLine(confirmation ? "Confirmed" : "Declined");
            }
        }
    }
}
