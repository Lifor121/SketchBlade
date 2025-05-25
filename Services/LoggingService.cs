using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Linq;

namespace SketchBlade.Services
{
    /// <summary>
    /// Упрощенный сервис логирования - только критические ошибки
    /// </summary>
    public static class LoggingService
    {
        private static readonly string LogFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error_log.txt");
        private static readonly object LogLock = new object();
        private static LogLevel _currentLogLevel = LogLevel.Debug;
        
        // Максимальный размер файла логов в байтах (10 МБ)
        private const long MAX_LOG_FILE_SIZE = 10 * 1024 * 1024;
        
        // Максимальное количество строк в логе (50000 строк)
        private const int MAX_LOG_LINES = 50000;

        public enum LogLevel
        {
            Debug = 0,
            Info = 1,
            Warning = 2,
            Error = 3
        }

        static LoggingService()
        {
            try
            {
                // Проверяем и очищаем лог при запуске, если он слишком большой
                CleanupLogFileIfNeeded();
                
                // Записываем заголовок нового сеанса
                File.AppendAllText(LogFilePath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] === НОВЫЙ ЗАПУСК ПРИЛОЖЕНИЯ ==={Environment.NewLine}");
            }
            catch (Exception ex)
            {
                // Если не можем записать в лог, выводим в консоль
                Console.WriteLine($"Failed to initialize logging: {ex.Message}");
            }
        }

        public static void SetLogLevel(LogLevel level)
        {
            _currentLogLevel = level;
        }

        public static void LogDebug(string message)
        {
            if (_currentLogLevel <= LogLevel.Debug)
            {
                WriteLog("DEBUG", message);
            }
        }

        public static void LogInfo(string message)
        {
            if (_currentLogLevel <= LogLevel.Info)
            {
                WriteLog("INFO", message);
            }
        }

        public static void LogWarning(string message)
        {
            if (_currentLogLevel <= LogLevel.Warning)
            {
                WriteLog("WARNING", message);
            }
        }

        public static void LogError(string message, Exception? exception = null)
        {
            string fullMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [ERROR] {message}";
            if (exception != null)
            {
                fullMessage += $"{Environment.NewLine}Exception: {exception}";
            }

            WriteToFile(fullMessage);
        }

        private static void WriteLog(string level, string message)
        {
            string fullMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}";
            WriteToFile(fullMessage);
        }

        private static void WriteToFile(string message)
        {
            try
            {
                lock (LogLock)
                {
                    File.AppendAllText(LogFilePath, message + Environment.NewLine);
                    
                    // Периодически проверяем размер файла
                    if (new Random().Next(100) == 0) // Проверяем в 1% случаев
                    {
                        CleanupLogFileIfNeeded();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to write to log file: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Очищает файл логов, если он превышает максимальный размер
        /// </summary>
        private static void CleanupLogFileIfNeeded()
        {
            try
            {
                if (!File.Exists(LogFilePath))
                    return;

                var fileInfo = new FileInfo(LogFilePath);
                
                // Проверяем размер файла
                if (fileInfo.Length > MAX_LOG_FILE_SIZE)
                {
                    CleanupLogFile();
                    return;
                }
                
                // Проверяем количество строк
                var lineCount = File.ReadAllLines(LogFilePath).Length;
                if (lineCount > MAX_LOG_LINES)
                {
                    CleanupLogFile();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking log file size: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Очищает файл логов, сохраняя только последние записи
        /// </summary>
        private static void CleanupLogFile()
        {
            try
            {
                if (!File.Exists(LogFilePath))
                    return;

                var allLines = File.ReadAllLines(LogFilePath);
                
                // Сохраняем только последние 25% записей
                int linesToKeep = Math.Max(1000, allLines.Length / 4);
                var linesToSave = allLines.Skip(allLines.Length - linesToKeep).ToArray();
                
                // Создаем резервную копию
                string backupPath = LogFilePath + ".backup";
                if (File.Exists(backupPath))
                {
                    File.Delete(backupPath);
                }
                File.Move(LogFilePath, backupPath);
                
                // Записываем очищенный лог
                File.WriteAllLines(LogFilePath, new[] { 
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] === ЛОГ ОЧИЩЕН (сохранены последние {linesToKeep} записей) ===" 
                }.Concat(linesToSave));
                
                Console.WriteLine($"Log file cleaned up. Kept {linesToKeep} lines out of {allLines.Length}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error cleaning up log file: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Принудительно очищает файл логов
        /// </summary>
        public static void ClearLogs()
        {
            try
            {
                lock (LogLock)
                {
                    if (File.Exists(LogFilePath))
                    {
                        File.Delete(LogFilePath);
                    }
                    
                    File.AppendAllText(LogFilePath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] === ЛОГИ ОЧИЩЕНЫ ВРУЧНУЮ ==={Environment.NewLine}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error clearing logs: {ex.Message}");
            }
        }
    }
} 