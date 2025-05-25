using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;
using SketchBlade.Utilities;
using SketchBlade.Helpers;

namespace SketchBlade.Services
{
    /// <summary>
    /// Консолидированный сервис управления ресурсами
    /// Объединяет управление изображениями, кэшированием и предзагрузкой
    /// </summary>
    public interface IResourceService
    {
        // Image operations
        BitmapImage? GetImage(string imagePath);
        BitmapImage? GetSprite(string relativePath);
        void PreloadImages(string[] imagePaths);
        void PreloadSprites(string[] paths);
        
        // Cache management
        void ClearCache();
        int CacheSize { get; }
        void SetCacheLimit(int maxItems);
        
        // Resource management
        Task PreloadCriticalResourcesAsync();
        void Dispose();
    }

    public class ResourceService : IResourceService, IDisposable
    {
        private static readonly Lazy<ResourceService> _instance = new(() => new ResourceService());
        public static ResourceService Instance => _instance.Value;

        private readonly ConcurrentDictionary<string, BitmapImage> _imageCache = new();
        private readonly object _loadLock = new object();
        private int _maxCacheSize = 200;
        private bool _isDisposed = false;

        public int CacheSize => _imageCache.Count;

        private ResourceService()
        {
            _maxCacheSize = ConfigService.Instance.GetValue(ConfigService.CACHE_SIZE_LIMIT, 200);
            LoggingService.LogDebug("ResourceService initialized");
        }

        #region Public Methods

