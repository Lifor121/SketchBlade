using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using SketchBlade.Models;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using System.Text.Json;
using System.IO;
using SketchBlade.ViewModels;
using SketchBlade.Views;
using SketchBlade.Services;
using SketchBlade.Helpers;
using SketchBlade.Utilities;

namespace SketchBlade.ViewModels
{
    // Define navigation direction enum
    public enum NavigationDirection
    {
        Previous,
        Next
    }
    
    // Define event args for location changes
    public class LocationChangedEventArgs : EventArgs
    {
        public Location NewLocation { get; }
        public NavigationDirection Direction { get; }
        
        public LocationChangedEventArgs(Location newLocation, NavigationDirection direction)
        {
            NewLocation = newLocation;
            Direction = direction;
        }
    }
    
    /// <summary>
    /// Упрощенный сервис логирования - только критические ошибки
    /// </summary>
    public class MapViewModel : INotifyPropertyChanged
    {
        private readonly GameData _gameState;
        private readonly Action<string> _navigateAction;
        private bool _isRefreshing = false; // Флаг для предотвращения циклических вызовов
        private static int _refreshCallCount = 0; // Счетчик для отслеживания глубины вызовов
        private const int MAX_REFRESH_DEPTH = 3; // Максимальная глубина рекурсивных вызовов
        
        // Публичное свойство для доступа к GameData
        public GameData GameData => _gameState;
        
        public event PropertyChangedEventHandler? PropertyChanged;
        
        // Event for location changes with direction
        public event EventHandler<LocationChangedEventArgs> LocationChanged;
        
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
        protected virtual void OnLocationChanged(Location location, NavigationDirection direction)
        {
            LocationChanged?.Invoke(this, new LocationChangedEventArgs(location, direction));
        }
        
        public Location CurrentLocation 
        { 
            get 
            {
                // Если CurrentLocation null, пытаемся установить его на основе CurrentLocationIndex
                if (_gameState.CurrentLocation == null && _gameState.Locations != null && _gameState.Locations.Count > 0)
                {
                    // Убеждаемся что индекс корректный
                    if (_gameState.CurrentLocationIndex < 0 || _gameState.CurrentLocationIndex >= _gameState.Locations.Count)
                    {
                        _gameState.CurrentLocationIndex = 0;
                    }
                    
                    _gameState.CurrentLocation = _gameState.Locations[_gameState.CurrentLocationIndex];
                    LoggingService.LogDebug($"Auto-initialized CurrentLocation to: {_gameState.CurrentLocation.Name}");
                }
                
                return _gameState.CurrentLocation ?? 
                    (_gameState.Locations != null && _gameState.Locations.Count > 0 && _gameState.CurrentLocationIndex >= 0 && _gameState.CurrentLocationIndex < _gameState.Locations.Count 
                        ? _gameState.Locations[_gameState.CurrentLocationIndex] 
                        : _gameState.Locations?[0]);
            }
        }
        
        private bool _canNavigatePrevious;
        private bool _canNavigateNext;
        
        public bool CanNavigatePrevious 
        { 
            get => _canNavigatePrevious;
            private set
            {
                if (_canNavigatePrevious != value)
                {
                    _canNavigatePrevious = value;
                    OnPropertyChanged(nameof(CanNavigatePrevious));
                }
            }
        }
        
        public bool CanNavigateNext 
        { 
            get => _canNavigateNext;
            private set
            {
                if (_canNavigateNext != value)
                {
                    _canNavigateNext = value;
                    OnPropertyChanged(nameof(CanNavigateNext));
                }
            }
        }
        
        public bool CanTravelToLocation
        {
            get
            {
                if (CurrentLocation == null) return false;
                
                // Рможем путешествовать, если локация разблокирована и доступна
                bool canTravel = CurrentLocation.IsUnlocked && CurrentLocation.CheckAvailability(_gameState);
                
                return canTravel;
            }
        }
        
        public bool CanFightHero
        {
            get
            {
                if (CurrentLocation == null) return false;
                
                // ИСПРАВЛЕННАЯ ЛОГИКА: Можем сражаться с героем только если:
                // 1. У локации есть герой
                // 2. Локация разблокирована
                // 3. Локация доступна для посещения
                // 4. Герой еще НЕ побежден (согласно README - только один раз)
                return CurrentLocation.Hero != null && 
                       CurrentLocation.IsUnlocked &&
                       CurrentLocation.CheckAvailability(_gameState) &&
                       !CurrentLocation.HeroDefeated;
            }
        }
        
        public bool CanFightMobs
        {
            get
            {
                if (CurrentLocation == null) return false;
                
                // ИСПРАВЛЕННАЯ ЛОГИКА: Можем сражаться с мобами только если:
                // 1. Локация разблокирована
                // 2. Локация доступна для посещения
                return CurrentLocation.IsUnlocked && CurrentLocation.CheckAvailability(_gameState);
            }
        }
        
        // Рпроверка, заблокирована ли локация
        public bool IsLocationLocked => CurrentLocation != null && 
                                     (!CurrentLocation.IsUnlocked || !CurrentLocation.CheckAvailability(_gameState));
        
