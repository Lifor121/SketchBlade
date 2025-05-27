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
    /// ���������� ������ ����������� - ������ ����������� ������
    /// </summary>
    public class MapViewModel : INotifyPropertyChanged
    {
        private readonly GameData _gameState;
        private readonly Action<string> _navigateAction;
        private bool _isRefreshing = false; // ���� ��� �������������� ����������� �������
        private static int _refreshCallCount = 0; // ������� ��� ������������ ������� �������
        private const int MAX_REFRESH_DEPTH = 3; // ������������ ������� ����������� �������
        
        // ��������� �������� ��� ������� � GameData
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
                // ���� CurrentLocation null, �������� ���������� ��� �� ������ CurrentLocationIndex
                if (_gameState.CurrentLocation == null && _gameState.Locations != null && _gameState.Locations.Count > 0)
                {
                    // ���������� ��� ������ ����������
                    if (_gameState.CurrentLocationIndex < 0 || _gameState.CurrentLocationIndex >= _gameState.Locations.Count)
                    {
                        _gameState.CurrentLocationIndex = 0;
                    }
                    
                    _gameState.CurrentLocation = _gameState.Locations[_gameState.CurrentLocationIndex];
                    // LoggingService.LogDebug($"Auto-initialized CurrentLocation to: {_gameState.CurrentLocation.Name}");
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
                
                // ������ ��������������, ���� ������� �������������� � ��������
                bool canTravel = CurrentLocation.IsUnlocked && CurrentLocation.CheckAvailability(_gameState);
                
                return canTravel;
            }
        }
        
        public bool CanFightHero
        {
            get
            {
                if (CurrentLocation == null) return false;
                
                // ������������ ������: ����� ��������� � ������ ������ ����:
                // 1. � ������� ���� �����
                // 2. ������� ��������������
                // 3. ������� �������� ��� ���������
                // 4. ����� ��� �� �������� (�������� README - ������ ���� ���)
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
                
                // ������������ ������: ����� ��������� � ������ ������ ����:
                // 1. ������� ��������������
                // 2. ������� �������� ��� ���������
                return CurrentLocation.IsUnlocked && CurrentLocation.CheckAvailability(_gameState);
            }
        }
        
        // ���������, ������������� �� �������
        public bool IsLocationLocked => CurrentLocation != null && 
                                     (!CurrentLocation.IsUnlocked || !CurrentLocation.CheckAvailability(_gameState));
        
        // ������� ������� ��� �����������
        public string LocationDetailsText
        {
            get
            {
                if (CurrentLocation == null)
                    return "������� �� �������";

                var details = new System.Text.StringBuilder();
                details.AppendLine($"��������: {CurrentLocation.Name}");
                details.AppendLine($"���: {GetLocationTypeText(CurrentLocation.Type)}");
                details.AppendLine($"���������: {GetDifficultyText(CurrentLocation.Difficulty)}");
                details.AppendLine($"��������: {CurrentLocation.Description}");

                if (CurrentLocation.IsUnlocked)
                {
                    details.AppendLine("������: ��������������");
                    
                    if (CurrentLocation.Hero != null)
                    {
                        if (CurrentLocation.HeroDefeated)
                        {
                            details.AppendLine($"����: {CurrentLocation.Hero.Name} (��������)");
                        }
                        else
                        {
                            details.AppendLine($"����: {CurrentLocation.Hero.Name} (�������� ��� �����)");
                        }
                    }
                    
                    if (CurrentLocation.IsCompleted)
                    {
                        details.AppendLine("������� ��������");
                    }
                }
                else
                {
                    details.AppendLine("������: �������������");
                    
                    // ���������� ���������� ��� ���������������
                    if (CurrentLocation.MinPlayerLevel > 1)
                    {
                        details.AppendLine($"��������� �������: {CurrentLocation.MinPlayerLevel}");
                    }
                    
                    if (CurrentLocation.RequiredCompletedLocations.Count > 0)
                    {
                        string requiredLocations = string.Join(", ", CurrentLocation.RequiredCompletedLocations);
                        details.AppendLine($"��������� ������: {requiredLocations}");
                    }
                }

                return details.ToString();
            }
        }
        
        private string GetLocationTypeText(LocationType type)
        {
            return type switch
            {
                LocationType.Village => "�������",
                LocationType.Forest => "���",
                LocationType.Cave => "������",
                LocationType.Ruins => "�����",
                LocationType.Castle => "�����",
                _ => "����������"
            };
        }
        
        private string GetDifficultyText(LocationDifficultyLevel difficulty)
        {
            return difficulty switch
            {
                LocationDifficultyLevel.Easy => "������",
                LocationDifficultyLevel.Medium => "�������",
                LocationDifficultyLevel.Hard => "�������",
                LocationDifficultyLevel.VeryHard => "����� �������",
                _ => "����������"
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
            
            // �������������� CurrentLocation ���� ��� null
            if (_gameState.CurrentLocation == null && _gameState.Locations != null && _gameState.Locations.Count > 0)
            {
                _gameState.CurrentLocationIndex = Math.Max(0, Math.Min(_gameState.CurrentLocationIndex, _gameState.Locations.Count - 1));
                _gameState.CurrentLocation = _gameState.Locations[_gameState.CurrentLocationIndex];
            }
            
            // Initialize commands
            InitializeCommands();
            
            DiagnoseLocationOrder();
            
            // �������������� �������� ���������
            UpdateNavigationProperties();
            
            // �������������� ���������� �������
            UpdateLocationIndicators();
            
            // ������������� ��������� ������� ����� �������������
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
        
        // ����� ��� ��������������� ���������� ���� ������
        private void RefreshCommands()
        {
            // LoggingService.LogDebug("RefreshCommands: ������������� ��������� ��� �������");
            
            // ��������� �������� ���������
            UpdateNavigationProperties();
            
            // ������������� ��������� ������� ����� CommandManager
            CommandManager.InvalidateRequerySuggested();
            
            // ����� ��������� ��� ��������� ��������
            OnPropertyChanged(nameof(CanNavigatePrevious));
            OnPropertyChanged(nameof(CanNavigateNext));
            OnPropertyChanged(nameof(CanTravelToLocation));
            OnPropertyChanged(nameof(CanFightHero));
            OnPropertyChanged(nameof(CanFightMobs));
            
            // LoggingService.LogDebug($"RefreshCommands: CanNavigatePrevious={CanNavigatePrevious}, CanNavigateNext={CanNavigateNext}");
            // LoggingService.LogDebug($"RefreshCommands: CanFightMobs={CanFightMobs}, CanFightHero={CanFightHero}");
        }
        
        // ����� ��� ���������� ������� ���������
        private void UpdateNavigationProperties()
        {
            CanNavigatePrevious = _gameState.CurrentLocationIndex > 0;
            CanNavigateNext = _gameState.Locations != null && 
                             _gameState.CurrentLocationIndex < _gameState.Locations.Count - 1;
            
            // LoggingService.LogDebug($"UpdateNavigationProperties: Index={_gameState.CurrentLocationIndex}, CanPrev={CanNavigatePrevious}, CanNext={CanNavigateNext}");
        }
        
        // Method to refresh the view when it becomes visible
        public void RefreshView()
        {
            // ������������� ����������� ������
            if (_isRefreshing)
            {
                // LoggingService.LogDebug("RefreshView: ��� � ��������� ����������, ����������");
                return;
            }
            
            // �������������� ������ �� ������� �������� ��������
            if (_refreshCallCount >= MAX_REFRESH_DEPTH)
            {
                // LoggingService.LogDebug($"RefreshView: ���������� ������������ ������� �������� ({_refreshCallCount}), ����������");
                return;
            }
            
            _isRefreshing = true;
            _refreshCallCount++;
            
            try
            {
                // LoggingService.LogDebug($"RefreshView: ������ ���������� ����������� ������� (����� #{_refreshCallCount})");
                
                // ����������, ��� CurrentLocation ����������
                if (_gameState.CurrentLocation == null && _gameState.Locations != null && _gameState.Locations.Count > 0)
                {
                    _gameState.CurrentLocation = _gameState.Locations[_gameState.CurrentLocationIndex];
                    // LoggingService.LogDebug($"RefreshView: ���������� CurrentLocation = {_gameState.CurrentLocation.Name}");
                }
                
                // LoggingService.LogDebug($"RefreshView: ��������� ������� ��� {_gameState.Locations?.Count ?? 0} �������");
                
                // ������������� ��������� ��� ����������� � ������� �������
                if (_gameState.Locations != null)
                {
                    foreach (var location in _gameState.Locations)
                    {
                        // LoggingService.LogDebug($"RefreshView: Checking location {location.Name}: Unlocked={location.IsUnlocked}, Completed={location.IsCompleted}, HeroDefeated={location.HeroDefeated}");
                        
                        // Update all the relevant properties for this location
                        if (location.IsUnlocked)
                        {
                            // Location is available if it's unlocked and either not completed or allows repeated completion
                            location.IsAvailable = !location.IsCompleted || location.CompletionCount < 10;
                        }
                        
                        // LoggingService.LogDebug($"RefreshView: ������� {location.Name}: ��������������={location.IsUnlocked}, ���������={location.IsCompleted}, ��������={location.IsAvailable}");
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
                
                // LoggingService.LogDebug("RefreshView: ��������� ���������� ����������� �������");
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
            // LoggingService.LogDebug("=== NavigateToPreviousLocation: ������ ===");
            // LoggingService.LogDebug($"CanNavigatePrevious: {CanNavigatePrevious}");
            
            if (!CanNavigatePrevious)
            {
                // LoggingService.LogDebug("NavigateToPreviousLocation: ��������� ����� ����������");
                return;
            }
                
            try
            {
                // LoggingService.LogDebug($"������� ������ �� ���������: {_gameState.CurrentLocationIndex}");
                _gameState.CurrentLocationIndex--;
                // LoggingService.LogDebug($"����� ������: {_gameState.CurrentLocationIndex}");
                
                if (_gameState.Locations != null && _gameState.CurrentLocationIndex >= 0 && 
                    _gameState.CurrentLocationIndex < _gameState.Locations.Count)
                {
                    _gameState.CurrentLocation = _gameState.Locations[_gameState.CurrentLocationIndex];
                    // LoggingService.LogDebug($"����������� ����� �������: {_gameState.CurrentLocation.Name}");
                    
                    // ��������� ������ ������ ������ � �����������
                    // LoggingService.LogDebug($"��������� ������ ������ ��� {LocationIndicators.Count} �����������");
                    foreach (var indicator in LocationIndicators)
                    {
                        bool wasSelected = indicator.IsSelected;
                        indicator.IsSelected = (indicator.Index == _gameState.CurrentLocationIndex);
                        if (wasSelected != indicator.IsSelected)
                        {
                            // LoggingService.LogDebug($"��������� {indicator.Index}: {wasSelected} -> {indicator.IsSelected}");
                        }
                    }
                    
                    // LoggingService.LogDebug("�������� OnLocationChanged");
                    OnLocationChanged(_gameState.CurrentLocation, NavigationDirection.Previous);
                }
                else
                {
                    // LoggingService.LogDebug("������ ����� �� �������, ���������� �� 0");
                    _gameState.CurrentLocationIndex = 0;
                }

                // ��������� UI ��������
                // LoggingService.LogDebug("��������� ���������� UI ������� ����� Dispatcher");
                Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                    // LoggingService.LogDebug("��������� ���������� UI �������");
                    OnPropertyChanged(nameof(CurrentLocation));
                    OnPropertyChanged(nameof(CanNavigatePrevious));
                    OnPropertyChanged(nameof(CanNavigateNext));
                    OnPropertyChanged(nameof(CanTravelToLocation));
                    OnPropertyChanged(nameof(CanFightHero));
                    OnPropertyChanged(nameof(CanFightMobs));
                    OnPropertyChanged(nameof(IsLocationLocked));
                    OnPropertyChanged(nameof(LocationDetailsText));
                    
                    RefreshCommands();
                    // LoggingService.LogDebug("UI �������� ���������");
                }));
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"������ � NavigateToPreviousLocation: {ex.Message}", ex);
            }
            
            // LoggingService.LogDebug("=== NavigateToPreviousLocation: ����� ===");
        }
        
        private void NavigateToNextLocation()
        {
            // LoggingService.LogDebug("=== NavigateToNextLocation: ������ ===");
            // LoggingService.LogDebug($"CanNavigateNext: {CanNavigateNext}");
            
            if (!CanNavigateNext)
            {
                // LoggingService.LogDebug("NavigateToNextLocation: ��������� ������ ����������");
                return;
            }
            
            try
            {
                // LoggingService.LogDebug($"������� ������ �� ���������: {_gameState.CurrentLocationIndex}");
                _gameState.CurrentLocationIndex++;
                // LoggingService.LogDebug($"����� ������: {_gameState.CurrentLocationIndex}");
                
                if (_gameState.Locations != null && _gameState.CurrentLocationIndex >= 0 && 
                    _gameState.CurrentLocationIndex < _gameState.Locations.Count)
                {
                    _gameState.CurrentLocation = _gameState.Locations[_gameState.CurrentLocationIndex];
                    // LoggingService.LogDebug($"����������� ����� �������: {_gameState.CurrentLocation.Name}");
                    
                    // ��������� ������ ������ ������ � �����������
                    // LoggingService.LogDebug($"��������� ������ ������ ��� {LocationIndicators.Count} �����������");
                    foreach (var indicator in LocationIndicators)
                    {
                        bool wasSelected = indicator.IsSelected;
                        indicator.IsSelected = (indicator.Index == _gameState.CurrentLocationIndex);
                        if (wasSelected != indicator.IsSelected)
                        {
                            // LoggingService.LogDebug($"��������� {indicator.Index}: {wasSelected} -> {indicator.IsSelected}");
                        }
                    }
                    
                    // LoggingService.LogDebug("�������� OnLocationChanged");
                    OnLocationChanged(_gameState.CurrentLocation, NavigationDirection.Next);
                }
                else
                {
                    // LoggingService.LogDebug("������ ����� �� �������, ���������� �� ������������");
                    _gameState.CurrentLocationIndex = Math.Max(0, _gameState.Locations?.Count - 1 ?? 0);
                }

                // ��������� UI ��������
                // LoggingService.LogDebug("��������� ���������� UI ������� ����� Dispatcher");
                Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                    // LoggingService.LogDebug("��������� ���������� UI �������");
                    OnPropertyChanged(nameof(CurrentLocation));
                    OnPropertyChanged(nameof(CanNavigatePrevious));
                    OnPropertyChanged(nameof(CanNavigateNext));
                    OnPropertyChanged(nameof(CanTravelToLocation));
                    OnPropertyChanged(nameof(CanFightHero));
                    OnPropertyChanged(nameof(CanFightMobs));
                    OnPropertyChanged(nameof(IsLocationLocked));
                    OnPropertyChanged(nameof(LocationDetailsText));
                    
                    RefreshCommands();
                    // LoggingService.LogDebug("UI �������� ���������");
                }));
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"������ � NavigateToNextLocation: {ex.Message}", ex);
            }
            
            // LoggingService.LogDebug("=== NavigateToNextLocation: ����� ===");
        }
        
        private void TravelToLocation()
        {
            // LoggingService.LogDebug("TravelToLocation method called");
            // LoggingService.LogDebug($"CanTravelToLocation: {CanTravelToLocation}, CurrentLocation is null: {CurrentLocation == null}");
            
            if (!CanTravelToLocation || CurrentLocation == null)
            {
                // LoggingService.LogDebug("Cannot travel to location - preconditions not met");
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
                        // LoggingService.LogDebug("Warning: Location sprite path is empty");
                        // Set default path based on location type
                        locationCopy.SpritePath = AssetPaths.Locations.GetLocationPath(locationCopy.Type.ToString().ToLower());
                        // LoggingService.LogDebug($"Set default sprite path: {locationCopy.SpritePath}");
                    }
                    else
                    {
                        // LoggingService.LogDebug($"Location sprite path: {locationCopy.SpritePath}");
                    }
                    
                    // Since we're using file paths instead of BitmapImage objects, we shouldn't 
                    // hit NotSupportedException during serialization
                    
                    // LoggingService.LogDebug($"Traveling to location: {locationCopy.Name}");
                    
                    // Check if this is the first time visiting this location
                    bool firstVisit = !locationCopy.IsCompleted;
                    // LoggingService.LogDebug($"First visit: {firstVisit}");
                    
                    // Random encounter check (30% chance if not completed yet)
                    bool triggerEncounter = false;
                    if (!locationCopy.IsCompleted)
                    {
                        Random random = new Random();
                        triggerEncounter = random.Next(100) < 30;
                    }
                    // LoggingService.LogDebug($"Trigger encounter: {triggerEncounter}");
                    
                    if (triggerEncounter)
                    {
                        // LoggingService.LogDebug("Random encounter triggered!");
                        
                        try
                        {
                            // Start a battle with random enemies from this location
                            _gameState.StartBattleWithMobs(CurrentLocation);
                            
                            // First set the current screen in GameData
                            _gameState.CurrentScreen = "BattleView";
                            // LoggingService.LogDebug($"GameData.CurrentScreen set to {_gameState.CurrentScreen}");
                            
                            // Navigate to battle using dispatcher to avoid thread issues
                            Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                                try
                                {
                                    // LoggingService.LogDebug("Navigating to battle screen");
                                    _navigateAction("BattleScreen");
                                    // LoggingService.LogDebug("Navigation to battle screen completed");
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
                            // LoggingService.LogDebug("First visit to location completed");
                            
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
                        // LoggingService.LogDebug("Marked location as completed despite image error");
                        
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
                MessageBox.Show($"������ ��� ����������� � �������: {ex.Message}", "������ �����������", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error traveling to location: {ex.Message}", ex);
                MessageBox.Show($"������ ��� ����������� � �������: {ex.Message}", "������ �����������", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void FightHero()
        {
            // LoggingService.LogDebug("FightHero method called");
            // LoggingService.LogDebug($"CanFightHero: {CanFightHero}, CurrentLocation is null: {CurrentLocation == null}, CurrentLocation.Hero is null: {CurrentLocation?.Hero == null}");
            
            if (!CanFightHero || CurrentLocation?.Hero == null)
            {
                // LoggingService.LogDebug("Cannot fight hero - preconditions not met");
                return;
            }
            
            try
            {
                // LoggingService.LogDebug($"Starting boss fight with {CurrentLocation.Hero.Name} in {CurrentLocation.Name}");
                
                // �����: ������������� ������� CurrentEnemies ����� ������ ����
                _gameState.CurrentEnemies.Clear();
                // LoggingService.LogDebug("Cleared CurrentEnemies before battle");
                
                // ������� ����� ����� ��� ���, ����� �� �������� ������������� �����
                var heroCopy = new Character
                {
                    Name = CurrentLocation.Hero.Name,
                    MaxHealth = CurrentLocation.Hero.MaxHealth,
                    CurrentHealth = CurrentLocation.Hero.MaxHealth, // �����: ������ ��������
                    Attack = CurrentLocation.Hero.Attack,
                    Defense = CurrentLocation.Hero.Defense,
                    Level = CurrentLocation.Hero.Level,
                    IsHero = true,
                    LocationType = CurrentLocation.Hero.LocationType,
                    ImagePath = CurrentLocation.Hero.ImagePath,
                    IsPlayer = false
                };
                
                // ������������� ���������� ������ ���������
                heroCopy.SetDefeated(false);
                
                // LoggingService.LogDebug($"Created hero copy: {heroCopy.Name}, Health: {heroCopy.CurrentHealth}/{heroCopy.MaxHealth}, IsDefeated: {heroCopy.IsDefeated}");
                
                // ��������� ����� ����� � CurrentEnemies
                _gameState.CurrentEnemies.Add(heroCopy);
                // LoggingService.LogDebug($"Added hero to CurrentEnemies. Total enemies: {_gameState.CurrentEnemies.Count}");
                
                // �����: ������������� CurrentScreen � BattleView
                _gameState.CurrentScreen = "BattleView";
                // LoggingService.LogDebug($"GameData.CurrentScreen set to: {_gameState.CurrentScreen}");
                
                // Navigate directly to the BattleView
                // LoggingService.LogDebug("Navigating to BattleView using _navigateAction");
                _navigateAction("BattleView");
                // LoggingService.LogDebug("Navigation completed successfully");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error starting boss battle: {ex.Message}", ex);
                MessageBox.Show($"Error starting boss battle: {ex.Message}", "Battle Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void FightMobs()
        {
            // LoggingService.LogDebug("FightMobs method called");
            // LoggingService.LogDebug($"CanFightMobs: {CanFightMobs}, CurrentLocation is null: {CurrentLocation == null}");
            
            if (!CanFightMobs || CurrentLocation == null)
            {
                // LoggingService.LogDebug("Cannot fight mobs - preconditions not met");
                return;
            }
            
            try
            {
                // LoggingService.LogDebug($"Starting mob battle in {CurrentLocation.Name}");
                
                // �����: ������������� ������� CurrentEnemies ����� ������ ����
                _gameState.CurrentEnemies.Clear();
                // LoggingService.LogDebug("Cleared CurrentEnemies before mob battle");
                
                // ���������� ����� ������ ��� ������� �������
                var enemies = GameLogicService.Instance.GenerateEnemies(CurrentLocation, _gameState.Player?.Level ?? 1);
                // LoggingService.LogDebug($"Generated {enemies.Count} enemies for {CurrentLocation.Name}");
                
                // ��������� ������ � CurrentEnemies � ����������, ��� ��� �� ���������
                foreach (var enemy in enemies)
                {
                    enemy.SetDefeated(false); // ������������� ���������� ������ ���������
                    enemy.CurrentHealth = enemy.MaxHealth; // ��������������� ������ ��������
                    _gameState.CurrentEnemies.Add(enemy);
                    // LoggingService.LogDebug($"Added enemy: {enemy.Name}, Health: {enemy.CurrentHealth}/{enemy.MaxHealth}, IsDefeated: {enemy.IsDefeated}");
                }
                
                // LoggingService.LogDebug($"Total enemies in CurrentEnemies: {_gameState.CurrentEnemies.Count}");
                
                // �����: ������������� CurrentScreen � BattleView
                _gameState.CurrentScreen = "BattleView";
                // LoggingService.LogDebug($"GameData.CurrentScreen set to: {_gameState.CurrentScreen}");
                
                // Navigate directly to the BattleView
                // LoggingService.LogDebug("Navigating to BattleView using _navigateAction");
                _navigateAction("BattleView");
                // LoggingService.LogDebug("Navigation completed successfully");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error starting mob battle: {ex.Message}", ex);
                MessageBox.Show($"Error starting mob battle: {ex.Message}", "Battle Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void UpdateLocationIndicators()
        {
            // LoggingService.LogDebug("=== UpdateLocationIndicators: ������ ===");
            
            if (_gameState.Locations == null || _gameState.Locations.Count == 0)
            {
                // LoggingService.LogDebug("UpdateLocationIndicators: ������� �����������, ������� ����������");
                LocationIndicators.Clear();
                return;
            }
            
            // LoggingService.LogDebug($"UpdateLocationIndicators: �������: {_gameState.Locations.Count}, �����������: {LocationIndicators.Count}");
            // LoggingService.LogDebug($"UpdateLocationIndicators: ������� ������ �������: {_gameState.CurrentLocationIndex}");
            
            // ���� ���������� ����������� �� ��������� � ����������� �������, ����������� ��
            if (LocationIndicators.Count != _gameState.Locations.Count)
            {
                // LoggingService.LogDebug("UpdateLocationIndicators: ���������� ����������� �� ���������, �����������");
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
                    // LoggingService.LogDebug($"UpdateLocationIndicators: ������ ��������� {i}: Selected={indicator.IsSelected}, Completed={indicator.IsCompleted}, Unlocked={indicator.IsUnlocked}");
                }
            }
            else
            {
                // LoggingService.LogDebug("UpdateLocationIndicators: ��������� ������������ ����������");
                // ��������� ������������ ���������� ��� ������������
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
                        // LoggingService.LogDebug($"UpdateLocationIndicators: ��������� {i} ������� �����: {wasSelected} -> {indicator.IsSelected}");
                    }
                }
            }
            
            // LoggingService.LogDebug($"UpdateLocationIndicators: ����� ����������� ����� ����������: {LocationIndicators.Count}");
            // LoggingService.LogDebug("=== UpdateLocationIndicators: ����� ===");
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
            // LoggingService.LogDebug("=== RefreshLocations: ������ ===");
            
            // ������������� ����������� ������ - �� �������� RefreshView!
            if (_isRefreshing)
            {
                // LoggingService.LogDebug("RefreshLocations: ��� � ��������� ����������, ����������");
                return;
            }
            
            try
            {
                // LoggingService.LogDebug("RefreshLocations: Refreshing world map data");
                // LoggingService.LogDebug($"RefreshLocations: ������� ������ �������: {_gameState.CurrentLocationIndex}");
                // LoggingService.LogDebug($"RefreshLocations: ���������� �������: {_gameState.Locations?.Count ?? 0}");
                // LoggingService.LogDebug($"RefreshLocations: ���������� �����������: {LocationIndicators.Count}");
                
                // �� �������� RefreshView() - ��� ������� ����������� ������!
                // ������ ����� ��������� ������ ����������� ����������
                
                // Update the location indicators without using reflection
                // LoggingService.LogDebug("RefreshLocations: �������� UpdateLocationIndicators");
                UpdateLocationIndicators();
                
                // Force update of current location data  
                // LoggingService.LogDebug("RefreshLocations: �������� UpdateLocationDetails");
                UpdateLocationDetails();
                
                // Notify property changes for UI-related properties
                // LoggingService.LogDebug("RefreshLocations: ��������� UI ��������");
                OnPropertyChanged(nameof(CurrentLocation));
                OnPropertyChanged(nameof(LocationDetailsText));
                OnPropertyChanged(nameof(CanTravelToLocation));
                OnPropertyChanged(nameof(CanFightHero));
                OnPropertyChanged(nameof(CanFightMobs));
                
                // LoggingService.LogDebug("RefreshLocations: ���������� ��������� �������");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error in RefreshLocations: {ex.Message}", ex);
            }
            
            // LoggingService.LogDebug("=== RefreshLocations: ����� ===");
        }

        // ���������� ����� ��� ����������� ������� �������
        private void DiagnoseLocationOrder()
        {
            // LoggingService.LogDebug("=== ����������� ������� ������� ===");
            
            if (_gameState.Locations == null || _gameState.Locations.Count == 0)
            {
                LoggingService.LogError("ERROR: ������� �� ����������������!", null);
                return;
            }

            // LoggingService.LogDebug($"����� �������: {_gameState.Locations.Count}");
            // LoggingService.LogDebug($"������� ������: {_gameState.CurrentLocationIndex}");
            
            for (int i = 0; i < _gameState.Locations.Count; i++)
            {
                var location = _gameState.Locations[i];
                // LoggingService.LogDebug($"������ {i}: {location.Name} ({location.Type}) -> {location.SpritePath}");
                
                // ��������� ������������ SpritePath � Type
                string expectedPath = AssetPaths.Locations.GetLocationPath(location.Type.ToString().ToLower());
                if (location.SpritePath != expectedPath)
                {
                    // LoggingService.LogDebug($"  WARNING: SpritePath mismatch! Expected: {expectedPath}, Actual: {location.SpritePath}");
                }
            }
            
            if (_gameState.CurrentLocation != null)
            {
                // LoggingService.LogDebug($"������� �������: {_gameState.CurrentLocation.Name} ({_gameState.CurrentLocation.Type})");
                // LoggingService.LogDebug($"������� SpritePath: {_gameState.CurrentLocation.SpritePath}");
                
                // ��������� ������������ ������� �������
                string expectedCurrentPath = AssetPaths.Locations.GetLocationPath(_gameState.CurrentLocation.Type.ToString().ToLower());
                if (_gameState.CurrentLocation.SpritePath != expectedCurrentPath)
                {
                    // LoggingService.LogDebug($"  WARNING: Current location SpritePath mismatch! Expected: {expectedCurrentPath}");
                }
            }
            else
            {
                LoggingService.LogError("ERROR: CurrentLocation ����� null!", null);
            }
            
            // LoggingService.LogDebug("=== ����� ����������� ===");
        }
    }
} 