        public BitmapImage? GetImage(string imagePath)
        {
            try
            {
                if (string.IsNullOrEmpty(imagePath))
                {
                    LoggingService.LogError("Image path is empty");
                    return LoadDefaultImage();
                }

                LoggingService.LogDebug($"[ResourceService] GetImage: Запрос изображения '{imagePath}'");

                // Check cache first
                if (_imageCache.TryGetValue(imagePath, out var cachedImage))
                {
                    LoggingService.LogDebug($"[ResourceService] GetImage: Изображение найдено в кэше '{imagePath}'");
                    return cachedImage;
                }

                // Load with lock to prevent duplicate loading
                lock (_loadLock)
                {
                    // Double-check after acquiring lock
                    if (_imageCache.TryGetValue(imagePath, out cachedImage))
                    {
                        LoggingService.LogDebug($"[ResourceService] GetImage: Изображение найдено в кэше после блокировки '{imagePath}'");
                        return cachedImage;
                    }

                    LoggingService.LogDebug($"[ResourceService] GetImage: Загружаем изображение из файла '{imagePath}'");
                    return LoadImageToCache(imagePath);
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error getting image '{imagePath}': {ex.Message}", ex);
                return LoadDefaultImage();
            }
        }

        public BitmapImage? GetSprite(string relativePath)
        {
            try
            {
                if (string.IsNullOrEmpty(relativePath))
                {
                    LoggingService.LogError("Sprite path is empty");
                    return LoadDefaultImage();
                }

                // Normalize path
                var fullPath = GetFullPath(relativePath);
                return GetImage(fullPath);
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error getting sprite '{relativePath}': {ex.Message}", ex);
                return LoadDefaultImage();
            }
        }

        public void PreloadImages(string[] imagePaths)
        {
            if (imagePaths == null || imagePaths.Length == 0)
                return;

            try
            {
                LoggingService.LogDebug($"Preloading {imagePaths.Length} images");

                Parallel.ForEach(imagePaths, imagePath =>
                {
                    try
                    {
                        GetImage(imagePath);
                    }
                    catch (Exception ex)
                    {
                        LoggingService.LogError($"Error preloading image '{imagePath}': {ex.Message}", ex);
                    }
                });

                LoggingService.LogDebug("Image preloading completed");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error in image preloading: {ex.Message}", ex);
            }
        }

        public void PreloadSprites(string[] paths)
        {
            if (paths == null || paths.Length == 0)
                return;

            try
            {
                var fullPaths = new string[paths.Length];
                for (int i = 0; i < paths.Length; i++)
                {
                    fullPaths[i] = GetFullPath(paths[i]);
                }

                PreloadImages(fullPaths);
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error preloading sprites: {ex.Message}", ex);
            }
        }

        public void ClearCache()
        {
            try
            {
                lock (_loadLock)
                {
                    foreach (var kvp in _imageCache)
                    {
                        try
                        {
                            // Don't dispose critical images
                            if (!IsCriticalResource(kvp.Key))
                            {
                                // Note: BitmapImage doesn't implement IDisposable
                                // but we can remove reference for GC
                            }
                        }
                        catch (Exception ex)
                        {
                            LoggingService.LogError($"Error disposing image '{kvp.Key}': {ex.Message}", ex);
                        }
                    }

                    var oldSize = _imageCache.Count;
                    _imageCache.Clear();
                    LoggingService.LogDebug($"Cache cleared: {oldSize} images removed");

                    // Reload critical resources
                    Task.Run(PreloadCriticalResourcesAsync);
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error clearing cache: {ex.Message}", ex);
            }
        }

        public void SetCacheLimit(int maxItems)
        {
            if (maxItems <= 0)
            {
                LoggingService.LogError("Cache limit must be positive");
                return;
            }

            _maxCacheSize = maxItems;
            LoggingService.LogDebug($"Cache limit set to {maxItems}");

            // If current cache exceeds new limit, trim it
            if (_imageCache.Count > _maxCacheSize)
            {
                TrimCache();
            }
        }

        public async Task PreloadCriticalResourcesAsync()
        {
            try
            {
                LoggingService.LogDebug("Starting critical resource preloading");

                var criticalPaths = AssetPaths.GetCriticalAssetPaths().ToArray();
                
                await Task.Run(() =>
                {
                    Parallel.ForEach(criticalPaths, path =>
                    {
                        try
                        {
                            GetImage(path);
                            LoggingService.LogDebug($"Preloaded critical resource: {path}");
                        }
                        catch (Exception ex)
                        {
                            LoggingService.LogError($"Failed to preload critical resource '{path}': {ex.Message}", ex);
                        }
                    });
                });

                LoggingService.LogDebug($"Critical resource preloading completed. Loaded {criticalPaths.Length} resources");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error in critical resource preloading: {ex.Message}", ex);
            }
        }

        #endregion

        #region Private Methods

        private BitmapImage? LoadImageToCache(string imagePath)
        {
            try
            {
                var fullPath = GetFullPath(imagePath);
                LoggingService.LogDebug($"[ResourceService] LoadImageToCache: Полный путь '{fullPath}'");
                
                if (!File.Exists(fullPath))
                {
                    LoggingService.LogError($"Image file not found: {fullPath}");
                    return LoadDefaultImage();
                }

                // Загружаем изображение напрямую
                var image = LoadImageDirectly(fullPath);
                if (image != null)
                {
                    _imageCache[imagePath] = image;
                    TrimCache();
                    LoggingService.LogDebug($"Image loaded and cached: {imagePath}");
                }
                else
                {
                    LoggingService.LogWarning($"[ResourceService] LoadImageToCache: LoadImageDirectly вернул null для '{fullPath}'");
                }

                return image;
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error loading image '{imagePath}': {ex.Message}", ex);
                return LoadDefaultImage();
            }
        }

        private BitmapImage? LoadDefaultImage()
        {
            try
            {
                // Try to get default image from cache first
                if (_imageCache.TryGetValue(AssetPaths.DEFAULT_IMAGE, out var cachedDefault))
                {
                    return cachedDefault;
                }

                // Загружаем существующий дефолтный файл def.png
                var defaultPath = GetFullPath(AssetPaths.DEFAULT_IMAGE);
                
                if (File.Exists(defaultPath))
                {
                    try
                    {
                        var defaultImage = LoadImageDirectly(defaultPath);
                        if (defaultImage != null)
                        {
                            // Кэшируем дефолтное изображение
                            _imageCache.TryAdd(AssetPaths.DEFAULT_IMAGE, defaultImage);
                            LoggingService.LogDebug($"Default image loaded successfully from {defaultPath}");
                            return defaultImage;
                        }
                    }
                    catch (Exception ex)
                    {
                        LoggingService.LogError($"Error loading default image from {defaultPath}: {ex.Message}", ex);
                    }
                }
                else
                {
                    LoggingService.LogError($"Default image file not found at {defaultPath}");
                }

                // Если не удалось загрузить def.png, создаем простое изображение в памяти
                LoggingService.LogWarning("Creating fallback default image in memory");
                var fallbackImage = CreateSimpleDefaultImage();
                _imageCache.TryAdd(AssetPaths.DEFAULT_IMAGE, fallbackImage);
                return fallbackImage;
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Critical error in LoadDefaultImage: {ex.Message}", ex);
                return CreateSimpleDefaultImage();
            }
        }

        private BitmapImage CreateSimpleDefaultImage()
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                
                // Создаем простое изображение 64x64 с серым фоном
                var drawingVisual = new DrawingVisual();
                using (var drawingContext = drawingVisual.RenderOpen())
                {
                    var rect = new Rect(0, 0, 64, 64);
                    drawingContext.DrawRectangle(System.Windows.Media.Brushes.LightGray, new System.Windows.Media.Pen(System.Windows.Media.Brushes.DarkGray, 2), rect);
                    
                    var formattedText = new FormattedText("?",
                        CultureInfo.InvariantCulture,
                        FlowDirection.LeftToRight,
                        new Typeface("Arial"),
                        32,
                        System.Windows.Media.Brushes.Black,
                        VisualTreeHelper.GetDpi(drawingVisual).PixelsPerDip);
                    
                    var textPoint = new System.Windows.Point(
                        (64 - formattedText.Width) / 2,
                        (64 - formattedText.Height) / 2);
                    
                    drawingContext.DrawText(formattedText, textPoint);
                }
                
                var renderTargetBitmap = new RenderTargetBitmap(64, 64, 96, 96, PixelFormats.Pbgra32);
                renderTargetBitmap.Render(drawingVisual);
                
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(renderTargetBitmap));
                
                using (var stream = new MemoryStream())
                {
                    encoder.Save(stream);
                    stream.Position = 0;
                    
                    bitmap.StreamSource = stream;
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze();
                }
                
                LoggingService.LogDebug("Simple default image created in memory");
                return bitmap;
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error creating simple default image: {ex.Message}", ex);
                
                // Создаем минимальное изображение как последний fallback
                return CreateEmptyBitmapImage();
            }
        }

