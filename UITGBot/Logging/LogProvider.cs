using UITGBot.Core;
using Serilog;

namespace UITGBot.Logging
{
    internal class LogProvider
    {
        public ILogger Logger { get; protected set; }
        public LogProvider(bool whiteConsole = true)
        {

            if (!Directory.Exists(Storage.SystemSettings.LogDirectory))
                throw new Exception("Не указан ни 1 из необходимых параметров: LogDirectory");
            try
            {
                if (whiteConsole)
                {
                    Logger = new LoggerConfiguration()
                    .WriteTo.Console()
                    .WriteTo.File($"{Storage.SystemSettings.LogDirectory}/tgbot_.log", rollingInterval: RollingInterval.Day,
                        fileSizeLimitBytes: Storage.SystemSettings.LogFileSizeLimitMB * 1024 * 1024, retainedFileCountLimit: Storage.SystemSettings.LogRetainedFileCountLimit)
                    .CreateLogger();
                }
                else
                {
                    Logger = new LoggerConfiguration()
                    .WriteTo.File($"{Storage.SystemSettings.LogDirectory}/tgbot_.log", rollingInterval: RollingInterval.Day,
                        fileSizeLimitBytes: Storage.SystemSettings.LogFileSizeLimitMB * 1024 * 1024, retainedFileCountLimit: Storage.SystemSettings.LogRetainedFileCountLimit)
                    .CreateLogger();
                }
                Logger.Information("Инициализация системы логирования");
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}