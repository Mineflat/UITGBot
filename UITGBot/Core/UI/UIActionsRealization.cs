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
using Terminal.Gui;

namespace UITGBot.Core.UI
{
    internal static class UIActionsRealization
    {
        private static Layout _OptionLayout = new Layout();
        private static int editSelectedCommand = 0;
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
                        if (editSelectedCommand - 1 < 0) editSelectedCommand = Storage.BotCommands.Count - 1;
                        else editSelectedCommand = editSelectedCommand - 1;
                        break;
                    case ConsoleKey.S:
                    case ConsoleKey.DownArrow:
                        if (editSelectedCommand + 1 >= Storage.BotCommands.Count) editSelectedCommand = 0;
                        else editSelectedCommand = editSelectedCommand + 1;
                        break;
                    case ConsoleKey.Enter:
                        string? editedText = TerminalEditor.Edit(JsonConvert.SerializeObject(
                            Storage.BotCommands[editSelectedCommand],
                            Formatting.Indented));
                        if (string.IsNullOrEmpty(editedText)) break;
                        var settings = new JsonSerializerSettings
                        {
                            Converters = { new BotCommandConverter() },
                            Formatting = Formatting.Indented
                        };
                        try
                        {
                            BotCommand? newCommand = JsonConvert.DeserializeObject<BotCommand>(editedText, settings);
                            if (newCommand != null)
                            {
                                if (!newCommand.Verify())
                                {
                                    UILogger.AddLog($"Не удалось применить изменения для команды \"{newCommand.Name}\" - команда не прошла верификацию", "WARNING");
                                }
                                else
                                {
                                    Storage.BotCommands[editSelectedCommand] = newCommand;
                                    UILogger.AddLog($"Команда \"{newCommand.Name}\" успешно изменена администратором");
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            UILogger.AddLog($"Не удалось применить изменения для команды \"{Storage.BotCommands[editSelectedCommand].Name}\": {e.Message}", "ERROR");
                        }
                        break;
                    // Сохранение изменений в файл
                    case ConsoleKey.F2:
                        // Добавить возможность сказать "нет" перед сохранением действий
                        UILogger.AddLog($"Администратор хочет изменить список действий");
                        try
                        {
                            string newActionList = JsonConvert.SerializeObject(Storage.BotCommands);
                            File.WriteAllText(Storage.SystemSettings.ActionsPath, newActionList);
                            UILogger.AddLog($"Администратор изменил список действий в файле {Storage.SystemSettings.ActionsPath}", "WARNING");
                        }
                        catch (Exception e)
                        {
                            UILogger.AddLog($"Ошибка при сохранении нового списка действий в файл:\n {e.Message}", "ERROR");
                        }
                        return;
                    // Создание нового действия (по шаблону)
                    case ConsoleKey.F5:
                        break;
                    case ConsoleKey.Escape:
                        editSelectedCommand = 0;
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
                .BorderColor(Spectre.Console.Color.LightSkyBlue1)
                .Expand();

            // B) Меню действий
            var actionsTable = new Table()
                .HideHeaders()
                .Border(TableBorder.Minimal)
                .BorderColor(Spectre.Console.Color.PaleTurquoise1)
                .Expand();
            actionsTable.AddColumn(string.Empty);
            for (int i = 0; i < Storage.BotCommands.Count; i++)
            {
                var cmd = Storage.BotCommands[i];
                if (i == editSelectedCommand)
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
                .BorderColor(Spectre.Console.Color.PaleTurquoise1)
                .Expand();

            // C) Название выбранного действия
            Panel infoPanel = (editSelectedCommand >= 0 && editSelectedCommand < Storage.BotCommands.Count)
                ? new Panel($"Настройки для действия [DeepSkyBlue3_1]{Storage.BotCommands[editSelectedCommand].Name}[/]")
                    .Border(BoxBorder.Rounded)
                    .BorderColor(Spectre.Console.Color.Yellow)
                    .Expand()
                : new Panel("[grey]Нет выбранного действия[/]")
                    .Border(BoxBorder.Rounded)
                    .BorderColor(Spectre.Console.Color.Yellow)
                    .Expand();

            // D) JSON-конфиг со синтаксической подсветкой
            Panel configPanel;
            if (editSelectedCommand >= 0 && editSelectedCommand < Storage.BotCommands.Count)
            {
                // Сериализуем в красиво отформатированный JSON
                string rawJson = JsonConvert.SerializeObject(
                    Storage.BotCommands[editSelectedCommand],
                    Formatting.None);

                // Создаём JsonText — он сам подсветит скобки, строки, числа и т.д.
                var jsonText = new JsonText(rawJson)
                    .BracesColor(Spectre.Console.Color.NavajoWhite1)
                    .BracketColor(Spectre.Console.Color.NavajoWhite3)
                    .StringColor(Spectre.Console.Color.SeaGreen1_1)
                    .NumberColor(Spectre.Console.Color.DeepSkyBlue3_1)
                    .BooleanColor(Spectre.Console.Color.LightPink1)
                    .NullColor(Spectre.Console.Color.LightSteelBlue1);

                configPanel = new Panel(jsonText)
                    .Collapse()               // свернуть, если JSON слишком большой
                    .Border(BoxBorder.Rounded)
                    .BorderColor(Spectre.Console.Color.Yellow)
                    .Expand();
            }
            else
            {
                configPanel = new Panel(string.Empty)
                    .Border(BoxBorder.Rounded)
                    .BorderColor(Spectre.Console.Color.Yellow)
                    .Expand();
            }

            // E) Футер
            var footerPanel = new Panel($"[grey]Enter - редактирование; Escape - для выхода; F2 - записать изменения в файл [underline][red1](ПЕРЕЗАПИШЕТ АКТУАЛЬНУЮ КОНФИГУРАЦИЮ ДЕЙСТВИЙ)[/][/]; F5 - создать новое действие. Конфигурационный файл: {Storage.SystemSettings.ActionsPath}[/]")
                .Border(BoxBorder.Rounded)
                .BorderColor(Spectre.Console.Color.Grey)
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
