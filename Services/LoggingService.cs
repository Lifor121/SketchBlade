using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Linq;
using SketchBlade.Utilities;

namespace SketchBlade.Services
{
    public static class LoggingService
    {
        private static readonly string LogFilePath = Path.Combine(ResourcePathManager.LogsPath, "error_log.txt");
        private static readonly object LogLock = new object();
        private static LogLevel _currentLogLevel = LogLevel.Debug;
        
        // ОПТИМИЗАЦИЯ ПРОИЗВОДИТЕЛЬНОСТИ: Флаг для быстрого включения/выключения debug логирования
        // Установите в true только для отладки критических проблем
        public static bool EnableDebugLogging = true;
        
        private const long MAX_LOG_FILE_SIZE = 10 * 1024 * 1024;
        
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
                // Ensure the log directory exists
                var logDirectory = Path.GetDirectoryName(LogFilePath);
                if (!string.IsNullOrEmpty(logDirectory) && !Directory.Exists(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory);
                }
                
                CleanupLogFileIfNeeded();
                // LogInfo("НОВЫЙ ЗАПУСК ПРИЛОЖЕНИЯ");
            }
            catch (Exception ex)
            {
                // Failed to initialize logging - continue silently
                Console.WriteLine($"Failed to initialize logging: {ex.Message}");
            }
        }

        public static void SetLogLevel(LogLevel level)
        {
            _currentLogLevel = level;
        }

        public static void LogDebug(string message)
        {
            // ОПТИМИЗАЦИЯ: Быстрый выход, если debug логирование отключено
            if (!EnableDebugLogging)
                return;
                
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
                    // Ensure the log directory exists before writing
                    var logDirectory = Path.GetDirectoryName(LogFilePath);
                    if (!string.IsNullOrEmpty(logDirectory) && !Directory.Exists(logDirectory))
                    {
                        Directory.CreateDirectory(logDirectory);
                    }
                    
                    File.AppendAllText(LogFilePath, message + Environment.NewLine);
                    
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
        
        private static void CleanupLogFileIfNeeded()
        {
            try
            {
                if (!File.Exists(LogFilePath))
                    return;

                var fileInfo = new FileInfo(LogFilePath);
                
                if (fileInfo.Length > MAX_LOG_FILE_SIZE)
                {
                    CleanupLogFile();
                    return;
                }
                
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

                // LogInfo("ЛОГ ОЧИЩЕН");                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error cleaning up log file: {ex.Message}");
            }
        }
        
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