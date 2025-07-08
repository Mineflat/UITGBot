using System;
using System.Linq;
using System.Collections.Concurrent;
using Spectre.Console;
using Spectre.Console.Rendering;
using Telegram.Bot;
using Telegram.Bot.Types;
using UITGBot.Core.Messaging;
using Color = Spectre.Console.Color;

namespace UITGBot.Core.UI
{
    public class ChatConsole
    {
        private readonly ChatActivity _chat;
        private readonly ITelegramBotClient _bot;
        private readonly ConcurrentQueue<string> _log = new();

        public ChatConsole(ChatActivity chatActivity, ITelegramBotClient botClient)
        {
            _chat = chatActivity;
            _bot = botClient;

            // 1) Подписываемся на любое новое сообщение
            _chat.MessageReceived += OnNewMessage;

            // 2) Грузим уже накопленную историю
            foreach (var m in _chat.ChatStory)
                OnNewMessage(m);
        }

        private void OnNewMessage(Message msg)
        {
            var who = msg.From?.Username ?? msg.From?.Id.ToString();
            var time = msg.Date.ToLocalTime().ToString("HH:mm");
            var txt = msg.Text ?? msg.Caption ?? "<non-text>";
            _log.Enqueue($"[{time}] {who}: {txt}");
        }

        /// <summary>
        /// Запускает консольный чат «на весь экран».
        /// Возвращает управление сразу после нажатия Esc.
        /// </summary>
        public void Run()
        {
            Console.Clear();
            Console.CursorVisible = false;

            bool exit = false;
            string inputBuf = "";

            // 1) Собираем Layout:
            //    Header(3) / Body(*) / Input(1) / Footer(1)
            var layout = new Layout("root")
                .SplitRows(
                    new Layout("hdr") { Size = 3 },
                    new Layout("body") { Ratio = 1 },
                    new Layout("input") { Size = 1 },
                    new Layout("footer") { Size = 1 }
                );
            layout["body"].SplitColumns(
                new Layout("buff") { Size = 3 },
                new Layout("chat") { Ratio = 1 }
            );

            // 2) Запуск Live-рендера
            AnsiConsole.Live(layout)
                .AutoClear(false)
                .Overflow(VerticalOverflow.Ellipsis)
                .Start(ctx =>
                {
                    while (!exit)
                    {
                        // ————— Обработка вводимых клавиш —————
                        while (Console.KeyAvailable)
                        {
                            var key = Console.ReadKey(true);

                            if (key.Key == ConsoleKey.Escape)
                            {
                                exit = true;
                                break;
                            }

                            if (key.Key == ConsoleKey.Backspace)
                            {
                                if (inputBuf.Length > 0)
                                    inputBuf = inputBuf[..^1];
                            }
                            else if (key.Key == ConsoleKey.Enter)
                            {
                                var txt = inputBuf.Trim();
                                inputBuf = "";
                                if (txt.Length > 0)
                                {
                                    try
                                    {
                                        // Вот ваш метод для отправки
                                        var sent = _bot
                                            .SendMessage(
                                                chatId: _chat.CurrentChat.Id,
                                                text: txt)
                                            .GetAwaiter()
                                            .GetResult();
                                        // Он попадёт в ChatStory → MessageReceived → _log
                                        _chat.UpdateChatStory(sent);
                                    }
                                    catch (Exception e)
                                    {
                                        // Можно также вывесить ошибку куда-нибудь в панель логов
                                    }
                                }
                            }
                            else if (!char.IsControl(key.KeyChar))
                            {
                                inputBuf += key.KeyChar;
                            }
                        }

                        // ————— 1) Header (3 строки) —————
                        var title = _chat.chatTitle ?? "(неизвестно)";
                        var header = new Panel($@"
 X    {title}
{"".PadRight(Console.WindowWidth - 1, '─')}")
                            .Border(BoxBorder.None)
                            .Expand();
                        layout["hdr"].Update(header);

                        // ————— 2) Левая буфер‐ступенька —————
                        layout["buff"].Update(
                            new Panel(string.Empty)
                                .Border(BoxBorder.Ascii)
                                .Expand()
                        );

                        // ————— 3) Основное окно истории —————
                        var table = new Table().Expand();
                        table.AddColumn(new TableColumn(""));
                        table.HideHeaders();
                        foreach (var line in _log.ToArray())
                            table.AddRow(new Text(line));

                        layout["chat"].Update(
                            new Panel(table)
                                .Border(BoxBorder.Rounded)
                                .BorderColor(Color.Grey)
                                .Expand()
                        );

                        // ————— 4) Поле ввода (1 строка) —————
                        var cursor = DateTime.Now.Millisecond < 500 ? "_" : " ";
                        layout["input"].Update(
                            new Panel($"> {inputBuf}{cursor}")
                                .Border(BoxBorder.Rounded)
                                .BorderColor(Color.Grey)
                                .Expand()
                        );

                        // ————— 5) Footer (1 строка) —————
                        layout["footer"].Update(
                            new Panel("[grey]Enter → send    Esc → back[/]")
                                .Border(BoxBorder.None)
                                .Expand()
                        );

                        ctx.Refresh();
                        Thread.Sleep(50);
                    }
                });

            // 3) По выходу «вернём» консоль
            Console.CursorVisible = true;
            Console.Clear();

            // 4) Отпишемся
            _chat.MessageReceived -= OnNewMessage;
        }
    }
}
