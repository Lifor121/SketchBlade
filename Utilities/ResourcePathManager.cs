using System;
using System.IO;
using System.Reflection;

namespace SketchBlade.Utilities
{
    /// <summary>
    /// Управляет путями к внешним ресурсам для self-contained приложения
    /// </summary>
    public static class ResourcePathManager
    {
        private static string? _resourcesBasePath;
        
        /// <summary>
        /// Базовый путь к папке Resources рядом с исполняемым файлом
        /// </summary>
        public static string ResourcesBasePath
        {
            get
            {
                if (_resourcesBasePath == null)
                {
                    // Получаем директорию исполняемого файла
                    var executablePath = Assembly.GetExecutingAssembly().Location;
                    var executableDir = Path.GetDirectoryName(executablePath);
                    
                    if (string.IsNullOrEmpty(executableDir))
                    {
                        // Fallback на текущую директорию
                        executableDir = Environment.CurrentDirectory;
                    }
                    
                    _resourcesBasePath = Path.Combine(executableDir, "Resources");
                }
                
                return _resourcesBasePath;
            }
        }
        
        /// <summary>
        /// Получить полный путь к ресурсу
        /// </summary>
        /// <param name="relativePath">Относительный путь от папки Resources</param>
        /// <returns>Полный путь к ресурсу</returns>
        public static string GetResourcePath(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
                return ResourcesBasePath;
                
            // Удаляем "Resources/" из начала пути если он есть
            if (relativePath.StartsWith("Resources/") || relativePath.StartsWith("Resources\\"))
            {
                relativePath = relativePath.Substring(10);
            }
            
            return Path.Combine(ResourcesBasePath, relativePath);
        }
        
        /// <summary>
        /// Получить путь к папке Assets
        /// </summary>
        public static string AssetsPath => Path.Combine(ResourcesBasePath, "Assets");
        
        /// <summary>
        /// Получить путь к папке Images
        /// </summary>
        public static string ImagesPath => Path.Combine(AssetsPath, "Images");
        
        /// <summary>
        /// Получить путь к папке Localizations
        /// </summary>
        public static string LocalizationsPath => Path.Combine(ResourcesBasePath, "Localizations");
        
        /// <summary>
        /// Получить путь к папке Saves
        /// </summary>
        public static string SavesPath => Path.Combine(ResourcesBasePath, "Saves");
        
        /// <summary>
        /// Получить путь к папке Logs
        /// </summary>
        public static string LogsPath => Path.Combine(ResourcesBasePath, "Logs");
        
        /// <summary>
        /// Проверить существование папки Resources и создать необходимые директории
        /// </summary>
        public static void EnsureResourceDirectoriesExist()
        {
            try
            {
                // Создаем основные директории если они не существуют
                Directory.CreateDirectory(ResourcesBasePath);
                Directory.CreateDirectory(AssetsPath);
                Directory.CreateDirectory(ImagesPath);
                Directory.CreateDirectory(LocalizationsPath);
                Directory.CreateDirectory(SavesPath);
                Directory.CreateDirectory(LogsPath);
                
                // Создаем поддиректории для изображений
                Directory.CreateDirectory(Path.Combine(ImagesPath, "items", "consumables"));
                Directory.CreateDirectory(Path.Combine(ImagesPath, "items", "weapons"));
                Directory.CreateDirectory(Path.Combine(ImagesPath, "items", "armor"));
                Directory.CreateDirectory(Path.Combine(ImagesPath, "items", "materials"));
                Directory.CreateDirectory(Path.Combine(ImagesPath, "Characters"));
                Directory.CreateDirectory(Path.Combine(ImagesPath, "Enemies"));
                Directory.CreateDirectory(Path.Combine(ImagesPath, "Locations"));
                Directory.CreateDirectory(Path.Combine(ImagesPath, "UI"));
            }
            catch (Exception ex)
            {
                // Логируем ошибку, но не прерываем работу приложения
                Console.WriteLine($"Warning: Could not create resource directories: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Проверить существует ли папка Resources
        /// </summary>
        public static bool ResourcesDirectoryExists()
        {
            return Directory.Exists(ResourcesBasePath);
        }
    }
} 