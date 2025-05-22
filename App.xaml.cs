using System.Configuration;
using System.Data;
using System.Windows;
using System;
using System.IO;

namespace SketchBlade;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        try
        {
            base.OnStartup(e);
            
            Console.WriteLine("SketchBlade starting up...");
            
            // Set up thread exception handling
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;
            
            // Copy assets from build directory to executable directory if needed
            EnsureAssetsAvailable();
            
            // First explicitly initialize the directories
            Helpers.ImageHelper.InitializeDirectories();
            
            // Verify asset directories and critical files on startup
            Helpers.ImageHelper.VerifyAssetDirectories();
            
            // Ensure all critical assets are in the correct locations
            Helpers.ImageHelper.EnsureCriticalAssets();
            
            // Проверяем наличие файла def.png
            Helpers.ImageHelper.EnsureDefaultImageExists();
            
            // Preload and freeze critical images to prevent Freezable context errors
            PreloadAndFreezeImages();
            
            // Initialize font directories
            Helpers.FontHelper.InitializeFontDirectories();
            
            // Pre-load common fonts to verify they work
            var mainFont = Helpers.FontHelper.GetMainFont();
            var titleFont = Helpers.FontHelper.GetTitleFont();
            
            // Debug translations
            Services.LanguageService.DebugTranslations();
            
            // Register essential services in Application Resources
            RegisterEssentialServices();
            
            Console.WriteLine("Asset initialization complete");
        }
        catch (Exception ex)
        {
            // Log error but allow application to continue
            Console.WriteLine($"ERROR during startup: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            
            // We could show an error dialog here, but we'll continue anyway
            MessageBox.Show($"Warning: Some assets may not load correctly. Error: {ex.Message}", 
                          "SketchBlade Asset Warning", 
                          MessageBoxButton.OK, 
                          MessageBoxImage.Warning);
        }
    }
    
    // Handle unhandled exceptions in non-UI threads
    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        try
        {
            Exception? ex = e.ExceptionObject as Exception;
            string errorMessage = ex != null ? ex.Message : "Unknown error occurred";
            string stackTrace = ex != null ? ex.StackTrace ?? "" : "";
            
            Console.WriteLine($"UNHANDLED EXCEPTION: {errorMessage}");
            Console.WriteLine($"Stack trace: {stackTrace}");
            
            // Log to file if possible
            File.AppendAllText("error_log.txt", 
                $"[{DateTime.Now}] UNHANDLED EXCEPTION: {errorMessage}\r\n{stackTrace}\r\n\r\n");
            
            // Don't show UI from here as it's not on the UI thread
        }
        catch
        {
            // Just in case logging itself fails
        }
    }
    
    // Handle unhandled exceptions in UI thread
    private void Current_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        try
        {
            Console.WriteLine($"UI THREAD EXCEPTION: {e.Exception.Message}");
            Console.WriteLine($"Stack trace: {e.Exception.StackTrace}");
            
            // Log to file if possible
            File.AppendAllText("error_log.txt", 
                $"[{DateTime.Now}] UI THREAD EXCEPTION: {e.Exception.Message}\r\n{e.Exception.StackTrace}\r\n\r\n");
            
            // Show error to user
            MessageBox.Show($"An error occurred: {e.Exception.Message}\r\nThe application will continue running, but some features may not work correctly.",
                          "SketchBlade Error",
                          MessageBoxButton.OK,
                          MessageBoxImage.Error);
            
            // Mark as handled so application doesn't crash
            e.Handled = true;
        }
        catch
        {
            // Just in case error handling itself fails
        }
    }
    
    // Preload and freeze critical images to prevent Freezable context errors
    private void PreloadAndFreezeImages()
    {
        try
        {
            Console.WriteLine("Preloading and freezing critical images...");
            
            // List of critical image paths that should be preloaded
            var criticalImagePaths = new[]
            {
                "Assets/Images/def.png",
                "Assets/Images/Characters/player.png",
                "Assets/Images/Characters/npc.png",
                "Assets/Images/Characters/hero.png",
                "Assets/Images/items/weapons/wooden_sword.png",
                "Assets/Images/items/consumables/healing_potion.png"
            };
            
            // Load and freeze each image
            foreach (var path in criticalImagePaths)
            {
                try
                {
                    var image = Helpers.ImageHelper.LoadImage(path);
                    if (image != null && image.CanFreeze && !image.IsFrozen)
                    {
                        image.Freeze();
                        Console.WriteLine($"Successfully loaded and froze image: {path}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error preloading image {path}: {ex.Message}");
                }
            }
            
            // Create and freeze an empty image for fallback use
            var emptyImage = Helpers.ImageHelper.CreateEmptyImage();
            if (emptyImage != null && emptyImage.CanFreeze && !emptyImage.IsFrozen)
            {
                emptyImage.Freeze();
                Console.WriteLine("Created and froze empty fallback image");
            }
            
            Console.WriteLine("Image preloading complete");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in PreloadAndFreezeImages: {ex.Message}");
        }
    }
    
    // Метод для обеспечения наличия необходимой структуры директорий, без копирования файлов
    private void EnsureAssetsAvailable()
    {
        try
        {
            string execDir = AppDomain.CurrentDomain.BaseDirectory;
            Console.WriteLine($"Executable directory: {execDir}");
            
            // Проверяем наличие директории Assets
            string assetsDir = Path.Combine(execDir, "Assets");
            if (!Directory.Exists(assetsDir))
            {
                Console.WriteLine("Assets directory not found, creating it");
                Directory.CreateDirectory(assetsDir);
            }
            
            // Ensure Resources directory exists
            string resourcesDir = Path.Combine(execDir, "Resources");
            if (!Directory.Exists(resourcesDir))
            {
                Console.WriteLine("Resources directory not found, creating it");
                Directory.CreateDirectory(resourcesDir);
            }
            
            // Ensure Localization directory exists
            string localizationDir = Path.Combine(resourcesDir, "Localization");
            if (!Directory.Exists(localizationDir))
            {
                Console.WriteLine("Localization directory not found, creating it");
                Directory.CreateDirectory(localizationDir);
            }
            
            // Log localization file paths
            string englishFile = Path.Combine(localizationDir, "english.json");
            string russianFile = Path.Combine(localizationDir, "russian.json");
            
            Console.WriteLine($"English localization file path: {englishFile}, exists: {File.Exists(englishFile)}");
            Console.WriteLine($"Russian localization file path: {russianFile}, exists: {File.Exists(russianFile)}");
            
            // Создаем основные директории для ресурсов
            string[] directories = new[]
            {
                Path.Combine(assetsDir, "Images"),
                Path.Combine(assetsDir, "Images", "Locations"),
                Path.Combine(assetsDir, "Images", "Characters"),
                Path.Combine(assetsDir, "Images", "Enemies"),
                Path.Combine(assetsDir, "Images", "UI"),
                Path.Combine(assetsDir, "Images", "items"),
                Path.Combine(assetsDir, "Images", "items", "weapons"),
                Path.Combine(assetsDir, "Images", "items", "armor"),
                Path.Combine(assetsDir, "Images", "items", "consumables"),
                Path.Combine(assetsDir, "Images", "items", "materials"),
                Path.Combine(assetsDir, "Fonts"),
                Path.Combine(assetsDir, "Sounds"),
                Path.Combine(assetsDir, "Styles")
            };
            
            foreach (var dir in directories)
            {
                if (!Directory.Exists(dir))
                {
                    Console.WriteLine($"Creating directory: {dir}");
                    Directory.CreateDirectory(dir);
                }
            }
            
            // Проверяем наличие дефолтного изображения
            string defPngPath = Path.Combine(execDir, "Assets", "Images", "def.png");
            if (!File.Exists(defPngPath))
            {
                Console.WriteLine("WARNING: Default image (def.png) not found in Assets/Images/");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error ensuring assets are available: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }

    // Method to register essential services in Application.Resources
    private void RegisterEssentialServices()
    {
        try
        {
            // Create and register GameState as a resource
            var gameState = new Models.GameState();
            if (gameState != null)
            {
                Application.Current.Resources["GameState"] = gameState;
                Console.WriteLine("GameState registered in Application.Resources");
            }
            
            // InventoryViewModel will be registered when it's created
            Console.WriteLine("Essential services registration complete");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error registering essential services: {ex.Message}");
        }
    }
}

