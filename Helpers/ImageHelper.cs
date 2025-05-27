using System;
using System.IO;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Threading;
using SketchBlade.Services;
using SketchBlade.Utilities;

namespace SketchBlade.Helpers
{
    public static class ImageHelper
    {
        [Obsolete("Use AssetPaths.DEFAULT_IMAGE instead")]
        private const string DefaultImagePath = "Assets/Images/def.png";
        [Obsolete("Use AssetPaths.Locations folder methods instead")]
        private const string LocationsFolder = "Assets/Images/Locations";
        [Obsolete("Use AssetPaths.Weapons methods instead")]
        private const string WeaponsFolder = "Assets/Images/items/weapons";
        [Obsolete("Use AssetPaths.Armor methods instead")]
        private const string ArmorFolder = "Assets/Images/items/armor";
        [Obsolete("Use AssetPaths.Consumables methods instead")]
        private const string ConsumablesFolder = "Assets/Images/items/consumables";
        [Obsolete("Use AssetPaths.Materials methods instead")]
        private const string MaterialsFolder = "Assets/Images/items/materials";
        [Obsolete("Use AssetPaths.Characters instead")]
        private const string CharactersFolder = "Assets/Images/Characters";
        [Obsolete("Use AssetPaths.Enemies methods instead")]
        private const string EnemiesFolder = "Assets/Images/Enemies";
        [Obsolete("Use AssetPaths.UI instead")]
        private const string UIFolder = "Assets/Images/UI";
        
        [Obsolete("Use AssetPaths.Characters.PLAYER instead")]
        public const string PlayerSpritePath = "Assets/Images/Characters/player.png";
        [Obsolete("Use AssetPaths.Characters.NPC instead")]
        public const string NpcSpritePath = "Assets/Images/Characters/npc.png";
        [Obsolete("Use AssetPaths.Characters.HERO instead")]
        public const string HeroSpritePath = "Assets/Images/Characters/hero.png";
        [Obsolete("Use AssetPaths.DEFAULT_IMAGE instead")]
        public const string DefaultSpritePath = "Assets/Images/def.png";
        
        private static readonly Dictionary<string, BitmapImage> _imageCache = new Dictionary<string, BitmapImage>();
        private static BitmapImage _defaultImage;
        private static bool _isCacheInitialized = false;
        
        static ImageHelper()
        {
            try
            {
                InitializeDirectories();
                Task.Factory.StartNew(InitializeImageCache);
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"–û—à–∏–±–∫–∞ –≤ –∫–æ–Ω—Å—Ç—Ä—É–∫—Ç–æ—Ä–µ ImageHelper: {ex.Message}", ex);
            }
        }
        
        private static void InitializeImageCache()
        {
            try
            {
                if (_isCacheInitialized)
                    return;
                    
                // LoggingService.LogDebug("»ÌËˆË‡ÎËÁ‡ˆËˇ Í˝¯‡ ËÁÓ·‡ÊÂÌËÈ");
                
                string basePath = AppDomain.CurrentDomain.BaseDirectory;
                string defaultImagePath = Path.Combine(basePath, AssetPaths.DEFAULT_IMAGE);
                
                if (File.Exists(defaultImagePath))
                {
                    _defaultImage = CreateCachedBitmapImage(defaultImagePath);
                    _imageCache[AssetPaths.DEFAULT_IMAGE] = _defaultImage; //  ˝¯ËÛÂÏ def.png ÔÓ Â„Ó ÔÛÚË
                    // LoggingService.LogDebug($"«‡„ÛÊÂÌÓ ‰ÂÙÓÎÚÌÓÂ ËÁÓ·‡ÊÂÌËÂ ËÁ {defaultImagePath}");
                }
                else
                {
                    _defaultImage = new BitmapImage();
                }
                
                string locationsPath = Path.Combine(basePath, "Assets/Images/Locations");
                if (Directory.Exists(locationsPath))
                {
                    foreach (var file in Directory.GetFiles(locationsPath, "*.png"))
                    {
                        string relativePath = file.Substring(basePath.Length);
                        relativePath = relativePath.Replace(Path.DirectorySeparatorChar, '/');
                        
                        if (!_imageCache.ContainsKey(relativePath) && File.Exists(file))
                        {
                            try
                            {
                                _imageCache[relativePath] = CreateCachedBitmapImage(file);
                            }
                            catch (Exception ex)
                            {
                                // Failed to cache image - continue silently
                            }
                        }
                    }
                }
                
                string charactersPath = Path.Combine(basePath, "Assets/Images/Characters");
                EnsureDirectoryExists(charactersPath);
                
                _isCacheInitialized = true;
            }
            catch (Exception ex)
            {
                // Error initializing image cache - continue silently
            }
        }
        