        // Рдетали локации для отображения
        public string LocationDetailsText
        {
            get
            {
                if (CurrentLocation == null)
                    return "Локация не выбрана";

                var details = new System.Text.StringBuilder();
                details.AppendLine($"Название: {CurrentLocation.Name}");
                details.AppendLine($"Тип: {GetLocationTypeText(CurrentLocation.Type)}");
                details.AppendLine($"Сложность: {GetDifficultyText(CurrentLocation.Difficulty)}");
                details.AppendLine($"Описание: {CurrentLocation.Description}");

                if (CurrentLocation.IsUnlocked)
                {
                    details.AppendLine("Статус: разблокирована");
                    
                    if (CurrentLocation.Hero != null)
                    {
                        if (CurrentLocation.HeroDefeated)
                        {
                            details.AppendLine($"Босс: {CurrentLocation.Hero.Name} (Побежден)");
                        }
                        else
                        {
                            details.AppendLine($"Босс: {CurrentLocation.Hero.Name} (Доступен для битвы)");
                        }
                    }
                    
                    if (CurrentLocation.IsCompleted)
                    {
                        details.AppendLine("Локация пройдена");
                    }
                }
                else
                {
                    details.AppendLine("Статус: заблокирована");
                    
                    // Показываем требования для разблокирования
                    if (CurrentLocation.MinPlayerLevel > 1)
                    {
                        details.AppendLine($"Требуемый уровень: {CurrentLocation.MinPlayerLevel}");
                    }
                    
                    if (CurrentLocation.RequiredCompletedLocations.Count > 0)
                    {
                        string requiredLocations = string.Join(", ", CurrentLocation.RequiredCompletedLocations);
                        details.AppendLine($"Требуется пройти: {requiredLocations}");
                    }
                }

                return details.ToString();
            }
        }
        
        private string GetLocationTypeText(LocationType type)
        {
            return type switch
            {
                LocationType.Village => "Деревня",
                LocationType.Forest => "Лес",
                LocationType.Cave => "Пещеры",
                LocationType.Ruins => "Руины",
                LocationType.Castle => "Замок",
                _ => "Неизвестно"
            };
        }
        
        private string GetDifficultyText(LocationDifficultyLevel difficulty)
        {
            return difficulty switch
            {
                LocationDifficultyLevel.Easy => "Легкая",
                LocationDifficultyLevel.Medium => "Средняя",
                LocationDifficultyLevel.Hard => "Сложная",
                LocationDifficultyLevel.VeryHard => "Очень сложная",
                _ => "Неизвестно"
            };
        }
        
        // Commands
        public ICommand PreviousLocationCommand { get; private set; }
        public ICommand NextLocationCommand { get; private set; }
        public ICommand TravelToLocationCommand { get; private set; }
        public ICommand FightHeroCommand { get; private set; }
        public ICommand FightMobsCommand { get; private set; }
        public ICommand NavigateCommand { get; private set; }
        
        // Constructor
        public MapViewModel(GameData gameData, Action<string> navigateAction)
        {
            _gameState = gameData ?? throw new ArgumentNullException(nameof(gameData));
            _navigateAction = navigateAction ?? throw new ArgumentNullException(nameof(navigateAction));
            
            // Инициализируем CurrentLocation если она null
            if (_gameState.CurrentLocation == null && _gameState.Locations != null && _gameState.Locations.Count > 0)
            {
                _gameState.CurrentLocationIndex = Math.Max(0, Math.Min(_gameState.CurrentLocationIndex, _gameState.Locations.Count - 1));
                _gameState.CurrentLocation = _gameState.Locations[_gameState.CurrentLocationIndex];
            }
            
            // Initialize commands
            InitializeCommands();
            
            DiagnoseLocationOrder();
            
            // Инициализируем свойства навигации
            UpdateNavigationProperties();
            
            // Инициализируем индикаторы локаций
            UpdateLocationIndicators();
            
            // Принудительно обновляем команды после инициализации
            RefreshCommands();
        }
        
        private void InitializeCommands()
        {
            PreviousLocationCommand = new RelayCommand<object>(
                _ => NavigateToPreviousLocation(), 
                _ => CanNavigatePrevious, 
                "PreviousLocation");
                
            NextLocationCommand = new RelayCommand<object>(
                _ => NavigateToNextLocation(), 
                _ => CanNavigateNext, 
                "NextLocation");
                
            TravelToLocationCommand = new RelayCommand<object>(_ => TravelToLocation(), _ => CanTravelToLocation, "TravelToLocation");
            FightHeroCommand = new RelayCommand<object>(_ => FightHero(), _ => CanFightHero, "FightHero");
            FightMobsCommand = new RelayCommand<object>(_ => FightMobs(), _ => CanFightMobs, "FightMobs");
        }
        
