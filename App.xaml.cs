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
/// ������ �������������� ��� App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            // Обработчики ошибок
            this.DispatcherUnhandledException += Current_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            // Инициализация ResourcePathManager и проверка внешней папки Resources
            ResourcePathManager.EnsureResourceDirectoriesExist();
            
            if (!ResourcePathManager.ResourcesDirectoryExists())
            {
                MessageBox.Show($"Папка Resources не найдена рядом с исполняемым файлом.\n" +
                              $"Ожидаемое расположение: {ResourcePathManager.ResourcesBasePath}\n\n" +
                              "Приложение может работать некорректно без необходимых ресурсов.",
                              "Внимание: Ресурсы не найдены",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            // Инициализация основных сервисов
            InitializeServices();

            // Обеспечиваем наличие папок и файлов
            EnsureAssetsAvailable();

            // Предзагрузка критических ресурсов
            PreloadAndFreezeImages();

            // Регистрация основных компонентов
            RegisterEssentialServices();
        }
        catch (Exception ex)
        {
            LoggingService.LogError("Error during application startup", ex);
            MessageBox.Show($"Ошибка запуска приложения: {ex.Message}", "Критическая ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            Environment.Exit(1);
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
            // ���������� ������ �����������
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
            // ���������� ������ ��������� ����������
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
            
            // ������� ������ ����������� ��� fallback
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
            // Проверяем наличие критически важного файла def.png
            string defPngPath = AssetPaths.DEFAULT_IMAGE;
            if (!File.Exists(defPngPath))
            {
                LoggingService.LogError($"CRITICAL: Default image (def.png) not found at {defPngPath}");
                LoggingService.LogError("The game may not function correctly without the default image file.");
            }
            else
            {
                // LoggingService.LogDebug($"Default image found at {defPngPath}");
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
                
                // После создания GameData применяем загруженные настройки
                ApplyLoadedSettings(GameData);
            }
            
            // LoggingService.LogDebug("Essential services registration complete");
        }
        catch (Exception ex)
        {
            LoggingService.LogError($"Error registering essential services: {ex.Message}", ex);
        }
    }

    private void ApplyLoadedSettings(Models.GameData gameData)
    {
        try
        {
            if (gameData?.Settings != null)
            {
                // Применяем язык к LocalizationService
                LocalizationService.Instance.CurrentLanguage = gameData.Settings.Language;
                
                // Применяем язык к UI
                UIService.Instance.ApplyLanguage(gameData.Settings.Language);
                
                LoggingService.LogInfo($"Applied loaded settings: Language={gameData.Settings.Language}, Difficulty={gameData.Settings.Difficulty}");
            }
        }
        catch (Exception ex)
        {
            LoggingService.LogError($"Error applying loaded settings: {ex.Message}", ex);
        }
    }

    private void Application_Startup(object sender, StartupEventArgs e)
    {
        try
        {
            // �������������� ������� ����� ��������� �������� ����
            InitializeServices();
            
            // ������� �������� ���� ����������
            var mainWindow = new MainWindow();
            
            // ������������� ��� ��� ������� ����
            MainWindow = mainWindow;
            
            // ���������� ����
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            // ����������� ������ �������
            string errorDetails = $"Critical error during application startup:\n\n" +
                                 $"Message: {ex.Message}\n" +
                                 $"Stack Trace: {ex.StackTrace}";
            MessageBox.Show(errorDetails, "Application Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
            
            // ���������� � ��� � ��������� ����������
            LoggingService.LogError("Application startup failed", ex);
            Environment.Exit(1);
        }
    }
    
    private void InitializeServices()
    {
        try
        {
            // �������������� ConfigService ������
            var configService = ConfigService.Instance;
            
            // ������������� ������� ����������� �� ������������
            var loggingLevelStr = configService.GetValue(ConfigService.LOGGING_LEVEL, "Warning");
            if (Enum.TryParse<LoggingService.LogLevel>(loggingLevelStr, out var loggingLevel))
            {
                LoggingService.SetLogLevel(loggingLevel);
            }
            else
            {
                // ������������� Warning ������� �� ��������� ��� ���������� ���������� �����
                LoggingService.SetLogLevel(LoggingService.LogLevel.Warning);
            }
            
            // �������������� ��������� �������
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
            MessageBox.Show($"������ ������������� ��������: {ex.Message}", "������", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Application_Exit(object sender, ExitEventArgs e)
    {
        try
        {
            // LoggingService.LogDebug("Application is shutting down");
            
            // ������������� ��� �������� �������
            CoreGameService.Instance?.Dispose();
            
            // LoggingService.LogDebug("Application shutdown completed");
        }
        catch (Exception ex)
        {
            // �������� ������ ��� ����������, �� �� ��������� ������� ��������
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
            
            // �������� ���������� ��� ������������, ����� ���������� �� ���������
            e.Handled = true;
        }
        catch (Exception logEx)
        {
            // ���� ���� ����������� �� ��������, ������ ���������� ����������� ���������
            MessageBox.Show($"Critical error: {e.Exception.Message}", "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// ���������� ����� ��� ������ ��������� � ���� ����� � ����������� ���������
    /// </summary>
    private void SafeLogToFile(string message)
    {
        // ����������� ���������
        /*
        try
        {
            var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [CRITICAL] {message}\r\n";
            File.AppendAllText("critical_error_log.txt", logEntry);
        }
        catch
        {
            // ���� �� ����� ������ � ����, ����������
        }
        */
    }
}


