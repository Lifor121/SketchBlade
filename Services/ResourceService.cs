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
    public interface IResourceService
    {
        BitmapImage? GetImage(string imagePath);
        BitmapImage? GetSprite(string relativePath);
        void PreloadImages(string[] imagePaths);
        void PreloadSprites(string[] paths);
        
        void ClearCache();
        int CacheSize { get; }
        void SetCacheLimit(int maxItems);
        
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

                if (_imageCache.TryGetValue(imagePath, out var cachedImage))
                {
                    return cachedImage;
                }

                lock (_loadLock)
                {
                    if (_imageCache.TryGetValue(imagePath, out cachedImage))
                    {
                        return cachedImage;
                    }

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

            if (_imageCache.Count > _maxCacheSize)
            {
                TrimCache();
            }
        }

        public async Task PreloadCriticalResourcesAsync()
        {
            try
            {
                var criticalPaths = AssetPaths.GetCriticalAssetPaths().ToArray();
                
                await Task.Run(() =>
                {
                    Parallel.ForEach(criticalPaths, path =>
                    {
                        try
                        {
                            GetImage(path);
                        }
                        catch (Exception ex)
                        {
                            LoggingService.LogError($"Failed to preload critical resource '{path}': {ex.Message}", ex);
                        }
                    });
                });
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
                
                if (!File.Exists(fullPath))
                {
                    LoggingService.LogError($"Image file not found: {fullPath}");
                    return LoadDefaultImage();
                }

                var image = LoadImageDirectly(fullPath);
                if (image != null)
                {
                    _imageCache[imagePath] = image;
                    TrimCache();
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
                if (_imageCache.TryGetValue(AssetPaths.DEFAULT_IMAGE, out var cachedDefault))
                {
                    return cachedDefault;
                }

                var defaultPath = GetFullPath(AssetPaths.DEFAULT_IMAGE);
                
                if (File.Exists(defaultPath))
                {
                    try
                    {
                        var defaultImage = LoadImageDirectly(defaultPath);
                        if (defaultImage != null)
                        {
                            _imageCache.TryAdd(AssetPaths.DEFAULT_IMAGE, defaultImage);
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
                
                return bitmap;
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error creating simple default image: {ex.Message}", ex);
                return CreateEmptyBitmapImage();
            }
        }

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
                bitmap.Freeze();
                
                return bitmap;
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error loading image directly from '{fullPath}': {ex.Message}", ex);
                return null;
            }
        }

        private BitmapImage CreateEmptyBitmapImage()
        {
            try
            {
                var bitmap = new BitmapImage();
                
                var writeableBitmap = new WriteableBitmap(1, 1, 96, 96, PixelFormats.Bgra32, null);
                byte[] pixels = new byte[4] { 0, 0, 0, 0 };
                writeableBitmap.WritePixels(new Int32Rect(0, 0, 1, 1), pixels, 4, 0);
                
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
                    return fallback;
                }
            }
        }

        private void TrimCache()
        {
            try
            {
                var itemsToRemove = _imageCache.Count - (_maxCacheSize * 3 / 4);
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

            if (relativePath.StartsWith("Assets/") || relativePath.StartsWith("Assets\\"))
            {
                return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath);
            }

            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Images", relativePath);
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_isDisposed) return;

            try
            {
                lock (_loadLock)
                {
                    _imageCache.Clear();
                }

                _isDisposed = true;
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error disposing ResourceService: {ex.Message}", ex);
            }
        }

        #endregion
    }
} 