        // Новый метод для прямой загрузки изображения без циклических зависимостей
        private BitmapImage? LoadImageDirectly(string fullPath)
        {
            try
            {
                if (!File.Exists(fullPath))
                    return null;

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.CreateOptions = BitmapCreateOptions.None;
                bitmap.UriSource = new Uri(fullPath, UriKind.Absolute);
                bitmap.EndInit();
                bitmap.Freeze(); // Делаем thread-safe
                
                return bitmap;
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error loading image directly from '{fullPath}': {ex.Message}", ex);
                return null;
            }
        }

        // Новый метод для создания пустого изображения без зависимостей
        private BitmapImage CreateEmptyBitmapImage()
        {
            try
            {
                // Create a 1x1 pixel transparent image
                var bitmap = new BitmapImage();
                
                // Create WriteableBitmap for programmatic generation
                var writeableBitmap = new WriteableBitmap(1, 1, 96, 96, PixelFormats.Bgra32, null);
                byte[] pixels = new byte[4] { 0, 0, 0, 0 }; // Transparent
                writeableBitmap.WritePixels(new Int32Rect(0, 0, 1, 1), pixels, 4, 0);
                
                // Convert to BitmapImage
                using (var stream = new MemoryStream())
                {
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(writeableBitmap));
                    encoder.Save(stream);
                    
                    stream.Position = 0;
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = stream;
                    bitmap.EndInit();
                    bitmap.Freeze();
                }
                
                return bitmap;
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error creating empty bitmap: {ex.Message}", ex);
                // Fallback - try to create the simplest possible bitmap
                var fallback = new BitmapImage();
                try
                {
                    fallback.BeginInit();
                    fallback.DecodePixelWidth = 1;
                    fallback.DecodePixelHeight = 1;
                    fallback.EndInit();
                    fallback.Freeze();
                    return fallback;
                }
                catch
                {
                    return fallback; // Return whatever we can
                }
            }
        }

        private void TrimCache()
        {
            try
            {
                // Remove excess items, keeping critical resources
                var itemsToRemove = _imageCache.Count - (_maxCacheSize * 3 / 4); // Trim to 75% of max
                if (itemsToRemove <= 0) return;

                var keysToRemove = new List<string>();
                foreach (var kvp in _imageCache)
                {
                    if (!IsCriticalResource(kvp.Key))
                    {
                        keysToRemove.Add(kvp.Key);
                        if (keysToRemove.Count >= itemsToRemove)
                            break;
                    }
                }

                foreach (var key in keysToRemove)
                {
                    _imageCache.TryRemove(key, out _);
                }

                LoggingService.LogDebug($"Cache trimmed: {keysToRemove.Count} items removed");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error trimming cache: {ex.Message}", ex);
            }
        }

        private bool IsCriticalResource(string imagePath)
        {
            return AssetPaths.IsCriticalAsset(imagePath);
        }

        private string GetFullPath(string relativePath)
        {
            if (Path.IsPathRooted(relativePath))
                return relativePath;

            // Handle different path formats
            if (relativePath.StartsWith("Assets/") || relativePath.StartsWith("Assets\\"))
            {
                return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath);
            }

            // Assume it's already relative to base directory
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Images", relativePath);
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_isDisposed) return;

            try
            {
                // Clear cache without reloading critical resources
                lock (_loadLock)
                {
                    _imageCache.Clear();
                }

                _isDisposed = true;
                LoggingService.LogDebug("ResourceService disposed");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error disposing ResourceService: {ex.Message}", ex);
            }
        }

        #endregion
    }
} 