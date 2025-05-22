using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Threading;

namespace SketchBlade.Models
{
    // Enum for game screens
    public enum GameScreen
    {
        MainMenu,
        Inventory,
        WorldMap,
        Battle,
        Settings
    }
    
    [Serializable]
    public class GameState : INotifyPropertyChanged
    {
        [field: NonSerialized]
        public event PropertyChangedEventHandler? PropertyChanged;
        
        // Делаем метод публичным, чтобы его можно было вызывать из внешних классов
        public virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
        // Core game state
        public Character? Player { get; set; }
        public Inventory Inventory { get; set; } = new Inventory();
        public ObservableCollection<Location> Locations { get; set; } = new ObservableCollection<Location>();
        public Location? CurrentLocation { get; set; }
        public int CurrentLocationIndex { get; set; } = 0;
        private string _currentScreen = "MainMenuView";
        public string CurrentScreen 
        { 
            get => _currentScreen; 
            set
            {
                if (_currentScreen != value)
                {
                    _currentScreen = value;
                    Console.WriteLine($"GameState.CurrentScreen changed to {value}");
                    OnPropertyChanged();
                }
            }
        }
        
        // Текущий ViewModel для экрана (используется в CraftingSystem)
        [NonSerialized]
        private object? _currentScreenViewModel;
        public object? CurrentScreenViewModel 
        { 
            get => _currentScreenViewModel; 
            set
            {
                if (_currentScreenViewModel != value)
                {
                    _currentScreenViewModel = value;
                    OnPropertyChanged();
                }
            }
        }
        
        // Dummy gold property (kept for compatibility, not used anymore)
        public int Gold { get; set; } = 0;
        
        // Add GameSettings property
        public GameSettings Settings { get; set; } = new GameSettings();
        
        private bool _hasSaveGame;
        public bool HasSaveGame 
        { 
            get => _hasSaveGame;
            set
            {
                if (_hasSaveGame != value)
                {
                    _hasSaveGame = value;
                    OnPropertyChanged();
                }
            }
        }
        
        // Auto-save service reference
        [NonSerialized]
        private Services.IFileSaveService _saveService = null!;
        [NonSerialized]
        private Services.IImageService _imageService = null!;
        [NonSerialized]
        private Services.GameBalanceService _balanceService = null!;
        [NonSerialized]
        private BattleManager _battleManager = null!;
        public BattleManager BattleManager => _battleManager;
        
        // Sound service reference
        [NonSerialized]
        private Services.SoundService _soundService = null!;
        
        // Flag to track if a major state change occurred that should trigger auto-save
        [NonSerialized]
        private bool _stateChanged = false;
        
        // Battle state
        public List<Character> CurrentEnemies { get; set; } = new List<Character>();
        
        // Battle rewards (for displaying after battle)
        public List<Item> BattleRewardItems { get; set; } = new List<Item>();
        public int BattleRewardGold { get; set; } = 0;
        
        // Crafting system
        [NonSerialized]
        private CraftingSystem? _craftingSystem;
        public CraftingSystem CraftingSystem => _craftingSystem ?? throw new InvalidOperationException("Crafting system is not initialized");
        
        // Player stats for display
        public string PlayerHealth => $"{Player?.CurrentHealth}/{Player?.GetTotalMaxHealth()}";
        public string PlayerStrength => Player?.Attack.ToString() ?? "0";
        public string PlayerDefense => Player?.GetTotalDefense().ToString() ?? "0";
        public string PlayerDamage => Player?.GetTotalAttack().ToString() ?? "0";
        
        private List<Location> _availableLocations = new List<Location>();
        
        // Constructor
        public GameState(Services.IFileSaveService saveService, Services.IImageService imageService, Services.GameBalanceService balanceService)
        {
            // Ensure we're on the UI thread for all UI operations
            if (Application.Current != null && !Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => 
                    InitializeGameState(saveService, imageService, balanceService)));
                return;
            }
            
