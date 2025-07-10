using Spectre.Console;
using UITGBot.TGBot;
using Spectre.Console.Rendering;
using UITGBot.Logging;
using Telegram.Bots.Http;
using UITGBot.Core.UI;
using Telegram.Bots.Requests;
using Quartz.Util;

namespace UITGBot.Core
{
    internal static class UIRenderer
    {
        private static System.Timers.Timer? _RenderTimer;
        private static int _selectedIndex = 0;
        private static Layout _CurrentLayout = new Layout();
        private static bool _canRender = true;
        private static List<UIScreenItem> _MainPageActions = new List<UIScreenItem>()
        {
            new UIScreenItem()
            {
                Title = "Управление списком действий",
                ExecAfter = UIActionsRealization.SetupActions
            },
            new UIScreenItem()
            {
                Title = "Открыть чат от имени бота",
                ExecAfter = UIActionsRealization.OpenBotChat
            },
            new UIScreenItem()
            {
                Title = "Рассылка по чатам",
                ExecAfter = UIActionsRealization.OpenBotChat
            },
            new UIScreenItem()
            {
                Title = "Перезапуск бота",
                ExecAfter = UIActionsRealization.RestartBot
            },
            new UIScreenItem()
            {
                Title = "Остановка бота и выход",
                ExecAfter = Program.OnPanic
            }
        };
        private static CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private static ushort _pageSelectedIndex = 0;
        /// <summary>
        /// Эта функция инициализирует условно-бесконечный цикл рендера административного интерфейса в отдельном потоке
        /// </summary>
        /// <returns>Успешность операции</returns>
        public static void RestartUI()
        {
            UpdateMainMenu();
            //_RenderTimer = new System.Timers.Timer(5000);
            //_RenderTimer.Elapsed += (s, e) => Task.Run(RenderScreen);
            //_RenderTimer.AutoReset = true;
            //_RenderTimer.Start();
            while (true)
            {
                switch (Console.ReadKey(true).Key)
                {
                    case ConsoleKey.W:
                    case ConsoleKey.UpArrow:
                        if (_selectedIndex - 1 < 0) _selectedIndex = _MainPageActions.Count - 1;
                        else _selectedIndex = _selectedIndex - 1;
                        break;
                    case ConsoleKey.S:
                    case ConsoleKey.DownArrow:
                        if (_selectedIndex + 1 >= _MainPageActions.Count) _selectedIndex = 0;
                        else _selectedIndex = _selectedIndex + 1;
                        break;
                    case ConsoleKey.Enter:
                        try
                        {
                            _canRender = false;
                            try
                            {
                                UILogger.AddLog($"Выполнение действия из меню ([cyan1]{_MainPageActions[_selectedIndex].Title}[/])", "DEBUG");
                                if (_MainPageActions[_selectedIndex].ExecAfter != null) { _MainPageActions[_selectedIndex].ExecAfter?.Invoke(); }
                            }
                            catch (Exception e)
                            {
                                UILogger.AddLog($"Ошибка при выполнении действия [underline][cyan1]{_MainPageActions[_selectedIndex].Title}[/][/]:\n{e.Message}", "ERROR");
                                throw;
                            }
                            _canRender = true;
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message + "\n" + e.StackTrace);
                            UILogger.AddLog($"Ошибка выполнения действия из меню ([cyan]{_MainPageActions[_selectedIndex].Title}[/]):\n {e.Message}", "CRITICAL");
                        }
                        break;
                    default:
                        break;
                }
                UpdateMainMenu();
            }
            //ListenForKeyPress(_cancellationTokenSource.Token);
        }
        /// <summary>
        /// Функция перезапуска таймера. Срабатывает каждый раз, когда пользователь нажимает на кнопку
        /// </summary>
        private static void RestartTimer()
        {
            if (_RenderTimer != null)
            {
                _RenderTimer.Stop();
                _RenderTimer.Start();
            }
        }

