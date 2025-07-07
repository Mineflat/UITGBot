using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terminal.Gui;

namespace UITGBot.Core.UI
{
    internal static class TerminalEditor
    {
        public static string? Edit(string currentText)
        {
            // 1. Инициализация
            Application.Init();

            Colors.Base.Normal = Terminal.Gui.Attribute.Make(Color.Cyan, Color.Black);
            Colors.Base.Focus = Terminal.Gui.Attribute.Make(Color.Cyan, Color.Black);
            Colors.Base.HotNormal = Terminal.Gui.Attribute.Make(Color.Cyan, Color.Black);
            Colors.Base.HotFocus = Terminal.Gui.Attribute.Make(Color.BrightYellow, Color.Black);
            Colors.Base.Disabled = Terminal.Gui.Attribute.Make(Color.DarkGray, Color.Black);

            // 2. Получаем корневой контейнер
            var top = Application.Top;

            // 3. Делаем меню (Ctrl+Q)
            var menu = new MenuBar(new MenuBarItem[] {
                new MenuBarItem("_File", new MenuItem[] {
                    new MenuItem("_Quit", "", () => Application.RequestStop(), null, null, Key.Esc),
                })
            });
            top.Add(menu);

            // 4. Создаём окно и обязательно задаём размер
            var win = new Window("Редактор (Ctrl+Q для выхода)")
            {
                X = 0,
                Y = 1,              // под меню
                Width = Dim.Fill(),
                Height = Dim.Fill(),
            };
            top.Add(win);

            // 5. Текстовый редактор
            var tv = new TextView()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill(),
                Text = currentText,
                WordWrap = true
            };
            win.Add(tv);

            // 6. Запуск цикла
            Application.Run();

            // 7. Можно корректно завершить
            Application.Shutdown();

            // 8. Возвращаем результат
            return tv.Text.ToString();
        }
    }
}
