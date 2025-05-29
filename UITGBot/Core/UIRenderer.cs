using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace UITGBot.Core
{
    internal static class UIRenderer
    {
        private static System.Timers.Timer? _RenderTimer;
        private static List<string> pageTitles = new List<string>(){
                    "хуй",
                    "хуй 2",
                    "Залупа",
                    "Я хз че написать",
                };
        private static CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private static ushort _pageSelectedIndex = 0;
        //private static ushort _optionSelectedIndex = 0;
        /// <summary>
        /// Эта функция инициализирует условно-бесконечный цикл рендера административного интерфейса в отдельном потоке
        /// </summary>
        /// <returns>Успешность операции</returns>
        public static void StartUI()
        {
            _RenderTimer = new System.Timers.Timer(5000);
            _RenderTimer.Elapsed += (s, e) => Task.Run(RenderScreen);
            _RenderTimer.AutoReset = true;
            _RenderTimer.Start();
            ListenForKeyPress(_cancellationTokenSource.Token);
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
            Console.Clear();
            var table = new Table().Border(TableBorder.Rounded);
            table.AddColumn("[bold]Опции[/]").Centered();
            table.AddRow("Скрыть логи");
            table.AddRow("Скрыть очередь скриптов");
            table.AddRow("Скрыть статистику");
            table.AddRow("Видимость");
            table.AddRow("Частота обновления экрана (сек)");
            table.AddRow("Справка");
            table.AddRow("Перезапуск бота");
            table.AddRow("Изменить настройки сервера");
            table.AddRow("Misc");
            table.AddRow("[bold]Выход[/]");

            var grid = new Grid();
            grid.AddColumn();
            grid.AddColumn();

            grid.AddRow(table, CreateSystemInfoPanel());
            grid.AddRow(CreateCreditsPanel(), new Panel("[bold]Вкладки[/]").Border(BoxBorder.Rounded));

            AnsiConsole.Write(grid);
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
        private static Panel CreateSystemInfoPanel()
        {
            var systemGrid = new Grid();
            systemGrid.AddColumn();
            systemGrid.AddColumn();
            systemGrid.AddRow(new Panel("[italic]Логи системы[/]").Border(BoxBorder.Rounded),
                              new Panel("[italic]Запущенные скрипты[/]").Border(BoxBorder.Rounded));
            systemGrid.AddRow(new Panel("[italic]Статистика[/]").Border(BoxBorder.Rounded));

            return new Panel(systemGrid).Header("[bold]Информация о системе[/]");
        }
        private static Panel CreateCreditsPanel()
        {
            return new Panel("[italic]Тут credits и доп. инфа[/]").Border(BoxBorder.Rounded);
        }
        /// <summary>
        /// Функция ожидания ввода с клавиатуры (без обработчика, в отдельном потоке)
        /// </summary>
        /// <param name="token">Токен для контроля выполнения функции. Когда токен будет отменен, функция завершится</param>
        private static void ListenForKeyPress(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true); // Считывание нажатой кнопки, без вывода значения в консоль
                    OnKeyPress(key.Key);
                }
                Thread.Sleep(50); // небольшая задержка, чтобы снизить нагрузку и рендер успевал проходить (CMD и PS сосут, оно не успевает)
            }
        }
        /// <summary>
        /// Обработчик нажатия клавиш (блок управления панелью администратора)
        /// </summary>
        /// <param name="key"></param>
        private static void OnKeyPress(ConsoleKey key)
        {
            RestartTimer();
            switch (key)
            {
                case ConsoleKey.Escape:
                    Program.OnPanic();
                    break;
                case ConsoleKey.LeftArrow:
                case ConsoleKey.A:
                    if (_pageSelectedIndex - 1 >= 0) _pageSelectedIndex--;
                    else _pageSelectedIndex = (ushort)(pageTitles.Count - 1);
                    break;
                case ConsoleKey.RightArrow:
                case ConsoleKey.D:
                    if (_pageSelectedIndex + 1 < pageTitles.Count) _pageSelectedIndex++;
                    else _pageSelectedIndex = 0;
                    break;
            }
            RenderScreen();
        }
    }
}
