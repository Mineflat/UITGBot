using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Hosting;
using Polly;
using Spectre.Console;
using System;
using System.IO;
using System.Reflection;
using System.Text;
using UITGBot.Core;
using UITGBot.Core.Messaging;
using UITGBot.Logging;

namespace UITGBot
{
    internal class Program
    {
        // Дата начала проекта: 02.02.2025
        // Реально дохуя писал в дней: 3
        // Без перерывов писал дней: 2
        private static ManualResetEvent _resetEvent = new ManualResetEvent(false);
        /// <summary>
        /// Вызов этого метода продолжит выполнения контекста в методе Main, что в большинстве случаев остановит все системные процессы
        /// </summary>
        public static void OnPanic(string errorMessage = "Сработал метод OnPanic()")
        {
            Console.WriteLine(errorMessage);
            Console.CursorVisible = true;
            _resetEvent.Set();
            Environment.Exit(1);
        }
        /// <summary>
        /// Этот метод спросит необходимую информацию у пользователя, зашифрует ее и выведет ему на экран, чтобы тот мог внести полученные значения в конфиг.
        /// Затрагивает параметры: токен бота, строка подключения к БД
        /// </summary>
        private static void EncryptSensetiveStrings()
        {
            string encryptedConnectionString = string.Empty;
            Console.WriteLine("Введите данные для шифрования");
            List<(string name, string option)> authData = new List<(string name, string option)>()
            {
                //new ("IP и порт для подключения к СУБД (в формате IP:port)", ""), // 0
                //new ("Имя пользователя для аутентификации", ""), // 1
                //new ("Пароль пользователя", ""), // 2
                new ("Токен телеграмм-бота", ""), // 3
                new ("Строка шифрования данных (БЕЗ ПРОБЕЛОВ)", "") // 4
            };
            for (int i = 0; i < authData.Count; i++)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write($"{authData[i].name} >> ");
                Console.ResetColor();
                string? option = Console.ReadLine();
                if (string.IsNullOrEmpty(option))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write($"{authData[i].name} не может быть пустым значением");
                    Console.ResetColor();
                    Environment.Exit(1);
                }
                authData[i] = new(authData[i].name, option);
            }
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Black;
            Console.BackgroundColor = ConsoleColor.Red;
            Console.WriteLine("\t    [!!!]    Обратите внимение!    [!!!]");
            Console.ResetColor();
            Console.WriteLine("\t\tСледующую секретную строку вы видите в последний раз:\n");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\t\t\t{authData[1].option}\n");
            Console.ResetColor();
            Console.WriteLine("\t\tВы не сможете запустить приложение, если забудете ее, т.к. ей зашифрована\n" +
                "\t\tстрока подключения к БД и токен бота. Сохраните и не распространыйте ее.\n\n" +
                "\tПримечание:\n" +
                "\t\tЕсли вы используете опцию EncryptDB=true, все записи в БД шифруются при помощи этого же ключа.\n" +
                "\t\tВ таком случае, при его утере, восстановление базы данных НЕВОЗМОЖНО\n\n" +
                "-------------------------------------------------------------------------\n");
            // Шифрование строки подключения
            //string connectionString = $"Host={authData[0].option};Username={authData[1].option};Password={authData[2].option}";
            string botToken = authData[0].option;
            string password = authData[1].option;
            Cryptor? cryptor = new Cryptor(password);
            Console.WriteLine("\tИнформация зашифрована. Добавьте следующие значения в конфигурационный файл:");
            Console.ForegroundColor = ConsoleColor.Cyan;
            //Console.WriteLine($"\t\tDB_SECRET=\"{cryptor.Encrypt(connectionString)}\"");
            Console.WriteLine($"\t\tBOT_SECRET=\"{cryptor.Encrypt(botToken)}\"");
            Console.ResetColor();
            Console.WriteLine("\tTIP:\n" +
                "\t\tЕсли вы не хотите вводить пароль при каждом перезапуске сервиса, добавьте переменную окружения:\n");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\t\texport TGBOT_SECRET_KEY=\'{password}\'\n");
            Console.ResetColor();
            Console.WriteLine("\t\tЕсли вы используете SystemD, добавьте в конфигурацию сервиса следующую строку:\n\n" +
                "\t\t\t[Service]\n" +
                "\t\t\t...");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\t\t\tEnvironment=\"TGBOT_SECRET_KEY={password}\"");
            Console.ResetColor();
            cryptor = null;
            OnPanic("Нормальный выход для EncryptSensetiveStrings");
        }
        static void Main(string[] args)
        {
            //Initialize(new string[] { "G:\\config.json", "QPZU-JT45-VZTT-RB53" });
            string? passwd = Environment.GetEnvironmentVariable("TGBOT_SECRET_KEY");
            switch (args.Length)
            {
                case 1:
                    if (args[0].ToLower() == "--secure")
                    {
                        EncryptSensetiveStrings();
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(passwd))
                        {
                            UILogger.AddLog($"Неверное использование!\nДля защиты данных используйте:\n\t\t./{AppDomain.CurrentDomain.FriendlyName} --secure", "FATAL");
                            Environment.Exit(1);
                        }
                        else
                        {
                            Initialize(args = new string[]{
                                args[0],
                                passwd
                            });
                        }
                    }
                    break;
                case 2:
                    Initialize(args);
                    break;
                default:
                    UILogger.AddLog($"Неверное использование!\nДля запуска используйте:\n\t\t./{AppDomain.CurrentDomain.FriendlyName} [/full/path/to/config.json] [decrypt_password]", "FATAL");
                    break;
            }

            // Ждем, пока эвент не скинет кто-то другой
            _resetEvent.WaitOne();
        }
        private static void Initialize(string[] args)
        {
            Console.Clear();
            Storage.SystemCriptor = new Cryptor(args[1]);
            // Кто жалуется на отсутствие try-catch блока - лохудра ;)
            // Мне нужны 2 РАЗНЫЕ ошибки на 2 РАЗНЫХ сценария
            if (!File.Exists(args[0]))
            {
                UILogger.AddLog($"Невозможно применить файл конфигурации {args[0]}: файл по указанному пути отсутствует", "FATAL");
            }
            if (File.ReadAllText(args[0]).Length == 0) // И вот для этого и нужен блок try-catch.
                                                       // Если мы не сможем открыть этот файл, будет необработанное исключение
            {
                UILogger.AddLog($"Невозможно применить файл конфигурации {args[0]}: пустой файл", "FATAL");
            }
            // Момент инициализации, если файл существует и не пуст
            // Вызов функции инициализаци
            (bool success, string errorMessage) setupResult = SystemInitializer.Initialize(args[0]).Result;
            if (setupResult.success)
            {
                // Тут опасненько, потому что если логгер еще не инициализирован - будет херово: этого сообщения тупо не будет
                // Однако, и ошибки не будет. Если метод вернул нам true, значит что ВСЕ этапы инициализации были успешно закончены
                // Это - узкое горлышко этого ПО
                UILogger.AddLog("System setup done successfully");
                Storage._configurationPath = args[1];
            }
            else
            {
                UILogger.AddLog($"Произошла ошибка при запуске бота:\n{setupResult.errorMessage}", "FATAL");
            }
        }
    }
}
