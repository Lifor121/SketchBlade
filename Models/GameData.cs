using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using SketchBlade.Services;

namespace SketchBlade.Models
{
    /// <summary>
    /// Чистая модель данных игры - только состояние без логики
    /// </summary>
    [Serializable]
    public class GameData : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        // Основные данные игры
        public Character? Player { get; set; }
        public Inventory Inventory { get; set; } = new Inventory();
        public ObservableCollection<Location> Locations { get; set; } = new ObservableCollection<Location>();
        public Location? CurrentLocation { get; set; }
        public int CurrentLocationIndex { get; set; } = 0;

        private string _currentScreen = "MainMenuView";
        public string CurrentScreen
        {
            get => _currentScreen;
            set => SetProperty(ref _currentScreen, value);
        }

        // Настройки игры
        public GameSettings Settings { get; set; } = new GameSettings();

        private bool _hasSaveGame = false;
        public bool HasSaveGame
        {
            get => _hasSaveGame;
            set => SetProperty(ref _hasSaveGame, value);
        }

        // Состояние боя
        public List<Character> CurrentEnemies { get; set; } = new List<Character>();
        public List<Item> BattleRewardItems { get; set; } = new List<Item>();
        public int BattleRewardGold { get; set; } = 0;

        // Устаревшее поле для совместимости
        public int Gold { get; set; } = 0;

        // Добавлены для совместимости с UI
        public object? CurrentScreenViewModel { get; set; }

        // Вычисляемые свойства для UI
        public string PlayerHealth => $"{Player?.CurrentHealth}/{Player?.GetTotalMaxHealth()}";
        public string PlayerStrength => Player?.Attack.ToString() ?? "0";
        public string PlayerDefense => Player?.GetTotalDefense().ToString() ?? "0";
        public string PlayerDamage => Player?.GetTotalAttack().ToString() ?? "0";

        public GameData()
        {
            // Базовая инициализация коллекций
            Locations = new ObservableCollection<Location>();
            CurrentEnemies = new List<Character>();
            // Используем только одну инициализацию инвентаря
            // Inventory = new Inventory(); - закомментировано, чтобы избежать дублирования инициализации
            Settings = new GameSettings();
        }

        /// <summary>
        /// Сброс данных игры к начальному состоянию
        /// </summary>
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
            BattleRewardItems.Clear();
            Locations.Clear();
            
            Inventory = new Inventory();
            Settings = new GameSettings();
        }

        /// <summary>
        /// Создает копию данных для сохранения
        /// </summary>
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
                // Копируем локации
                Locations = new ObservableCollection<Location>(Locations)
            };
        }

        /// <summary>
        /// Валидация данных игры
        /// </summary>
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

        /// <summary>
        /// Инициализация игры (делегирует к GameLogicService)
        /// </summary>
        public void Initialize()
        {
            LoggingService.LogInfo("=== ИНИЦИАЛИЗАЦИЯ НОВОЙ ИГРЫ (GameData.Initialize) ===");
            
            // Используем GameInitializer для более полной инициализации
            var gameInitializer = new Services.GameInitializer();
            gameInitializer.InitializeNewGame(this);
            
            LoggingService.LogInfo("=== ИГРА ИНИЦИАЛИЗИРОВАНА УСПЕШНО ===");
        }

        /// <summary>
        /// Сохранение игры (делегирует к CoreGameService)
        /// </summary>
        public void SaveGame()
        {
            CoreGameService.Instance.SaveGame(this);
        }

        /// <summary>
        /// Загрузка игры (делегирует к CoreGameService)
        /// </summary>
        public void LoadGame()
        {
            var loadedData = CoreGameService.Instance.LoadGame() as GameData;
            if (loadedData != null)
            {
                // Copy loaded data to this instance
                CopyFrom(loadedData);
            }
        }

        /// <summary>
        /// Проверка наличия сохранения (делегирует к CoreGameService)
        /// </summary>
        public bool CheckForSaveGame()
        {
            HasSaveGame = CoreGameService.Instance.HasSaveFile();
            return HasSaveGame;
        }

        /// <summary>
        /// Начало боя с мобами (делегирует к GameLogicService)
        /// </summary>
        public void StartBattleWithMobs(Location location)
        {
            if (Player == null) return;
            
            var enemies = GameLogicService.Instance.GenerateEnemies(location, Player.Level);
            CurrentEnemies.Clear();
            CurrentEnemies.AddRange(enemies);
        }

        /// <summary>
        /// Начало боя с героем (делегирует к GameLogicService)
        /// </summary>
        public void StartBattleWithHero(Location location)
        {
            if (location.Hero == null) return;
            
            CurrentEnemies.Clear();
            CurrentEnemies.Add(location.Hero);
        }

        /// <summary>
        /// Копирование данных из другого экземпляра
        /// </summary>
        private void CopyFrom(GameData source)
        {
            Player = source.Player;
            Inventory = source.Inventory;
            CurrentLocation = source.CurrentLocation;
            CurrentLocationIndex = source.CurrentLocationIndex;
            Settings = source.Settings;
            Gold = source.Gold;
            HasSaveGame = source.HasSaveGame;

            // Копируем локации
            Locations.Clear();
            foreach (var location in source.Locations)
            {
                Locations.Add(location);
            }
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