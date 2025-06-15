using Microsoft.AspNetCore.Connections.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using UITGBot.Logging;
using UITGBot.TGBot;

namespace UITGBot.Core
{
    internal static class SystemInitializer
    {
        /// <summary>
        /// Основная функция, вызов которой инициализирует приложение заново
        /// </summary>
        /// <param name="configPath">Путь к основному конфигурационному файлу приложения</param>
        /// <returns>Кортеж: true - если инициализация прошла успешно, *string - сообщение об ошибке</returns>
        public static Task<(bool success, string errorMessage)> Initialize(string configPath)
        {
            // Чтение конфигурации
            (bool success, string errorMessage) setupResult = SetupSystem(configPath);
            if (!setupResult.success)
                return Task.FromResult((false, "Не удалось выполнить предварительную подготовку:\n" +
                    $"{setupResult.errorMessage}\n" +
                    "TIP: Проверьте конфигурационный файл на наличие ошибок"));
            // Все действия ниже будут происходит исходя из текущей конфигурации
            // Если она не спарсилась методом выше - это пиздец, че сказать
            var initFunctions = new List<Func<(bool success, string errorMessage)>>()
            {
                VerifyConfigiration, // Проверка и применение конфигурационного файла
                DecryptSecrets, // Расшифровка секретов
                //StartDBConnection, // Подключение к СУБД
                //InitRoles, // Применение ролей (их больше нет)
                InitCommands, // Применение действий
                InitTelegramBot // Запуск телеграмм-бота
            };
            // Создание логгера
            foreach (var func in initFunctions)
            {
                (bool success, string errorMessage) execResult = func();
                if (!execResult.success)
                {
                    Storage.Logger?.Logger.Fatal(execResult.errorMessage);
                    return Task.FromResult(execResult);
                }
                else Storage.Logger?.Logger.Information(execResult.errorMessage);
            }

            // Запуск административного интерфейса
            //UIRenderer.StartUI();
            return Task.FromResult((true, "Система успешно инициализирована"));
        }
        #region Функции инициализации приложения
        /// <summary>
        /// Эта функция спарсит конфигурационный файл
        /// </summary>
        /// <param name="configPath">Путь к конфигурационному файлу на диске</param>
        /// <returns>Кортеж: true - если инициализация прошла успешно, *string - сообщение об ошибке</returns>
        private static (bool success, string errorMessage) SetupSystem(string configPath)
        {
            try
            {
                string fileContent = System.IO.File.ReadAllText(configPath);
                Preferences? p = JsonConvert.DeserializeObject<Preferences>(fileContent);
                if (p != null)
                {
                    Storage.SystemSettings = p;
                    Storage.Logger = new LogProvider();
                    return (true, "Конфигурация успешно прочитана");
                }
                return (false, "Не удалось применить конфигурациию: неверная структура JSON-файла");
            }
            catch (Exception configurationReadException)
            {
                return (false, configurationReadException.Message);
            }
        }
        /// <summary>
        /// Проверит конфигурационный файл
        /// </summary>
        /// <returns>Кортеж: true - если инициализация прошла успешно, *string - сообщение об ошибке</returns>
        /// <exception cref="Exception">Произвольное исключение при чтении файла с диска</exception>
        private static (bool success, string errorMessage) VerifyConfigiration()
        {
            string errorMessage = string.Empty;
            // Проверка, что токен бота задан
            if (string.IsNullOrEmpty(Storage.SystemSettings.BOT_SECRET))
            {
                errorMessage += "\t- Не указан параметр BOT_SECRET: зашифрованный токен телеграмм-бота" + Environment.NewLine;
            }
            // Проверка, что строка начала любой команды телеграмм-бота была задана
            if (string.IsNullOrEmpty(Storage.SystemSettings.BOT_INIT_TOKEN))
            {
                errorMessage += "\t- Не указан параметр BOT_INIT_TOKEN: с него начинается любая команда телеграмм-бота" + Environment.NewLine;

            }
            // Проверка, что ID администратора телеграмм-бота была задан
            if (Storage.SystemSettings.ROOT_ID == 0)
            {
                errorMessage += "\t- Не указан параметр ROOT_ID: только этот пользователь может выполнять системные команды телеграмм-бота" + Environment.NewLine;
            }
            // Проверка, что зашифрованная строка подключения к СУБД задана
            /*
             if (string.IsNullOrEmpty(Storage.SystemSettings.DB_SECRET))
            {
                errorMessage += "\t- Не указан параметр DB_SECRET: зашифрованная строка подключения к СУБД" + Environment.NewLine;
            }
             */
            // Проверка, что файл с ролями пользователей для бота существует и не пуст
            //if (!File.Exists(Storage.SystemSettings.RolesPath) || string.IsNullOrEmpty(File.ReadAllText(Storage.SystemSettings.RolesPath)))
            //{
            //    errorMessage += "\t- Не указан параметр RolesPath: путь к файлу с белым списом бота или файл пуст" + Environment.NewLine;
            //}
            // Проверка, что файл с действиями бота существует и не пуст
            if (!File.Exists(Storage.SystemSettings.ActionsPath) || string.IsNullOrEmpty(File.ReadAllText(Storage.SystemSettings.ActionsPath)))
            {
                errorMessage += "\t- Не указан параметр ActionsPath: путь к файлу с действиями бота или файл пуст" + Environment.NewLine;
            }
            // Если строка пустая, значит, ошибок нет
            if (string.IsNullOrEmpty(errorMessage)) return (true, "Верицикация конфигурации прошла успешно");
            // Если строка НЕ пустая, значит, есть ошибки
            return (false, $"В конфигурации пропущены некоторые обязательные параметры:{Environment.NewLine}{errorMessage}");
        }
        /// <summary>
        /// Эта функия применит конфигурацию, которая была ранее считана, а точнее расшифрует все необходимые поля для работы с СУБД и телеграмм-ботом
        /// </summary>
        /// <returns>Кортеж: true - если инициализация прошла успешно, *string - сообщение об ошибке</returns>
        private static (bool success, string errorMessage) DecryptSecrets()
        {
            Storage.PlaintextTelegramBotToken = Storage.SystemCriptor?.Decrypt(Storage.SystemSettings.BOT_SECRET) ?? "";
            //Storage.PlaintextConnectionString = Storage.SystemCriptor?.Decrypt(Storage.SystemSettings.DB_SECRET) ?? "";
            //if (string.IsNullOrEmpty(Storage.PlaintextConnectionString) || string.IsNullOrEmpty(Storage.PlaintextTelegramBotToken))
            if (string.IsNullOrEmpty(Storage.PlaintextTelegramBotToken))
                return (false, $"Не удалось применить конфигурацию: пустая строка токена бота после расшифровки");
            //return (false, $"Не удалось применить конфигурацию: пустая строка токена бота или строки подключения к СУБД после расшифровки");
            return (true, $"Расшифрованы приватные значения");
        }
        /// <summary>
        /// Метод для проверки подключения к СУБД
        /// </summary>
        /// <returns>Кортеж: true - если инициализация прошла успешно, *string - сообщение об ошибке</returns>
        private static (bool success, string errorMessage) StartDBConnection()
        {
            return (false, "Эта функция еще не написана");
        }
        /// <summary>
        /// Эта функция спарсит роли. Если не будет ни одной рабочей роли, вернет false
        /// </summary>
        /// <returns>Кортеж: true - если инициализация прошла успешно, *string - сообщение об ошибке</returns>
        //private static (bool success, string errorMessage) InitRoles()
        //{
        //    try
        //    {
        //        string fileContent = System.IO.File.ReadAllText(Storage.SystemSettings.ActionsPath);
        //        Preferences? p = JsonConvert.DeserializeObject<Preferences>(fileContent);
        //        if (p != null)
        //        {
        //            Storage.SystemSettings = p;
        //            return (true, "Конфигурация успешно прочитана");
        //        }
        //        return (false, "Не удалось применить конфигурациию: неверная структура JSON-файла");
        //    }
        //    catch (Exception configurationReadException)
        //    {
        //        return (false, configurationReadException.Message);
        //    }
        //    //return (false, "Эта функция еще не написана");
        //}
        /// <summary>
        /// Эта функция спарсит и построит дерево команд бота. Если не будет ни одной рабочей команды, вернет false
        /// </summary>
        /// <returns>Кортеж: true - если инициализация прошла успешно, *string - сообщение об ошибке</returns>
        private static (bool success, string errorMessage) InitCommands()
        {
            var settings = new JsonSerializerSettings
            {
                Converters = { new BotCommandConverter() },
                Formatting = Formatting.Indented
            };
            try
            {
                if (!File.Exists(Storage.SystemSettings.ActionsPath)) throw new Exception("Путь к файлу с действиями бота может быть пустым, либо файл не существует (ActionsPath)");
                string json = File.ReadAllText(Storage.SystemSettings.ActionsPath); // Читаем JSON из файла
                List<BotCommand> commands = JsonConvert.DeserializeObject<List<BotCommand>>(json, settings) ?? new List<BotCommand>();
                foreach (BotCommand command in commands)
                {
                    // Верификация команды
                    if (!command.Verify())
                    {
                        //Storage.Logger?.Logger.Warning($"Не удалось верифицировать команду \"{command.Name}\" - пропускается");
                        continue;
                    }
                    // Проверка, что команда является уникальной
                    if (Storage.BotCommands.FirstOrDefault(x => x.Name.Trim().ToLower() == command.Name.Trim().ToLower()) != null)
                    {
                        Storage.Logger?.Logger.Warning($"Найдена неуникальная команда \"{command.Name}\" - пропускается");
                        continue;
                    }
                    Storage.BotCommands.Add(command);
                    Storage.Logger?.Logger.Information($"Успешно добавлена команда \"{command.Name}\"");

                }
                if (Storage.BotCommands.Count > 0)
                {
                    return (true, $"Список команд пополнен. Доступно команд: {Storage.BotCommands.Count}");
                }
                else
                {
                    return (false, $"Список команд пуст: нет ни одной команды для бота");
                }
            }
            catch (Exception e)
            {
                return (false, $"Ошибка парсинга списка команд бота:{Environment.NewLine}{e.Message}");
                throw;
            }
        }
        /// <summary>
        /// Эта функция инициализирует телеграмм-бота по указанному токену
        /// </summary>
        /// <returns>Кортеж: true - если инициализация прошла успешно, *string - сообщение об ошибке</returns>
        private static (bool success, string errorMessage) InitTelegramBot()
        {
            try
            {
                Storage.botClient = new TGBot.TGBotClient();
                return (true, "Похоже, ты разобрался с конфигами :D\tИнициализация телеграмм-бота прошла успешно");
            }
            catch (Exception e)
            {
                return (false, $"Не удалось инициализировать телеграмм-бота:\n{e.Message}");
            }
        }
        #endregion
    }
}