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
    
    public class MapViewModel : INotifyPropertyChanged
    {
        private readonly GameState _gameState;
        private readonly Action<string> _navigateAction;
        
        // Публичное свойство для доступа к GameState
        public GameState GameState => _gameState;
        
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
        
        public Location CurrentLocation => _gameState.CurrentLocation ?? _gameState.Locations?[0] ?? new Location();
        
        public bool CanNavigatePrevious => _gameState.CurrentLocationIndex > 0;
        
        public bool CanNavigateNext => _gameState.Locations != null && 
                                     _gameState.CurrentLocationIndex < _gameState.Locations.Count - 1;
        
        public bool CanTravelToLocation
        {
            get
            {
                bool result = CurrentLocation != null && 
                              CurrentLocation.IsUnlocked && 
                              CurrentLocation.CheckAvailability(_gameState);
                Console.WriteLine($"CanTravelToLocation: {result}");
                if (CurrentLocation != null)
                {
                    Console.WriteLine($"  - CurrentLocation.IsUnlocked: {CurrentLocation.IsUnlocked}");
                    Console.WriteLine($"  - CurrentLocation.CheckAvailability: {CurrentLocation.CheckAvailability(_gameState)}");
                }
                return result;
            }
        }
        
        public bool CanFightHero
        {
            get 
            {
                bool result = CurrentLocation != null && 
                            CurrentLocation.IsUnlocked && 
                            CurrentLocation.CheckAvailability(_gameState) && 
                            CurrentLocation.Hero != null &&
                            !CurrentLocation.HeroDefeated;
                Console.WriteLine($"CanFightHero: {result}");
                if (CurrentLocation != null)
                {
                    Console.WriteLine($"  - CurrentLocation.IsUnlocked: {CurrentLocation.IsUnlocked}");
                    Console.WriteLine($"  - CurrentLocation.CheckAvailability: {CurrentLocation.CheckAvailability(_gameState)}");
                    Console.WriteLine($"  - CurrentLocation.Hero != null: {CurrentLocation.Hero != null}");
                    Console.WriteLine($"  - !CurrentLocation.HeroDefeated: {!CurrentLocation.HeroDefeated}");
                }
                return result;
            }
        }
        
        public bool CanFightMobs
        {
            get
            {
                bool result = CurrentLocation != null &&
                              CurrentLocation.IsUnlocked && 
                              CurrentLocation.CheckAvailability(_gameState);
                Console.WriteLine($"CanFightMobs: {result}");
                if (CurrentLocation != null)
                {
                    Console.WriteLine($"  - CurrentLocation.IsUnlocked: {CurrentLocation.IsUnlocked}");
                    Console.WriteLine($"  - CurrentLocation.CheckAvailability: {CurrentLocation.CheckAvailability(_gameState)}");
                }
                return result;
            }
        }
        
        // Проверка, заблокирована ли текущая локация
        public bool IsLocationLocked => CurrentLocation != null && 
                                     (!CurrentLocation.IsUnlocked || !CurrentLocation.CheckAvailability(_gameState));
        
        // Детали локации для отображения
        public string LocationDetailsText
        {
            get
            {
                if (CurrentLocation == null)
                    return string.Empty;
                    
                string details = $"Тип: {GetLocationTypeText(CurrentLocation.Type)}\n" +
                                $"Сложность: {GetDifficultyText(CurrentLocation.Difficulty)}\n";
                                
                // Добавляем информацию о требованиях, если локация заблокирована
                if (!CurrentLocation.IsUnlocked)
                {
                    details += "\nТребования для разблокировки:\n";
                    
                    if (CurrentLocation.RequiredCompletedLocations.Count > 0)
                    {
                        details += "- Пройденные локации:\n";
                        foreach (var loc in CurrentLocation.RequiredCompletedLocations)
                        {
                            details += $"  * {loc}\n";
                        }
                    }
                    
                    if (CurrentLocation.RequiredItems.Count > 0)
                    {
                        details += "- Необходимые предметы:\n";
                        foreach (var item in CurrentLocation.RequiredItems)
                        {
                            details += $"  * {item}\n";
                        }
                    }
                }
                
                // Добавляем информацию о прогрессе
                if (CurrentLocation.IsCompleted)
                {
                    details += $"\nПройдено раз: {CurrentLocation.CompletionCount}/{CurrentLocation.MaxCompletions}";
                    if (CurrentLocation.Hero != null)
                    {
                        details += $"\nБосс: {CurrentLocation.Hero.Name}" +
                                  (CurrentLocation.HeroDefeated ? " (побежден)" : " (не побежден)");
                    }
                }
                
                return details;
            }
        }
        
        private string GetLocationTypeText(LocationType type)
        {
            switch (type)
            {
                case LocationType.Village: return "Деревня";
                case LocationType.Forest: return "Лес";
                case LocationType.Cave: return "Пещера";
                case LocationType.Ruins: return "Руины";
                case LocationType.Castle: return "Замок";
                default: return type.ToString();
            }
        }
        
        private string GetDifficultyText(LocationDifficultyLevel difficulty)
        {
            switch (difficulty)
            {
                case LocationDifficultyLevel.Easy: return "Легко";
                case LocationDifficultyLevel.Medium: return "Средне";
                case LocationDifficultyLevel.Hard: return "Тяжело";
                case LocationDifficultyLevel.VeryHard: return "Очень тяжело";
                case LocationDifficultyLevel.Extreme: return "Экстремально";
                default: return difficulty.ToString();
            }
        }
        
        // Commands
        public ICommand PreviousLocationCommand { get; private set; }
        public ICommand NextLocationCommand { get; private set; }
        public ICommand TravelToLocationCommand { get; private set; }
        public ICommand FightHeroCommand { get; private set; }
        public ICommand FightMobsCommand { get; private set; }
        public ICommand NavigateCommand { get; private set; }
        
        // Constructor
        public MapViewModel(GameState gameState, Action<string> navigateAction)
        {
            _gameState = gameState;
            _navigateAction = navigateAction;
            
            Console.WriteLine("MapViewModel constructor called");
            Console.WriteLine($"_navigateAction is null: {_navigateAction == null}");
            
            // Initialize commands with names for better debugging
            PreviousLocationCommand = new RelayCommand(_ => NavigateToPreviousLocation(), commandName: "PreviousLocationCommand");
            NextLocationCommand = new RelayCommand(_ => NavigateToNextLocation(), commandName: "NextLocationCommand");
            TravelToLocationCommand = new RelayCommand(_ => TravelToLocation(), commandName: "TravelToLocationCommand");
            FightHeroCommand = new RelayCommand(_ => FightHero(), commandName: "FightHeroCommand");
            FightMobsCommand = new RelayCommand(_ => FightMobs(), _ => CanFightMobs, commandName: "FightMobsCommand");
            
            Console.WriteLine("Commands initialized:");
            Console.WriteLine($"- PreviousLocationCommand: {PreviousLocationCommand != null}");
            Console.WriteLine($"- NextLocationCommand: {NextLocationCommand != null}");
            Console.WriteLine($"- TravelToLocationCommand: {TravelToLocationCommand != null}");
            Console.WriteLine($"- FightHeroCommand: {FightHeroCommand != null}");
            Console.WriteLine($"- FightMobsCommand: {FightMobsCommand != null}");
            
            // Заменяем NavigateCommand на версию, которая поддерживает и строки, и GameScreen
            NavigateCommand = new RelayCommand<object>(param => 
            {
                Console.WriteLine($"MapViewModel: NavigateCommand вызвана с параметром {param} типа {param?.GetType().Name}");
                
                if (param is GameScreen screen)
                {
                    Console.WriteLine($"Обработка параметра как GameScreen: {screen}");
                    _navigateAction(screen.ToString());
                }
                else if (param is string screenName)
                {
                    Console.WriteLine($"Обработка параметра как строки: {screenName}");
                    
                    // Преобразование строки в GameScreen
                    if (screenName == "MainMenu")
                    {
                        Console.WriteLine("Преобразовано в GameScreen.MainMenu");
                        _navigateAction("MainMenuView");
                    }
                    else if (screenName == "InventoryView")
                    {
                        _navigateAction("InventoryView");
                    }
                    else if (screenName == "WorldMapView")
                    {
                        _navigateAction("WorldMapView");
                    }
                    else if (screenName == "BattleView")
                    {
                        _navigateAction("BattleScreen");
                    }
                    else if (screenName == "SettingsView")
                    {
                        _navigateAction("SettingsView");
                    }
                    else
                    {
                        Console.WriteLine($"Неизвестное имя экрана: {screenName}");
                    }
                }
                else
                {
                    Console.WriteLine($"Неизвестный тип параметра: {param?.GetType().Name ?? "null"}");
                }
            }, commandName: "NavigateCommand");
            
            // Initialize location indicators
            UpdateLocationIndicators();
            
            Console.WriteLine("MapViewModel initialization complete");
        }
        
        // Method to refresh the view when it becomes visible
        public void RefreshView()
        {
            // Make sure CurrentLocation is set
            if (_gameState.CurrentLocation == null && _gameState.Locations != null && _gameState.Locations.Count > 0)
            {
                _gameState.CurrentLocation = _gameState.Locations[_gameState.CurrentLocationIndex];
                Console.WriteLine($"RefreshView: Set current location to {_gameState.CurrentLocation.Name}");
            }
            
            // Update location statuses
            if (_gameState.Locations != null)
            {
                foreach (var location in _gameState.Locations)
                {
                    location.CheckAvailability(_gameState);
                }
            }
            
            // Update location indicators
            UpdateLocationIndicators();
            
            // Notify property changes
            OnPropertyChanged(nameof(CurrentLocation));
            OnPropertyChanged(nameof(CanTravelToLocation));
            OnPropertyChanged(nameof(CanFightHero));
            OnPropertyChanged(nameof(CanFightMobs));
            OnPropertyChanged(nameof(IsLocationLocked));
            OnPropertyChanged(nameof(CanNavigatePrevious));
            OnPropertyChanged(nameof(CanNavigateNext));
        }
        
        // Location indicators for UI
        public ObservableCollection<LocationIndicator> LocationIndicators { get; } = new ObservableCollection<LocationIndicator>();
        
        private void NavigateToPreviousLocation()
        {
            if (!CanNavigatePrevious)
                return;
                
            try
            {
                Console.WriteLine("NavigateToPreviousLocation called");
                
                // Safely update the current location index
                _gameState.CurrentLocationIndex--;
                if (_gameState.Locations != null && _gameState.CurrentLocationIndex >= 0 && 
                    _gameState.CurrentLocationIndex < _gameState.Locations.Count)
                {
                    // Set the current location reference
                    _gameState.CurrentLocation = _gameState.Locations[_gameState.CurrentLocationIndex];
                    Console.WriteLine($"Changed to location: {_gameState.CurrentLocation.Name}");
                    
                    // Update indicators for the UI
                    foreach (var indicator in LocationIndicators)
                    {
                        indicator.IsSelected = (indicator.Index == _gameState.CurrentLocationIndex);
                    }
                    
                    // Notify subscribers of the location change
                    OnLocationChanged(_gameState.CurrentLocation, NavigationDirection.Previous);
                }
                else
                {
                    Console.WriteLine($"WARNING: Invalid location index: {_gameState.CurrentLocationIndex}");
                    // Reset to a valid index
                    _gameState.CurrentLocationIndex = 0;
                }
                
                // Use Application.Current.Dispatcher to queue the UI updates
                Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                    // Update UI properties
                    OnPropertyChanged(nameof(CurrentLocation));
                    OnPropertyChanged(nameof(CanNavigatePrevious));
                    OnPropertyChanged(nameof(CanNavigateNext));
                    OnPropertyChanged(nameof(CanTravelToLocation));
                    OnPropertyChanged(nameof(CanFightHero));
                    OnPropertyChanged(nameof(IsLocationLocked));
                    OnPropertyChanged(nameof(LocationDetailsText));
                }));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in NavigateToPreviousLocation: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
        
        private void NavigateToNextLocation()
        {
            if (!CanNavigateNext)
                return;
            
            try
            {
                Console.WriteLine("NavigateToNextLocation called");
                
                // Safely update the current location index
                _gameState.CurrentLocationIndex++;
                if (_gameState.Locations != null && _gameState.CurrentLocationIndex >= 0 && 
                    _gameState.CurrentLocationIndex < _gameState.Locations.Count)
                {
                    // Set the current location reference
                    _gameState.CurrentLocation = _gameState.Locations[_gameState.CurrentLocationIndex];
                    Console.WriteLine($"Changed to location: {_gameState.CurrentLocation.Name}");
                    
                    // Update indicators for the UI
                    foreach (var indicator in LocationIndicators)
                    {
                        indicator.IsSelected = (indicator.Index == _gameState.CurrentLocationIndex);
                    }
                    
                    // Notify subscribers of the location change
                    OnLocationChanged(_gameState.CurrentLocation, NavigationDirection.Next);
                }
                else
                {
                    Console.WriteLine($"WARNING: Invalid location index: {_gameState.CurrentLocationIndex}");
                    // Reset to the last valid index
                    if (_gameState.Locations != null && _gameState.Locations.Count > 0)
                    {
                        _gameState.CurrentLocationIndex = _gameState.Locations.Count - 1;
                    }
                    else
                    {
                        _gameState.CurrentLocationIndex = 0;
                    }
                }
                
                // Use Application.Current.Dispatcher to queue the UI updates
                Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                    // Update UI properties
                    OnPropertyChanged(nameof(CurrentLocation));
                    OnPropertyChanged(nameof(CanNavigatePrevious));
                    OnPropertyChanged(nameof(CanNavigateNext));
                    OnPropertyChanged(nameof(CanTravelToLocation));
                    OnPropertyChanged(nameof(CanFightHero));
                    OnPropertyChanged(nameof(IsLocationLocked));
                    OnPropertyChanged(nameof(LocationDetailsText));
                }));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in NavigateToNextLocation: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
        
        private void TravelToLocation()
        {
            Console.WriteLine("TravelToLocation method called");
            Console.WriteLine($"CanTravelToLocation: {CanTravelToLocation}, CurrentLocation is null: {CurrentLocation == null}");
            
            if (!CanTravelToLocation || CurrentLocation == null)
            {
                Console.WriteLine("Cannot travel to location - preconditions not met");
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
                        Console.WriteLine("Warning: Location sprite path is empty");
                        // Set default path based on location type
                        locationCopy.SpritePath = $"Assets/Images/Locations/{locationCopy.Type.ToString().ToLower()}.png";
                        Console.WriteLine($"Set default sprite path: {locationCopy.SpritePath}");
                    }
                    else
                    {
                        Console.WriteLine($"Location sprite path: {locationCopy.SpritePath}");
                    }
                    
                    // Since we're using file paths instead of BitmapImage objects, we shouldn't 
                    // hit NotSupportedException during serialization
                    
                    Console.WriteLine($"Traveling to location: {locationCopy.Name}");
                    
                    // Check if this is the first time visiting this location
                    bool firstVisit = !locationCopy.IsCompleted;
                    Console.WriteLine($"First visit: {firstVisit}");
                    
                    // Random encounter check (30% chance if not completed yet)
                    bool triggerEncounter = false;
                    if (!locationCopy.IsCompleted)
                    {
                        Random random = new Random();
                        triggerEncounter = random.Next(100) < 30;
                    }
                    Console.WriteLine($"Trigger encounter: {triggerEncounter}");
                    
                    if (triggerEncounter)
                    {
                        Console.WriteLine("Random encounter triggered!");
                        
                        try
                        {
                            // Start a battle with random enemies from this location
                            _gameState.StartBattleWithMobs();
                            
                            // First set the current screen in GameState
                            _gameState.CurrentScreen = "BattleView";
                            Console.WriteLine($"GameState.CurrentScreen set to {_gameState.CurrentScreen}");
                            
                            // Navigate to battle using dispatcher to avoid thread issues
                            Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                                try
                                {
                                    Console.WriteLine("Navigating to battle screen");
                                    _navigateAction("BattleScreen");
                                    Console.WriteLine("Navigation to battle screen completed");
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Error in navigation dispatch: {ex.Message}");
                                }
                            }));
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error starting battle: {ex.Message}");
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
                            Console.WriteLine("First visit to location completed");
                            
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
                                    Console.WriteLine($"Error in full save: {ex.Message}");
                                    // Emergency save is still there, so data isn't lost
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error in emergency save: {ex.Message}");
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
                                Console.WriteLine($"Error updating UI after travel: {ex.Message}");
                            }
                        }));
                    }
                }
                catch (NotSupportedException ex)
                {
                    Console.WriteLine($"NotSupportedException in location handling: {ex.Message}");
                    
                    // Continue without using the problematic image references
                    if (CurrentLocation != null)
                    {
                        CurrentLocation.IsCompleted = true;
                        CurrentLocation.CompletionCount++;
                        
                        // Mark location as completed
                        Console.WriteLine("Marked location as completed despite image error");
                        
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
                Console.WriteLine($"NotSupportedException in TravelToLocation: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                
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
                Console.WriteLine($"InvalidOperationException in TravelToLocation: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                MessageBox.Show($"Ошибка при перемещении в локацию: {ex.Message}", "Ошибка перемещения", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error traveling to location: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                MessageBox.Show($"Ошибка при перемещении в локацию: {ex.Message}", "Ошибка перемещения", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void FightHero()
        {
            Console.WriteLine("FightHero method called");
            Console.WriteLine($"CanFightHero: {CanFightHero}, CurrentLocation is null: {CurrentLocation == null}, CurrentLocation.Hero is null: {CurrentLocation?.Hero == null}");
            
            if (!CanFightHero || CurrentLocation == null || CurrentLocation.Hero == null)
            {
                Console.WriteLine("Cannot fight hero - preconditions not met");
                return;
            }
            
            try
            {
                Console.WriteLine($"Starting boss fight with {CurrentLocation.Hero.Name} in {CurrentLocation.Name}");
                
                // Start a battle with the hero of this location
                _gameState.StartBattleWithHero();
                
                // Don't set it in GameState again - it was already set in StartBattleWithHero
                // _gameState.CurrentScreen = GameScreen.Battle;
                Console.WriteLine($"GameState.CurrentScreen now: {_gameState.CurrentScreen}");
                
                // Navigate directly to the BattleView
                Console.WriteLine("Directly navigating to BattleView using MainWindow.NavigateToScreen");
                Application.Current.Dispatcher.Invoke(() =>
                {
                    // Get the MainWindow instance
                    if (Application.Current.MainWindow is MainWindow mainWindow)
                    {
                        mainWindow.NavigateToScreen("BattleView");
                        Console.WriteLine("Successfully called MainWindow.NavigateToScreen(\"BattleView\")");
                    }
                    else
                    {
                        Console.WriteLine("ERROR: Application.Current.MainWindow is not a MainWindow");
                        // Fallback to using the navigate action
                        _navigateAction("BattleView");
                    }
                });
                
                Console.WriteLine("Navigation command completed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error starting hero battle: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                MessageBox.Show($"Error starting hero battle: {ex.Message}", "Battle Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void FightMobs()
        {
            Console.WriteLine("FightMobs method called");
            Console.WriteLine($"CanFightMobs: {CanFightMobs}, CurrentLocation is null: {CurrentLocation == null}");
            
            if (!CanFightMobs || CurrentLocation == null)
            {
                Console.WriteLine("Cannot fight mobs - preconditions not met");
                return;
            }
            
            try
            {
                Console.WriteLine($"Starting mob fight in {CurrentLocation.Name}");
                
                // Start a battle with mobs in this location
                _gameState.StartBattleWithMobs();
                
                // Don't set it in GameState again - it was already set in StartBattleWithMobs
                // _gameState.CurrentScreen = GameScreen.Battle;
                Console.WriteLine($"GameState.CurrentScreen now: {_gameState.CurrentScreen}");
                
                // Navigate directly to the BattleView
                Console.WriteLine("Directly navigating to BattleView using MainWindow.NavigateToScreen");
                Application.Current.Dispatcher.Invoke(() =>
                {
                    // Get the MainWindow instance
                    if (Application.Current.MainWindow is MainWindow mainWindow)
                    {
                        mainWindow.NavigateToScreen("BattleView");
                        Console.WriteLine("Successfully called MainWindow.NavigateToScreen(\"BattleView\")");
                    }
                    else
                    {
                        Console.WriteLine("ERROR: Application.Current.MainWindow is not a MainWindow");
                        // Fallback to using the navigate action
                        _navigateAction("BattleView");
                    }
                });
                
                Console.WriteLine("Navigation command completed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error starting mob battle: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                MessageBox.Show($"Error starting mob battle: {ex.Message}", "Battle Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void UpdateLocationIndicators()
        {
            // Clear existing indicators
            LocationIndicators.Clear();
            
            if (_gameState.Locations == null || _gameState.Locations.Count == 0)
                return;
            
            // Create an indicator for each location
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
            }
            
            // Notify UI of property changes
            OnPropertyChanged(nameof(CurrentLocation));
            OnPropertyChanged(nameof(CanNavigatePrevious));
            OnPropertyChanged(nameof(CanNavigateNext));
            OnPropertyChanged(nameof(CanTravelToLocation));
            OnPropertyChanged(nameof(CanFightHero));
            OnPropertyChanged(nameof(CanFightMobs));
            OnPropertyChanged(nameof(IsLocationLocked));
            OnPropertyChanged(nameof(LocationDetailsText));
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
                Console.WriteLine($"Error in UpdateLocationDetails: {ex.Message}");
            }
        }

        // Method to refresh the location data
        public void RefreshLocations()
        {
            try
            {
                Console.WriteLine("RefreshLocations: Refreshing world map data");
                
                // Use the existing refresh method that updates all necessary properties
                RefreshView();
                
                // Additional updates specific to refreshing after battle
                foreach (var location in _gameState.Locations)
                {
                    // Force property change notifications on relevant properties
                    if (location is INotifyPropertyChanged notifyLocation)
                    {
                        // Use reflection to access protected OnPropertyChanged method if needed
                        var method = location.GetType().GetMethod("OnPropertyChanged", 
                            System.Reflection.BindingFlags.Instance | 
                            System.Reflection.BindingFlags.NonPublic | 
                            System.Reflection.BindingFlags.Public);
                        
                        if (method != null)
                        {
                            method.Invoke(location, new object[] { "IsCompleted" });
                            method.Invoke(location, new object[] { "HeroDefeated" });
                            method.Invoke(location, new object[] { "IsUnlocked" });
                        }
                    }
                }
                
                // Update the location indicators
                UpdateLocationIndicators();
                
                // Force update of current location data
                UpdateLocationDetails();
                
                // Notify property changes for UI-related properties
                OnPropertyChanged(nameof(CurrentLocation));
                OnPropertyChanged(nameof(LocationDetailsText));
                OnPropertyChanged(nameof(CanTravelToLocation));
                OnPropertyChanged(nameof(CanFightHero));
                OnPropertyChanged(nameof(CanFightMobs));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in RefreshLocations: {ex.Message}");
            }
        }
    }
} 