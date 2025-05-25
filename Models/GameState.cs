using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Linq;
using SketchBlade.Services;

namespace SketchBlade.Models
{
    /// <summary>
    /// Упрощенная версия GameState - координирует компоненты без лишней логики
    /// Было 1756 строк, стало ~150 строк
    /// </summary>
    public class GameState : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        // Основные компоненты
        private readonly GameData _gameData;
        private readonly BattleManager _battleManager;

        // Автосохранение
        private DateTime _lastAutoSave = DateTime.MinValue;
        private readonly TimeSpan _autoSaveInterval = TimeSpan.FromMinutes(2);

        public GameState()
        {
            _gameData = new GameData();
            _battleManager = new BattleManager(_gameData);

            // Подписываемся на изменения данных для автосохранения
            _gameData.PropertyChanged += OnGameDataChanged;

            // Проверяем наличие сохранения
            CheckForSaveGame();
        }

        // Делегирование к GameData
        public Character? Player => _gameData.Player;
        public Inventory Inventory => _gameData.Inventory;
        public System.Collections.ObjectModel.ObservableCollection<Location> Locations => _gameData.Locations;
        public Location? CurrentLocation => _gameData.CurrentLocation;
        public int CurrentLocationIndex => _gameData.CurrentLocationIndex;
        public string CurrentScreen => _gameData.CurrentScreen;
        public GameSettings Settings => _gameData.Settings;
        public bool HasSaveGame => _gameData.HasSaveGame;

        // Состояние боя
        public System.Collections.Generic.List<Character> CurrentEnemies => _gameData.CurrentEnemies;
        public System.Collections.Generic.List<Item> BattleRewardItems => _gameData.BattleRewardItems;
        public int BattleRewardGold => _gameData.BattleRewardGold;

        // Вычисляемые свойства
        public string PlayerHealth => _gameData.PlayerHealth;
        public string PlayerStrength => _gameData.PlayerStrength;
        public string PlayerDefense => _gameData.PlayerDefense;
        public string PlayerDamage => _gameData.PlayerDamage;

        // Менеджер боя для обратной совместимости
        public BattleManager BattleManager => _battleManager;

        // UI совместимость (убираем в будущем)
        [NonSerialized]
        private object? _currentScreenViewModel;
        public object? CurrentScreenViewModel 
        { 
            get => _currentScreenViewModel; 
            set => SetProperty(ref _currentScreenViewModel, value);
        }

        /// <summary>
        /// Инициализация новой игры
        /// </summary>
        public void InitializeNewGame()
        {
            var newGameData = GameLogicService.Instance.CreateNewGame();
            CopyDataFrom(newGameData);

            OnPropertyChanged(nameof(Player));
            OnPropertyChanged(nameof(Locations));
            OnPropertyChanged(nameof(CurrentLocation));
        }

        /// <summary>
        /// Сохранение игры
        /// </summary>
        public bool SaveGame()
        {
            if (CoreGameService.Instance.SaveGame(_gameData))
            {
                _gameData.HasSaveGame = true;
                _lastAutoSave = DateTime.Now;
                LoggingService.LogInfo("Game saved successfully");
                return true;
            }
            return false;
        }

        /// <summary>
        /// Загрузка игры
        /// </summary>
        public void LoadGame()
        {
            var loadedData = CoreGameService.Instance.LoadGame() as GameData;
            if (loadedData != null)
            {
                CopyDataFrom(loadedData);
                LoggingService.LogInfo("Game loaded successfully");
                
                // Уведомляем UI об изменениях
                NotifyAllPropertiesChanged();
            }
        }

        /// <summary>
        /// Проверка наличия сохранения
        /// </summary>
        public void CheckForSaveGame()
        {
            _gameData.HasSaveGame = CoreGameService.Instance.HasSaveFile();
        }

        /// <summary>
        /// Начало боя с мобами
        /// </summary>
        public void StartBattleWithMobs()
        {
            if (CurrentLocation == null) return;

            // Генерируем врагов для текущей локации
            var enemies = GameLogicService.Instance.GenerateEnemies(CurrentLocation, Player?.Level ?? 1);
            
            CurrentEnemies.Clear();
            foreach (var enemy in enemies)
            {
                CurrentEnemies.Add(enemy);
            }

            LoggingService.LogInfo($"Started battle with {enemies.Count} enemies in {CurrentLocation.Name}");
        }

        /// <summary>
        /// Начало боя с героем (боссом)
        /// </summary>
        public void StartBattleWithHero()
        {
            if (CurrentLocation?.Hero == null) return;

            CurrentEnemies.Clear();
            CurrentEnemies.Add(CurrentLocation.Hero);

            LoggingService.LogInfo($"Started boss battle with {CurrentLocation.Hero.Name} in {CurrentLocation.Name}");
        }

        /// <summary>
        /// Завершение боя
        /// </summary>
        public void CompleteBattle(bool isVictory)
        {
            if (isVictory && CurrentLocation != null)
            {
                // Разблокируем следующую локацию при победе над боссом
                var heroDefeated = CurrentEnemies.Any(e => e.IsHero && e.IsDefeated);
                if (heroDefeated)
                {
                    CurrentLocation.IsCompleted = true;
                    UnlockNextLocation();
                }
            }

            // Очищаем врагов
            CurrentEnemies.Clear();
            
            // Автосохранение после боя
            TryAutoSave();
        }

        /// <summary>
        /// Разблокировка следующей локации
        /// </summary>
        private void UnlockNextLocation()
        {
            int nextIndex = CurrentLocationIndex + 1;
            if (nextIndex < Locations.Count)
            {
                Locations[nextIndex].IsUnlocked = true;
                LoggingService.LogInfo($"Unlocked location: {Locations[nextIndex].Name}");
            }
        }

        /// <summary>
        /// Автосохранение (если прошло достаточно времени)
        /// </summary>
        private void TryAutoSave()
        {
            if (DateTime.Now - _lastAutoSave >= _autoSaveInterval)
            {
                SaveGame();
            }
        }

        /// <summary>
        /// Копирование данных из загруженного состояния
        /// </summary>
        private void CopyDataFrom(GameData source)
        {
            // Простое копирование - можно улучшить через AutoMapper
            _gameData.Player = source.Player;
            _gameData.Inventory = source.Inventory;
            _gameData.CurrentLocation = source.CurrentLocation;
            _gameData.CurrentLocationIndex = source.CurrentLocationIndex;
            _gameData.Settings = source.Settings;
            _gameData.Gold = source.Gold;
            _gameData.HasSaveGame = true;

            // Копируем локации
            _gameData.Locations.Clear();
            foreach (var location in source.Locations)
            {
                _gameData.Locations.Add(location);
            }
        }

        /// <summary>
        /// Уведомление всех свойств об изменении
        /// </summary>
        private void NotifyAllPropertiesChanged()
        {
            OnPropertyChanged(nameof(Player));
            OnPropertyChanged(nameof(Inventory));
            OnPropertyChanged(nameof(CurrentLocation));
            OnPropertyChanged(nameof(Locations));
            OnPropertyChanged(nameof(Settings));
            OnPropertyChanged(nameof(HasSaveGame));
        }

        /// <summary>
        /// Обработчик изменений в данных игры
        /// </summary>
        private void OnGameDataChanged(object? sender, PropertyChangedEventArgs e)
        {
            // Пробрасываем изменения
            OnPropertyChanged(e.PropertyName);
            
            // Планируем автосохранение
            TryAutoSave();
        }

        protected virtual void SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (!Equals(field, value))
            {
                field = value;
                OnPropertyChanged(propertyName);
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 