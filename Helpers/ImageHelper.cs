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
                LoggingService.LogError($"РћС€РёР±РєР° РІ РєРѕРЅСЃС‚СЂСѓРєС‚РѕСЂРµ ImageHelper: {ex.Message}", ex);
            }
        }
        
        private static void InitializeImageCache()
        {
            try
            {
                if (_isCacheInitialized)
                    return;
                    
                LoggingService.LogDebug("Инициализация кэша изображений");
                
                string basePath = AppDomain.CurrentDomain.BaseDirectory;
                string defaultImagePath = Path.Combine(basePath, AssetPaths.DEFAULT_IMAGE);
                
                if (File.Exists(defaultImagePath))
                {
                    _defaultImage = CreateCachedBitmapImage(defaultImagePath);
                    _imageCache[AssetPaths.DEFAULT_IMAGE] = _defaultImage; // Кэшируем def.png по его пути
                    LoggingService.LogDebug($"Загружено дефолтное изображение из {defaultImagePath}");
                }
                else
                {
                    Console.WriteLine($"WARNING: Default image not found at {defaultImagePath}");
                    Console.WriteLine("You should manually add the def.png file to Assets/Images/ directory.");
                    
                    _defaultImage = new BitmapImage();
                    Console.WriteLine($"Created an empty BitmapImage as temporary default");
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
                                Console.WriteLine($"Cached image: {relativePath}");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Failed to cache image {relativePath}: {ex.Message}");
                            }
                        }
                    }
                }
                
                string charactersPath = Path.Combine(basePath, "Assets/Images/Characters");
                EnsureDirectoryExists(charactersPath);
                
                Console.WriteLine("Checking character sprites:");
                
                string playerPath = Path.Combine(basePath, AssetPaths.Characters.PLAYER);
                string npcPath = Path.Combine(basePath, AssetPaths.Characters.NPC);
                string heroPath = Path.Combine(basePath, AssetPaths.Characters.HERO);
                
                Console.WriteLine($"Player sprite: {(File.Exists(playerPath) ? "Found" : "Missing")}");
                Console.WriteLine($"NPC sprite: {(File.Exists(npcPath) ? "Found" : "Missing")}");
                Console.WriteLine($"Hero sprite: {(File.Exists(heroPath) ? "Found" : "Missing")}");
                
                if (!File.Exists(playerPath) || !File.Exists(npcPath) || !File.Exists(heroPath))
                {
                    Console.WriteLine("Some character sprites are missing. Please add them manually.");
                }
                
                _isCacheInitialized = true;
                Console.WriteLine("Image cache initialization completed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing image cache: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
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
                        Console.WriteLine($"Image frozen successfully: {filePath}");
                    }
                    else
                    {
                        Console.WriteLine($"WARNING: Image cannot be frozen: {filePath}");
                    }
                }
                
                return image;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in CreateCachedBitmapImage for {filePath}: {ex.Message}");
                
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
                Console.WriteLine($"Base path: {basePath}");
                
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
                Console.WriteLine($"Error initializing image directories: {ex.Message}");
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
                    
                    Console.WriteLine($"WARNING: Default image file not found at {defaultImagePath}");
                    Console.WriteLine("You should manually add the def.png file to Assets/Images/ directory.");
                    Console.WriteLine("The application will attempt to continue but some images might be missing.");
                }
                else
                {
                    Console.WriteLine($"Default image exists at {defaultImagePath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error ensuring default image exists: {ex.Message}");
            }
        }
        
        private static void CreateAndSaveSimpleImage(string path)
        {
            try
            {
                // РЎРѕР·РґР°РµРј РїСЂРѕСЃС‚РѕРµ РёР·РѕР±СЂР°Р¶РµРЅРёРµ 32x32
                int width = 32;
                int height = 32;
                
                // РЎРѕР·РґР°РµРј WriteableBitmap
                WriteableBitmap writeableBitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
                
                // Р—Р°РїРѕР»РЅСЏРµРј РµРіРѕ РїРёРєСЃРµР»СЏРјРё (СЃРµСЂС‹Р№ С„РѕРЅ СЃ С‡РµСЂРЅРѕР№ СЂР°РјРєРѕР№)
                byte[] pixels = new byte[width * height * 4];
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int index = (y * width + x) * 4;
                        // BGRA С„РѕСЂРјР°С‚
                        if (x == 0 || y == 0 || x == width - 1 || y == height - 1)
                        {
                            // Р§РµСЂРЅР°СЏ СЂР°РјРєР°
                            pixels[index] = 0;     // B
                            pixels[index + 1] = 0; // G
                            pixels[index + 2] = 0; // R
                            pixels[index + 3] = 255; // A
                        }
                        else
                        {
                            // РЎРІРµС‚Р»Рѕ-СЃРµСЂС‹Р№ С„РѕРЅ
                            pixels[index] = 200;     // B
                            pixels[index + 1] = 200; // G
                            pixels[index + 2] = 200; // R
                            pixels[index + 3] = 255; // A
                        }
                    }
                }
                
                // Р—Р°РїРёСЃС‹РІР°РµРј РїРёРєСЃРµР»Рё РІ WriteableBitmap
                writeableBitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, width * 4, 0);
                
                // РЎРѕС…СЂР°РЅСЏРµРј РІ С„Р°Р№Р»
                using (FileStream stream = new FileStream(path, FileMode.Create))
                {
                    PngBitmapEncoder encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(writeableBitmap));
                    encoder.Save(stream);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in CreateAndSaveSimpleImage: {ex.Message}");
                
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
                
                Console.WriteLine($"Created fallback image at {path}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Even fallback image creation failed: {ex.Message}");
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
                LoggingService.LogError("LoadImage вызван с пустым путем");
                return GetDefaultImage();
            }
            
            try 
            {
                string basePath = AppDomain.CurrentDomain.BaseDirectory;
                
                string normalizedPath = path.Replace('\\', '/');
                
                if (!normalizedPath.Contains("Assets/"))
                {
                    Console.WriteLine($"WARNING: Path {normalizedPath} does not contain Assets/ folder");
                    return GetDefaultImage(); 
                }
                
                string fullPath = Path.Combine(basePath, normalizedPath);
                
                if (File.Exists(fullPath))
                {
                    if (_imageCache.TryGetValue(normalizedPath, out BitmapImage cachedImage))
                    {
                        Console.WriteLine($"Cache hit: {path}");
                        return cachedImage;
                    }
                    
                    Console.WriteLine($"Loading image from: {fullPath}");
                    
                    BitmapImage image = CreateCachedBitmapImage(fullPath);
                    
                    _imageCache[normalizedPath] = image;
                    
                    Console.WriteLine($"Successfully loaded image: {path}");
                    return image;
                }
                else
                {
                    Console.WriteLine($"File not found: {fullPath}");
                    
                    string lowercasePath = normalizedPath.ToLower();
                    string fullLowercasePath = Path.Combine(basePath, lowercasePath);
                    
                    if (File.Exists(fullLowercasePath) && fullLowercasePath != fullPath)
                    {
                        Console.WriteLine($"Found lowercase version: {fullLowercasePath}");
                        BitmapImage image = CreateCachedBitmapImage(fullLowercasePath);
                        _imageCache[lowercasePath] = image;
                        return image;
                    }
                    
                    string directoryPath = Path.GetDirectoryName(fullPath);
                    if (Directory.Exists(directoryPath))
                    {
                        Console.WriteLine($"Directory exists: {directoryPath}. Files in directory:");
                        foreach (var file in Directory.GetFiles(directoryPath))
                        {
                            Console.WriteLine($" - {Path.GetFileName(file)}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Directory does not exist: {directoryPath}");
                    }
                    
                    Console.WriteLine($"Using default image for: {path}");
                    return GetDefaultImage();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading image {path}: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
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
                    Console.WriteLine($"Default image not found at {defaultPath}");
                    
                    BitmapImage emptyImage = CreateEmptyImage();
                    _imageCache["default"] = emptyImage;
                    return emptyImage;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading default image: {ex.Message}");
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
                        Console.WriteLine("Empty image created and frozen successfully");
                    }
                    else
                    {
                        Console.WriteLine("WARNING: Empty image cannot be frozen");
                    }
                }
                
                return emptyImage;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating empty image: {ex.Message}");
                
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
            
            Console.WriteLine("VerifyAllImageFiles: Checking critical image files...");
            
            string defaultImagePath = Path.Combine(basePath, DefaultImagePath);
            bool defaultImageExists = File.Exists(defaultImagePath);
            
            if (!defaultImageExists)
            {
                Console.WriteLine($"WARNING: Default image missing at {defaultImagePath}");
                Console.WriteLine("You should manually add the def.png file to Assets/Images/ directory.");
                Console.WriteLine("The application will attempt to continue but some images might be missing.");
            }
            else
            {
                Console.WriteLine($"VerifyAllImageFiles: Default image found at {defaultImagePath}");
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
                    Console.WriteLine($"VerifyAllImageFiles: Creating missing directory {dir}");
                    Directory.CreateDirectory(dir);
                }
            }
            
            int missingCount = 0;
            
            foreach (var path in imagePaths)
            {
                bool exists = File.Exists(path);
                Console.WriteLine($"VerifyAllImageFiles: {Path.GetFileName(path)} - {(exists ? "OK" : "MISSING")}");
                
                if (!exists)
                {
                    missingCount++;
                    
                    string relativePath = path.Substring(basePath.Length).Replace(Path.DirectorySeparatorChar, '/');
                    
                    if (_imageCache.ContainsKey(relativePath))
                    {
                        Console.WriteLine($"VerifyAllImageFiles: Removing cached entry for {relativePath}");
                        _imageCache.Remove(relativePath);
                    }
                }
            }
            
            Console.WriteLine($"VerifyAllImageFiles: Check complete. Found {missingCount} missing files.");
            if (missingCount > 0)
            {
                Console.WriteLine("Please add necessary image files manually to their respective directories.");
            }
        }
    }
} 
