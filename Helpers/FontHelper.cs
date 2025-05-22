using System;
using System.IO;
using System.Windows.Media;
using System.Windows;
using System.Collections.Generic;

namespace SketchBlade.Helpers
{
    public static class FontHelper
    {
        // Пути относительно exe файла в папке bin/Debug/net9.0-windows
        private const string FontsFolder = "Assets/Fonts";
        private const string MainFontName = "main_font.ttf";
        private const string TitleFontName = "title_font.ttf";
        
        private static readonly Dictionary<string, FontFamily> _fontCache = new Dictionary<string, FontFamily>();
        
        // Статический конструктор для инициализации
        static FontHelper()
        {
            try
            {
                Console.WriteLine("Initializing FontHelper...");
                InitializeFontDirectories();
                Console.WriteLine("FontHelper initialization complete");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in FontHelper constructor: {ex.Message}");
            }
        }
        
        // Метод для создания необходимых каталогов
        public static void InitializeFontDirectories()
        {
            try
            {
                string basePath = AppDomain.CurrentDomain.BaseDirectory;
                Console.WriteLine($"Font base path: {basePath}");
                
                // Создаем директорию для шрифтов
                ImageHelper.EnsureDirectoryExists(Path.Combine(basePath, FontsFolder));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing font directories: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }
        
        // Метод для получения шрифта по имени
        public static FontFamily GetFont(string fontName)
        {
            if (string.IsNullOrEmpty(fontName))
            {
                Console.WriteLine("Font name is null or empty, using default font");
                return GetDefaultFont();
            }
            
            // Проверяем, есть ли этот шрифт в кэше
            if (_fontCache.TryGetValue(fontName, out FontFamily cachedFont))
            {
                Console.WriteLine($"Using cached font: {fontName}");
                return cachedFont;
            }
            
            try
            {
                string basePath = AppDomain.CurrentDomain.BaseDirectory;
                Console.WriteLine($"Font base path: {basePath}");
                
                // Try different path variations for the font file
                string[] possiblePaths = new[]
                {
                    Path.Combine(basePath, FontsFolder, fontName),
                    Path.Combine(basePath, "Assets", "Fonts", fontName),
                    Path.Combine(basePath, fontName)
                };
                
                string fontPath = null;
                foreach (string path in possiblePaths)
                {
                    Console.WriteLine($"Checking font path: {path}");
                    if (File.Exists(path))
                    {
                        fontPath = path;
                        Console.WriteLine($"Found font at: {fontPath}");
                        break;
                    }
                }
                
                if (fontPath != null)
                {
                    // Normalize path for URI
                    string normalizedPath = fontPath.Replace('\\', '/');
                    // Create URI for the font
                    string fontUri = $"file:///{normalizedPath}";
                    Console.WriteLine($"Loading font from URI: {fontUri}");
                    
                    FontFamily font = new FontFamily(fontUri);
                    _fontCache[fontName] = font;
                    return font;
                }
                else
                {
                    Console.WriteLine($"Font file not found: {fontName}, using default font");
                    return GetDefaultFont();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading font {fontName}: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return GetDefaultFont();
            }
        }
        
        // Получить основной шрифт
        public static FontFamily GetMainFont()
        {
            return GetFont(MainFontName);
        }
        
        // Получить шрифт для заголовков
        public static FontFamily GetTitleFont()
        {
            return GetFont(TitleFontName);
        }
        
        // Получить шрифт по умолчанию
        public static FontFamily GetDefaultFont()
        {
            // Возвращаем стандартный системный шрифт
            return new FontFamily("Segoe UI");
        }
    }
} 