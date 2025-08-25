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
using UITGBot.TGBot.CommandTypes;
using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using UITGBot.Core.Messaging;
using System.Collections.Concurrent;
using Spectre.Console.Rendering;
using Color = Spectre.Console.Color;    // <-- для Text, TableColumn и т.п.

namespace UITGBot.Core.UI
{
    internal static class UIActionsRealization
    {
        #region TESTED
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
                        try
                        {
                            var settings = new JsonSerializerSettings
                            {
                                Converters = { new BotCommandConverter() },
                                Formatting = Formatting.Indented
                            };
                            TGBot.BotCommand? newCommand = JsonConvert.DeserializeObject<TGBot.BotCommand>(editedText, settings);
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
                        var saveConfirmation = AnsiConsole.Prompt(
                        new TextPrompt<bool>($"Сохранить изменения в файл {Storage.SystemSettings.ActionsPath}?")
                            .AddChoice(true)
                            .AddChoice(false)
                            .DefaultValue(false)
                            .WithConverter(choice => choice ? "y" : "n"));
                        if (!saveConfirmation) break;
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
                    // Удаление выбранного действия
                    case ConsoleKey.F3:
                        var deletionConfirmation = AnsiConsole.Prompt(
                            new TextPrompt<bool>($"Вы уверены, что хотите удалить действие [green1]{Storage.BotCommands[editSelectedCommand].Name}[/]?")
                                .AddChoice(true)
                                .AddChoice(false)
                                .DefaultValue(false)
                                .WithConverter(choice => choice ? "y" : "n"));
                        if (!deletionConfirmation) break;
                        Storage.BotCommands = Storage.BotCommands.FindAll(x => x.Name != Storage.BotCommands[editSelectedCommand].Name);
                        UILogger.AddLog($"Администратор удалил действие [green1]{Storage.BotCommands[editSelectedCommand].Name}[/]", "WARNING");
                        UILogger.AddLog($"Количество действий изменено: [green1]{Storage.BotCommands.Count}[/]", "DEBUG");
                        break;
                    // Создание нового действия (по шаблону)
                    case ConsoleKey.F5:
                        Console.Clear();
                        Console.CursorVisible = false;
                        var selectedCommandType = AnsiConsole.Prompt(
                        new SelectionPrompt<string>()
                            .Title("[green]Какой тип будет у нового действия?[/]")
                            .PageSize(10)
                            .MoreChoicesText("[grey](Стрелка вверх и вниз меняет тип действия)[/]")
                            .AddChoices(new[] {
                                "Назад",
                                "Фиксированный ответ (simple)",
                                "Полный текст из файла (full_text)",
                                "Отправка [bold]файла[/] по комане (file)",
                                "Отправка [bold]изображения[/] по комане (image)",
                                "Выполнение скрипта (script)",
                                "Отправка произвольного [bold]текста[/] из JSON-файла (random_text)",
                                "Отправка произвольного [bold]файла[/] (random_file)",
                                "Отправка произвольного [bold]изображения[/] (random_image)",
                                "Выполнение произвольного [bold]скрипта[/] из директории (random_script)",
                                "Позволить пользователю загружать файлы в директорию (remote_file)"
                            }));
                        // Осторожно! Switch-case 1 к 1!
                        string targetCommandJSON = string.Empty;
                        try
                        {
                            switch (selectedCommandType)
                            {
                                case "Фиксированный ответ (simple)":
                                    targetCommandJSON = JsonConvert.SerializeObject(new SimpleCommand() { Message = string.Empty, CommandType = "simple" }, Formatting.Indented);
                                    break;
                                case "Полный текст из файла (full_text)":
                                    targetCommandJSON = JsonConvert.SerializeObject(new TextCommand() { FilePath = string.Empty, CommandType = "full_text" }, Formatting.Indented);
                                    break;
                                case "Отправка [bold]файла[/] по комане (file)":
                                    targetCommandJSON = JsonConvert.SerializeObject(new FileCommand() { FilePath = string.Empty, CommandType = "file" }, Formatting.Indented);
                                    break;
                                case "Отправка [bold]изображения[/] по комане (image)":
                                    targetCommandJSON = JsonConvert.SerializeObject(new ImageCommand() { FilePath = string.Empty, CommandType = "image" }, Formatting.Indented);
                                    break;
                                case "Выполнение скрипта (script)":
                                    targetCommandJSON = JsonConvert.SerializeObject(new ScriptCommand() { FilePath = string.Empty, CommandType = "script" }, Formatting.Indented);
                                    break;
                                case "Отправка произвольного [bold]текста[/] из JSON-файла (random_text)":
                                    targetCommandJSON = JsonConvert.SerializeObject(new RandomTextCommand() { FilePath = string.Empty, CommandType = "random_text" }, Formatting.Indented);
                                    break;
                                case "Отправка произвольного [bold]файла[/] (random_file)":
                                    targetCommandJSON = JsonConvert.SerializeObject(new RandomFileCommand() { DirPath = string.Empty, CommandType = "random_file" }, Formatting.Indented);
                                    break;
                                case "Отправка произвольного [bold]изображения[/] (random_image)":
                                    targetCommandJSON = JsonConvert.SerializeObject(new RandomImageCommand() { DirPath = string.Empty, CommandType = "random_image" }, Formatting.Indented);
                                    break;
                                case "Выполнение произвольного [bold]скрипта[/] из директории (random_script)":
                                    targetCommandJSON = JsonConvert.SerializeObject(new RandomScriptCommand() { FilePath = string.Empty, ScriptDirectory = string.Empty, CommandType = "random_script" }, Formatting.Indented);
                                    break;
                                case "Позволить пользователю загружать файлы в директорию (remote_file)":
                                    targetCommandJSON = JsonConvert.SerializeObject(new RemoteFileCommand() { DirPath = string.Empty, CommandType = "remote_file" }, Formatting.Indented);
                                    break;
                                case "Назад": default: return;
                            }
                            if (string.IsNullOrEmpty(targetCommandJSON))
                            {
                                UILogger.AddLog("Не удалось создать новое действие: пустой выбор", "ERROR");
                                return;
                            }

                            editedText = TerminalEditor.Edit(targetCommandJSON);
                            TGBot.BotCommand? newCommand = new TGBot.BotCommand();
                            if (string.IsNullOrEmpty(editedText)) break;
                            try
                            {
                                var settings = new JsonSerializerSettings
                                {
                                    Converters = { new BotCommandConverter() },
                                    Formatting = Formatting.Indented
                                };
                                newCommand = JsonConvert.DeserializeObject<TGBot.BotCommand>(editedText, settings);
                                if (newCommand != null)
                                {
                                    if (!newCommand.Verify())
                                    {
                                        UILogger.AddLog($"Не удалось применить изменения для [underline]новой[/] команды \"{newCommand.Name}\" - команда не прошла верификацию", "WARNING");
                                    }
                                    else
                                    {
                                        var exists = Storage.BotCommands.Any(c => string.Equals(c.Name?.Trim(), newCommand.Name?.Trim(), StringComparison.OrdinalIgnoreCase));
                                        if (exists)
                                        {
                                            UILogger.AddLog($"Невозможно добавить команду [green1]\"{newCommand.Name}\"[/] с типом [underline]{newCommand.CommandType}[/]: команда с таким именем уже существует", "ERROR");
                                        }
                                        else
                                        {
                                            Storage.BotCommands.Add(newCommand);
                                            // Сортировка по имени
                                            Storage.BotCommands.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
                                            UILogger.AddLog($"Успешно добавлена команда [green1]\"{newCommand.Name}\"[/] с типом [underline]{newCommand.CommandType}[/]");
                                        }
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                UILogger.AddLog($"Не удалось применить изменения для команды \"{newCommand?.Name}\": {e.Message}", "ERROR");
                            }
                        }
                        catch (Exception e)
                        {
                            UILogger.AddLog($"Ошибка при создании нового действия: {e.Message}", "ERROR");
                        }
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
                    new Layout("footer") { Size = 4 }
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
                    actionsTable.AddRow($">> [bold][green]{cmd.Name}[/][/] [grey]{cmd.CommandType.ToLower()}[/]");
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
                ? new Panel($"Настройки для действия [bold][green1]{Storage.BotCommands[editSelectedCommand].Name}[/][/] " +
                $"с типом [deepskyblue2]{Storage.BotCommands[editSelectedCommand].CommandType.ToLower()}[/]")
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
            var footerPanel = new Panel($"[silver]Enter[/][grey] - редактирование; Escape - для выхода;[/] " +
                $"[silver]F2[/][grey] - записать изменения в файл [underline][red1](ПЕРЕЗАПИШЕТ АКТУАЛЬНУЮ КОНФИГУРАЦИЮ ДЕЙСТВИЙ)[/][/];[/] " +
                $"[silver]F5[/][grey] - создать новое действие;[/] " +
                $"[silver]F3[/][grey] - удалить выбранное действие[/]\n" +
                $"[silver]Конфигурационный файл:[/] [grey]{Storage.SystemSettings.ActionsPath}[/]")
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

        public static void RestartBot()
        {
            UILogger.AddLog($"Инициализирован перезапуск бота @{TGBotClient.BotName}", "WARNING");
            Storage.SetupOK = false;
            Storage.botClient?.InitializeBot();
            UILogger.AddLog($"Бот @{TGBotClient.BotName} перезапущен");
        }
        #endregion
        /// <summary>
        /// Точка входа: вызывается при клике "OpenBotChat" в меню
        /// </summary>


        // ======== Новый код для полноэкранного чата ========

        /// <summary>
        /// Открывает интерфейс чата на весь экран через Spectre.Console
        /// </summary>
        public static void OpenBotChat()
        {
            var bot = TGBotClient.botClient;
            if (bot == null)
            {
                UILogger.AddLog("Невозможно открыть чат: бот не запущен", "ERROR");
                return;
            }

            var chats = Storage.CurrenetChats;
            if (!chats.Any())
            {
                UILogger.AddLog("Нет чата для открытия", "WARNING");
                return;
            }

            // выбор чата
            var chatChoice = AnsiConsole.Prompt(
                new SelectionPrompt<ChatActivity>()
                    .Title("Выберите чат для открытия")
                    .PageSize(10)
                    .UseConverter(c => $"{c.chatUniqID}: {c.chatTitle}")
                    .AddChoices(chats)
            );
            if (chatChoice == null) return;

            // запуск чат-runner’а
            var runner = new ChatRunner(chatChoice, bot);
            runner.Run();
        }

        /// <summary>Вспомогательный класс для полноэкранного чата</summary>
        internal class ChatRunner
        {
            private readonly ChatActivity _chat;
            private readonly ITelegramBotClient _bot;
            // теперь очередь кортежей, чтобы хранить раздельно время, имя и текст
            private readonly ConcurrentQueue<(string time, string who, string txt)> _log = new();

            public ChatRunner(ChatActivity chatActivity, ITelegramBotClient botClient)
            {
                _chat = chatActivity;
                _bot = botClient;

                // подписываемся на события
                _chat.MessageReceived += Enqueue;
                // прогреваем историю
                foreach (var m in _chat.ChatStory)
                    Enqueue(m);
            }

            private void Enqueue(Message m)
            {
                string time = m.Date.ToLocalTime().ToString("dd.MM.yy HH:mm");
                string who = m.From?.Username ?? m.From?.Id.ToString() ?? "(неизвестный пользователь)";
                string txt = m.Text ?? m.Caption ?? "<media>";
                _log.Enqueue((time, who, txt));
            }

            public void Run()
            {
                Console.Clear();
                Console.CursorVisible = false;

                bool exit = false;
                string inputBuf = "";

                AnsiConsole.Live(new Panel(string.Empty).Expand())
                    .AutoClear(false)
                    .Overflow(VerticalOverflow.Ellipsis)
                    .Start(ctx =>
                    {
                        while (!exit)
                        {
                            // 1) ввод
                            while (Console.KeyAvailable)
                            {
                                var key = Console.ReadKey(true);
                                if (key.Key == ConsoleKey.Escape) { exit = true; break; }
                                if (key.Key == ConsoleKey.Backspace && inputBuf.Length > 0)
                                    inputBuf = inputBuf[..^1];
                                else if (key.Key == ConsoleKey.Enter)
                                {
                                    var txt = inputBuf.Trim();
                                    inputBuf = "";
                                    if (!string.IsNullOrEmpty(txt))
                                    {
                                        try
                                        {
                                            var sent = _bot
                                    .SendMessage(_chat.CurrentChat.Id, txt)
                                    .GetAwaiter().GetResult();
                                            _chat.UpdateChatStory(sent);
                                        }
                                        catch (Exception ex)
                                        {
                                            UILogger.AddLog($"Ошибка отправки: {ex.Message}", "ERROR");
                                        }
                                    }
                                }
                                else if (!char.IsControl(key.KeyChar))
                                    inputBuf += key.KeyChar;
                            }

                            // 2) HEADER
                            var header = new Panel(new Text($"[CHAT]  {_chat.chatTitle}", new Style(Color.Green1)))
                    .Border(BoxBorder.None)
                    .Expand();

                            // 3) рассчитываем высоту панели чата
                            int chatPanelHeight = Console.WindowHeight / 2;
                            int innerRows = Math.Max(1, chatPanelHeight - 2);

                            // 4) хвост из последних innerRows сообщений
                            var allMsgs = _log.ToArray();
                            var tail = allMsgs.Reverse().Take(innerRows).Reverse().ToArray();
                            int blanks = innerRows - tail.Length;

                            // 5) собираем таблицу с пустыми строками сверху
                            var table = new Table()
                    .HideHeaders()
                    .AddColumn(new TableColumn(string.Empty))
                    .Expand();

                            for (int i = 0; i < blanks; i++)
                                table.AddRow("");

                            foreach (var (time, who, txt) in tail)
                            {
                                // выбираем цвет имени
                                var whoColor = who == TGBotClient.BotName
                        ? "deepskyblue1"
                        : "green1";

                                // форматируем
                                var lineMarkup =
                        $"[grey]{Markup.Escape(time)}[/] " +
                        $"[{whoColor}][underline]{Markup.Escape(who)}[/][/]  " +
                        $"[white]{Markup.Escape(txt).Replace("\n", "\n      ")}[/]";

                                table.AddRow(new Markup(lineMarkup));
                            }

                            var chatPanel = new Panel(table)
                    .Border(BoxBorder.Rounded)
                    .BorderColor(Color.Grey)
                    // убрали .Height(...) — Panel займёт ровно свою «контентную» высоту
                    .Expand();

                            // 6) INPUT
                            var cursor = DateTime.Now.Millisecond < 500 ? "_" : " ";
                            var inputPanel = new Panel(new Text($"> {Markup.Escape(inputBuf)}{cursor}"))
                    .Border(BoxBorder.Rounded)
                    .BorderColor(Color.Grey)
                    .Expand();

                            // 7) FOOTER
                            var footer = new Panel(new Text("Enter → отправить    Esc → назад"))
                    .Border(BoxBorder.None)
                    .Expand();

                            // 8) собираем во «внешний» Panel
                            var root = new Panel(new Rows(
                        header,
                        chatPanel,
                        inputPanel,
                        footer
                    ))
                    .Border(BoxBorder.None)
                    .Expand();

                            // 9) обновляем
                            ctx.UpdateTarget(root);
                            Thread.Sleep(50);
                        }
                    });

                // выход
                Console.CursorVisible = true;
                Console.Clear();
                _chat.MessageReceived -= Enqueue;
            }
        }
    }
}
