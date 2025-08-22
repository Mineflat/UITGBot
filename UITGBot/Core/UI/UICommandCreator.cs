using Newtonsoft.Json;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using UITGBot.Logging;

namespace UITGBot.Core.UI
{
    internal static class UICommandCreator
    {

        public static (UITGBot.TGBot.BotCommand? result, bool success) GetCommand()
        {
            Console.Clear();
            Console.CursorVisible = false;
            // Пункты меню
            List<string> options = new List<string>();
            options.Add("Создать новую команду");
            options.AddRange(Storage.BotCommands
                .Select(c => c.Name.Replace("[", "[[").Replace("]", "]]"))
                .ToList<string>());
            options.Add("Назад");

            string? chosenOption = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Хотите [green]отредактировать[/] команду или создать [bold]новую[/]?")
                    .PageSize(10)
                    .MoreChoicesText("[grey](Снизу, еще есть варианты)[/]")
                    .AddChoices(options));
            if (string.IsNullOrEmpty(chosenOption) || chosenOption == "Назад") return (null, false);

            UITGBot.TGBot.BotCommand? targetCommand = Storage.BotCommands.FirstOrDefault(x => x.Name == chosenOption);
            return (targetCommand, true);
        }

        /// <summary>
        /// Отрисовывает графический интерфейс для создания команды бота.
        /// </summary>
        public static void CreateCommand()
        {
            UILogger.AddLog($"Администратор открыл UI-Creator. Вероятно, список команд бота изменится", "DEBUG");
            (UITGBot.TGBot.BotCommand? result, bool success) selectionResult = GetCommand();
            switch (selectionResult)
            {
                case (UITGBot.TGBot.BotCommand command, false) when command == null:
                    UILogger.AddLog("Администратор хочет создать новую команду через UI-Creator", "DEBUG");
                    // Пользователь выбрал создание новой команды 
                    ConstructNewCommand(selectionResult.result);
                    return;
                case (UITGBot.TGBot.BotCommand command, true) when command != null:
                    // Пользователь выбрал команду из списка
                    UILogger.AddLog($"Администратор хочет изменить команду {selectionResult.result.Name} через UI-Creator", "DEBUG");
                    UpdateCommand(selectionResult.result);
                    break;
                case (null, false):
                    UILogger.AddLog($"Администратор не менял команд через UI-Creator", "DEBUG");
                    return; // Пользователь выбрал "Назад" или не выбрал ничего
                default:
                    break;
            }
            // Выбор команды из списка доступных
        }

        /// <summary>
        /// Функция для создания новой команды бота
        /// </summary>
        public static void ConstructNewCommand(UITGBot.TGBot.BotCommand command)
        {

        } 
        /// <summary>
        /// Функция для обновления существующей команды бота
        /// </summary>
        public static void UpdateCommand(UITGBot.TGBot.BotCommand command)
        {
            var properties = command.GetType()
               .GetProperties(BindingFlags.Public | BindingFlags.Instance)
               .Where(p => p.CanRead && p.CanWrite)
               .ToList();

            int selectedIndex = 0;
            string status = "[grey]Отключена[/]";
            string[] actions = { "Добавить локально", "Сохранить глобально (в файл)", "Отмена" };

            while (true)
            {
                bool requiresEdit = false;
                PropertyInfo? propToEdit = null;

                AnsiConsole.Live(new Panel("")).Start(ctx =>
                {
                    while (true)
                    {
                        var table = new Table().Border(TableBorder.Rounded).Expand();
                        table.AddColumn("Параметр");
                        table.AddColumn("Значение");

                        for (int i = 0; i < properties.Count; i++)
                        {
                            var prop = properties[i];
                            string name = Markup.Escape(prop.Name);
                            string val = Markup.Escape(prop.GetValue(command)?.ToString() ?? "null");

                            string rowStyle = i == selectedIndex ? "[yellow]" : "[grey]";
                            table.AddRow($"{rowStyle}{name}[/]", $"{rowStyle}{val}[/]");
                        }

                        // Кнопки
                        var buttonTable = new Table().HideHeaders().Expand();
                        buttonTable.AddColumn("A");
                        for (int i = 0; i < actions.Length; i++)
                        {
                            int actionIndex = i + properties.Count;
                            string label = (selectedIndex == actionIndex) ? $"[reverse]{actions[i]}[/]" : actions[i];
                            buttonTable.AddRow(label);
                        }

                        // Макет на 100% консоли
                        var layout = new Layout()
                            .SplitRows(
                                new Layout("header").Size(3),
                                new Layout("content"),
                                new Layout("buttons").Size(5),
                                new Layout("footer").Size(3)
                            );

                        layout["header"].Update(new Panel(new Markup($"[bold]{command.GetType().Name}[/]\nСтатус: {status}")).Expand());
                        layout["content"].Update(table);
                        layout["buttons"].Update(buttonTable);
                        layout["footer"].Update(new Panel("Справка: стрелки — перемещение, Enter — редактировать, Esc — выход").Expand());

                        ctx.UpdateTarget(layout);

                        var key = Console.ReadKey(true);
                        int total = properties.Count + actions.Length;

                        if (key.Key == ConsoleKey.DownArrow)
                            selectedIndex = (selectedIndex + 1) % total;
                        else if (key.Key == ConsoleKey.UpArrow)
                            selectedIndex = (selectedIndex - 1 + total) % total;
                        else if (key.Key == ConsoleKey.Escape)
                            return;
                        else if (key.Key == ConsoleKey.Enter)
                        {
                            if (selectedIndex < properties.Count)
                            {
                                var prop = properties[selectedIndex];
                                if (prop.PropertyType == typeof(bool))
                                {
                                    var current = (bool)(prop.GetValue(command) ?? false);
                                    prop.SetValue(command, !current);

                                    // очистка экрана после изменения
                                    AnsiConsole.Clear();
                                }
                                else if (prop.PropertyType == typeof(string))
                                {
                                    requiresEdit = true;
                                    propToEdit = prop;
                                    return; // выйти из Live, чтобы показать Ask
                                }
                            }
                            else
                            {
                                int actionIndex = selectedIndex - properties.Count;
                                switch (actionIndex)
                                {
                                    case 0:
                                        AnsiConsole.MarkupLine("[green]Добавлено локально[/]");
                                        return;
                                    case 1:
                                        AnsiConsole.MarkupLine("[green]Сохранено глобально в файл[/]");
                                        return;
                                    case 2:
                                        AnsiConsole.MarkupLine("[red]Отмена[/]");
                                        return;
                                }
                            }
                        }
                    }
                });

                if (requiresEdit && propToEdit != null)
                {
                    // Выход из Live, очистка экрана
                    AnsiConsole.Clear();

                    string newValue = AnsiConsole.Ask<string>($"Введите значение для {propToEdit.Name}:");
                    propToEdit.SetValue(command, newValue);

                    // Очистка перед перерисовкой
                    AnsiConsole.Clear();
                }
                else
                {
                    break; // Завершение редактирования
                }
            }
        }
    }
}
