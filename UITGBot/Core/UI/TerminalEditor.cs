using System.Diagnostics;

namespace UITGBot.Core.UI
{
    internal static class TerminalEditor
    {
        /// <summary>
        /// Открывает внешний редактор (из переменной $EDITOR или nano по-умолчанию),
        /// даёт пользователю отредактировать текст и возвращает финальный результат.
        /// </summary>
        public static string Edit(string currentText)
        {
            // 1. Создаём временный файл
            var tmpFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + Guid.NewGuid() + ".txt");
            File.WriteAllText(tmpFile, currentText ?? "");

            try
            {
                // 2. Определяем, какой редактор запускать
                //    Пользователь мог задать $EDITOR, иначе будем использовать nano
                var editor = Environment.GetEnvironmentVariable("EDITOR")
                             ?? "nano";

                // 3. Запускаем процесс и ждём его выхода
                var psi = new ProcessStartInfo
                {
                    FileName = editor,
                    Arguments = tmpFile,
                    RedirectStandardInput = false,
                    RedirectStandardOutput = false,
                    UseShellExecute = true,   // чтобы подхватить терминал
                };
                using var proc = Process.Start(psi);
                proc?.WaitForExit();

                // 4. Читаем результат
                return File.ReadAllText(tmpFile);
            }
            finally
            {
                // 5. Убираем временный файл
                try { File.Delete(tmpFile); } catch { /* ignore */ }
            }
        }
    }
}