        private static void RenderScreen()
        {
            UpdateMainMenu();
            //AnsiConsole.Write(CurrentTable);
            //Console.Clear();
            //var table = new Table().Border(TableBorder.Rounded);
            //table.AddColumn("[bold]Опции[/]").Centered();
            //table.AddRow("Управление списком действий");
            //table.AddRow("Скрыть очередь скриптов");
            //table.AddRow("Скрыть статистику");
            //table.AddRow("Видимость");
            //table.AddRow("Частота обновления экрана (сек)");
            //table.AddRow("Справка");
            //table.AddRow("Перезапуск бота");
            //table.AddRow("Изменить настройки сервера");
            //table.AddRow("Misc");
            //table.AddRow("[bold]Выход[/]");

            //var grid = new Grid();
            //grid.AddColumn();
            //grid.AddColumn();
            //grid.AddRow(table, CreateSystemInfoPanel());
            //grid.AddRow(CreateCreditsPanel(), new Panel("[bold]Вкладки[/]").Border(BoxBorder.Rounded));
            //grid.Expand = true;
            //AnsiConsole.Write(grid);
            /*try
            //{
                
            //    string buttons = string.Empty;
            //    for (global::System.Int32 i = 0; i < pageTitle.Count; i++)
            //    {
            //        if (i == pageIndex)
            //        {
            //            pageTitle[i] = $"[blue]{pageTitle[i]}[/]";
            //        }
            //        else if (pageTitle[i].Contains("[/]"))
            //        {
            //            pageTitle[i] = pageTitle[i].Replace("[blue]", "").Replace("[/]", "");
            //        }
            //        buttons += $"  {pageTitle[i]}  ";
            //    }
            //    Layout navigationGrid = new Layout("Root");
            //    navigationGrid.SplitRows(
            //                new Layout("UP").SplitColumns(
            //                    new Layout("LEFT"),
            //                    new Layout("RIGTH")
            //                    ),
            //                new Layout("MIDDLE").Size(1),
            //                new Layout("DOWN").Size(3)
            //        );
            //    navigationGrid["MIDDLE"].Update(new Rule());
            //    switch (Console.ReadKey(true).Key)
            //    {
            //        case ConsoleKey.Escape:
            //            Environment.Exit(0);
            //            break;
            //        case ConsoleKey.LeftArrow:
            //        case ConsoleKey.A:
            //            if (pageIndex - 1 >= 0) pageIndex--;
            //            else pageIndex = pageTitle.Count - 1;
            //            break;
            //        case ConsoleKey.RightArrow:
            //        case ConsoleKey.D:
            //            if (pageIndex + 1 < pageTitle.Count) pageIndex++;
            //            else pageIndex = 0;
            //            break;
            //    }
            //    //navigationGrid.SplitColumns(
            //    //    new Layout("Left")
            //    //        .SplitRows(
            //    //            new Layout("1"),
            //    //            new Layout("2"),
            //    //            new Layout("3"),
            //    //            new Layout("4"),
            //    //            new Layout("5")
            //    //        ),
            //    //    new Layout("Right")
            //    //    );

            //    //navigationGrid.SplitRows();
            //    //navigationGrid.SplitRows();
            //    //navigationGrid.SplitRows();
            //    //navigationGrid.SplitRows();
            //    //navigationGrid.AddRow(new Rule("Выбранная вкладка"));
            //    //navigationGrid.AddRow(new );
            //    //navigationGrid.AddRow();
            //    //navigationGrid.AddRow(new Rule("Элементы управления"));
            //    //navigationGrid.AddRow();
            //    AnsiConsole.Write(navigationGrid);
            //}
            //catch
            //{
            //    throw;
            */
        }

        public static void UpdateMainMenu()
        {
            if (!_canRender) return;
            // 1) Заголовок на всю ширину
            var headerPanel = new Panel($"[bold]Панель управления ботом[/] [green1]@{TGBotClient.BotName}[/]")
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.LightSkyBlue1)
                .Expand();

            // 2) Левая колонка: меню действий

