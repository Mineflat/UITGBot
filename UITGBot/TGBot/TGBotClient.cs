using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using UITGBot.Core;
using UITGBot.Logging;

namespace UITGBot.TGBot
{
    internal class TGBotClient
    {
        public static string BotName { get; private set; } = string.Empty;
        public static CancellationTokenSource? cancellationToken { get; protected set; }
        public static ITelegramBotClient? botClient { get; protected set; }
        public static ReceiverOptions? receiverOptions { get; protected set; }
        public static int botErrorsLeft { get; private set; } = 5;

        public static System.Timers.Timer? _errorTimer { get; protected set; }
        public TGBotClient()
        {
            if (!Storage.SystemSettings.IgnoreErrors)
            {
                _errorTimer = new System.Timers.Timer(30000);
                _errorTimer.Elapsed += (s, e) => Task.Run(CheckDropdown);
                _errorTimer.AutoReset = true;
                _errorTimer.Start();
            }
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
            UILogger.AddLog("Can't keep up! Too many errors occured in last 15 seconds", "FATAL");
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
            UILogger.AddLog("Starting Telegramm bot...");
            cancellationToken?.Cancel();
            receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = { }, // Принимаем все возможные апдейты от телеграм
                DropPendingUpdates = false // Не разрешаем боту забивать на сообщение после перезапуска,
                                           // если оно получено до того, как он включился
            };
            cancellationToken = new CancellationTokenSource();
            botClient = new TelegramBotClient(Storage.PlaintextTelegramBotToken);
            botClient.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                receiverOptions,
                cancellationToken.Token
            );
            BotName = botClient.GetMe(cancellationToken.Token).Result.Username ?? "[не определено]";
            UILogger.AddLog($"Started bot @{BotName}");
            Logging.UILogger.InitUILogger();
            Storage.SetupOK = true;
            Core.UIRenderer.RestartUI();
        }
        /// <summary>
        /// Функция обработки сообщений Телеграмм-ботом
        /// </summary>
        /// <param name="client">Интерфейс обработчика сообщений телеграмм-бота</param>
        /// <param name="update">Обновление, получаемое с серверов телеграмм (сообщение)</param>
        /// <param name="token">Токен для управления отменой запроса</param>
        private async Task HandleUpdateAsync(ITelegramBotClient client, Update update, CancellationToken token)
        {
            // Базовые проверки обновления
            if (update.Type != Telegram.Bot.Types.Enums.UpdateType.Message) return;
            if (string.IsNullOrEmpty(update.Message?.Text) && string.IsNullOrEmpty(update.Message?.Caption)) return;
            var userID = update.Message?.From?.Id; // Компилятор долбоеб. Забавно, но если сунуть это
                                                   // в IF, он скажет что долбоеб я. Семейка долбоебов крч
                                                   // Проверка ID пользователя и поиск юзернейма
            if (userID == null) return;
            if (update.Message == null) return;

            string userName = string.IsNullOrEmpty(update.Message.From?.Username)
                ? update.Message.From?.Id.ToString() ?? "Unknown"
                : update.Message.From.Username;
            // Добавляем неизвестный чат в список чатов
            var currentChat = Storage.CurrenetChats
                .FirstOrDefault(x => x.CurrentChat.Id == update.Message.Chat.Id);
            if (currentChat == null)
            {
                Storage.CurrenetChats.Add(new Core.Messaging.ChatActivity(update.Message.Chat));
                currentChat = Storage.CurrenetChats.FirstOrDefault(x => x.CurrentChat.Id == update.Message.Chat.Id); // Вопрос: а зачем? А затем, что его до этого момента не было там
                Storage.Statisticks.botChatsKnown++;
            }
            currentChat?.UpdateChatStory(update.Message); // Добавляем сообщение в пул чатов
            Storage.Statisticks.botMessagesReceived++;
            if (update.Message.ForwardFrom != null) return; // Чтобы бот не воспринимал пересланные сообщения как команды
            // Обработка текста сообщения (проверка на пустоту)
            string? msgText = update.Message.Text ?? update.Message.Caption;
            if (string.IsNullOrEmpty(msgText)) return;
            msgText = msgText.ToLower().Trim();
            // Логирование
            UILogger.AddLog(
                $"[[{(string.IsNullOrEmpty(update.Message.Chat.Title) ? $"{update.Message.Chat.Id}" : update.Message.Chat.Title)}]]" +
                $"[[{userName}]]: " +
                $"{msgText}", "MESSAGE");

            // Проверка, что это именно команда, а не какая-то дроч
            if (!msgText.StartsWith(Storage.SystemSettings.BOT_INIT_TOKEN.ToLower().Trim())) return;
            msgText = msgText.Replace(Storage.SystemSettings.BOT_INIT_TOKEN.ToLower().Trim(), "");
            msgText = msgText.Trim();
            // Получаем команду и проверяем. Здесь использоавн новый удобный формат логирования
            // (при чем, двухуровневый: одибки до и после запуска команды будут отливаться)
            UpdateHandleResult proccessResult = SearchValidCommand(msgText, userID);
            if (proccessResult.HasErrors)
            {
                UILogger.AddLog($"{proccessResult.ErrorMessage}", "ERROR");
                if (!string.IsNullOrEmpty(proccessResult.ReplyMessage))
                    await BotCommand.SendMessage($"{proccessResult.ReplyMessage}", false, client, update, token);
                return;
            }
            else UILogger.AddLog($"{proccessResult.ErrorMessage}");
            if (proccessResult.SelectedCommand != null)
            {
                try
                {
                    UILogger.AddLog($"Выполнение команды \"{proccessResult.SelectedCommand.Name}\" пользователем {userName} ...");
                    Storage.Statisticks.botMessagesProccessed++;
                    await proccessResult.SelectedCommand.ExecuteCommand(client, update, token);
                    // Выполнение каскадной команды (независимо от результата выполнения предыдущей команды)
                    //if (proccessResult.SelectedCommand.RunAfter != null)
                    //{
                    //    if (proccessResult.SelectedCommand.RunAfter.Enabled)
                    //    {
                    //        UILogger.AddLog($"Выполнение КАСКАДНОЙ команды: {proccessResult.SelectedCommand.Name} => {proccessResult.SelectedCommand.RunAfter.Name}");
                    //        await proccessResult.SelectedCommand.RunAfter.ExecuteCommand(client, update, token);
                    //    }
                    //}
                }
                catch (Exception e)
                {
                    UILogger.AddLog($"Ошибка при выполнении команды \"{proccessResult.SelectedCommand.Name}\":\n{e.Message}", "ERROR");
                }
            }
            else UILogger.AddLog($"Ошибка при обработке списка команд: команда не была найдена, но метод SearchValidCommand не вернул HasErrors = true", "ERROR");
        }
        /// <summary>
        /// Функция обработки ошибок Телеграмм-бота
        /// </summary>
        /// <param name="botClient">Интерфейс обработчика сообщений телеграмм-бота</param>
        /// <param name="exception">Генерируемое исключение</param>
        /// <param name="cancellationToken">Токен для управления отменой запроса</param>
        protected static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            UILogger.AddLog(exception.Message, "ERROR");
            botErrorsLeft -= 1;
            return Task.CompletedTask;
        }
        /// <summary>
        /// Выполняет поиск по командам на основе сообщения пользователя и его идентификатора. 
        /// Определяет, является ли его сообщение командой и возможно ли ее выполнение
        /// </summary>
        /// <param name="message">Текст сообщения для поиска по командам</param>
        /// <param name="userID">ID пользователя, который хочет выполнить команду</param>
        /// <returns></returns>
        protected static UpdateHandleResult SearchValidCommand(string message, long? userID)
        {
            string ErrorMessage = string.Empty;
            string ReplyMessage = string.Empty;
            bool hasErrors = false;

            BotCommand? selectedCommand = Storage.BotCommands.FirstOrDefault(x => x.Name.ToLower().Trim() == message.ToLower());
            // Если не получается найти команду по ее полному имени, то ищем только по первой части (остальное считаем аргументами)
            if (selectedCommand == null) selectedCommand = Storage.BotCommands.FirstOrDefault(x => x.Name.ToLower().Trim() == message.Split(' ')[0].ToLower());
            // Обработка найденных команд
            if (selectedCommand == null)
            {
                hasErrors = true;
                ErrorMessage = $"Не удалось найти ни одной подходящей команды по ее основному имени\n        > {userID}: \"{message}\"";
                // Тут будет логика поиска команды по ее альтернативным именам (если есть)

            }
            else
            {
                /// Приоритет определения ошибок для команды:
                /// 1. -> Недоступность команды 
                /// 2. -> Нет прав на исполнение команды (неактивна) 
                /// 3. -> БАН пользователя на исполнение команды 
                /// 4. -> Неверное кол-во аргументов к команде 
                // Проверка, что команда на самом деле является исполняемой 
                if (!selectedCommand.Enabled)
                {
                    hasErrors = true;
                    ReplyMessage = $"Эта команда сейчас недоступна";
                    ErrorMessage = $"Найдена подходящая команда, но она отключена" +
                        $"        > {userID}: \"{selectedCommand.Name}\" != \"{message}\"";
                    return new UpdateHandleResult(ErrorMessage, ReplyMessage, hasErrors, selectedCommand);
                }
                // Проверяет, что пользователь имеет права на выполнение этой команды.
                // ВАЖНО: Это не уязвимость, потому что в телеграмм невозможно сделать UserID непубличным.
                // Он всегда придет в запросе. В любом случае, проверка на NULL есть выше, в методе HandleUpdateAsync()
                if (!selectedCommand.IsPublic && !selectedCommand.UserIDs.Contains(userID ?? 0))
                {
                    hasErrors = true;
                    ReplyMessage = $"Эта команда не для тебя";
                    ErrorMessage = $"Найдена подходящая команда, но у пользователя нет прав на ее выполнение" +
                        $"        > {userID}: \"{selectedCommand.Name}\" != \"{message}\"";
                    return new UpdateHandleResult(ErrorMessage, ReplyMessage, hasErrors, selectedCommand);
                }
                // Проверяем, не числится этот ID пользователя в черном списке для этой команды
                if (selectedCommand.BannedUserIDs.Contains(userID ?? 0))
                {
                    hasErrors = true;
                    ReplyMessage = $"Администратор ограничил для тебя доступ к этой команде";
                    ErrorMessage = $"Найдена подходящая команда, но пользоватлелю не разрешено ее выполнить (персональный БАН)" +
                        $"        > {userID}: \"{selectedCommand.Name}\" != \"{message}\"";
                    return new UpdateHandleResult(ErrorMessage, ReplyMessage, hasErrors, selectedCommand);
                }
                // Парсинг аргументов команды (существуют и разрешены ли они) 
                if ((!selectedCommand.IgnoreMessageText) && (selectedCommand.Name.Trim().ToLower() != message.Trim().ToLower()))
                {
                    hasErrors = true;
                    ReplyMessage = $"Эта команда не подразумевает аргументов";
                    ErrorMessage = $"Найдена подходящая команда, но ей передано слишком много аргументов\n" +
                        $"        > {userID}: \"{selectedCommand.Name}\" != \"{message}\"";
                    return new UpdateHandleResult(ErrorMessage, ReplyMessage, hasErrors, selectedCommand);
                }
                ErrorMessage = $"Найдена подходящая команда\n        > {userID}: \"{selectedCommand.Name}\"";
            }
            return new UpdateHandleResult(ErrorMessage, ReplyMessage, hasErrors, selectedCommand);
        }
        #endregion

    }
}