        // Метод для принудительного обновления всех команд
        private void RefreshCommands()
        {
            LoggingService.LogDebug("RefreshCommands: Принудительно обновляем все команды");
            
            // Обновляем свойства навигации
            UpdateNavigationProperties();
            
            // Принудительно обновляем команды через CommandManager
            CommandManager.InvalidateRequerySuggested();
            
            // Также обновляем все связанные свойства
            OnPropertyChanged(nameof(CanNavigatePrevious));
            OnPropertyChanged(nameof(CanNavigateNext));
            OnPropertyChanged(nameof(CanTravelToLocation));
            OnPropertyChanged(nameof(CanFightHero));
            OnPropertyChanged(nameof(CanFightMobs));
            
            LoggingService.LogDebug($"RefreshCommands: CanNavigatePrevious={CanNavigatePrevious}, CanNavigateNext={CanNavigateNext}");
            LoggingService.LogDebug($"RefreshCommands: CanFightMobs={CanFightMobs}, CanFightHero={CanFightHero}");
        }
        
        // Метод для обновления свойств навигации
        private void UpdateNavigationProperties()
        {
            CanNavigatePrevious = _gameState.CurrentLocationIndex > 0;
            CanNavigateNext = _gameState.Locations != null && 
                             _gameState.CurrentLocationIndex < _gameState.Locations.Count - 1;
            
            LoggingService.LogDebug($"UpdateNavigationProperties: Index={_gameState.CurrentLocationIndex}, CanPrev={CanNavigatePrevious}, CanNext={CanNavigateNext}");
        }
        
        // Method to refresh the view when it becomes visible
        public void RefreshView()
        {
            // Предотвращаем циклические вызовы
            if (_isRefreshing)
            {
                LoggingService.LogDebug("RefreshView: Уже в состоянии обновления, пропускаем");
                return;
            }
            
            // Дополнительная защита от слишком глубокой рекурсии
            if (_refreshCallCount >= MAX_REFRESH_DEPTH)
            {
                LoggingService.LogDebug($"RefreshView: Достигнута максимальная глубина рекурсии ({_refreshCallCount}), пропускаем");
                return;
            }
            
            _isRefreshing = true;
            _refreshCallCount++;
            
            try
            {
                LoggingService.LogDebug($"RefreshView: Начало обновления отображения локаций (вызов #{_refreshCallCount})");
                
                // Убеждаемся, что CurrentLocation установлен
                if (_gameState.CurrentLocation == null && _gameState.Locations != null && _gameState.Locations.Count > 0)
                {
                    _gameState.CurrentLocation = _gameState.Locations[_gameState.CurrentLocationIndex];
                    LoggingService.LogDebug($"RefreshView: Установлен CurrentLocation = {_gameState.CurrentLocation.Name}");
                }
                
                LoggingService.LogDebug($"RefreshView: Обновляем статусы для {_gameState.Locations?.Count ?? 0} локаций");
                
                // Принудительно проверяем все зависимости и статусы локаций
                if (_gameState.Locations != null)
                {
                    foreach (var location in _gameState.Locations)
                    {
                        LoggingService.LogDebug($"RefreshView: Checking location {location.Name}: Unlocked={location.IsUnlocked}, Completed={location.IsCompleted}, HeroDefeated={location.HeroDefeated}");
                        
                        // Update all the relevant properties for this location
                        if (location.IsUnlocked)
                        {
                            // Location is available if it's unlocked and either not completed or allows repeated completion
                            location.IsAvailable = !location.IsCompleted || location.CompletionCount < 10;
                        }
                        
                        LoggingService.LogDebug($"RefreshView: Локация {location.Name}: Разблокирована={location.IsUnlocked}, Завершена={location.IsCompleted}, Доступна={location.IsAvailable}");
                    }
                }
                
                // Update the location indicators
                UpdateLocationIndicators();
                UpdateLocationDetails();
                
                // Ensure UI state is fully updated
                Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                    // Force refresh all relevant properties
                    OnPropertyChanged(nameof(CanNavigatePrevious));
                    OnPropertyChanged(nameof(CanNavigateNext));
                    OnPropertyChanged(nameof(CanTravelToLocation));
                    OnPropertyChanged(nameof(CanFightHero));
                    OnPropertyChanged(nameof(CanFightMobs));
                    OnPropertyChanged(nameof(CurrentLocation));
                    OnPropertyChanged(nameof(LocationDetailsText));
                    OnPropertyChanged(nameof(IsLocationLocked));
                    
                    // Update location indicators without clearing them first
                    // This prevents the visual disappearance of circles and arrows
                    UpdateLocationIndicators();
                }));
                
