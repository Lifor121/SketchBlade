using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Linq;
using SketchBlade.Services;

namespace SketchBlade.Models
{
    [Serializable]
    public class GameData : INotifyPropertyChanged
    {
        [field: NonSerialized]
        public event PropertyChangedEventHandler? PropertyChanged;

        private Character? _player;
        public Character? Player 
        { 
            get => _player; 
            set 
            { 
                if (_player != value)
                {
                    // Отписываемся от старого игрока
                    if (_player != null)
                    {
                        _player.PropertyChanged -= OnPlayerPropertyChanged;
                    }
                    
                    _player = value;
                    
                    // Подписываемся на нового игрока
                    if (_player != null)
                    {
                        _player.PropertyChanged += OnPlayerPropertyChanged;
                    }
                    
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(PlayerHealth));
                    OnPropertyChanged(nameof(PlayerStrength));
                    OnPropertyChanged(nameof(PlayerDefense));
                    OnPropertyChanged(nameof(PlayerDamage));
                }
            }
        }

        private Inventory _inventory = new Inventory();
        public Inventory Inventory 
        { 
            get => _inventory; 
            set 
            { 
                if (_inventory != value)
                {
                    // Отписываемся от старого инвентаря
                    if (_inventory != null)
                    {
                        _inventory.PropertyChanged -= OnInventoryPropertyChanged;
                        _inventory.InventoryChanged -= OnInventoryChanged;
                    }
                    
                    _inventory = value ?? new Inventory();
                    
                    // Подписываемся на новый инвентарь
                    _inventory.PropertyChanged += OnInventoryPropertyChanged;
                    _inventory.InventoryChanged += OnInventoryChanged;
                    
                    OnPropertyChanged();
                }
            }
        }
        
        public ObservableCollection<Location> Locations { get; set; } = new ObservableCollection<Location>();
        public Location? CurrentLocation { get; set; }
        public int CurrentLocationIndex { get; set; } = 0;

        private string _currentScreen = "MainMenuView";
        public string CurrentScreen
        {
            get => _currentScreen;
            set => SetProperty(ref _currentScreen, value);
        }

        public GameSettings Settings { get; set; } = new GameSettings();

        private bool _hasSaveGame = false;
        public bool HasSaveGame
        {
            get => _hasSaveGame;
            set => SetProperty(ref _hasSaveGame, value);
        }

        public List<Character> CurrentEnemies { get; set; } = new List<Character>();
        private List<Item> _battleRewardItems = new List<Item>();
        public List<Item> BattleRewardItems 
        { 
            get => _battleRewardItems; 
            set => SetProperty(ref _battleRewardItems, value);
        }
        public int BattleRewardGold { get; set; } = 0;

        private int _gold = 0;
        public int Gold 
        { 
            get => _gold; 
            set 
            {
                if (_gold != value)
                {
                    _gold = value;
                    OnPropertyChanged();
                    LoggingService.LogDebug($"Gold changed to {value}");
                }
            }
        }

        public object? CurrentScreenViewModel { get; set; }

        public string PlayerHealth => $"{Player?.CurrentHealth}/{Player?.GetTotalMaxHealth()}";
        public string PlayerStrength => Player?.Attack.ToString() ?? "0";
        public string PlayerDefense => Player?.GetTotalDefense().ToString() ?? "0";
        public string PlayerDamage => Player?.GetTotalAttack().ToString() ?? "0";

        public GameData()
        {
            Locations = new ObservableCollection<Location>();
            CurrentEnemies = new List<Character>();
            Settings = new GameSettings();
            
            // Загружаем настройки из файла при создании
            CoreGameService.Instance.LoadGameSettings(Settings);
            
            // Подписываемся на изменения в инвентаре
            _inventory.PropertyChanged += OnInventoryPropertyChanged;
            _inventory.InventoryChanged += OnInventoryChanged;
        }

        private void OnPlayerPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // Уведомляем об изменениях характеристик игрока
            OnPropertyChanged(nameof(PlayerHealth));
            OnPropertyChanged(nameof(PlayerStrength));
            OnPropertyChanged(nameof(PlayerDefense));
            OnPropertyChanged(nameof(PlayerDamage));
            
