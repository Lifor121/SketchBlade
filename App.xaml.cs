using System.Configuration;
using System.Data;
using System.Windows;
using System;
using System.IO;
using SketchBlade.ViewModels;
using SketchBlade.Services;
using SketchBlade.Models;
using SketchBlade.Utilities;

namespace SketchBlade;

/// <summary>
/// Логика взаимодействия для App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        try
        {
            // Устанавливаем уровень логирования на Debug для максимальной детализации
            LoggingService.SetLogLevel(LoggingService.LogLevel.Debug);
            
            // Устанавливаем обработчики исключений как можно раньше
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;
            
            base.OnStartup(e);
            
            EnsureAssetsAvailable();
            
            // Предзагружаем критические ресурсы
            _ = ResourceService.Instance.PreloadCriticalResourcesAsync();
            
            PreloadAndFreezeImages();
            
            RegisterEssentialServices();
        }
        catch (Exception ex)
        {
            LoggingService.LogError($"ERROR during startup: {ex.Message}", ex);
            
            MessageBox.Show($"Warning: Some assets may not load correctly. Error: {ex.Message}", 
                          "SketchBlade Asset Warning", 
                          MessageBoxButton.OK, 
                          MessageBoxImage.Warning);
        }
    }
    
    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        try
        {
            Exception? ex = e.ExceptionObject as Exception;
            string errorMessage = ex != null ? ex.Message : "Unknown error occurred";
            
            LoggingService.LogError($"UNHANDLED EXCEPTION: {errorMessage}", ex);
        }
        catch
        {
            // Игнорируем ошибки логирования
        }
    }
    
    private void Current_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        try
        {
            LoggingService.LogError($"UI THREAD EXCEPTION: {e.Exception.Message}", e.Exception);
            
            MessageBox.Show($"An error occurred: {e.Exception.Message}\r\nThe application will continue running, but some features may not work correctly.",
                          "SketchBlade Error",
                          MessageBoxButton.OK,
                          MessageBoxImage.Error);
            
            e.Handled = true;
        }
        catch
        {
            // Игнорируем ошибки обработки исключений
        }
    }
    
    private void PreloadAndFreezeImages()
    {
        try
        {
            var criticalImagePaths = new[]
            {
                AssetPaths.DEFAULT_IMAGE,
                AssetPaths.Characters.PLAYER,
                AssetPaths.Characters.NPC,
                AssetPaths.Characters.HERO,
                AssetPaths.Weapons.WOODEN_SWORD,
                AssetPaths.Consumables.HEALING_POTION
            };
            
            foreach (var path in criticalImagePaths)
            {
                try
                {
                    var image = ResourceService.Instance.GetImage(path);
                    if (image != null && image.CanFreeze && !image.IsFrozen)
                    {
                        image.Freeze();
                    }
                }
                catch (Exception ex)
                {
                    LoggingService.LogError($"Error preloading image {path}: {ex.Message}", ex);
                }
            }
            
            // Создаем пустое изображение для fallback
            try 
            {
                var emptyImage = ResourceService.Instance.GetImage(AssetPaths.DEFAULT_IMAGE);
                if (emptyImage != null && emptyImage.CanFreeze && !emptyImage.IsFrozen)
                {
                    emptyImage.Freeze();
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error creating empty image: {ex.Message}", ex);
            }
        }
        catch (Exception ex)
        {
            LoggingService.LogError($"Error in PreloadAndFreezeImages: {ex.Message}", ex);
        }
    }
    
    private void EnsureAssetsAvailable()
    {
        try
        {
            string execDir = AppDomain.CurrentDomain.BaseDirectory;
            
            // Проверяем наличие основных папок
            string[] requiredDirectories = {
                Path.Combine(execDir, "Assets"),
                Path.Combine(execDir, "Assets", "Images"),
                Path.Combine(execDir, "Assets", "Images", "Locations"),
                Path.Combine(execDir, "Assets", "Images", "Characters"),
                Path.Combine(execDir, "Assets", "Images", "items"),
                Path.Combine(execDir, "Assets", "Images", "items", "weapons"),
                Path.Combine(execDir, "Assets", "Images", "items", "armor"),
                Path.Combine(execDir, "Assets", "Images", "items", "consumables"),
                Path.Combine(execDir, "Assets", "Images", "items", "materials")
            };
            
            foreach (var dir in requiredDirectories)
            {
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                    LoggingService.LogDebug($"Created directory: {dir}");
                }
            }
            
            // Проверяем наличие критически важного файла def.png
            string defPngPath = Path.Combine(execDir, "Assets", "Images", "def.png");
            if (!File.Exists(defPngPath))
            {
                LoggingService.LogError($"CRITICAL: Default image (def.png) not found at {defPngPath}");
                LoggingService.LogError("The game may not function correctly without the default image file.");
            }
            else
            {
                LoggingService.LogDebug($"Default image found at {defPngPath}");
            }
        }
        catch (Exception ex)
        {
            LoggingService.LogError($"Error ensuring assets are available: {ex.Message}", ex);
        }
    }

    private void RegisterEssentialServices()
    {
        try
        {
            var GameData = new Models.GameData();
            if (GameData != null)
            {
                Application.Current.Resources["GameData"] = GameData;
            }
            
            LoggingService.LogDebug("Essential services registration complete");
        }
        catch (Exception ex)
        {
            LoggingService.LogError($"Error registering essential services: {ex.Message}", ex);
        }
    }

    private void Application_Startup(object sender, StartupEventArgs e)
    {
        try
        {
            // Инициализируем сервисы перед созданием главного окна
            InitializeServices();
            
            // Создаем основное окно приложения
            var mainWindow = new MainWindow();
            
            // Устанавливаем его как главное окно
            MainWindow = mainWindow;
            
            // Показываем окно
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            // Критическая ошибка запуска
            string errorDetails = $"Critical error during application startup:\n\n" +
                                 $"Message: {ex.Message}\n" +
                                 $"Stack Trace: {ex.StackTrace}";
            MessageBox.Show(errorDetails, "Application Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
            
            // Записываем в лог и завершаем приложение
            LoggingService.LogError("Application startup failed", ex);
            Environment.Exit(1);
        }
    }
    
    private void InitializeServices()
    {
        try
        {
            // Инициализируем ConfigService первым
            var configService = ConfigService.Instance;
            
            // Устанавливаем уровень логирования из конфигурации
            var loggingLevelStr = configService.GetValue(ConfigService.LOGGING_LEVEL, "Debug");
            if (Enum.TryParse<LoggingService.LogLevel>(loggingLevelStr, out var loggingLevel))
            {
                LoggingService.SetLogLevel(loggingLevel);
            }
            else
            {
                // Временно устанавливаем Debug уровень для отладки навигации
                LoggingService.SetLogLevel(LoggingService.LogLevel.Debug);
            }
            
            // Инициализируем остальные сервисы
            var resourceService = ResourceService.Instance;
            var localizationService = LocalizationService.Instance;
            var gameLogicService = GameLogicService.Instance;
            var notificationService = NotificationService.Instance;
            var uiService = UIService.Instance;
            
            LoggingService.LogInfo("All services initialized successfully");
        }
        catch (Exception ex)
        {
            LoggingService.LogError($"Error initializing services: {ex.Message}", ex);
            MessageBox.Show($"Ошибка инициализации сервисов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Application_Exit(object sender, ExitEventArgs e)
    {
        try
        {
            LoggingService.LogDebug("Application is shutting down");
            
            // Останавливаем все активные сервисы
            CoreGameService.Instance?.Dispose();
            
            LoggingService.LogDebug("Application shutdown completed");
        }
        catch (Exception ex)
        {
            // Логируем ошибку при завершении, но не прерываем процесс закрытия
            LoggingService.LogError("Error during application shutdown", ex);
        }
    }

    private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        try
        {
            string errorMessage = $"Unhandled exception in application:\n\n" +
                                 $"Message: {e.Exception.Message}\n" +
                                 $"Type: {e.Exception.GetType().Name}";

            LoggingService.LogError("Unhandled exception", e.Exception);

            MessageBox.Show(errorMessage, "Unexpected Error", MessageBoxButton.OK, MessageBoxImage.Error);
            
            // Помечаем исключение как обработанное, чтобы приложение не закрылось
            e.Handled = true;
        }
        catch (Exception logEx)
        {
            // Если даже логирование не работает, просто показываем минимальное сообщение
            MessageBox.Show($"Critical error: {e.Exception.Message}", "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Безопасный метод для записи сообщений в файл логов в критических ситуациях
    /// </summary>
    private void SafeLogToFile(string message)
    {
        try
        {
            var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [CRITICAL] {message}\r\n";
            File.AppendAllText("critical_error_log.txt", logEntry);
        }
        catch
        {
            // Если не можем писать в файл, игнорируем
        }
    }
}