        private static BitmapImage CreateCachedBitmapImage(string filePath)
        {
            if (Application.Current != null && !Application.Current.Dispatcher.CheckAccess())
            {
                return (BitmapImage)Application.Current.Dispatcher.Invoke(() => CreateCachedBitmapImage(filePath));
            }

            try
            {
                BitmapImage image = new BitmapImage();
                
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad; // Ensure image is fully loaded in memory
                    image.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
                    image.StreamSource = stream;
                    image.EndInit();
                    
                    if (image.CanFreeze)
                    {
                        image.Freeze();
                    }
                }
                
                return image;
            }
            catch (Exception ex)
            {
                
                BitmapImage fallbackImage = new BitmapImage();
                fallbackImage.BeginInit();
                fallbackImage.CacheOption = BitmapCacheOption.OnLoad;
                fallbackImage.CreateOptions = BitmapCreateOptions.None;
                fallbackImage.UriSource = null; // No source, will be an empty image
                fallbackImage.EndInit();
                
                if (fallbackImage.CanFreeze)
                {
                    fallbackImage.Freeze();
                }
                
                return fallbackImage;
            }
        }
        
        public static void InitializeDirectories()
        {
            try
            {
                string basePath = AppDomain.CurrentDomain.BaseDirectory;
                
                EnsureDirectoryExists(Path.Combine(basePath, "Assets"));
                EnsureDirectoryExists(Path.Combine(basePath, "Assets/Images"));
                
                EnsureDirectoryExists(Path.Combine(basePath, LocationsFolder));
                EnsureDirectoryExists(Path.Combine(basePath, "Assets/Images/items"));
                EnsureDirectoryExists(Path.Combine(basePath, WeaponsFolder));
                EnsureDirectoryExists(Path.Combine(basePath, ArmorFolder));
                EnsureDirectoryExists(Path.Combine(basePath, ConsumablesFolder));
                EnsureDirectoryExists(Path.Combine(basePath, MaterialsFolder));
                EnsureDirectoryExists(Path.Combine(basePath, CharactersFolder));
                EnsureDirectoryExists(Path.Combine(basePath, EnemiesFolder));
                EnsureDirectoryExists(Path.Combine(basePath, UIFolder));
                
                EnsureDefaultImageExists();
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        
        public static void EnsureDefaultImageExists()
        {
            try
            {
                string basePath = AppDomain.CurrentDomain.BaseDirectory;
                string defaultImagePath = Path.Combine(basePath, DefaultImagePath);
                
                if (!File.Exists(defaultImagePath))
                {
                    string directory = Path.GetDirectoryName(defaultImagePath);
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }
                }
            }
            catch (Exception ex)
            {
                // Error ensuring default image exists - continue silently
            }
        }
        
        private static void CreateAndSaveSimpleImage(string path)
        {
            try
            {
                // –°–æ–∑–¥–∞–µ–º –ø—Ä–æ—Å—Ç–æ–µ –∏–∑–æ–±—Ä–∞–∂–µ–Ω–∏–µ 32x32
                int width = 32;
                int height = 32;
                
                // –°–æ–∑–¥–∞–µ–º WriteableBitmap
                WriteableBitmap writeableBitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
                
                // –ó–∞–ø–æ–ª–Ω—è–µ–º –µ–≥–æ –ø–∏–∫—Å–µ–ª—è–º–∏ (—Å–µ—Ä—ã–π —Ñ–æ–Ω —Å —á–µ—Ä–Ω–æ–π —Ä–∞–º–∫–æ–π)
                byte[] pixels = new byte[width * height * 4];
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int index = (y * width + x) * 4;
                        // BGRA —Ñ–æ—Ä–º–∞—Ç
                        if (x == 0 || y == 0 || x == width - 1 || y == height - 1)
                        {
                            // –ß–µ—Ä–Ω–∞—è —Ä–∞–º–∫–∞
                            pixels[index] = 0;     // B
                            pixels[index + 1] = 0; // G
                            pixels[index + 2] = 0; // R
                            pixels[index + 3] = 255; // A
                        }
                        else
                        {
                            // –°–≤–µ—Ç–ª–æ-—Å–µ—Ä—ã–π —Ñ–æ–Ω
                            pixels[index] = 200;     // B
                            pixels[index + 1] = 200; // G
                            pixels[index + 2] = 200; // R
                            pixels[index + 3] = 255; // A
                        }
                    }
                }
                
                // –ó–∞–ø–∏—Å—ã–≤–∞–µ–º –ø–∏–∫—Å–µ–ª–∏ –≤ WriteableBitmap
                writeableBitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, width * 4, 0);
                
                // –°–æ—Ö—Ä–∞–Ω—è–µ–º –≤ —Ñ–∞–π–ª
                using (FileStream stream = new FileStream(path, FileMode.Create))
                {
                    PngBitmapEncoder encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(writeableBitmap));
                    encoder.Save(stream);
                }
            }
            catch (Exception ex)
            {
                // Fallback method if primary method fails
                FallbackCreateSimpleImage(path);
            }
        }
        
        private static void FallbackCreateSimpleImage(string path)
        {
            try
            {
                WriteableBitmap writeableBitmap = new WriteableBitmap(1, 1, 96, 96, PixelFormats.Bgra32, null);
                
                byte[] pixels = new byte[4] { 255, 255, 255, 255 }; // White in BGRA
                writeableBitmap.WritePixels(new Int32Rect(0, 0, 1, 1), pixels, 4, 0);
                
                using (FileStream stream = new FileStream(path, FileMode.Create))
                {
                    PngBitmapEncoder encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(writeableBitmap));
                    encoder.Save(stream);
                }
                
            }
            catch (Exception ex)
            {
                // Even fallback image creation failed - continue silently
            }
        }
        
        public static BitmapImage LoadImage(string path)
        {
            if (Application.Current != null && !Application.Current.Dispatcher.CheckAccess())
            {
                return (BitmapImage)Application.Current.Dispatcher.Invoke(() => LoadImage(path));
            }

            if (string.IsNullOrEmpty(path))
            {
                LoggingService.LogError("LoadImage ‚˚Á‚‡Ì Ò ÔÛÒÚ˚Ï ÔÛÚÂÏ");
                return GetDefaultImage();
            }
            
            try 
            {
                string basePath = AppDomain.CurrentDomain.BaseDirectory;
                
                string normalizedPath = path.Replace('\\', '/');
                
                            if (!normalizedPath.Contains("Assets/"))
            {
                // Path doesn't contain Assets folder - return default
                return GetDefaultImage();
            }
                
                string fullPath = Path.Combine(basePath, normalizedPath);
                
                if (File.Exists(fullPath))
                {
                    if (_imageCache.TryGetValue(normalizedPath, out BitmapImage cachedImage))
                    {
                        // Cache hit
                        return cachedImage;
                    }
                    
                    // Loading image from file
                    
                    BitmapImage image = CreateCachedBitmapImage(fullPath);
                    
                    _imageCache[normalizedPath] = image;
                    
                    // Successfully loaded image
                    return image;
                }
                else
                {
                    // File not found - try alternatives
                    
                    string lowercasePath = normalizedPath.ToLower();
                    string fullLowercasePath = Path.Combine(basePath, lowercasePath);
                    
                    if (File.Exists(fullLowercasePath) && fullLowercasePath != fullPath)
                    {
                        // Found lowercase version
                        BitmapImage image = CreateCachedBitmapImage(fullLowercasePath);
                        _imageCache[lowercasePath] = image;
                        return image;
                    }
                    
                    string directoryPath = Path.GetDirectoryName(fullPath);
                    if (Directory.Exists(directoryPath))
                    {
                        // Directory exists but file not found
                        foreach (var file in Directory.GetFiles(directoryPath))
                        {
                            // Check available files silently
                        }
                    }
                    else
                    {
                        // Directory does not exist
                    }
                    
                    // Using default image
                    return GetDefaultImage();
                }
            }
            catch (Exception ex)
            {
                // Error loading image - return default
                return GetDefaultImage();
            }
        }
        
        public static BitmapImage GetDefaultImage()
        {
            if (Application.Current != null && !Application.Current.Dispatcher.CheckAccess())
            {
                return (BitmapImage)Application.Current.Dispatcher.Invoke(() => GetDefaultImage());
            }

            if (_imageCache.TryGetValue("default", out BitmapImage cachedImage))
            {
                return cachedImage;
            }
            
            try
            {
                string defaultPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets/Images/def.png");
                
                if (File.Exists(defaultPath))
                {
                    BitmapImage defaultImage = CreateCachedBitmapImage(defaultPath);
                    _imageCache["default"] = defaultImage;
                    return defaultImage;
                }
                else
                {
                    // Default image not found - create empty
                    
                    BitmapImage emptyImage = CreateEmptyImage();
                    _imageCache["default"] = emptyImage;
                    return emptyImage;
                }
            }
            catch (Exception ex)
            {
                // Error loading default image
                return CreateEmptyImage();
            }
        }
        
        public static void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
        
        public static BitmapImage CreateEmptyImage()
        {
            if (Application.Current != null && !Application.Current.Dispatcher.CheckAccess())
            {
                return (BitmapImage)Application.Current.Dispatcher.Invoke(() => CreateEmptyImage());
            }

            try
            {
                int width = 1;
                int height = 1;
                WriteableBitmap writeableBitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
                
                byte[] pixels = new byte[4] { 0, 0, 0, 0 };
                writeableBitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, width * 4, 0);
                
                BitmapImage emptyImage = new BitmapImage();
                using (MemoryStream stream = new MemoryStream())
                {
                    PngBitmapEncoder encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(writeableBitmap));
                    encoder.Save(stream);
                    
                    stream.Position = 0;
                    
                    emptyImage.BeginInit();
                    emptyImage.CacheOption = BitmapCacheOption.OnLoad;
                    emptyImage.CreateOptions = BitmapCreateOptions.None;
                    emptyImage.StreamSource = stream;
                    emptyImage.EndInit();
                    
                    if (emptyImage.CanFreeze)
                    {
                        emptyImage.Freeze();
                        // Empty image created and frozen successfully
                    }
                    else
                    {
                        // Empty image cannot be frozen
                    }
                }
                
                return emptyImage;
            }
            catch (Exception ex)
            {
                // Error creating empty image - try fallback
                
                try
                {
                    BitmapImage fallbackImage = new BitmapImage();
                    fallbackImage.BeginInit();
                    fallbackImage.UriSource = new Uri("pack://application:,,,/Assets/Images/def.png", UriKind.Absolute);
                    fallbackImage.CacheOption = BitmapCacheOption.OnLoad;
                    fallbackImage.EndInit();
                    
                    if (fallbackImage.CanFreeze)
                    {
                        fallbackImage.Freeze();
                    }
                    
                    return fallbackImage;
                }
                catch
                {
                    BitmapImage lastResort = new BitmapImage();
                    return lastResort;
                }
            }
        }
        
        public static void VerifyAssetDirectories()
        {
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            string assetsDir = Path.Combine(basePath, "Assets");
            string imagesDir = Path.Combine(assetsDir, "Images");
            string defaultImagePath = Path.Combine(basePath, DefaultImagePath);
            
            if (!Directory.Exists(assetsDir))
                Directory.CreateDirectory(assetsDir);
            
            if (!Directory.Exists(imagesDir))
                Directory.CreateDirectory(imagesDir);
            
            if (!File.Exists(defaultImagePath))
                EnsureDefaultImageExists();
        }

        public static void EnsureCriticalAssets()
        {
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            
            string defaultImagePath = Path.Combine(basePath, DefaultImagePath);
            if (!File.Exists(defaultImagePath))
            {
                EnsureDefaultImageExists();
            }
            
            VerifyAllImageFiles();
        }
        
        public static BitmapImage GetImageWithFallback(string path)
        {
            if (Application.Current != null && !Application.Current.Dispatcher.CheckAccess())
            {
                return (BitmapImage)Application.Current.Dispatcher.Invoke(() => GetImageWithFallback(path));
            }

            if (string.IsNullOrEmpty(path))
            {
                return GetDefaultImage();
            }
            
            try
            {
                return LoadImage(path);
            }
            catch
            {
                return GetDefaultImage();
            }
        }

        public static void VerifyAllImageFiles()
        {
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            
            // Checking critical image files
            
            string defaultImagePath = Path.Combine(basePath, DefaultImagePath);
            bool defaultImageExists = File.Exists(defaultImagePath);
            
            if (!defaultImageExists)
            {
                // Default image missing - application will continue with fallbacks
            }
            else
            {
                // Default image found
            }
            
            var imagePaths = new List<string>
            {
                Path.Combine(basePath, "Assets/Images/items/weapons/wooden_sword.png"),
                Path.Combine(basePath, "Assets/Images/items/weapons/iron_sword.png"),
                Path.Combine(basePath, "Assets/Images/items/weapons/golden_sword.png"),
                Path.Combine(basePath, "Assets/Images/items/weapons/luminite_sword.png"),
                Path.Combine(basePath, "Assets/Images/items/weapons/wooden_axe.png"),
                Path.Combine(basePath, "Assets/Images/items/weapons/iron_axe.png"),
                Path.Combine(basePath, "Assets/Images/items/weapons/golden_axe.png"),
                Path.Combine(basePath, "Assets/Images/items/weapons/luminite_axe.png"),
                
                Path.Combine(basePath, "Assets/Images/items/armor/wooden_helmet.png"),
                Path.Combine(basePath, "Assets/Images/items/armor/iron_helmet.png"),
                Path.Combine(basePath, "Assets/Images/items/armor/golden_helmet.png"),
                Path.Combine(basePath, "Assets/Images/items/armor/luminite_helmet.png"),
                
                Path.Combine(basePath, "Assets/Images/items/armor/wooden_chest.png"),
                Path.Combine(basePath, "Assets/Images/items/armor/iron_chest.png"),
                Path.Combine(basePath, "Assets/Images/items/armor/golden_chest.png"),
                Path.Combine(basePath, "Assets/Images/items/armor/luminite_chest.png"),
                
                Path.Combine(basePath, "Assets/Images/items/armor/wooden_legs.png"),
                Path.Combine(basePath, "Assets/Images/items/armor/iron_legs.png"),
                Path.Combine(basePath, "Assets/Images/items/armor/golden_legs.png"),
                Path.Combine(basePath, "Assets/Images/items/armor/luminite_legs.png"),
                
                Path.Combine(basePath, "Assets/Images/items/armor/wooden_shield.png"),
                Path.Combine(basePath, "Assets/Images/items/armor/iron_shield.png"),
                Path.Combine(basePath, "Assets/Images/items/armor/golden_shield.png"),
                Path.Combine(basePath, "Assets/Images/items/armor/luminite_shield.png"),
                
                Path.Combine(basePath, "Assets/Images/items/consumables/health_potion.png"),
                Path.Combine(basePath, "Assets/Images/items/consumables/healing_potion.png"),
                Path.Combine(basePath, "Assets/Images/items/consumables/rage_potion.png"),
                Path.Combine(basePath, "Assets/Images/items/consumables/invulnerability_potion.png"),
                Path.Combine(basePath, "Assets/Images/items/consumables/bomb.png"),
                Path.Combine(basePath, "Assets/Images/items/consumables/pillow.png"),
                Path.Combine(basePath, "Assets/Images/items/consumables/poisoned_shuriken.png"),
                Path.Combine(basePath, "Assets/Images/items/consumables/generic_potion.png"),
                
                Path.Combine(basePath, "Assets/Images/Locations/village.png"),
                Path.Combine(basePath, "Assets/Images/Locations/forest.png"),
                Path.Combine(basePath, "Assets/Images/Locations/cave.png"),
                Path.Combine(basePath, "Assets/Images/Locations/ruins.png"),
                Path.Combine(basePath, "Assets/Images/Locations/castle.png"),
                
                Path.Combine(basePath, "Assets/Images/Characters/player.png"),
                Path.Combine(basePath, "Assets/Images/Characters/npc.png"),
                Path.Combine(basePath, "Assets/Images/Characters/hero.png")
            };
            
            string[] criticalDirectories = {
                Path.Combine(basePath, "Assets/Images/items/weapons"),
                Path.Combine(basePath, "Assets/Images/items/armor"),
                Path.Combine(basePath, "Assets/Images/items/consumables"),
                Path.Combine(basePath, "Assets/Images/items/materials"),
                Path.Combine(basePath, "Assets/Images/Characters"),
                Path.Combine(basePath, "Assets/Images/Locations")
            };
            
            foreach (var dir in criticalDirectories)
            {
                if (!Directory.Exists(dir))
                {
                    // Creating missing directory
                    Directory.CreateDirectory(dir);
                }
            }
            
            int missingCount = 0;
            
            foreach (var path in imagePaths)
            {
                bool exists = File.Exists(path);
                // Check file existence silently
                
                if (!exists)
                {
                    missingCount++;
                    
                    string relativePath = path.Substring(basePath.Length).Replace(Path.DirectorySeparatorChar, '/');
                    
                    if (_imageCache.ContainsKey(relativePath))
                    {
                        // Removing cached entry for missing file
                        _imageCache.Remove(relativePath);
                    }
                }
            }
            
            // Check complete - found missing files
            if (missingCount > 0)
            {
                // Missing files detected - application will use fallbacks
            }
        }
    }
} 