            InitializeGameState(saveService, imageService, balanceService);
        }
        
        private void InitializeGameState(Services.IFileSaveService saveService, Services.IImageService imageService, Services.GameBalanceService balanceService)
        {
            _saveService = saveService;
            _imageService = imageService;
            _balanceService = balanceService;
            if (imageService is Services.SoundService soundService)
            {
                _soundService = soundService;
            }
            else
            {
                throw new ArgumentException("Image service must implement SoundService");
            }
            
            // Initialize collections
            Locations = new ObservableCollection<Location>();
            CurrentEnemies = new List<Character>();
            Inventory = new Inventory();
            
            // Initialize battle manager
            _battleManager = new BattleManager(this);
            
            // Initialize with default settings
            Initialize();
            
            // Mark the Village location as immediately available for first-time play
            if (Locations != null && Locations.Count > 0 && Locations[0].Name == "Village")
            {
                Locations[0].IsUnlocked = true;
                Locations[0].IsCompleted = false; // Make sure it's not marked as completed
                CurrentLocation = Locations[0];
                CurrentLocationIndex = 0;
                Console.WriteLine("Village location set as current and available");
            }
            
            // Subscribe to events that should trigger auto-save
            PropertyChanged += GameState_PropertyChanged;
            
            // Subscribe to settings changes
            Settings.PropertyChanged += Settings_PropertyChanged;
        }
        
        // Default constructor for serialization
        public GameState() : this(new Services.GameSaveService(), new Services.SoundService(), new Services.GameBalanceService())
        {
        }
        
        private void GameState_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // State change tracking for auto-saving on significant changes
            if (e.PropertyName == nameof(CurrentScreen) ||
                e.PropertyName == nameof(CurrentLocation) ||
                e.PropertyName == nameof(Gold))
            {
                _stateChanged = true;
                
                // On certain screens, trigger auto-save after delay
                if (CurrentScreen == "InventoryView" || 
                    CurrentScreen == "WorldMapView")
                {
                    TriggerDelayedAutoSave();
                }
                
                // Play appropriate sounds based on screen change
                if (e.PropertyName == nameof(CurrentScreen))
                {
                    HandleScreenChange();
                }
                
                // Play location-appropriate music
                if (e.PropertyName == nameof(CurrentLocation) && CurrentLocation != null)
                {
                    _soundService.PlayLocationMusic(CurrentLocation.Type);
                }
            }
        }
        
        private void HandleScreenChange()
        {
            switch (CurrentScreen)
            {
                case "MainMenuView":
                    _soundService.PlaySound(Services.SoundType.MenuOpen);
                    break;
                case "BattleView":
                    // Battle screen logic
                    OnPropertyChanged(nameof(CurrentEnemies));
                    OnPropertyChanged(nameof(Player));
                    break;
                // Add other cases as needed
            }
        }
        
        // Auto-save settings
        public bool AutoSaveEnabled
        {
            get => _saveService.AutoSaveEnabled;
            set => _saveService.AutoSaveEnabled = value;
        }
        
        public TimeSpan AutoSaveInterval
        {
            get => _saveService.AutoSaveInterval;
            set => _saveService.AutoSaveInterval = value;
        }
        
        private System.Threading.Timer? _delayedSaveTimer = null;
        
        private void TriggerDelayedAutoSave()
        {
            if (_stateChanged && AutoSaveEnabled)
            {
                // Cancel any pending timer
                _delayedSaveTimer?.Dispose();
                
                // Create new timer that will save after delay
                _delayedSaveTimer = new System.Threading.Timer(
                    _ => 
                    {
                        _saveService.TriggerAutoSave();
                        _stateChanged = false;
                    },
                    null,
                    TimeSpan.FromSeconds(2), // 2 second delay
                    Timeout.InfiniteTimeSpan); // Don't repeat
            }
        }
        
        public void Initialize()
        {
            // Use a more reliable approach to ensure we're on the UI thread
            if (Application.Current != null)
            {
                if (!Application.Current.Dispatcher.CheckAccess())
                {
                    Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => 
                    {
                        InitializeOnUIThread();
                    }));
                    return;
                }
                else
                {
                    InitializeOnUIThread();
                }
            }
            else
            {
                // Fallback if Application.Current is null (should not happen in normal WPF app)
                InitializeOnUIThread();
            }
        }
        
        private void InitializeOnUIThread()
        {
            Console.WriteLine("Initializing game state on UI thread...");
            
            try
            {
                // Preload and freeze all critical images to prevent freezable context errors
                PreloadCriticalImages();
                
                // Initialize with basic player stats
                Player = new Character
                {
                    Name = "Hero",
                    CurrentHealth = 100,
                    MaxHealth = 100,
                    Attack = 10,
                    Defense = 5,
                    Level = 1,
                    XP = 0,
                    XPToNextLevel = 100,
                    Money = 50
                };
                
                // Initialize inventory
                Inventory = new Inventory(15);
                InitializeInventory();
                
                // Initialize crafting system (after inventory)
                _craftingSystem = new CraftingSystem(this);
                
                // Initialize locations
                Locations.Clear();
                InitializeLocations();
                
                // Initialize game balance service
                _balanceService = new Services.GameBalanceService();
                
                // Set initial location to Village
                CurrentLocationIndex = 0;
                CurrentLocation = Locations.FirstOrDefault();
                
                // Set initial screen to main menu
                CurrentScreen = "MainMenuView";
                
                // Setup loot tables for locations
                SetupLootTables();
                
                // Apply game settings
                ApplyGameSettings();
                
                Console.WriteLine("Game state initialization complete on UI thread.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing game state: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
        
        // Preload all critical images to prevent freezable context errors
        private void PreloadCriticalImages()
        {
            try
            {
                Console.WriteLine("Preloading critical images...");
                
                // Ensure image directories exist
                Helpers.ImageHelper.InitializeDirectories();
                
                // Create a list of critical image paths to preload
                var criticalImagePaths = new List<string>
                {
                    // Default image
                    "Assets/Images/def.png",
                    
                    // Character images
                    "Assets/Images/Characters/player.png",
                    "Assets/Images/Characters/npc.png",
                    "Assets/Images/Characters/hero.png",
                    
                    // Item images for starting inventory
                    "Assets/Images/items/materials/wood.png",
                    "Assets/Images/items/materials/herb.png",
                    "Assets/Images/items/materials/cloth.png",
                    "Assets/Images/items/materials/flask.png",
                    "Assets/Images/items/consumables/healing_potion.png",
                    "Assets/Images/items/weapons/wooden_sword.png"
                };
                
                // Use a safer approach to preload images
                if (Application.Current != null)
                {
                    // Ensure we're on the UI thread
                    if (!Application.Current.Dispatcher.CheckAccess())
                    {
                        Application.Current.Dispatcher.Invoke(() => 
                        {
                            PreloadImagesOnUIThread(criticalImagePaths);
                        });
                    }
                    else
                    {
                        PreloadImagesOnUIThread(criticalImagePaths);
                    }
                }
                
                Console.WriteLine("Finished preloading critical images");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in PreloadCriticalImages: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }
        
        private void PreloadImagesOnUIThread(List<string> imagePaths)
        {
            foreach (var path in imagePaths)
            {
                try
                {
                    // Load image and ensure it's frozen
                    var image = Helpers.ImageHelper.LoadImage(path);
                    if (image != null && !image.IsFrozen && image.CanFreeze)
                    {
                        image.Freeze();
                        Console.WriteLine($"Preloaded and froze image: {path}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error preloading image {path}: {ex.Message}");
                }
            }
            
            // Create and freeze an empty image for use when needed
            var emptyImage = Helpers.ImageHelper.CreateEmptyImage();
            if (emptyImage != null && !emptyImage.IsFrozen && emptyImage.CanFreeze)
            {
                emptyImage.Freeze();
                Console.WriteLine("Created and froze empty image");
            }
        }
        
        // Initialize inventory with starting items
        private void InitializeInventory()
        {
            // Ensure that inventory slots are initialized
            if (Inventory.Items.Count < 15)
            {
                Console.WriteLine("InitializeInventory: inventory slots not initialized, initializing now");
                Inventory.Clear(); // This also ensures collections exist
            }
            
            // Find empty slots directly
            int slotIndex = 0;
            
            // Add basic materials directly to inventory slots
            Inventory.Items[slotIndex++] = ItemFactory.CreateWood(10);
            Inventory.Items[slotIndex++] = ItemFactory.CreateHerb(8);
            Inventory.Items[slotIndex++] = ItemFactory.CreateCloth(5);
            Inventory.Items[slotIndex++] = ItemFactory.CreateFlask(2);
            
            // Add healing potion
            Inventory.Items[slotIndex++] = ItemFactory.CreateHealingPotion(2);
            
            // Add starting weapon
            var startingWeapon = ItemFactory.CreateWoodenWeapon();
            Player?.EquipItem(startingWeapon);
        }
        
        private void InitializeLocations()
        {
            // Очистим существующие локации перед инициализацией
            Locations.Clear();
            
            // Базовая деревня - начальная локация
            Locations.Add(new Location
            {
                Name = "Village",
                Description = "A peaceful village with friendly inhabitants",
                Type = LocationType.Village,
                IsCompleted = false,
                IsUnlocked = true, // Стартовая локация
                Difficulty = LocationDifficultyLevel.Easy,
                MinPlayerLevel = 1,
                MaxCompletions = 10, // Деревню можно посещать много раз
                Hero = new Character
                {
                    Name = "Village Elder",
                    MaxHealth = 120,
                    CurrentHealth = 120,
                    Attack = 12,
                    Defense = 8,
                    IsHero = true
                }
            });
            
            // Лес - вторая локация, требует прохождения деревни
            Locations.Add(new Location
            {
                Name = "Forest",
                Description = "A dense forest filled with dangerous creatures",
                Type = LocationType.Forest,
                IsCompleted = false,
                IsUnlocked = false, // Нужно разблокировать
                Difficulty = LocationDifficultyLevel.Medium,
                MinPlayerLevel = 3,
                RequiredCompletedLocations = new List<string> { "Village" },
                MaxCompletions = 8,
                Hero = new Character
                {
                    Name = "Forest Guardian",
                    MaxHealth = 200,
                    CurrentHealth = 200,
                    Attack = 20,
                    Defense = 15,
                    IsHero = true
                }
            });
            
            // Пещера - требует прохождения леса
            Locations.Add(new Location
            {
                Name = "Cave",
                Description = "A dark cave with precious minerals and lurking monsters",
                Type = LocationType.Cave,
                IsCompleted = false,
                IsUnlocked = false,
                Difficulty = LocationDifficultyLevel.Hard,
                MinPlayerLevel = 5,
                RequiredCompletedLocations = new List<string> { "Forest" },
                MaxCompletions = 6,
                Hero = new Character
                {
                    Name = "Cave Troll",
                    MaxHealth = 350,
                    CurrentHealth = 350,
                    Attack = 30,
                    Defense = 25,
                    IsHero = true
                }
            });
            
            // Руины - требуют прохождения пещеры
            Locations.Add(new Location
            {
                Name = "Ancient Ruins",
                Description = "Mysterious ruins filled with ancient treasures and dangers",
                Type = LocationType.Ruins,
                IsCompleted = false,
                IsUnlocked = false,
                Difficulty = LocationDifficultyLevel.VeryHard,
                MinPlayerLevel = 8,
                RequiredCompletedLocations = new List<string> { "Cave" },
                MaxCompletions = 4,
                Hero = new Character
                {
                    Name = "Guardian Golem",
                    MaxHealth = 500,
                    CurrentHealth = 500,
                    Attack = 45,
                    Defense = 40,
                    IsHero = true
                }
            });
            
            // Замок - финальная локация
            Locations.Add(new Location
            {
                Name = "Castle",
                Description = "A grand castle where the final hero awaits",
                Type = LocationType.Castle,
                IsCompleted = false,
                IsUnlocked = false,
                Difficulty = LocationDifficultyLevel.Extreme,
                MinPlayerLevel = 10,
                RequiredCompletedLocations = new List<string> { "Ancient Ruins" },
                MaxCompletions = 2, // Limited number of completions
                Hero = new Character
                {
                    Name = "King of Darkness",
                    MaxHealth = 1000,
                    CurrentHealth = 1000,
                    Attack = 70,
                    Defense = 60,
                    IsHero = true
                }
            });
            
            // Настраиваем таблицы дропа для локаций
            SetupLootTables();
            
            // Load sprites for each location based on location type
            foreach (var location in Locations)
            {
                string imagePath = $"Assets/Images/Locations/{location.Type.ToString().ToLower()}.png";
                location.SpritePath = imagePath;
                
                // Предварительно проверяем и кэшируем изображения локаций
                Console.WriteLine($"Preloading location sprite for {location.Name}: {imagePath}");
                Helpers.ImageHelper.GetImageWithFallback(imagePath);
            }
            
            // Ensure all locations have unique names to avoid UI issues
            for (int i = 0; i < Locations.Count; i++)
            {
                if (string.IsNullOrEmpty(Locations[i].Name))
                {
                    Locations[i].Name = $"Location {i+1}";
                }
            }
            
            CurrentLocation = Locations[0];
        }
        
        private void SetupLootTables()
        {
            if (Locations == null) return;
            
            // Village - base items and materials per README
            var villageItems = new Item[]
            {
                ItemFactory.CreateHealingPotion(1),
                ItemFactory.CreateWood(1),
                ItemFactory.CreateHerb(1),
                ItemFactory.CreateCloth(1),
                ItemFactory.CreateFlask(1)
            };
            Locations[0].PossibleLoot = villageItems;
            
            // Forest - items per README
            var forestItems = new Item[]
            {
                ItemFactory.CreateRagePotion(1),
                ItemFactory.CreateIronOre(1),
                ItemFactory.CreateCrystalDust(1),
                ItemFactory.CreateFeather(1)
            };
            Locations[1].PossibleLoot = forestItems;
            
            // Cave - items per README
            var caveItems = new Item[]
            {
                ItemFactory.CreateInvulnerabilityPotion(1),
                ItemFactory.CreateIronIngot(1),
                ItemFactory.CreateGoldOre(1),
                ItemFactory.CreateGunpowder(1)
            };
            Locations[2].PossibleLoot = caveItems;
            
            // Ruins - items per README
            var ruinsItems = new Item[]
            {
                ItemFactory.CreateBomb(1),
                ItemFactory.CreatePillow(1),
                ItemFactory.CreateGoldIngot(1),
                ItemFactory.CreatePoisonExtract(1),
                ItemFactory.CreateLuminiteFragment(1)
            };
            Locations[3].PossibleLoot = ruinsItems;
            
            // Castle - best items in game per README
            var castleItems = new Item[]
            {
                ItemFactory.CreatePoisonedShuriken(1),
                ItemFactory.CreateGoldIngot(1),
                ItemFactory.CreateLuminiteFragment(1),
                ItemFactory.CreateLuminite(1)
            };
            Locations[4].PossibleLoot = castleItems;
        }
        
        public void CheckForSaveGame()
        {
            HasSaveGame = _saveService.SaveExists();
        }
        
        public void SaveGame()
        {
            // Ensure we're on the UI thread for all UI operations
            if (Application.Current != null && !Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => SaveGame()));
                return;
            }

            try
            {
                // Проверка, что все необходимые данные присутствуют перед сохранением
                if (Player == null)
                {
                    Console.WriteLine("Cannot save game: Player is null");
                    return;
                }
                
                if (Locations == null || Locations.Count == 0)
                {
                    Console.WriteLine("Cannot save game: Locations collection is empty");
                    return;
                }
                
                if (CurrentLocation == null)
                {
                    Console.WriteLine("Warning: CurrentLocation is null, will try to recover during load");
                }
                
                // Обновляем индекс текущей локации перед сохранением
                if (CurrentLocation != null && Locations != null)
                {
                    int index = Locations.IndexOf(CurrentLocation);
                    if (index >= 0)
                    {
                        CurrentLocationIndex = index;
                        Console.WriteLine($"Set CurrentLocationIndex to {index} before saving");
                    }
                }
                
                // Сохраняем в аварийный файл основные данные
                try
                {
                    var emergencyData = new Dictionary<string, object>
                    {
                        ["PlayerHealth"] = Player.CurrentHealth,
                        ["PlayerMaxHealth"] = Player.MaxHealth,
                        ["Gold"] = Gold,
                        ["CurrentLocationIndex"] = CurrentLocationIndex
                    };
                    
                    File.WriteAllText("emergency_backup.json", 
                                     JsonSerializer.Serialize(emergencyData));
                }
                catch
                {
                    // Игнорируем ошибки аварийного сохранения
                }
                
                Console.WriteLine("Calling save service to save game...");
                _saveService.SaveGame(this);
                Console.WriteLine("Game saved successfully.");
                HasSaveGame = true;
            }
            catch (NotSupportedException ex)
            {
                // Специфические ошибки сериализации из-за несериализуемых типов
                Console.WriteLine($"Error during game save (NotSupportedException): {ex.Message}");
                Console.WriteLine($"This is likely due to a serialization issue with a non-serializable type");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
            catch (InvalidOperationException ex)
            {
                // Другие ошибки в процессе сериализации
                Console.WriteLine($"Error during game save (InvalidOperationException): {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
            catch (Exception ex)
            {
                // Общий обработчик ошибок
                Console.WriteLine($"Error during game save: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
        
        public void LoadGame()
        {
            // Ensure we're on the UI thread for all UI operations
            if (Application.Current != null && !Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => LoadGame()));
                return;
            }

            try
            {
                Console.WriteLine("Starting to load game...");
                
                // Load saved game data from file
                var saveData = _saveService.LoadGame();
                if (saveData != null)
                {
                    // Keep reference to current services since they can't be serialized
                    var currentSaveService = _saveService;
                    var currentImageService = _imageService;
                    var currentBalanceService = _balanceService;
                    var currentSoundService = _soundService;
                    
                    // Apply save data to this instance
                    _saveService.ApplySaveData(this, saveData);
                    
                    // Restore references to services that can't be serialized
                    _saveService = currentSaveService;
                    _imageService = currentImageService;
                    _balanceService = currentBalanceService;
                    _soundService = currentSoundService;
                    
                    // Re-create crafting system with the loaded state
                    _craftingSystem = new CraftingSystem(this);
                    
                    // Set up battle manager
                    _battleManager = new BattleManager(this);
                    
                    // Validate player character
                    if (Player == null)
                    {
                        Console.WriteLine("WARNING: Player is null after loading, creating new player");
                        Player = new Character
                        {
                            Name = "Hero",
                            MaxHealth = 100,
                            CurrentHealth = 100,
                            Attack = 10,
                            Defense = 5,
                            Level = 1
                        };
                    }
                    
                    // Validate inventory
                    if (Inventory == null)
                    {
                        Console.WriteLine("WARNING: Inventory is null after loading, creating new inventory");
                        Inventory = new Inventory(15);
                    }
                    
                    // Ensure inventory slots are properly initialized
                    if (Inventory.Items.Count < 15)
                    {
                        Console.WriteLine("WARNING: Inventory slots not fully initialized, fixing...");
                        Inventory.Clear();
                    }
                    
                    // Make sure we have a valid current location
                    if (CurrentLocationIndex >= 0 && CurrentLocationIndex < Locations.Count)
                    {
                        CurrentLocation = Locations[CurrentLocationIndex];
                    }
                    else if (Locations.Count > 0)
                    {
                        Console.WriteLine("WARNING: Invalid CurrentLocationIndex, resetting to 0");
                        CurrentLocation = Locations[0];
                        CurrentLocationIndex = 0;
                    }
                    
                    // Set flag to indicate a save game is loaded
                    HasSaveGame = true;
                    
                    // Apply current settings
                    ApplyGameSettings();
                    
                    // Log success
                    Console.WriteLine("Game loaded successfully");
                }
                else
                {
                    Console.WriteLine("No valid save game found, starting new game");
                    Initialize();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading game: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                
                // Fall back to initializing a new game
                Initialize();
                Console.WriteLine("Initialized new game after load failure");
            }
        }
        
        // Check if a GameState is valid enough to load
        private bool IsValid(GameState? state)
        {
            if (state == null) return false;
            
            // Basic validation checks
            if (state.Player == null) return false;
            if (state.Inventory == null) return false;
            if (state.Locations == null || state.Locations.Count == 0) return false;
            
            return true;
        }
        
        // Sound service properties
        public bool IsMusicEnabled
        {
            get => _soundService.IsMusicEnabled; 
            set => _soundService.IsMusicEnabled = value;
        }
        
        public bool AreSoundEffectsEnabled
        {
            get => _soundService.AreSoundEffectsEnabled;
            set => _soundService.AreSoundEffectsEnabled = value;
        }
        
        public double MusicVolume
        {
            get => _soundService.MusicVolume;
            set => _soundService.MusicVolume = value;
        }
        
        public double SoundEffectsVolume
        {
            get => _soundService.SoundEffectsVolume;
            set => _soundService.SoundEffectsVolume = value;
        }
        
        // Get battle rewards based on battle type
        public int GetBattleRewards(bool isBossHeroBattle)
        {
            // Gold is no longer used, return 0
            return 0;
        }
        
        // Play a sound effect
        public void PlaySound(Services.SoundType soundType)
        {
            _soundService.PlaySound(soundType);
        }
        
        private void Settings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // Apply settings changes
            switch (e.PropertyName)
            {
                case nameof(GameSettings.MusicVolume):
                    MusicVolume = Settings.MusicVolume;
                    break;
                case nameof(GameSettings.SoundEffectsVolume):
                    SoundEffectsVolume = Settings.SoundEffectsVolume;
                    break;
                case nameof(GameSettings.IsMusicEnabled):
                    IsMusicEnabled = Settings.IsMusicEnabled;
                    break;
                case nameof(GameSettings.AreSoundEffectsEnabled):
                    AreSoundEffectsEnabled = Settings.AreSoundEffectsEnabled;
                    break;
                // Other settings can be handled here as needed
            }
            
            // Сохраняем настройки в файл
            Services.SettingsSaveService.SaveSettings(Settings);
            
            // Trigger save of settings
            _stateChanged = true;
            TriggerDelayedAutoSave();
        }
        
        public void StartBattleWithMobs()
        {
            Console.WriteLine("Starting battle with mobs...");
            
            // Create enemies based on the current location
            if (CurrentLocation == null)
            {
                Console.WriteLine("No current location for battle");
                return;
            }
            
            // Clear any existing enemies
            CurrentEnemies.Clear();
            
            // Create enemies based on the location's enemy data
            var enemyData = CurrentLocation.GetRandomEnemies();
            foreach (var data in enemyData)
            {
                var enemy = new Character
                {
                    Name = data.Name,
                    MaxHealth = data.Health,
                    CurrentHealth = data.Health,
                    Attack = data.Attack,
                    Defense = data.Defense,
                    Level = data.Level,
                    SpritePath = data.SpritePath
                };
                
                Console.WriteLine($"Added enemy: {enemy.Name}, HP: {enemy.CurrentHealth}/{enemy.MaxHealth}");
                CurrentEnemies.Add(enemy);
            }
            
            // Set the current screen to battle
            CurrentScreen = "BattleView";
            
            // Play battle music
            _soundService.PlaySound(Services.SoundType.BattleStart);
            _soundService.PlayMusic(Services.MusicType.Battle);
        }
        
        public void StartBattleWithHero()
        {
            Console.WriteLine("Starting boss hero battle...");
            
            // Create hero enemy based on the current location
            if (CurrentLocation == null)
            {
                Console.WriteLine("No current location for hero battle");
                return;
            }
            
            // Clear any existing enemies
            CurrentEnemies.Clear();
            
            // Get the hero data for this location
            var heroData = CurrentLocation.GetHeroData();
            if (heroData == null)
            {
                Console.WriteLine("No hero data for this location");
                return;
            }
            
            // Create the hero enemy
            var heroEnemy = new Character
            {
                Name = heroData.Name,
                MaxHealth = heroData.Health,
                CurrentHealth = heroData.Health,
                Attack = heroData.Attack,
                Defense = heroData.Defense,
                Level = heroData.Level,
                SpritePath = heroData.SpritePath,
                IsBoss = true
            };
            
            Console.WriteLine($"Added hero enemy: {heroEnemy.Name}, HP: {heroEnemy.CurrentHealth}/{heroEnemy.MaxHealth}");
            CurrentEnemies.Add(heroEnemy);
            
            // Set the current screen to battle
            CurrentScreen = "BattleView";
            
            // Play boss battle music
            _soundService.PlaySound(Services.SoundType.BattleStart);
            _soundService.PlayMusic(Services.MusicType.BossBattle);
        }
        
        // Обработка завершения сражения с обычными врагами
        public void CompleteBattle(bool isVictory)
        {
            Console.WriteLine($"\n================ CompleteBattle CALLED with isVictory={isVictory} ================");
            Console.WriteLine($"Current screen before: {CurrentScreen}");
            
            try
            {
                // Process battle results
                if (isVictory)
                {
                    Console.WriteLine("Processing victory rewards");
                    
                    // Проверяем, были ли уже добавлены награды через BattleViewModel
                    bool hasExistingRewards = BattleRewardItems != null && BattleRewardItems.Count > 0;
                    Console.WriteLine($"Has existing rewards: {hasExistingRewards} ({BattleRewardItems?.Count ?? 0} items)");
                    
                    if (hasExistingRewards)
                    {
                        Console.WriteLine($"Processing {BattleRewardItems.Count} existing reward items:");
                        
                        // Создаем копию списка наград, чтобы избежать проблем при модификации коллекции
                        var rewardsCopy = new List<Item>(BattleRewardItems);
                        
                        // Добавляем предметы в инвентарь
                        int successCount = 0;
                        foreach (var item in rewardsCopy)
                        {
                            if (item == null) continue;
                            
                            Console.WriteLine($"  - Adding {item.Name} (x{item.StackSize}) to inventory");
                            
                            // Создаем копию предмета для безопасности
                            Item itemCopy = new Item
                            {
                                Name = item.Name ?? "Unknown Item",
                                Description = item.Description ?? "",
                                Type = item.Type,
                                Rarity = item.Rarity,
                                Material = item.Material,
                                StackSize = item.StackSize,
                                MaxStackSize = item.MaxStackSize,
                                Value = item.Value,
                                Weight = item.Weight,
                                Damage = item.Damage,
                                Defense = item.Defense,
                                EffectPower = item.EffectPower,
                                SpritePath = item.SpritePath ?? ""
                            };
                            
                            // Копируем статистические бонусы
                            foreach (var bonus in item.StatBonuses)
                            {
                                itemCopy.StatBonuses.Add(bonus.Key, bonus.Value);
                            }
                            
                            // Добавляем предмет в инвентарь
                            bool added = Inventory.AddItem(itemCopy);
                            if (added)
                            {
                                successCount++;
                            }
                            else
                            {
                                Console.WriteLine($"    WARNING: Failed to add {item.Name} to inventory");
                            }
                        }
                        
                        Console.WriteLine($"Successfully added {successCount} of {rewardsCopy.Count} items to inventory");
                        
                        // Добавляем золото за победу
                        int goldToAdd = BattleRewardGold;
                        Gold += goldToAdd;
                        Console.WriteLine($"Added {goldToAdd} gold for victory");
                        
                        // Очищаем список наград, чтобы избежать дублирования
                        BattleRewardItems.Clear();
                        Console.WriteLine("Cleared BattleRewardItems to prevent duplication");
                        
                        // Принудительно обновляем отображение инвентаря
                        OnPropertyChanged(nameof(Inventory));
                        OnPropertyChanged(nameof(Gold));
                        
                        // Обновляем коллекцию предметов через Force Refresh
                        try
                        {
                            // Создаем временную копию коллекции и перезаполняем ее
                            var itemsCopy = new List<Item?>(Inventory.Items.Count);
                            foreach (var item in Inventory.Items)
                            {
                                itemsCopy.Add(item);
                            }
                            
                            Inventory.Items.Clear();
                            foreach (var item in itemsCopy)
                            {
                                Inventory.Items.Add(item);
                            }
                            
                            // Обновляем свойство Inventory для UI
                            Console.WriteLine("Forced refresh of inventory collection");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error during inventory refresh: {ex.Message}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("No rewards found in BattleRewardItems, generating rewards in GameState");
                        
                        // Get rewards only if they weren't already processed
                        var currentEnemies = CurrentEnemies != null ? CurrentEnemies : new List<Character>();
                        (int gold, List<Item> items) = BattleManager.CalculateRewards(false, currentEnemies);
                        
                        // Добавляем золото
                        if (gold > 0)
                        {
                            Gold += gold;
                            Console.WriteLine($"Added {gold} gold for victory");
                        }
                        
                        // Добавляем предметы в инвентарь
                        if (items != null && items.Count > 0)
                        {
                            Console.WriteLine($"Adding {items.Count} generated items to inventory");
                            
                            int addedCount = 0;
                            foreach (var item in items)
                            {
                                if (item == null) continue;
                                
                                bool added = Inventory.AddItem(item);
                                if (added)
                                {
                                    addedCount++;
                                }
                                else
                                {
                                    Console.WriteLine($"WARNING: Failed to add {item.Name} to inventory");
                                }
                            }
                            
                            Console.WriteLine($"Successfully added {addedCount} of {items.Count} items to inventory");
                            
                            // Принудительно обновляем UI после добавления предметов
                            OnPropertyChanged(nameof(Inventory));
                            OnPropertyChanged(nameof(Gold));
                        }
                        else
                        {
                            Console.WriteLine("No items to add to inventory");
                        }
                    }
                    
                    // Play victory sound
                    PlaySound(Services.SoundType.Victory);
                }
                else
                {
                    Console.WriteLine("Processing defeat");
                    // Play defeat sound
                    PlaySound(Services.SoundType.Defeat);
                    
                    // Handle defeat (e.g., lose some gold, return to village)
                    HandlePlayerDefeat();
                }
                
                // Clear current enemies
                if (CurrentEnemies != null)
                {
                    Console.WriteLine($"Clearing {CurrentEnemies.Count} enemies");
                    CurrentEnemies.Clear();
                }
                else
                {
                    Console.WriteLine("CurrentEnemies is null, nothing to clear");
                }
                
                // Return to previous screen (usually WorldMap)
                Console.WriteLine("Setting CurrentScreen to WorldMapView");
                CurrentScreen = "WorldMapView";
                Console.WriteLine($"Current screen after: {CurrentScreen}");
                
                // Save game after battle completion
                SaveGame();
                Console.WriteLine("Game state saved after battle completion");
                
                Console.WriteLine("CompleteBattle method finished successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in CompleteBattle: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                
                // Аварийное сохранение и обработка
                try
                {
                    // Установка экрана карты мира
                    CurrentScreen = "WorldMapView";
                    
                    // Сохранение игры в любом случае
                    SaveGame();
                    Console.WriteLine("Emergency save completed after error");
                }
                catch (Exception saveEx)
                {
                    Console.WriteLine($"CRITICAL: Failed emergency save: {saveEx.Message}");
                }
            }
            
            Console.WriteLine("================ CompleteBattle COMPLETED ================\n");
        }
        
        // Метод для обработки поражения игрока
        private void HandlePlayerDefeat()
        {
            // Игрок теряет часть золота при поражении
            int goldLoss = Math.Min(Gold, Gold / 5); // Потеря 20% золота, но не меньше 0
            Gold -= goldLoss;
            Console.WriteLine($"Player lost {goldLoss} gold due to defeat");
            
            // Восстанавливаем здоровье персонажа частично
            if (Player != null)
            {
                int recoveryAmount = Player.MaxHealth / 2; // Восстанавливаем 50% здоровья
                Player.CurrentHealth = recoveryAmount;
                Console.WriteLine($"Player health restored to {recoveryAmount}");
            }
            
            // Возвращаем игрока в деревню (первая локация)
            if (Locations.Count > 0)
            {
                CurrentLocation = Locations[0]; // Деревня обычно первая локация
                CurrentLocationIndex = 0;
                Console.WriteLine("Player returned to Village after defeat");
            }
        }
        
        // Обработка завершения сражения с героем локации
        public void CompleteBossHeroBattle(bool isVictory)
        {
            Console.WriteLine($"================ CompleteBossHeroBattle CALLED with isVictory={isVictory} ================");
            Console.WriteLine($"Current screen before: {CurrentScreen}");
            
            try
            {
                if (isVictory && CurrentLocation != null)
                {
                    Console.WriteLine($"Boss hero battle completed with victory: {isVictory}");
                    
                    // Mark the hero as defeated
                    CurrentLocation.HeroDefeated = true;
                    
                    // Mark location as completed if first time
                    if (!CurrentLocation.IsCompleted)
                    {
                        CurrentLocation.IsCompleted = true;
                        CurrentLocation.CompletionCount = 1;
                    }
                    else
                    {
                        // Increment completion count if already completed before
                        CurrentLocation.CompletionCount++;
                    }
                    
                    // Process drop from hero
                    int itemCount = new Random().Next(3, 6); // Hero drops 3-5 items
                    var loot = CurrentLocation.GenerateLoot(itemCount);
                    
                    // Add loot to inventory
                    int successCount = 0;
                    foreach (var item in loot)
                    {
                        bool added = Inventory.AddItem(item);
                        if (added)
                        {
                            successCount++;
                        }
                        else
                        {
                            Console.WriteLine($"Could not add {item.Name} to inventory (full)");
                        }
                    }
                    
                    // Calculate gold reward
                    int goldReward = GetBattleRewards(true);
                    Gold += goldReward;
                    
                    // Unlock the next location if conditions are met
                    UnlockNextLocation();
                    
                    // Формируем лог о наградах
                    if (CurrentLocation.Hero != null) // Добавляем проверку на null
                    {
                        Console.WriteLine($"Defeated {CurrentLocation.Hero.Name} and earned {goldReward} gold");
                    }
                    else
                    {
                        Console.WriteLine($"Defeated hero and earned {goldReward} gold");
                    }
                    
                    if (loot.Count > 0)
                    {
                        string itemsLog = string.Join(", ", loot.Select(i => i.Name));
                        Console.WriteLine($"Received items: {itemsLog}");
                    }
                    
                    // Принудительно обновляем инвентарь после добавления предметов
                    OnPropertyChanged(nameof(Inventory));
                    OnPropertyChanged(nameof(Gold));
                    
                    // Принудительно обновляем коллекцию предметов инвентаря
                    try
                    {
                        // Создаем временную копию и заново заполняем
                        var tempItems = new List<Item?>(Inventory.Items.Count);
                        foreach (var item in Inventory.Items)
                        {
                            tempItems.Add(item);
                        }
                        
                        Inventory.Items.Clear();
                        foreach (var item in tempItems)
                        {
                            Inventory.Items.Add(item);
                        }
                    }
                    catch (Exception refreshEx)
                    {
                        Console.WriteLine($"Error refreshing inventory: {refreshEx.Message}");
                    }
                    
                    // Save game after hero defeated
                    SaveGame();
                }
                else
                {
                    Console.WriteLine("Boss battle ended in defeat, handling player defeat");
                    // Handle defeat
                    HandlePlayerDefeat();
                }
                
                // Clear current enemies
                Console.WriteLine($"Clearing {CurrentEnemies.Count} enemies");
                CurrentEnemies.Clear();
                
                // Return to previous screen (usually WorldMap)
                Console.WriteLine("Setting CurrentScreen to WorldMapView");
                CurrentScreen = "WorldMapView";
                Console.WriteLine($"Current screen after: {CurrentScreen}");
                
                Console.WriteLine("CompleteBossHeroBattle method finished successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in CompleteBossHeroBattle: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                
                // Аварийная обработка ошибки
                try
                {
                    // Вернуться на экран карты мира
                    CurrentScreen = "WorldMapView";
                    SaveGame();
                }
                catch
                {
                    // Игнорируем ошибки аварийной обработки
                }
            }
            
            Console.WriteLine("================ CompleteBossHeroBattle COMPLETED ================");
        }
        
        // Helper method to unlock the next location if conditions are met
        private void UnlockNextLocation()
        {
            if (Locations == null || CurrentLocation == null)
                return;
            
            int nextIndex = CurrentLocationIndex + 1;
            
            // Check if there's a next location to unlock
            if (nextIndex < Locations.Count)
            {
                var nextLocation = Locations[nextIndex];
                
                // Check if the next location requires this one to be completed
                if (nextLocation.RequiredCompletedLocations.Contains(CurrentLocation.Name))
                {
                    bool allRequirementsMet = true;
                    
                    // Check all requirements for the next location
                    foreach (var requiredLocName in nextLocation.RequiredCompletedLocations)
                    {
                        bool foundCompleted = false;
                        
                        // Find the required location and check if it's completed
                        foreach (var loc in Locations)
                        {
                            if (loc.Name == requiredLocName && loc.IsCompleted)
                            {
                                foundCompleted = true;
                                break;
                            }
                        }
                        
                        if (!foundCompleted)
                        {
                            allRequirementsMet = false;
                            break;
                        }
                    }
                    
                    // If all requirements are met, unlock the next location
                    if (allRequirementsMet)
                    {
                        nextLocation.IsUnlocked = true;
                        Console.WriteLine($"Unlocked next location: {nextLocation.Name}");
                        
                        // Show message about unlocking new location
                        MessageBox.Show($"You've unlocked a new location: {nextLocation.Name}!", 
                                      "New Location Available", 
                                      MessageBoxButton.OK, 
                                      MessageBoxImage.Information);
                    }
                }
            }
        }
        
        // Helper method to apply game settings
        private void ApplyGameSettings()
        {
            // Implement the logic to apply settings to the game state
            // This method should be implemented based on the specific requirements of your game
        }
    }
} 