            var actionsTable = new Table()
                .HideHeaders()
                .Border(TableBorder.Minimal)
                .BorderColor(Color.PaleTurquoise1)
                .Expand();
            actionsTable.AddColumn(string.Empty);
            for (int i = 0; i < _MainPageActions.Count; i++)
            {
                if (i == _selectedIndex)
                    actionsTable.AddRow($">> [green]{_MainPageActions[i].Title}[/]");
                else actionsTable.AddRow($"[white]{_MainPageActions[i].Title}[/]");
            }
            var actionsPanel = new Panel(actionsTable)
                .Header("[bold]Меню действий[/]")
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.PaleTurquoise1)
                .Expand();

            // 3) Правая колонка: две диаграммы, которые займут по пол-колонки
            // 3.1) Первая диаграмма
            var chartA = new BarChart()
                .AddItem("Счетчик ошибок:", TGBotClient.botErrorsLeft, Color.Red1)
                .AddItem("Получено сообщений:", Storage.Statisticks.botMessagesReceived, Color.DodgerBlue2)
                .AddItem("Из них было команд:", Storage.Statisticks.botMessagesProccessed, Color.DeepSkyBlue1)
                .AddItem("Активных команд:", Storage.Statisticks.botActiveActionsCount, Color.Green3)
                .AddItem("Всего команд:", Storage.Statisticks.botActionsCount, Color.LightSkyBlue3_1)
                .AddItem("Всего чатов с этим ботом:", Storage.Statisticks.botChatsKnown, Color.Green1)
                .AddItem("Из них писали в чат с ботом (человек):", Storage.Statisticks.botUsersKnown, Color.DarkOliveGreen1);

            chartA.Label = "[underline][bold]Информация[/][/]\n";
            chartA.LabelAlignment = Justify.Right;

            var panelA = new Panel(chartA)
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.PaleTurquoise1)
                .Expand();

            // 3.2) Вторая диаграмма
            //var chartB = new BarChart()
            //    .AddItem("Простая текстовая команда ([green1]simple[/]):", Storage.Statisticks.ActionsCountTypeOf_simple, Color.DodgerBlue1)
            //    .AddItem("Текст из файла по указанному пути ([green1]full_text[/]):", Storage.Statisticks.ActionsCountTypeOf_full_text, Color.Green3_1)
            //    .AddItem("Произвольный текст из файла ([green1]random_text[/]):", Storage.Statisticks.ActionsCountTypeOf_random_text, Color.DodgerBlue1)
            //    .AddItem("Отправка изображения ([green1]image[/]):", Storage.Statisticks.ActionsCountTypeOf_image, Color.Green3_1)
            //    .AddItem("Отправка произвольного изображения ([green1]random_image[/]):", Storage.Statisticks.ActionsCountTypeOf_random_image, Color.DodgerBlue1)
            //    .AddItem("Отправка файла ([green1]file[/]):", Storage.Statisticks.ActionsCountTypeOf_file, Color.Green3_1)
            //    .AddItem("Отправка произвольного файла ([green1]random_file[/]):", Storage.Statisticks.ActionsCountTypeOf_random_file, Color.DodgerBlue1)
            //    .AddItem("Выполнение скрипта ([green1]script[/]):", Storage.Statisticks.ActionsCountTypeOf_script, Color.Green3_1)
            //    .AddItem("Выполнение произвольного скрипта ([green1]random_script[/]):", Storage.Statisticks.ActionsCountTypeOf_random_script, Color.DodgerBlue1)
            //    .AddItem("Загрузка данных из чата ([green1]remote_file[/]):", Storage.Statisticks.ActionsCountTypeOf_remote_file, Color.Green3_1);
            //chartB.Label = "[underline][bold]Список действий по категориям[/][/]\n";
            //chartB.LabelAlignment = Justify.Right;

            var chartB = new BreakdownChart()
                .AddItem("Простая текстовая команда (simple):", Storage.Statisticks.ActionsCountTypeOf_simple, Color.DodgerBlue1)
                .AddItem("Текст из файла по указанному пути (full_text):", Storage.Statisticks.ActionsCountTypeOf_full_text, Color.Green3_1)
                .AddItem("Произвольный текст из файла (random_text):", Storage.Statisticks.ActionsCountTypeOf_random_text, Color.RoyalBlue1)
                .AddItem("Отправка изображения (image):", Storage.Statisticks.ActionsCountTypeOf_image, Color.BlueViolet)
                .AddItem("Отправка произвольного изображения (random_image):", Storage.Statisticks.ActionsCountTypeOf_random_image, Color.SteelBlue1)
                .AddItem("Отправка файла (file):", Storage.Statisticks.ActionsCountTypeOf_file, Color.HotPink)
                .AddItem("Отправка произвольного файла (random_file):", Storage.Statisticks.ActionsCountTypeOf_random_file, Color.DarkOrange3)
                .AddItem("Выполнение скрипта (script):", Storage.Statisticks.ActionsCountTypeOf_script, Color.Magenta2)
                .AddItem("Выполнение произвольного скрипта (random_script):", Storage.Statisticks.ActionsCountTypeOf_random_script, Color.Plum3)
                .AddItem("Загрузка данных из чата (remote_file):", Storage.Statisticks.ActionsCountTypeOf_remote_file, Color.Khaki3);

            var panelB = new Panel(chartB)
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.PaleTurquoise1)
                .Expand();

            // 4) Футер: логи системы
            int logHeight = Console.WindowHeight / 2;
            var logs = UILogger.GetLogs(logHeight - 5);
            var logsTable = new Table()
                .HideHeaders()
                .Border(TableBorder.Minimal)
                .BorderColor(Color.PaleTurquoise1)
                .Expand();
            logsTable.AddColumn(string.Empty);
            foreach (var line in logs)
                logsTable.AddRow(line);

            var logsPanel = new Panel(logsTable)
                .Header("[bold]Логи системы[/]")
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.PaleTurquoise1)
                .Expand();

            // 5) Собираем Layout: шапка / тело (2 колонки) / футер
            var layout = new Layout("root")
                .SplitRows(
                    new Layout("header") { Size = 3 },
                    new Layout("body") { Ratio = 1 },
                    new Layout("footer") { Size = logHeight }
                );

            // body → две колонки: левая меню, правая статистика
            layout["body"].SplitColumns(
                new Layout("left") { Ratio = 1 },
                new Layout("right") { Ratio = 3 }
            );

            int t = (Console.BufferWidth / 2) - "Автор проекта: @ElijahKamsky".Length; // считаем размер разделителя
            string target = $"  [green3_1]Используемые пути, согласно конфигурационому файлу[/]" +
                        new string(' ', t) + // разделитель
                        $" [grey15]Автор проекта: @ElijahKamsky[/]{Environment.NewLine}" +
                        $"  [silver]Файл действий:[/] [grey]{Storage.SystemSettings.ActionsPath}[/]{Environment.NewLine}" +
                        $"  [silver]Файл ответов (положительные):[/] [grey]{Storage.SystemSettings.SuccessReplyPath}[/]{Environment.NewLine}" +
                        $"  [silver]Файл ответов (отрицательные):[/] [grey]{Storage.SystemSettings.ErrorReplyPath}[/]{Environment.NewLine}" +
                        $"  [silver]Логи хранятся в:[/] [grey]{Storage.SystemSettings.LogDirectory}[/]{Environment.NewLine}";

            // правая колонка → две строки: по одной диаграмме
            layout["right"].SplitRows(
                new Layout("statA") { Size = 12 },
                new Layout("statB") { Ratio = 1 },
                new Layout(
                    new Table().AddColumn(target)
                        .Border(TableBorder.None)
                        .BorderColor(Color.PaleTurquoise1)
                        .Expand()
                )
            );

            // 6) Привязываем панели к Layout
            layout["header"].Update(headerPanel);
            layout["left"].Update(actionsPanel);
            layout["statA"].Update(panelA);
            layout["statB"].Update(panelB);
            layout["footer"].Update(logsPanel);

            // 7) Рендерим всё одним Write
            _CurrentLayout = layout;
            Console.Clear();
            Console.CursorVisible = false;
            AnsiConsole.Write(_CurrentLayout);
        }

        //private static Panel CreateSystemInfoPanel()
        //{
        //    var systemGrid = new Grid();
        //    systemGrid.AddColumn();
        //    systemGrid.AddColumn();
        //    systemGrid.AddRow(new Panel("[italic]Логи системы[/]").Border(BoxBorder.Rounded),
        //                      new Panel("[italic]Запущенные скрипты[/]").Border(BoxBorder.Rounded));
        //    systemGrid.AddRow(new Panel("[italic]Статистика[/]").Border(BoxBorder.Rounded));

        //    return new Panel(systemGrid).Header("[bold]Информация о системе[/]");
        //}
        //private static Panel CreateCreditsPanel()
        //{
        //    return new Panel("[italic]Тут credits и доп. инфа[/]").Border(BoxBorder.Rounded);
        //}
        /// <summary>
        /// Функция ожидания ввода с клавиатуры (без обработчика, в отдельном потоке)
        /// </summary>
        /// <param name="token">Токен для контроля выполнения функции. Когда токен будет отменен, функция завершится</param>
        //private static void ListenForKeyPress(CancellationToken token)
        //{
        //    while (!token.IsCancellationRequested)
        //    {
        //        if (Console.KeyAvailable)
        //        {
        //            var key = Console.ReadKey(true); // Считывание нажатой кнопки, без вывода значения в консоль
        //            OnKeyPress(key.Key);
        //        }
        //        Thread.Sleep(50); // небольшая задержка, чтобы снизить нагрузку и рендер успевал проходить (CMD и PS сосут, оно не успевает)
        //    }
        //}
        /// <summary>
        /// Обработчик нажатия клавиш (блок управления панелью администратора)
        /// </summary>
        /// <param name="key"></param>
        //private static void OnKeyPress(ConsoleKey key)
        //{
        //    RestartTimer();
        //    switch (key)
        //    {
        //        case ConsoleKey.Escape:
        //            Program.OnPanic();
        //            break;
        //        case ConsoleKey.LeftArrow:
        //        case ConsoleKey.A:
        //            if (_pageSelectedIndex - 1 >= 0) _pageSelectedIndex--;
        //            else _pageSelectedIndex = (ushort)(pageTitles.Count - 1);
        //            break;
        //        case ConsoleKey.RightArrow:
        //        case ConsoleKey.D:
        //            if (_pageSelectedIndex + 1 < pageTitles.Count) _pageSelectedIndex++;
        //            else _pageSelectedIndex = 0;
        //            break;
        //    }
        //    RenderScreen();
        //}
    }
}
