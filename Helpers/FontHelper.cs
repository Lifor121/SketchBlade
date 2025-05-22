using System;
using System.IO;
using System.Windows.Media;
using System.Windows;
using System.Collections.Generic;

namespace SketchBlade.Helpers
{
    public static class FontHelper
    {
        private const string FontsFolder = "Assets/Fonts";
        private const string MainFontName = "main_font.ttf";
        private const string TitleFontName = "title_font.ttf";
        
        private static readonly Dictionary<string, FontFamily> _fontCache = new Dictionary<string, FontFamily>();
        
        static FontHelper()
        {
            try
            {
                InitializeFontDirectories();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"ERROR in FontHelper constructor: {ex.Message}");
            }
        }
        
        public static void InitializeFontDirectories()
        {
            try
            {
                string basePath = AppDomain.CurrentDomain.BaseDirectory;
                
                ImageHelper.EnsureDirectoryExists(Path.Combine(basePath, FontsFolder));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing font directories: {ex.Message}");
                throw;
            }
        }
        
        public static FontFamily GetFont(string fontName)
        {
            if (string.IsNullOrEmpty(fontName))
            {
                return GetDefaultFont();
            }
            
            if (_fontCache.TryGetValue(fontName, out FontFamily cachedFont))
            {
                return cachedFont;
            }
            
            try
            {
                string basePath = AppDomain.CurrentDomain.BaseDirectory;
                
                string[] possiblePaths = new[]
                {
                    Path.Combine(basePath, FontsFolder, fontName),
                    Path.Combine(basePath, "Assets", "Fonts", fontName),
                    Path.Combine(basePath, fontName)
                };
                
                string fontPath = null;
                foreach (string path in possiblePaths)
                {
                    if (File.Exists(path))
                    {
                        fontPath = path;
                        break;
                    }
                }
                
                if (fontPath != null)
                {
                    string normalizedPath = fontPath.Replace('\\', '/');
                    string fontUri = $"file:///{normalizedPath}";
                    
                    FontFamily font = new FontFamily(fontUri);
                    _fontCache[fontName] = font;
                    return font;
                }
                else
                {
                    return GetDefaultFont();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading font {fontName}: {ex.Message}");
                return GetDefaultFont();
            }
        }
        
        public static FontFamily GetMainFont()
        {
            return GetFont(MainFontName);
        }
        
        public static FontFamily GetTitleFont()
        {
            return GetFont(TitleFontName);
        }
        
        public static FontFamily GetDefaultFont()
        {
            return new FontFamily("Segoe UI");
        }
    }
} 