            LoggingService.LogDebug($"Player property changed: {e.PropertyName}");
        }

        private void OnInventoryPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            LoggingService.LogDebug($"Inventory property changed: {e.PropertyName}");
            OnPropertyChanged(nameof(Inventory));
        }

        private void OnInventoryChanged(object? sender, EventArgs e)
        {
            LoggingService.LogDebug("Inventory contents changed");
            OnPropertyChanged(nameof(Inventory));
        }

        public void Reset()
        {
            Player = null;
            CurrentLocation = null;
            CurrentLocationIndex = 0;
            CurrentScreen = "MainMenuView";
            HasSaveGame = false;
            Gold = 0;
            BattleRewardGold = 0;

            CurrentEnemies.Clear();
            BattleRewardItems = new List<Item>();
            Locations.Clear();
            
            Inventory = new Inventory();
            Settings = new GameSettings();
            
            // Загружаем настройки из файла после сброса
            CoreGameService.Instance.LoadGameSettings(Settings);
            
            // Уведомляем об изменении ключевых свойств
            OnPropertyChanged(nameof(Player));
            OnPropertyChanged(nameof(Inventory));
            OnPropertyChanged(nameof(Locations));
            OnPropertyChanged(nameof(CurrentLocation));
            OnPropertyChanged(nameof(HasSaveGame));
            OnPropertyChanged(nameof(Gold));
        }

        public GameData CreateSaveCopy()
        {
            return new GameData
            {
                Player = Player,
                Inventory = Inventory,
                CurrentLocation = CurrentLocation,
                CurrentLocationIndex = CurrentLocationIndex,
                Settings = Settings,
                Gold = Gold,
                HasSaveGame = true,
                Locations = new ObservableCollection<Location>(Locations)
            };
        }

        public bool IsValid()
        {
            if (Player == null) return false;
            if (Inventory == null) return false;
            if (Locations == null || Locations.Count == 0) return false;
            if (Settings == null) return false;
            
            return true;
        }

        // ================ МЕТОДЫ ОБРАТНОЙ СОВМЕСТИМОСТИ ================
        // Делегируют к соответствующим сервисам для сохранения архитектуры

        public void Initialize()
        {
            LoggingService.LogInfo("=== ИНИЦИАЛИЗАЦИЯ НОВОЙ ИГРЫ (GameData.Initialize) ===");
            
            // Очищаем все данные перед инициализацией новой игры
            Reset();
            
            var gameInitializer = new Services.GameInitializer();
            gameInitializer.InitializeNewGame(this);
            
            // После инициализации новой игры проверяем наличие сохранений на диске
            // Это важно для корректного отображения кнопки "Продолжить"
            CheckForSaveGame();
            
            // Автоматически сохраняем игру после инициализации
            // Это гарантирует, что кнопка "Продолжить" будет активна сразу
            SaveGame();
            
            LoggingService.LogInfo("=== ИГРА ИНИЦИАЛИЗИРОВАНА УСПЕШНО ===");
        }

        public void SaveGame()
        {
            try
            {
                LoggingService.LogInfo("=== СОХРАНЕНИЕ ИГРЫ ===");
                
                // Используем новую оптимизированную систему сохранений
                bool success = OptimizedSaveSystem.SaveGame(this);
                
                if (success)
                {
                    HasSaveGame = true;
                    LoggingService.LogInfo("Игра успешно сохранена в оптимизированном формате");
                }
                else
                {
                    LoggingService.LogError("Не удалось сохранить игру");
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Ошибка при сохранении игры: {ex.Message}", ex);
            }
        }

        public void LoadGame()
        {
            try
            {
                LoggingService.LogInfo("=== ЗАГРУЗКА ИГРЫ ===");
                
                // Загружаем из оптимизированного формата
                var loadedData = OptimizedSaveSystem.LoadGame();
                
                if (loadedData != null)
                {
                    CopyFrom(loadedData);
                    HasSaveGame = true;
                    
                    // Загружаем настройки из файла после загрузки игры
                    CoreGameService.Instance.LoadGameSettings(Settings);
                    
                    LoggingService.LogInfo("Игра успешно загружена из оптимизированного формата");
                }
                else
                {
                    LoggingService.LogError("Не удалось загрузить игру");
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Ошибка при загрузке игры: {ex.Message}", ex);
            }
        }

        private void CheckForSaveGame()
        {
            // Проверяем наличие оптимизированного сохранения
            HasSaveGame = OptimizedSaveSystem.HasSaveFile();
            if (HasSaveGame)
            {
                LoggingService.LogInfo("Найдено сохранение игры");
            }
            else
            {
                LoggingService.LogInfo("Сохранения игры не найдены");
            }
        }

        public void StartBattleWithMobs(Location location)
        {
            if (Player == null) return;
            
            var enemies = GameLogicService.Instance.GenerateEnemies(location, Player.Level);
            CurrentEnemies.Clear();
            CurrentEnemies.AddRange(enemies);
        }

        public void StartBattleWithHero(Location location)
        {
            if (Player == null || location?.Hero == null) return;
            
            CurrentEnemies.Clear();
            CurrentEnemies.Add(location.Hero);
        }

        public void CompleteBattle(bool isVictory)
        {
            if (isVictory && CurrentLocation != null)
            {
                var heroDefeated = CurrentEnemies.Any(e => e.IsHero && e.IsDefeated);
                if (heroDefeated)
                {
                    // Помечаем героя как побежденного в локации
                    CurrentLocation.HeroDefeated = true;
                    CurrentLocation.IsCompleted = true;
                    
                    // Используем встроенную логику разблокировки локации
                    CurrentLocation.CompleteLocation(this);
                    
                    LoggingService.LogInfo($"Hero defeated in {CurrentLocation.Name}, location completed");
                    
                    // Принудительно сохраняем игру после важного события
                    SaveGame();
                    LoggingService.LogInfo("Game saved after hero defeat and location unlock");
                }
                else
                {
                    // Обычная битва с мобами - тоже сохраняем
                    LoggingService.LogInfo($"Battle won in {CurrentLocation.Name}, mobs defeated");
                    SaveGame();
                    LoggingService.LogInfo("Game saved after battle victory");
                }
            }
            else
            {
                LoggingService.LogInfo($"Battle completed with victory: {isVictory}");
            }

            CurrentEnemies.Clear();
        }

        private void CopyFrom(GameData source)
        {
            Player = source.Player;
            Inventory = source.Inventory;
            CurrentLocation = source.CurrentLocation;
            CurrentLocationIndex = source.CurrentLocationIndex;
            Settings = source.Settings;
            Gold = source.Gold;
            HasSaveGame = source.HasSaveGame;

            Locations.Clear();
            foreach (var location in source.Locations)
            {
                Locations.Add(location);
            }
            
            // Уведомляем об изменении всех ключевых свойств после загрузки
            OnPropertyChanged(nameof(Player));
            OnPropertyChanged(nameof(Inventory));
            OnPropertyChanged(nameof(CurrentLocation));
            OnPropertyChanged(nameof(Locations));
            OnPropertyChanged(nameof(Gold));
            OnPropertyChanged(nameof(HasSaveGame));
        }

        protected virtual void SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (!Equals(field, value))
            {
                field = value;
                OnPropertyChanged(propertyName);
            }
        }

        public virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 