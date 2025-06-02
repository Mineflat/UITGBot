using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using UITGBot.Core;

namespace UITGBot.TGBot
{
    internal class TGBotClient
    {
        public static CancellationTokenSource? cancellationToken { get; protected set; }
        public static ITelegramBotClient? botClient { get; protected set; }
        public static ReceiverOptions? receiverOptions { get; protected set; }
        public static int botErrorsLeft { get; set; } = 5;
        public static System.Timers.Timer? _errorTimer { get; protected set; }
        public TGBotClient()
        {
            _errorTimer = new System.Timers.Timer(30000);
            _errorTimer.Elapsed += (s, e) => Task.Run(CheckDropdown);
            _errorTimer.AutoReset = true;
            _errorTimer.Start();
            InitializeBot();
        }
        #region Системное
        /// <summary>
        /// Этот метод вызывается раз в несколько секунд и проверяет, как много ошибок за это время произошло. 
        /// Если их слишком много, приложение останавливается
        /// </summary>
        private void CheckDropdown()
        {
            if (botErrorsLeft > 0) { botErrorsLeft = 5; return; }
            Storage.Logger?.Logger.Fatal("Can't keep up! Too many errors occured in last 15 seconds");
            cancellationToken?.Cancel();
            Program.OnPanic();
        }
        #endregion
        #region Функциональное
        /// <summary>
        /// Функция инициализации телеграмм-бота. Здесь происходит его запуск
        /// </summary>
        protected void InitializeBot()
        {
            botErrorsLeft = 5;
            Storage.Logger?.Logger.Information("Starting Telegramm bot...");
            cancellationToken?.Cancel();
            receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = { }, // Принимаем все возможные апдейты от телеграм
                DropPendingUpdates = false // Не разрешаем боту забивать бот после перезапуска,
                                           // если сообщение получено до того, как он включился
            };
            cancellationToken = new CancellationTokenSource();
            botClient = new TelegramBotClient(Storage.PlaintextTelegramBotToken);
            botClient.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                receiverOptions,
                cancellationToken.Token
            );
            Storage.Logger?.Logger.Information($"Started bot: {botClient.GetMe(cancellationToken.Token).Result.Username}");
        }
        /// <summary>
        /// Функция обработки сообщений Телеграмм-ботом
        /// </summary>
        /// <param name="client">Интерфейс обработчика сообщений телеграмм-бота</param>
        /// <param name="update">Обновление, получаемое с серверов телеграмм (сообщение)</param>
        /// <param name="token">Токен для управления отменой запроса</param>
        private async Task HandleUpdateAsync(ITelegramBotClient client, Update update, CancellationToken token)
        {
            if (update.Type != Telegram.Bot.Types.Enums.UpdateType.Message) return;
            var userID = update.Message?.From?.Id; // Компилятор долбоеб. Забавно, но если сунуть это
                                                   // в IF, он скажет что долбоеб я. Семейка долбоебов крч
            if (userID == null) return;
            if (string.IsNullOrEmpty(update.Message?.Text) && string.IsNullOrEmpty(update.Message?.Caption)) return;
            string userName = string.IsNullOrEmpty(update.Message.From?.Username)
                ? update.Message.From?.Id.ToString() ?? "Unknown"
                : update.Message.From.Username;

            string? msgText = update.Message.Text ?? update.Message.Caption;
            // Логирование
            if (string.IsNullOrEmpty(msgText)) return;
            Storage.Logger?.Logger.Information($"[MESSAGE]" +
                $"[{(string.IsNullOrEmpty(update.Message.Chat.Title) ? $"{update.Message.Chat.Id}" : update.Message.Chat.Title)}]" +
                $"[{userName}]: " +
                $"{update.Message.Text ?? update.Message.Caption}");
            msgText = msgText.ToLower().Trim();
            if (!msgText.StartsWith(Storage.SystemSettings.BOT_INIT_TOKEN.ToLower().Trim())) return;
            msgText = msgText.Replace(Storage.SystemSettings.BOT_INIT_TOKEN.ToLower().Trim(), "");
            msgText = msgText.Trim();
            // Проверка, что это именно команда, а не какая-то дроч
            //BotCommand? selectedCommand = Storage.BotCommands.FirstOrDefault(x => x.Name.ToLower().Trim() == msgText);
            string[] keywords = msgText.Split(' ');
            // Новая логика по ключевому слову. Берем первую часть /[keyword] и ищем уже по ней
            BotCommand? selectedCommand = Storage.BotCommands.FirstOrDefault(x => x.Name.ToLower().Trim() == msgText.ToLower()); 
            
            if (selectedCommand == null || !selectedCommand.Enabled)
            {
                selectedCommand = Storage.BotCommands.FirstOrDefault(x => x.Name.ToLower().Trim() == keywords[0].ToLower());
                if (selectedCommand == null || !selectedCommand.Enabled) return;

            }
            // Проверка, что пользователь с указанным From.ID может выполнять эту команду
            if (!selectedCommand.IsPublic)
                if (!selectedCommand.UserIDs.Contains(userID.Value)) return;

            //await InvokeCommand(selectedCommand, client, update, token); // Очково, что это заблокитруется поток.
            //                                                             // Но он вроде как на каждый прилет создается,
            //                                                             // так что похуй наверное
            // Отбой, больше не очково
            // Очково с таких мувов будет ЦП и ОЗУ сервера, когда бота начнут дудосить
            // UPD: все еще очково, но теперь оно ебашит с логами :D
            Storage.Logger?.Logger.Information($"Запущена команда {keywords[0]} пользователем {userName}");
            //Storage.Logger?.Logger.Information(Newtonsoft.Json.JsonConvert.SerializeObject(selectedCommand, Newtonsoft.Json.Formatting.Indented));
            try
            {
                if (selectedCommand.IgnoreMessageText)
                {
                    await selectedCommand.ExecuteCommand(client, update, token);
                }
                else
                {
                    if (keywords.Length == 1)
                    {
                        await selectedCommand.ExecuteCommand(client, update, token);
                    }
                    else
                    {
                        Storage.Logger?.Logger.Warning($"Команда {selectedCommand.Name} не может быть выполнена, т.к. передано слишком много аргументов");
                        await BotCommand.SendMessage($"Команда {selectedCommand.Name} не может быть выполнена, т.к. передано слишком много аргументов", false, client, update, token);
                    }
                }
            }
            catch (Exception e)
            {
                Storage.Logger?.Logger.Error($"Ошибка при выполнении команды \"{selectedCommand.Name}\":\n{e.Message}");
                throw;
            }
        }
        //private async Task<(bool success, string errorMessage, bool selfSending)> InvokeCommand(BotCommand cmd, ITelegramBotClient client, Update update, CancellationToken token)
        //{
        //    var executionResult = await cmd.ExecuteCommand(client, update, token);
        //    string replyText = CryptoRandomizer.GetRandomReply(cmd, executionResult.success);
        //    return (executionResult.success, replyText + Environment.NewLine + executionResult.errorMessage, executionResult.selfSending);
        //}
        /// <summary>
        /// Функция обработки ошибок Телеграмм-бота
        /// </summary>
        /// <param name="botClient">Интерфейс обработчика сообщений телеграмм-бота</param>
        /// <param name="exception">Генерируемое исключение</param>
        /// <param name="cancellationToken">Токен для управления отменой запроса</param>
        protected static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            Storage.Logger?.Logger.Error(exception.Message);
            botErrorsLeft -= 1;
            return Task.CompletedTask;
        }
        #endregion

    }
}