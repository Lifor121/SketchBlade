using System;
using System.IO;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace SketchBlade.Helpers
{
    public static class ImageHelper
    {
        private const string DefaultImagePath = "Assets/Images/def.png";
        private const string LocationsFolder = "Assets/Images/Locations";
        private const string WeaponsFolder = "Assets/Images/items/weapons";
        private const string ArmorFolder = "Assets/Images/items/armor";
        private const string ConsumablesFolder = "Assets/Images/items/consumables";
        private const string MaterialsFolder = "Assets/Images/items/materials";
        private const string CharactersFolder = "Assets/Images/Characters";
        private const string EnemiesFolder = "Assets/Images/Enemies";
        private const string UIFolder = "Assets/Images/UI";
        
        public const string PlayerSpritePath = "Assets/Images/Characters/player.png";
        public const string NpcSpritePath = "Assets/Images/Characters/npc.png";
        public const string HeroSpritePath = "Assets/Images/Characters/hero.png";
        public const string DefaultSpritePath = "Assets/Images/def.png";
        
        private static readonly Dictionary<string, BitmapImage> _imageCache = new Dictionary<string, BitmapImage>();
        private static BitmapImage _defaultImage;
        private static bool _isCacheInitialized = false;
        
        static ImageHelper()
        {
            try
            {
                InitializeDirectories();
                // Инициализируем кэш изображений асинхронно
                Task.Factory.StartNew(InitializeImageCache);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in ImageHelper constructor: {ex.Message}");
            }
        }
        
        // Метод для инициализации кэша изображений
        private static void InitializeImageCache()
        {
            try
            {
                if (_isCacheInitialized)
                    return;
                    
                Console.WriteLine("Initializing image cache...");
                
                // Загружаем дефолтное изображение
                string basePath = AppDomain.CurrentDomain.BaseDirectory;
                string defaultImagePath = Path.Combine(basePath, DefaultImagePath);
                
                if (File.Exists(defaultImagePath))
                {
                    _defaultImage = CreateCachedBitmapImage(defaultImagePath);
                    _imageCache[DefaultImagePath] = _defaultImage; // Кэшируем def.png по его пути
                    Console.WriteLine($"Loaded default image from {defaultImagePath}");
                }
                else
                {
                    Console.WriteLine($"WARNING: Default image not found at {defaultImagePath}");
                    Console.WriteLine("You should manually add the def.png file to Assets/Images/ directory.");
                    
                    // Создаем пустое изображение в памяти как временное решение
                    _defaultImage = new BitmapImage();
                    Console.WriteLine($"Created an empty BitmapImage as temporary default");
                }
                
                // Предзагружаем все изображения локаций
                string locationsPath = Path.Combine(basePath, LocationsFolder);
                if (Directory.Exists(locationsPath))
                {
                    foreach (var file in Directory.GetFiles(locationsPath, "*.png"))
                    {
                        // Преобразуем абсолютный путь в относительный для кэширования
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
                
                // Убедимся, что директория для персонажей существует
                string charactersPath = Path.Combine(basePath, CharactersFolder);
                EnsureDirectoryExists(charactersPath);
                
                // Просто логируем информацию о спрайтах персонажей
                Console.WriteLine("Checking character sprites:");
                
                string playerPath = Path.Combine(basePath, PlayerSpritePath);
                string npcPath = Path.Combine(basePath, NpcSpritePath);
                string heroPath = Path.Combine(basePath, HeroSpritePath);
                
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
        
        // Метод для создания кэшированного BitmapImage из файла
        private static BitmapImage CreateCachedBitmapImage(string filePath)
        {
            // Ensure we're on the UI thread for all UI operations
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
                    
                    // Make sure the image is frozen to be thread-safe and avoid the Freezable context error
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
                
                // Create a simple fallback image that's already frozen
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
        
        // Метод для создания всех необходимых каталогов
        public static void InitializeDirectories()
        {
            try
            {
                string basePath = AppDomain.CurrentDomain.BaseDirectory;
                Console.WriteLine($"Base path: {basePath}");
                
                // основные каталоги
                EnsureDirectoryExists(Path.Combine(basePath, "Assets"));
                EnsureDirectoryExists(Path.Combine(basePath, "Assets/Images"));
                
                // подкаталоги для категорий изображений
                EnsureDirectoryExists(Path.Combine(basePath, LocationsFolder));
                EnsureDirectoryExists(Path.Combine(basePath, "Assets/Images/items"));
                EnsureDirectoryExists(Path.Combine(basePath, WeaponsFolder));
                EnsureDirectoryExists(Path.Combine(basePath, ArmorFolder));
                EnsureDirectoryExists(Path.Combine(basePath, ConsumablesFolder));
                EnsureDirectoryExists(Path.Combine(basePath, MaterialsFolder));
                EnsureDirectoryExists(Path.Combine(basePath, CharactersFolder));
                EnsureDirectoryExists(Path.Combine(basePath, EnemiesFolder));
                EnsureDirectoryExists(Path.Combine(basePath, UIFolder));
                
                // проверяем наличие изображения по умолчанию
                EnsureDefaultImageExists();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing image directories: {ex.Message}");
                throw;
            }
        }
        
        // Метод для гарантированного создания изображения по умолчанию
        public static void EnsureDefaultImageExists()
        {
            try
            {
                string basePath = AppDomain.CurrentDomain.BaseDirectory;
                string defaultImagePath = Path.Combine(basePath, DefaultImagePath);
                
                if (!File.Exists(defaultImagePath))
                {
                    // Создаем директорию, если ее нет
                    string directory = Path.GetDirectoryName(defaultImagePath);
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }
                    
                    // Выводим предупреждение вместо создания заглушки
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
        
        // Метод для создания и сохранения простого изображения
        private static void CreateAndSaveSimpleImage(string path)
        {
            try
            {
                // Создаем простое изображение 32x32
                int width = 32;
                int height = 32;
                
                // Создаем WriteableBitmap
                WriteableBitmap writeableBitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
                
                // Заполняем его пикселями (серый фон с черной рамкой)
                byte[] pixels = new byte[width * height * 4];
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int index = (y * width + x) * 4;
                        // BGRA формат
                        if (x == 0 || y == 0 || x == width - 1 || y == height - 1)
                        {
                            // Черная рамка
                            pixels[index] = 0;     // B
                            pixels[index + 1] = 0; // G
                            pixels[index + 2] = 0; // R
                            pixels[index + 3] = 255; // A
                        }
                        else
                        {
                            // Светло-серый фон
                            pixels[index] = 200;     // B
                            pixels[index + 1] = 200; // G
                            pixels[index + 2] = 200; // R
                            pixels[index + 3] = 255; // A
                        }
                    }
                }
                
                // Записываем пиксели в WriteableBitmap
                writeableBitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, width * 4, 0);
                
                // Сохраняем в файл
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
        
        // Запасной метод для создания простого изображения без System.Drawing
        private static void FallbackCreateSimpleImage(string path)
        {
            try
            {
                // Create a very simple 1-pixel image
                WriteableBitmap writeableBitmap = new WriteableBitmap(1, 1, 96, 96, PixelFormats.Bgra32, null);
                
                // Fill with white pixel
                byte[] pixels = new byte[4] { 255, 255, 255, 255 }; // White in BGRA
                writeableBitmap.WritePixels(new Int32Rect(0, 0, 1, 1), pixels, 4, 0);
                
                // Save to file
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
            // Ensure we're on the UI thread for all UI operations
            if (Application.Current != null && !Application.Current.Dispatcher.CheckAccess())
            {
                // Use Invoke with return value instead of storing in a variable
                return (BitmapImage)Application.Current.Dispatcher.Invoke(() => LoadImage(path));
            }

            if (string.IsNullOrEmpty(path))
            {
                Console.WriteLine("WARNING: LoadImage called with null or empty path");
                return GetDefaultImage();
            }
            
            try 
            {
                // Получаем путь к исполняемому файлу
                string basePath = AppDomain.CurrentDomain.BaseDirectory;
                
                // Нормализуем путь (приводим слеши к единому виду)
                string normalizedPath = path.Replace('\\', '/');
                
                // Проверяем, что путь не начинается с Assets/ и содержит его
                if (!normalizedPath.Contains("Assets/"))
                {
                    Console.WriteLine($"WARNING: Path {normalizedPath} does not contain Assets/ folder");
                    return GetDefaultImage(); // Просто возвращаем дефолтное изображение без кэширования
                }
                
                // Формируем полный путь к файлу
                string fullPath = Path.Combine(basePath, normalizedPath);
                
                // Проверяем кэш только если файл существует
                if (File.Exists(fullPath))
                {
                    // Проверяем кэш
                    if (_imageCache.TryGetValue(normalizedPath, out BitmapImage cachedImage))
                    {
                        Console.WriteLine($"Cache hit: {path}");
                        return cachedImage;
                    }
                    
                    Console.WriteLine($"Loading image from: {fullPath}");
                    
                    BitmapImage image = CreateCachedBitmapImage(fullPath);
                    
                    // Кэшируем для будущего использования
                    _imageCache[normalizedPath] = image;
                    
                    Console.WriteLine($"Successfully loaded image: {path}");
                    return image;
                }
                else
                {
                    Console.WriteLine($"File not found: {fullPath}");
                    
                    // Пробуем файл с нижним регистром
                    string lowercasePath = normalizedPath.ToLower();
                    string fullLowercasePath = Path.Combine(basePath, lowercasePath);
                    
                    if (File.Exists(fullLowercasePath) && fullLowercasePath != fullPath)
                    {
                        Console.WriteLine($"Found lowercase version: {fullLowercasePath}");
                        BitmapImage image = CreateCachedBitmapImage(fullLowercasePath);
                        _imageCache[lowercasePath] = image; // Кэшируем по правильному пути с нижним регистром
                        return image;
                    }
                    
                    // Проверяем фактическое наличие файла на диске
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
                    
                    // Используем дефолтное изображение без кэширования
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
        
        // Получение дефолтного изображения
        public static BitmapImage GetDefaultImage()
        {
            // Ensure we're on the UI thread for all UI operations
            if (Application.Current != null && !Application.Current.Dispatcher.CheckAccess())
            {
                return (BitmapImage)Application.Current.Dispatcher.Invoke(() => GetDefaultImage());
            }

            // Проверяем кэш
            if (_imageCache.TryGetValue("default", out BitmapImage cachedImage))
            {
                return cachedImage;
            }
            
            try
            {
                // Пробуем загрузить дефолтное изображение
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
                    
                    // Создаем пустое изображение
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
        
        // Метод для гарантированного создания каталога
        public static void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
        
        // Создание пустого изображения, гарантированно замороженного
        public static BitmapImage CreateEmptyImage()
        {
            // Ensure we're on the UI thread for all UI operations
            if (Application.Current != null && !Application.Current.Dispatcher.CheckAccess())
            {
                return (BitmapImage)Application.Current.Dispatcher.Invoke(() => CreateEmptyImage());
            }

            try
            {
                // Create a 1x1 pixel image with a transparent pixel
                int width = 1;
                int height = 1;
                WriteableBitmap writeableBitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
                
                // Create a transparent pixel
                byte[] pixels = new byte[4] { 0, 0, 0, 0 }; // Transparent in BGRA
                writeableBitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, width * 4, 0);
                
                // Convert to BitmapImage
                BitmapImage emptyImage = new BitmapImage();
                using (MemoryStream stream = new MemoryStream())
                {
                    // Save the WriteableBitmap to a memory stream
                    PngBitmapEncoder encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(writeableBitmap));
                    encoder.Save(stream);
                    
                    // Reset the stream position
                    stream.Position = 0;
                    
                    // Load the image from the memory stream
                    emptyImage.BeginInit();
                    emptyImage.CacheOption = BitmapCacheOption.OnLoad;
                    emptyImage.CreateOptions = BitmapCreateOptions.None;
                    emptyImage.StreamSource = stream;
                    emptyImage.EndInit();
                    
                    // Freeze the image to make it thread-safe
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
                
                // Fallback to an even simpler method
                try
                {
                    // Create the simplest possible BitmapImage
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
                    // Last resort - create a minimal image with no source
                    BitmapImage lastResort = new BitmapImage();
                    return lastResort;
                }
            }
        }
        
        // Метод для диагностики наличия основных ресурсов
        public static void VerifyAssetDirectories()
        {
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            string assetsDir = Path.Combine(basePath, "Assets");
            string imagesDir = Path.Combine(assetsDir, "Images");
            string defaultImagePath = Path.Combine(basePath, DefaultImagePath);
            
            // Проверяем и создаем основные директории, если нужно
            if (!Directory.Exists(assetsDir))
                Directory.CreateDirectory(assetsDir);
            
            if (!Directory.Exists(imagesDir))
                Directory.CreateDirectory(imagesDir);
            
            // Проверяем наличие дефолтного изображения
            if (!File.Exists(defaultImagePath))
                EnsureDefaultImageExists();
        }

        // Method to ensure critical assets are in the correct locations
        public static void EnsureCriticalAssets()
        {
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            
            // First, ensure the default image exists as a fallback
            string defaultImagePath = Path.Combine(basePath, DefaultImagePath);
            if (!File.Exists(defaultImagePath))
            {
                EnsureDefaultImageExists();
            }
            
            // Verify all game images
            VerifyAllImageFiles();
        }
        
        // Метод для получения изображения с запасным вариантом
        public static BitmapImage GetImageWithFallback(string path)
        {
            // Ensure we're on the UI thread for all UI operations
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

        // Метод для проверки существования всех основных файлов изображений
        public static void VerifyAllImageFiles()
        {
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            
            Console.WriteLine("VerifyAllImageFiles: Checking critical image files...");
            
            // Первым делом проверяем наличие def.png
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
            
            // Список путей к основным изображениям
            var imagePaths = new List<string>
            {
                // Оружие
                Path.Combine(basePath, "Assets/Images/items/weapons/wooden_sword.png"),
                Path.Combine(basePath, "Assets/Images/items/weapons/iron_sword.png"),
                Path.Combine(basePath, "Assets/Images/items/weapons/golden_sword.png"),
                Path.Combine(basePath, "Assets/Images/items/weapons/luminite_sword.png"),
                Path.Combine(basePath, "Assets/Images/items/weapons/wooden_axe.png"),
                Path.Combine(basePath, "Assets/Images/items/weapons/iron_axe.png"),
                Path.Combine(basePath, "Assets/Images/items/weapons/golden_axe.png"),
                Path.Combine(basePath, "Assets/Images/items/weapons/luminite_axe.png"),
                
                // Броня - шлемы
                Path.Combine(basePath, "Assets/Images/items/armor/wooden_helmet.png"),
                Path.Combine(basePath, "Assets/Images/items/armor/iron_helmet.png"),
                Path.Combine(basePath, "Assets/Images/items/armor/golden_helmet.png"),
                Path.Combine(basePath, "Assets/Images/items/armor/luminite_helmet.png"),
                
                // Броня - нагрудники
                Path.Combine(basePath, "Assets/Images/items/armor/wooden_chest.png"),
                Path.Combine(basePath, "Assets/Images/items/armor/iron_chest.png"),
                Path.Combine(basePath, "Assets/Images/items/armor/golden_chest.png"),
                Path.Combine(basePath, "Assets/Images/items/armor/luminite_chest.png"),
                
                // Броня - поножи
                Path.Combine(basePath, "Assets/Images/items/armor/wooden_legs.png"),
                Path.Combine(basePath, "Assets/Images/items/armor/iron_legs.png"),
                Path.Combine(basePath, "Assets/Images/items/armor/golden_legs.png"),
                Path.Combine(basePath, "Assets/Images/items/armor/luminite_legs.png"),
                
                // Щиты
                Path.Combine(basePath, "Assets/Images/items/armor/wooden_shield.png"),
                Path.Combine(basePath, "Assets/Images/items/armor/iron_shield.png"),
                Path.Combine(basePath, "Assets/Images/items/armor/golden_shield.png"),
                Path.Combine(basePath, "Assets/Images/items/armor/luminite_shield.png"),
                
                // Расходники
                Path.Combine(basePath, "Assets/Images/items/consumables/health_potion.png"),
                Path.Combine(basePath, "Assets/Images/items/consumables/healing_potion.png"),
                Path.Combine(basePath, "Assets/Images/items/consumables/rage_potion.png"),
                Path.Combine(basePath, "Assets/Images/items/consumables/invulnerability_potion.png"),
                Path.Combine(basePath, "Assets/Images/items/consumables/bomb.png"),
                Path.Combine(basePath, "Assets/Images/items/consumables/pillow.png"),
                Path.Combine(basePath, "Assets/Images/items/consumables/poisoned_shuriken.png"),
                Path.Combine(basePath, "Assets/Images/items/consumables/generic_potion.png"),
                
                // Локации
                Path.Combine(basePath, "Assets/Images/Locations/village.png"),
                Path.Combine(basePath, "Assets/Images/Locations/forest.png"),
                Path.Combine(basePath, "Assets/Images/Locations/cave.png"),
                Path.Combine(basePath, "Assets/Images/Locations/ruins.png"),
                Path.Combine(basePath, "Assets/Images/Locations/castle.png"),
                
                // Персонажи
                Path.Combine(basePath, "Assets/Images/Characters/player.png"),
                Path.Combine(basePath, "Assets/Images/Characters/npc.png"),
                Path.Combine(basePath, "Assets/Images/Characters/hero.png")
            };
            
            // Создаём критически важные директории
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
            
            // Только проверяем наличие файлов и выводим информацию о них
            int missingCount = 0;
            
            foreach (var path in imagePaths)
            {
                bool exists = File.Exists(path);
                Console.WriteLine($"VerifyAllImageFiles: {Path.GetFileName(path)} - {(exists ? "OK" : "MISSING")}");
                
                // Подсчитываем отсутствующие файлы
                if (!exists)
                {
                    missingCount++;
                    
                    // Получаем относительный путь для проверки и очистки кэша
                    string relativePath = path.Substring(basePath.Length).Replace(Path.DirectorySeparatorChar, '/');
                    
                    // Удаляем изображение из кэша, если оно там есть
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