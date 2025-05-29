using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Linq;
using SketchBlade.Services;

namespace SketchBlade.Models
{
    public class GameState : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private readonly GameData _gameData;
        private readonly BattleManager _battleManager;

        private DateTime _lastAutoSave = DateTime.MinValue;
        private readonly TimeSpan _autoSaveInterval = TimeSpan.FromSeconds(30);

        public GameState()
        {
            _gameData = new GameData();
            _battleManager = new BattleManager(_gameData);

            _gameData.PropertyChanged += OnGameDataChanged;

            CheckForSaveGame();
        }

        public Character? Player => _gameData.Player;
        public Inventory Inventory => _gameData.Inventory;
        public System.Collections.ObjectModel.ObservableCollection<Location> Locations => _gameData.Locations;
        public Location? CurrentLocation => _gameData.CurrentLocation;
        public int CurrentLocationIndex => _gameData.CurrentLocationIndex;
        public string CurrentScreen => _gameData.CurrentScreen;
        public GameSettings Settings => _gameData.Settings;
        public bool HasSaveGame => _gameData.HasSaveGame;

        public System.Collections.Generic.List<Character> CurrentEnemies => _gameData.CurrentEnemies;
        public System.Collections.Generic.List<Item> BattleRewardItems => _gameData.BattleRewardItems;
        public int BattleRewardGold => _gameData.BattleRewardGold;

        public string PlayerHealth => _gameData.PlayerHealth;
        public string PlayerStrength => _gameData.PlayerStrength;
        public string PlayerDefense => _gameData.PlayerDefense;
        public string PlayerDamage => _gameData.PlayerDamage;

        public BattleManager BattleManager => _battleManager;

        [NonSerialized]
        private object? _currentScreenViewModel;
        public object? CurrentScreenViewModel 
        { 
            get => _currentScreenViewModel; 
            set => SetProperty(ref _currentScreenViewModel, value);
        }

        public void InitializeNewGame()
        {
            var newGameData = GameLogicService.Instance.CreateNewGame();
            CopyDataFrom(newGameData);

            OnPropertyChanged(nameof(Player));
            OnPropertyChanged(nameof(Locations));
            OnPropertyChanged(nameof(CurrentLocation));
        }

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

        public void LoadGame()
        {
            var loadedData = CoreGameService.Instance.LoadGame() as GameData;
            if (loadedData != null)
            {
                CopyDataFrom(loadedData);
                LoggingService.LogInfo("Game loaded successfully");
                
                NotifyAllPropertiesChanged();
            }
        }

        public void CheckForSaveGame()
        {
            _gameData.HasSaveGame = CoreGameService.Instance.HasSaveFile();
        }

        public void StartBattleWithMobs()
        {
            if (CurrentLocation == null) return;

            var enemies = GameLogicService.Instance.GenerateEnemies(CurrentLocation, Player?.Level ?? 1);
            
            CurrentEnemies.Clear();
            foreach (var enemy in enemies)
            {
                CurrentEnemies.Add(enemy);
            }

            LoggingService.LogInfo($"Started battle with {enemies.Count} enemies in {CurrentLocation.Name}");
        }

        public void StartBattleWithHero()
        {
            if (CurrentLocation?.Hero == null) return;

            CurrentEnemies.Clear();
            CurrentEnemies.Add(CurrentLocation.Hero);

            LoggingService.LogInfo($"Started boss battle with {CurrentLocation.Hero.Name} in {CurrentLocation.Name}");
        }

        public void CompleteBattle(bool isVictory)
        {
            if (isVictory && CurrentLocation != null)
            {
                var heroDefeated = CurrentEnemies.Any(e => e.IsHero && e.IsDefeated);
                if (heroDefeated)
                {
                    CurrentLocation.HeroDefeated = true;
                    CurrentLocation.IsCompleted = true;
                    
                    CurrentLocation.CompleteLocation(_gameData);
                    
                    LoggingService.LogInfo($"Hero defeated in {CurrentLocation.Name}, location completed");
                    
                    SaveGame();
                    LoggingService.LogInfo("Game saved after hero defeat and location unlock");
                }
                else
                {
                    LoggingService.LogInfo($"Battle won in {CurrentLocation.Name}, mobs defeated");
                }
            }
            else
            {
                LoggingService.LogInfo($"Battle completed with victory: {isVictory}");
            }

            CurrentEnemies.Clear();
            TryAutoSave();
        }

        private void UnlockNextLocation()
        {
            int nextIndex = CurrentLocationIndex + 1;
            if (nextIndex < Locations.Count)
            {
                Locations[nextIndex].IsUnlocked = true;
                LoggingService.LogInfo($"Unlocked location: {Locations[nextIndex].Name}");
            }
        }

        private void TryAutoSave()
        {
            if (DateTime.Now - _lastAutoSave >= _autoSaveInterval)
            {
                SaveGame();
            }
        }

        private void CopyDataFrom(GameData source)
        {
            _gameData.Player = source.Player;
            _gameData.Inventory = source.Inventory;
            _gameData.CurrentLocation = source.CurrentLocation;
            _gameData.CurrentLocationIndex = source.CurrentLocationIndex;
            _gameData.Settings = source.Settings;
            _gameData.Gold = source.Gold;
            _gameData.HasSaveGame = true;

            _gameData.Locations.Clear();
            foreach (var location in source.Locations)
            {
                _gameData.Locations.Add(location);
            }
        }

        private void NotifyAllPropertiesChanged()
        {
            OnPropertyChanged(nameof(Player));
            OnPropertyChanged(nameof(Inventory));
            OnPropertyChanged(nameof(CurrentLocation));
            OnPropertyChanged(nameof(Locations));
            OnPropertyChanged(nameof(Settings));
            OnPropertyChanged(nameof(HasSaveGame));
        }

        private void OnGameDataChanged(object? sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged(e.PropertyName);
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