                LoggingService.LogDebug("RefreshView: Завершено обновление отображения локаций");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"ERROR in RefreshView: {ex.Message}", ex);
            }
            finally
            {
                _refreshCallCount--;
                _isRefreshing = false;
            }
        }
        
        // Location indicators for UI
        public ObservableCollection<LocationIndicator> LocationIndicators { get; } = new ObservableCollection<LocationIndicator>();
        
        private void NavigateToPreviousLocation()
        {
            LoggingService.LogDebug("=== NavigateToPreviousLocation: НАЧАЛО ===");
            LoggingService.LogDebug($"CanNavigatePrevious: {CanNavigatePrevious}");
            
            if (!CanNavigatePrevious)
            {
                LoggingService.LogDebug("NavigateToPreviousLocation: Навигация назад недоступна");
                return;
            }
                
            try
            {
                LoggingService.LogDebug($"Текущий индекс до изменения: {_gameState.CurrentLocationIndex}");
                _gameState.CurrentLocationIndex--;
                LoggingService.LogDebug($"Новый индекс: {_gameState.CurrentLocationIndex}");
                
                if (_gameState.Locations != null && _gameState.CurrentLocationIndex >= 0 && 
                    _gameState.CurrentLocationIndex < _gameState.Locations.Count)
                {
                    _gameState.CurrentLocation = _gameState.Locations[_gameState.CurrentLocationIndex];
                    LoggingService.LogDebug($"Установлена новая локация: {_gameState.CurrentLocation.Name}");
                    
                    // Обновляем только статус выбора в индикаторах
                    LoggingService.LogDebug($"Обновляем статус выбора для {LocationIndicators.Count} индикаторов");
                    foreach (var indicator in LocationIndicators)
                    {
                        bool wasSelected = indicator.IsSelected;
                        indicator.IsSelected = (indicator.Index == _gameState.CurrentLocationIndex);
                        if (wasSelected != indicator.IsSelected)
                        {
                            LoggingService.LogDebug($"Индикатор {indicator.Index}: {wasSelected} -> {indicator.IsSelected}");
                        }
                    }
                    
                    LoggingService.LogDebug("Вызываем OnLocationChanged");
                    OnLocationChanged(_gameState.CurrentLocation, NavigationDirection.Previous);
                }
                else
                {
                    LoggingService.LogDebug("Индекс вышел за границы, сбрасываем на 0");
                    _gameState.CurrentLocationIndex = 0;
                }

                // Обновляем UI свойства
                LoggingService.LogDebug("Планируем обновление UI свойств через Dispatcher");
                Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                    LoggingService.LogDebug("Выполняем обновление UI свойств");
                    OnPropertyChanged(nameof(CurrentLocation));
                    OnPropertyChanged(nameof(CanNavigatePrevious));
                    OnPropertyChanged(nameof(CanNavigateNext));
                    OnPropertyChanged(nameof(CanTravelToLocation));
                    OnPropertyChanged(nameof(CanFightHero));
                    OnPropertyChanged(nameof(CanFightMobs));
                    OnPropertyChanged(nameof(IsLocationLocked));
                    OnPropertyChanged(nameof(LocationDetailsText));
                    
                    RefreshCommands();
                    LoggingService.LogDebug("UI свойства обновлены");
                }));
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Ошибка в NavigateToPreviousLocation: {ex.Message}", ex);
            }
            
            LoggingService.LogDebug("=== NavigateToPreviousLocation: КОНЕЦ ===");
        }
        
        private void NavigateToNextLocation()
        {
            LoggingService.LogDebug("=== NavigateToNextLocation: НАЧАЛО ===");
            LoggingService.LogDebug($"CanNavigateNext: {CanNavigateNext}");
            
            if (!CanNavigateNext)
            {
                LoggingService.LogDebug("NavigateToNextLocation: Навигация вперед недоступна");
                return;
            }
            
            try
            {
                LoggingService.LogDebug($"Текущий индекс до изменения: {_gameState.CurrentLocationIndex}");
                _gameState.CurrentLocationIndex++;
                LoggingService.LogDebug($"Новый индекс: {_gameState.CurrentLocationIndex}");
                
                if (_gameState.Locations != null && _gameState.CurrentLocationIndex >= 0 && 
                    _gameState.CurrentLocationIndex < _gameState.Locations.Count)
                {
                    _gameState.CurrentLocation = _gameState.Locations[_gameState.CurrentLocationIndex];
                    LoggingService.LogDebug($"Установлена новая локация: {_gameState.CurrentLocation.Name}");
                    
                    // Обновляем только статус выбора в индикаторах
                    LoggingService.LogDebug($"Обновляем статус выбора для {LocationIndicators.Count} индикаторов");
                    foreach (var indicator in LocationIndicators)
                    {
                        bool wasSelected = indicator.IsSelected;
                        indicator.IsSelected = (indicator.Index == _gameState.CurrentLocationIndex);
                        if (wasSelected != indicator.IsSelected)
                        {
                            LoggingService.LogDebug($"Индикатор {indicator.Index}: {wasSelected} -> {indicator.IsSelected}");
                        }
                    }
                    
                    LoggingService.LogDebug("Вызываем OnLocationChanged");
                    OnLocationChanged(_gameState.CurrentLocation, NavigationDirection.Next);
                }
                else
                {
                    LoggingService.LogDebug("Индекс вышел за границы, сбрасываем на максимальный");
                    _gameState.CurrentLocationIndex = Math.Max(0, _gameState.Locations?.Count - 1 ?? 0);
                }

                // Обновляем UI свойства
                LoggingService.LogDebug("Планируем обновление UI свойств через Dispatcher");
                Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                    LoggingService.LogDebug("Выполняем обновление UI свойств");
                    OnPropertyChanged(nameof(CurrentLocation));
                    OnPropertyChanged(nameof(CanNavigatePrevious));
                    OnPropertyChanged(nameof(CanNavigateNext));
                    OnPropertyChanged(nameof(CanTravelToLocation));
                    OnPropertyChanged(nameof(CanFightHero));
                    OnPropertyChanged(nameof(CanFightMobs));
                    OnPropertyChanged(nameof(IsLocationLocked));
                    OnPropertyChanged(nameof(LocationDetailsText));
                    
                    RefreshCommands();
                    LoggingService.LogDebug("UI свойства обновлены");
                }));
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Ошибка в NavigateToNextLocation: {ex.Message}", ex);
            }
            
            LoggingService.LogDebug("=== NavigateToNextLocation: КОНЕЦ ===");
        }
        
        private void TravelToLocation()
        {
            LoggingService.LogDebug("TravelToLocation method called");
            LoggingService.LogDebug($"CanTravelToLocation: {CanTravelToLocation}, CurrentLocation is null: {CurrentLocation == null}");
            
            if (!CanTravelToLocation || CurrentLocation == null)
            {
                LoggingService.LogDebug("Cannot travel to location - preconditions not met");
                return;
            }
            
            try
            {
                // Clear any previous image-related data that might cause serialization issues
                try
                {
                    // Create a clean working copy of the current location to prevent image errors
                    var locationCopy = CurrentLocation;
                    
                    // Defensive checks for current location
                    if (string.IsNullOrEmpty(locationCopy.SpritePath))
                    {
                        LoggingService.LogDebug("Warning: Location sprite path is empty");
                        // Set default path based on location type
                        locationCopy.SpritePath = AssetPaths.Locations.GetLocationPath(locationCopy.Type.ToString().ToLower());
                        LoggingService.LogDebug($"Set default sprite path: {locationCopy.SpritePath}");
                    }
                    else
                    {
                        LoggingService.LogDebug($"Location sprite path: {locationCopy.SpritePath}");
                    }
                    
                    // Since we're using file paths instead of BitmapImage objects, we shouldn't 
                    // hit NotSupportedException during serialization
                    
                    LoggingService.LogDebug($"Traveling to location: {locationCopy.Name}");
                    
                    // Check if this is the first time visiting this location
                    bool firstVisit = !locationCopy.IsCompleted;
                    LoggingService.LogDebug($"First visit: {firstVisit}");
                    
                    // Random encounter check (30% chance if not completed yet)
                    bool triggerEncounter = false;
                    if (!locationCopy.IsCompleted)
                    {
                        Random random = new Random();
                        triggerEncounter = random.Next(100) < 30;
                    }
                    LoggingService.LogDebug($"Trigger encounter: {triggerEncounter}");
                    
                    if (triggerEncounter)
                    {
                        LoggingService.LogDebug("Random encounter triggered!");
                        
                        try
                        {
                            // Start a battle with random enemies from this location
                            _gameState.StartBattleWithMobs(CurrentLocation);
                            
                            // First set the current screen in GameData
                            _gameState.CurrentScreen = "BattleView";
                            LoggingService.LogDebug($"GameData.CurrentScreen set to {_gameState.CurrentScreen}");
                            
                            // Navigate to battle using dispatcher to avoid thread issues
                            Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                                try
                                {
                                    LoggingService.LogDebug("Navigating to battle screen");
                                    _navigateAction("BattleScreen");
                                    LoggingService.LogDebug("Navigation to battle screen completed");
                                }
                                catch (Exception ex)
                                {
                                    LoggingService.LogError("Error in navigation dispatch: {ex.Message}", ex);
                                }
                            }));
                        }
                        catch (Exception ex)
                        {
                            LoggingService.LogError("Error starting battle: {ex.Message}", ex);
                            // Continue with location travel despite battle error
                        }
                    }
                    else
                    {
                        // Just update the location status
                        if (!locationCopy.IsCompleted && firstVisit)
                        {
                            locationCopy.IsCompleted = true;
                            locationCopy.CompletionCount++;
                            LoggingService.LogDebug("First visit to location completed");
                            
                            // Make sure the actual CurrentLocation gets updated too
                            if (_gameState.CurrentLocation != null)
                            {
                                _gameState.CurrentLocation.IsCompleted = true;
                                _gameState.CurrentLocation.CompletionCount = locationCopy.CompletionCount;
                            }
                        }
                        
                        // Save game state in a separate thread to avoid UI freezing
                        System.Threading.Tasks.Task.Run(() => {
                            try
                            {
                                // Create minimal object for emergency save
                                var simpleData = new Dictionary<string, object>
                                {
                                    ["CurrentLocationName"] = locationCopy.Name,
                                    ["CurrentLocationIndex"] = _gameState.CurrentLocationIndex,
                                    ["IsLocationCompleted"] = locationCopy.IsCompleted
                                };
                                
                                // Save emergency data first
                                var emergencyPath = "emergency_save.json";
                                string jsonData = JsonSerializer.Serialize(simpleData);
                                File.WriteAllText(emergencyPath, jsonData);
                                
                                // Safely attempt full save
                                try
                                {
                                    // Make sure we're not trying to serialize BitmapImage objects
                                    // We only need to save the sprite paths
                                    _gameState.SaveGame();
                                    
                                    // Delete emergency save if full save succeeded
                                    if (File.Exists(emergencyPath))
                                    {
                                        File.Delete(emergencyPath);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    LoggingService.LogError("Error in full save: {ex.Message}", ex);
                                    // Emergency save is still there, so data isn't lost
                                }
                            }
                            catch (Exception ex)
                            {
                                LoggingService.LogError("Error in emergency save: {ex.Message}", ex);
                            }
                        });
                        
                        // Update UI on the UI thread
                        Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                            try
                            {
                                // Update location indicators to refresh UI
                                UpdateLocationIndicators();
                                
                                // Show success message
                                MessageBox.Show($"You have traveled to {locationCopy.Name}.", 
                                                "Location Travel", 
                                                MessageBoxButton.OK, 
                                                MessageBoxImage.Information);
                            }
                            catch (Exception ex)
                            {
                                LoggingService.LogError("Error updating UI after travel: {ex.Message}", ex);
                            }
                        }));
                    }
                }
                catch (NotSupportedException ex)
                {
                    LoggingService.LogError($"NotSupportedException in location handling: {ex.Message}", ex);
                    
                    // Continue without using the problematic image references
                    if (CurrentLocation != null)
                    {
                        CurrentLocation.IsCompleted = true;
                        CurrentLocation.CompletionCount++;
                        
                        // Mark location as completed
                        LoggingService.LogDebug("Marked location as completed despite image error");
                        
                        // Update UI safely
                        Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                            UpdateLocationIndicators();
                            MessageBox.Show($"You have traveled to {CurrentLocation.Name}.", 
                                           "Location Travel", 
                                           MessageBoxButton.OK, 
                                           MessageBoxImage.Information);
                        }));
                    }
                }
            }
            catch (System.NotSupportedException ex)
            {
                LoggingService.LogError($"NotSupportedException in TravelToLocation: {ex.Message}", ex);
                
                // Try to continue despite the error
                if (CurrentLocation != null)
                {
                    CurrentLocation.IsCompleted = true;
                    CurrentLocation.CompletionCount++;
                }
                
                // Update UI safely
                Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                    UpdateLocationIndicators();
                    MessageBox.Show($"You have traveled to {CurrentLocation?.Name ?? "the location"}.\n\nThere was a minor issue with images, but your progress has been saved.", 
                                    "Location Travel", 
                                    MessageBoxButton.OK, 
                                    MessageBoxImage.Information);
                }));
            }
            catch (System.InvalidOperationException ex)
            {
                LoggingService.LogError($"InvalidOperationException in TravelToLocation: {ex.Message}", ex);
                MessageBox.Show($"Ошибка при перемещении в локацию: {ex.Message}", "Ошибка перемещения", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error traveling to location: {ex.Message}", ex);
                MessageBox.Show($"Ошибка при перемещении в локацию: {ex.Message}", "Ошибка перемещения", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void FightHero()
        {
            LoggingService.LogDebug("FightHero method called");
            LoggingService.LogDebug($"CanFightHero: {CanFightHero}, CurrentLocation is null: {CurrentLocation == null}, CurrentLocation.Hero is null: {CurrentLocation?.Hero == null}");
            
            if (!CanFightHero || CurrentLocation?.Hero == null)
            {
                LoggingService.LogDebug("Cannot fight hero - preconditions not met");
                return;
            }
            
            try
            {
                LoggingService.LogDebug($"Starting boss fight with {CurrentLocation.Hero.Name} in {CurrentLocation.Name}");
                
                // ВАЖНО: Принудительно очищаем CurrentEnemies перед каждым боем
                _gameState.CurrentEnemies.Clear();
                LoggingService.LogDebug("Cleared CurrentEnemies before battle");
                
                // Создаем КОПИЮ героя для боя, чтобы не изменять оригинального героя
                var heroCopy = new Character
                {
                    Name = CurrentLocation.Hero.Name,
                    MaxHealth = CurrentLocation.Hero.MaxHealth,
                    CurrentHealth = CurrentLocation.Hero.MaxHealth, // Важно: полное здоровье
                    Attack = CurrentLocation.Hero.Attack,
                    Defense = CurrentLocation.Hero.Defense,
                    Level = CurrentLocation.Hero.Level,
                    IsHero = true,
                    LocationType = CurrentLocation.Hero.LocationType,
                    ImagePath = CurrentLocation.Hero.ImagePath,
                    IsPlayer = false
                };
                
                // Принудительно сбрасываем статус поражения
                heroCopy.SetDefeated(false);
                
                LoggingService.LogDebug($"Created hero copy: {heroCopy.Name}, Health: {heroCopy.CurrentHealth}/{heroCopy.MaxHealth}, IsDefeated: {heroCopy.IsDefeated}");
                
                // Добавляем копию героя в CurrentEnemies
                _gameState.CurrentEnemies.Add(heroCopy);
                LoggingService.LogDebug($"Added hero to CurrentEnemies. Total enemies: {_gameState.CurrentEnemies.Count}");
                
                // ВАЖНО: Устанавливаем CurrentScreen в BattleView
                _gameState.CurrentScreen = "BattleView";
                LoggingService.LogDebug($"GameData.CurrentScreen set to: {_gameState.CurrentScreen}");
                
                // Navigate directly to the BattleView
                LoggingService.LogDebug("Navigating to BattleView using _navigateAction");
                _navigateAction("BattleView");
                LoggingService.LogDebug("Navigation completed successfully");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error starting boss battle: {ex.Message}", ex);
                MessageBox.Show($"Error starting boss battle: {ex.Message}", "Battle Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void FightMobs()
        {
            LoggingService.LogDebug("FightMobs method called");
            LoggingService.LogDebug($"CanFightMobs: {CanFightMobs}, CurrentLocation is null: {CurrentLocation == null}");
            
            if (!CanFightMobs || CurrentLocation == null)
            {
                LoggingService.LogDebug("Cannot fight mobs - preconditions not met");
                return;
            }
            
            try
            {
                LoggingService.LogDebug($"Starting mob battle in {CurrentLocation.Name}");
                
                // ВАЖНО: Принудительно очищаем CurrentEnemies перед каждым боем
                _gameState.CurrentEnemies.Clear();
                LoggingService.LogDebug("Cleared CurrentEnemies before mob battle");
                
                // Генерируем новых врагов для текущей локации
                var enemies = GameLogicService.Instance.GenerateEnemies(CurrentLocation, _gameState.Player?.Level ?? 1);
                LoggingService.LogDebug($"Generated {enemies.Count} enemies for {CurrentLocation.Name}");
                
                // Добавляем врагов в CurrentEnemies и убеждаемся, что они не побеждены
                foreach (var enemy in enemies)
                {
                    enemy.SetDefeated(false); // Принудительно сбрасываем статус поражения
                    enemy.CurrentHealth = enemy.MaxHealth; // Восстанавливаем полное здоровье
                    _gameState.CurrentEnemies.Add(enemy);
                    LoggingService.LogDebug($"Added enemy: {enemy.Name}, Health: {enemy.CurrentHealth}/{enemy.MaxHealth}, IsDefeated: {enemy.IsDefeated}");
                }
                
                LoggingService.LogDebug($"Total enemies in CurrentEnemies: {_gameState.CurrentEnemies.Count}");
                
                // ВАЖНО: Устанавливаем CurrentScreen в BattleView
                _gameState.CurrentScreen = "BattleView";
                LoggingService.LogDebug($"GameData.CurrentScreen set to: {_gameState.CurrentScreen}");
                
                // Navigate directly to the BattleView
                LoggingService.LogDebug("Navigating to BattleView using _navigateAction");
                _navigateAction("BattleView");
                LoggingService.LogDebug("Navigation completed successfully");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error starting mob battle: {ex.Message}", ex);
                MessageBox.Show($"Error starting mob battle: {ex.Message}", "Battle Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void UpdateLocationIndicators()
        {
            LoggingService.LogDebug("=== UpdateLocationIndicators: НАЧАЛО ===");
            
            if (_gameState.Locations == null || _gameState.Locations.Count == 0)
            {
                LoggingService.LogDebug("UpdateLocationIndicators: Локации отсутствуют, очищаем индикаторы");
                LocationIndicators.Clear();
                return;
            }
            
            LoggingService.LogDebug($"UpdateLocationIndicators: Локаций: {_gameState.Locations.Count}, Индикаторов: {LocationIndicators.Count}");
            LoggingService.LogDebug($"UpdateLocationIndicators: Текущий индекс локации: {_gameState.CurrentLocationIndex}");
            
            // Если количество индикаторов не совпадает с количеством локаций, пересоздаем их
            if (LocationIndicators.Count != _gameState.Locations.Count)
            {
                LoggingService.LogDebug("UpdateLocationIndicators: Количество индикаторов не совпадает, пересоздаем");
                LocationIndicators.Clear();
                
                for (int i = 0; i < _gameState.Locations.Count; i++)
                {
                    var location = _gameState.Locations[i];
                    var indicator = new LocationIndicator
                    {
                        Index = i,
                        IsSelected = (i == _gameState.CurrentLocationIndex),
                        IsCompleted = location.IsCompleted,
                        IsUnlocked = location.IsUnlocked,
                        IsAvailable = location.CheckAvailability(_gameState)
                    };
                    
                    LocationIndicators.Add(indicator);
                    LoggingService.LogDebug($"UpdateLocationIndicators: Создан индикатор {i}: Selected={indicator.IsSelected}, Completed={indicator.IsCompleted}, Unlocked={indicator.IsUnlocked}");
                }
            }
            else
            {
                LoggingService.LogDebug("UpdateLocationIndicators: Обновляем существующие индикаторы");
                // Обновляем существующие индикаторы без пересоздания
                for (int i = 0; i < _gameState.Locations.Count; i++)
                {
                    var location = _gameState.Locations[i];
                    var indicator = LocationIndicators[i];
                    
                    bool wasSelected = indicator.IsSelected;
                    
                    indicator.Index = i;
                    indicator.IsSelected = (i == _gameState.CurrentLocationIndex);
                    indicator.IsCompleted = location.IsCompleted;
                    indicator.IsUnlocked = location.IsUnlocked;
                    indicator.IsAvailable = location.CheckAvailability(_gameState);
                    
                    if (wasSelected != indicator.IsSelected)
                    {
                        LoggingService.LogDebug($"UpdateLocationIndicators: Индикатор {i} изменил выбор: {wasSelected} -> {indicator.IsSelected}");
                    }
                }
            }
            
            LoggingService.LogDebug($"UpdateLocationIndicators: Итого индикаторов после обновления: {LocationIndicators.Count}");
            LoggingService.LogDebug("=== UpdateLocationIndicators: КОНЕЦ ===");
        }

        // Update the details for the current location
        private void UpdateLocationDetails()
        {
            try
            {
                // Ensure we have a valid current location
                if (_gameState.CurrentLocation == null && _gameState.Locations != null && _gameState.Locations.Count > 0)
                {
                    _gameState.CurrentLocation = _gameState.Locations[_gameState.CurrentLocationIndex];
                }
                
                // Force update of location-related properties
                OnPropertyChanged(nameof(CurrentLocation));
                OnPropertyChanged(nameof(LocationDetailsText));
                OnPropertyChanged(nameof(CanTravelToLocation));
                OnPropertyChanged(nameof(CanFightHero));
                OnPropertyChanged(nameof(CanFightMobs));
                OnPropertyChanged(nameof(IsLocationLocked));
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error in UpdateLocationDetails: {ex.Message}", ex);
            }
        }

        // Method to refresh the location data
        public void RefreshLocations()
        {
            LoggingService.LogDebug("=== RefreshLocations: НАЧАЛО ===");
            
            // Предотвращаем циклические вызовы - НЕ ВЫЗЫВАЕМ RefreshView!
            if (_isRefreshing)
            {
                LoggingService.LogDebug("RefreshLocations: Уже в состоянии обновления, пропускаем");
                return;
            }
            
            try
            {
                LoggingService.LogDebug("RefreshLocations: Refreshing world map data");
                LoggingService.LogDebug($"RefreshLocations: Текущий индекс локации: {_gameState.CurrentLocationIndex}");
                LoggingService.LogDebug($"RefreshLocations: Количество локаций: {_gameState.Locations?.Count ?? 0}");
                LoggingService.LogDebug($"RefreshLocations: Количество индикаторов: {LocationIndicators.Count}");
                
                // НЕ ВЫЗЫВАЕМ RefreshView() - это создает циклические вызовы!
                // Вместо этого выполняем только необходимые обновления
                
                // Update the location indicators without using reflection
                LoggingService.LogDebug("RefreshLocations: Вызываем UpdateLocationIndicators");
                UpdateLocationIndicators();
                
                // Force update of current location data  
                LoggingService.LogDebug("RefreshLocations: Вызываем UpdateLocationDetails");
                UpdateLocationDetails();
                
                // Notify property changes for UI-related properties
                LoggingService.LogDebug("RefreshLocations: Обновляем UI свойства");
                OnPropertyChanged(nameof(CurrentLocation));
                OnPropertyChanged(nameof(LocationDetailsText));
                OnPropertyChanged(nameof(CanTravelToLocation));
                OnPropertyChanged(nameof(CanFightHero));
                OnPropertyChanged(nameof(CanFightMobs));
                
                LoggingService.LogDebug("RefreshLocations: Обновление завершено успешно");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error in RefreshLocations: {ex.Message}", ex);
            }
            
            LoggingService.LogDebug("=== RefreshLocations: КОНЕЦ ===");
        }

        // Отладочный метод для диагностики порядка локаций
        private void DiagnoseLocationOrder()
        {
            LoggingService.LogDebug("=== ДИАГНОСТИКА ПОРЯДКА ЛОКАЦИЙ ===");
            
            if (_gameState.Locations == null || _gameState.Locations.Count == 0)
            {
                LoggingService.LogError("ERROR: Локации не инициализированы!", null);
                return;
            }

            LoggingService.LogDebug($"Всего локаций: {_gameState.Locations.Count}");
            LoggingService.LogDebug($"Текущий индекс: {_gameState.CurrentLocationIndex}");
            
            for (int i = 0; i < _gameState.Locations.Count; i++)
            {
                var location = _gameState.Locations[i];
                LoggingService.LogDebug($"Индекс {i}: {location.Name} ({location.Type}) -> {location.SpritePath}");
                
                // Проверяем соответствие SpritePath и Type
                string expectedPath = AssetPaths.Locations.GetLocationPath(location.Type.ToString().ToLower());
                if (location.SpritePath != expectedPath)
                {
                    LoggingService.LogDebug($"  WARNING: SpritePath mismatch! Expected: {expectedPath}, Actual: {location.SpritePath}");
                }
            }
            
            if (_gameState.CurrentLocation != null)
            {
                LoggingService.LogDebug($"Текущая локация: {_gameState.CurrentLocation.Name} ({_gameState.CurrentLocation.Type})");
                LoggingService.LogDebug($"Текущий SpritePath: {_gameState.CurrentLocation.SpritePath}");
                
                // Проверяем правильность текущей локации
                string expectedCurrentPath = AssetPaths.Locations.GetLocationPath(_gameState.CurrentLocation.Type.ToString().ToLower());
                if (_gameState.CurrentLocation.SpritePath != expectedCurrentPath)
                {
                    LoggingService.LogDebug($"  WARNING: Current location SpritePath mismatch! Expected: {expectedCurrentPath}");
                }
            }
            else
            {
                LoggingService.LogError("ERROR: CurrentLocation равна null!", null);
            }
            
            LoggingService.LogDebug("=== КОНЕЦ ДИАГНОСТИКИ ===");
        }
    }
} 
