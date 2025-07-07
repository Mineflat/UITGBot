using Newtonsoft.Json;
using Spectre.Console.Json;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UITGBot.Logging;
using UITGBot.TGBot;
using Polly;

namespace UITGBot.Core.UI
{
    internal static class UIActionsRealization
    {
        private static Layout _OptionLayout = new Layout();
        private static int selectedAction = 0;
        private static int selectedGlobal = 0;
        public static void SetupActions()
        {
            while (true)
            {
                UpdateActions();
                switch (Console.ReadKey(true).Key)
                {
                    case ConsoleKey.W:
                    case ConsoleKey.UpArrow:
                        if (selectedAction - 1 < 0) selectedAction = Storage.BotCommands.Count - 1;
                        else selectedAction = selectedAction - 1;
                        break;
                    case ConsoleKey.S:
                    case ConsoleKey.DownArrow:
                        if (selectedAction + 1 >= Storage.BotCommands.Count) selectedAction = 0;
                        else selectedAction = selectedAction + 1;
                        break;
                    case ConsoleKey.Enter:

                        break;
                    case ConsoleKey.Escape:
                        return;
                }
            }
        }
        private static void UpdateActions()
        {
            Console.Clear();
            Console.CursorVisible = false;

            // 1) Корневой Layout: шапка / тело / футер
            var layout = new Layout("root")
                .SplitRows(
                    new Layout("header") { Size = 3 },
                    new Layout("body") { Ratio = 1 },
                    new Layout("footer") { Size = 3 }
                );

            // 2) Body → две колонки
            layout["body"].SplitColumns(
                new Layout("left") { Ratio = 1 },
                new Layout("right") { Ratio = 3 }
            );

            // 3) Справа → две зоны: название и JSON
            layout["right"].SplitRows(
                new Layout("actionInfo") { Size = 3 },
                new Layout("actionSetupMenu") { Ratio = 1, IsVisible = false },
                new Layout("actionConfig") { Ratio = 1 }
            );

            // A) Header
            var headerPanel = new Panel(
                    $"[bold]Панель управления действиями бота[/] [green1]@{TGBotClient.BotName}[/]")
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.LightSkyBlue1)
                .Expand();

            // B) Меню действий
            var actionsTable = new Table()
                .HideHeaders()
                .Border(TableBorder.Minimal)
                .BorderColor(Color.PaleTurquoise1)
                .Expand();
            actionsTable.AddColumn(string.Empty);
            for (int i = 0; i < Storage.BotCommands.Count; i++)
            {
                var cmd = Storage.BotCommands[i];
                if (i == selectedAction)
                    actionsTable.AddRow($">> [green]{cmd.Name}[/]");
                else
                    actionsTable.AddRow(
                        cmd.Enabled
                          ? cmd.Name
                          : $"[silver]{cmd.Name} (disabled)[/]"
                    );
            }
            var actionsPanel = new Panel(actionsTable)
                .Header("[bold]Список доступных действий[/]")
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.PaleTurquoise1)
                .Expand();

            // C) Название выбранного действия
            Panel infoPanel = (selectedAction >= 0 && selectedAction < Storage.BotCommands.Count)
                ? new Panel($"Настройки для действия [DeepSkyBlue3_1]{Storage.BotCommands[selectedAction].Name}[/]")
                    .Border(BoxBorder.Rounded)
                    .BorderColor(Color.Yellow)
                    .Expand()
                : new Panel("[grey]Нет выбранного действия[/]")
                    .Border(BoxBorder.Rounded)
                    .BorderColor(Color.Yellow)
                    .Expand();

            // D) JSON-конфиг со синтаксической подсветкой
            Panel configPanel;
            if (selectedAction >= 0 && selectedAction < Storage.BotCommands.Count)
            {
                // Сериализуем в красиво отформатированный JSON
                string rawJson = JsonConvert.SerializeObject(
                    Storage.BotCommands[selectedAction],
                    Formatting.None);

                // Создаём JsonText — он сам подсветит скобки, строки, числа и т.д.
                var jsonText = new JsonText(rawJson)
                    .BracesColor(Color.NavajoWhite1)
                    .BracketColor(Color.NavajoWhite3)
                    .StringColor(Color.SeaGreen1_1)
                    .NumberColor(Color.DeepSkyBlue3_1)
                    .BooleanColor(Color.LightPink1)
                    .NullColor(Color.LightSteelBlue1);

                configPanel = new Panel(jsonText)
                    .Collapse()               // свернуть, если JSON слишком большой
                    .Border(BoxBorder.Rounded)
                    .BorderColor(Color.Yellow)
                    .Expand();
            }
            else
            {
                configPanel = new Panel(string.Empty)
                    .Border(BoxBorder.Rounded)
                    .BorderColor(Color.Yellow)
                    .Expand();
            }

            // E) Футер
            var footerPanel = new Panel($"[grey]Enter - для редактирования; Escape - для выхода. Конфигурационный файл: {Storage.SystemSettings.ActionsPath}[/]")
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Grey)
                .Expand();

            // 4) Привязываем всё к Layout
            layout["header"].Update(headerPanel);
            layout["left"].Update(actionsPanel);
            layout["actionInfo"].Update(infoPanel);
            layout["actionConfig"].Update(configPanel);
            layout["footer"].Update(footerPanel);

            // 5) Рендерим и ждём ввода
            _OptionLayout = layout;
            AnsiConsole.Write(_OptionLayout);
        }
        public static void OpenBotChat()
        {
            _OptionLayout = new Layout();


            Console.Clear();
            Console.CursorVisible = false;
            AnsiConsole.Write(_OptionLayout);
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
