using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using SketchBlade.Models;
using SketchBlade.Services;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using System.IO;

namespace SketchBlade.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private GameData _gameState = new GameData();
        
        public GameData GameData => _gameState;
        
        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
        private string _currentScreen = "MainMenuView";
        public string CurrentScreen
        {
            get => _currentScreen;
            set
            {
                if (_currentScreen != value)
                {
                    _currentScreen = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsMainMenuVisible));
                    OnPropertyChanged(nameof(IsInventoryVisible));
                    OnPropertyChanged(nameof(IsBattleVisible));
                    OnPropertyChanged(nameof(IsMapVisible));
                    OnPropertyChanged(nameof(IsShopVisible));
                }
            }
        }
        
        public bool HasSaveGame => _gameState.HasSaveGame;
        
        // Screen visibility properties
        public bool IsMainMenuVisible => CurrentScreen == "MainMenuView";
        public bool IsInventoryVisible => CurrentScreen == "InventoryView";
        public bool IsBattleVisible => CurrentScreen == "BattleView";
        public bool IsMapVisible => CurrentScreen == "MapView";
        public bool IsShopVisible => CurrentScreen == "ShopView";
        
        // Command to start a new game
        private ICommand? _startNewGameCommand;
        public ICommand StartNewGameCommand => _startNewGameCommand ??= new RelayCommand(
            _ => StartNewGame(),
            _ => true,
            "StartNewGameCommand");
        
        // Command to load a saved game
        private ICommand? _loadGameCommand;
        public ICommand LoadGameCommand => _loadGameCommand ??= new RelayCommand(
            _ => LoadGame(),
            _ => HasSaveGame,
            "LoadGameCommand");
        
        // Command to navigate screens
        private ICommand? _navigateCommand;
        public ICommand NavigateCommand => _navigateCommand ??= new RelayCommand(
            param => {
                // LoggingService.LogDebug($"NavigateCommand ����������� � ����������: {param?.ToString() ?? "null"}");
                Navigate(param?.ToString() ?? string.Empty);
            },
            _ => {
                // LoggingService.LogDebug("NavigateCommand.CanExecute �����������");
                return true;
            },
            "NavigateCommand");
        
        // Command to start battle with mobs
        private ICommand? _battleWithMobsCommand;
        public ICommand BattleWithMobsCommand => _battleWithMobsCommand ??= new RelayCommand(
            _ => StartBattleWithMobs(),
            _ => true,
            "BattleWithMobsCommand");
        
        // Command to start battle with hero
        private ICommand? _battleWithHeroCommand;
        public ICommand BattleWithHeroCommand => _battleWithHeroCommand ??= new RelayCommand(
            _ => StartBattleWithHero(),
            _ => true,
            "BattleWithHeroCommand");
        
        private readonly Action<string> _navigateAction;
        
        // Commands
        public ICommand NewGameCommand { get; private set; } = null!;
        public ICommand ContinueGameCommand { get; private set; } = null!;
        public ICommand OptionsCommand { get; private set; } = null!;
        public ICommand ExitGameCommand { get; private set; } = null!;
        public ICommand SaveGameCommand { get; private set; } = null!;
        public ICommand HomeCommand { get; private set; } = null!;
        public ICommand InventoryCommand { get; private set; } = null!;
        public ICommand BattleCommand { get; private set; } = null!;
        public ICommand ShopCommand { get; private set; } = null!;
        public ICommand RefreshUICommand { get; private set; } = null!;
        
        private InventoryViewModel _inventoryViewModel;
        public InventoryViewModel InventoryViewModel 
        { 
            get => _inventoryViewModel; 
            private set 
            { 
                if (_inventoryViewModel != value)
                {
                    _inventoryViewModel = value; 
                    OnPropertyChanged(); 
                }
            }
        }
        
        public MainViewModel(Action<string> navigateAction)
        {
            // LoggingService.LogDebug("=== ������������� MainViewModel ===");
            _navigateAction = navigateAction ?? throw new ArgumentNullException(nameof(navigateAction));
            // LoggingService.LogDebug("navigateAction ����������");
            _inventoryViewModel = new InventoryViewModel(_gameState);
            // LoggingService.LogDebug("InventoryViewModel ������");
            InitializeViewModel(navigateAction);
            // LoggingService.LogDebug("=== MainViewModel ��������������� ===");
        }
        
        private void InitializeViewModel(Action<string> navigateAction)
        {
            try
            {
                // ���������, ��������� �� �������� ���������
                if (navigateAction == null)
                {
                    LoggingService.LogError("navigateAction is null!", null);
                    return;
                }
                
                // Initialize _gameState if needed
                if (_gameState == null)
                {
                    _gameState = new GameData();
                    // LoggingService.LogDebug("���������������� ������� ������� ��� UI: {_gameState.Locations.Count} �������");
                }
                
                // Create commands
                InitializeCommands();
                
                // Create delegate with proper type
                Action<string> navigateToViewAction = screenName => NavigateToView(screenName);
                
                // Create the view models with delegate for navigation
                InventoryViewModel = new InventoryViewModel(_gameState);
                
                // Register the ViewModel in Application Resources
                if (Application.Current != null)
                {
                    // LoggingService.LogDebug("������������ InventoryViewModel � Application.Resources");
                    Application.Current.Resources["InventoryViewModel"] = InventoryViewModel;
                    
                    // Also register the GameData
                    if (!Application.Current.Resources.Contains("GameData"))
                    {
                        // LoggingService.LogDebug("������������ GameData � Application.Resources");
                        Application.Current.Resources["GameData"] = _gameState;
                    }
                }
                
                // Set initial screen
                CurrentScreen = "MainMenuView";
                
                // Subscribe to GameData property changes
                _gameState.PropertyChanged += GameState_PropertyChanged;
                
                // Check for existing save game
                _gameState.CheckForSaveGame();
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"������ ������������� MainViewModel: {ex.Message}", ex);
            }
        }
        
        private void InitializeCommands()
        {
            try
            {
                // Ensure we're on the UI thread for all UI operations
                if (!Application.Current.Dispatcher.CheckAccess())
                {
                    Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => InitializeCommands()));
                    return;
                }

                // All commands will use explicit lambda syntax for consistency
                Action<object?> newGameAction = _ => StartNewGame();
                NewGameCommand = new RelayCommand(newGameAction, null, "NewGameCommand");
                
                ContinueGameCommand = new RelayCommand(
                    _ => ContinueGame(), 
                    _ => HasSaveGame, 
                    "ContinueGameCommand");
                
                Action<object?> saveGameAction = _ => SaveGame(_);
                SaveGameCommand = new RelayCommand(saveGameAction, null, "SaveGameCommand");
                
                HomeCommand = new RelayCommand(_ => _navigateAction("MainMenuView"), null, "HomeCommand");
                InventoryCommand = new RelayCommand(_ => _navigateAction("InventoryView"), null, "InventoryCommand");
                BattleCommand = new RelayCommand(_ => _navigateAction("BattleView"), null, "BattleCommand");
                ShopCommand = new RelayCommand(_ => _navigateAction("ShopView"), null, "ShopCommand");
                OptionsCommand = new RelayCommand(_ => _navigateAction("SettingsView"), null, "OptionsCommand");
                
                Action<object?> exitGameAction = _ => ExitGame();
                ExitGameCommand = new RelayCommand(exitGameAction, null, "ExitGameCommand");
                
                RefreshUICommand = new RelayCommand(_ => RefreshUI(), null, "RefreshUICommand");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"������ ������������� ������: {ex.Message}", ex);
            }
        }
        
        private void StartNewGame()
        {
            try
            {
                // Ensure we're on the UI thread for all UI operations
                if (!Application.Current.Dispatcher.CheckAccess())
                {
                    Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => StartNewGame()));
                    return;
                }

                // LoggingService.LogInfo("=== ������ �������� ����� ���� ===");

                // �������������� ����
                _gameState.Initialize();
                
                // �����������: ������� ���������� ������ ���������������
                // ������� ������ ����� ���������������� ������������� ��� ������ ���������
                // LoggingService.LogInfo("���� ����������������, ��������� � ������ ���������");
                
                // ������������� ������� �����
                // LoggingService.LogDebug("������������� CurrentScreen = InventoryView");
                CurrentScreen = "InventoryView";
                // LoggingService.LogDebug($"CurrentScreen ����������: {CurrentScreen}");
                
                // ���������
                // LoggingService.LogDebug("�������� _navigateAction � InventoryView");
                _navigateAction?.Invoke("InventoryView");
                // LoggingService.LogDebug("_navigateAction ��������");
                
                // ���������, ��� ��������� ������ �������
                // LoggingService.LogDebug($"CurrentScreen ����� ���������: {CurrentScreen}");
                
                // ���� ��������� �� ���������, ��������� ��� ��� ����� Dispatcher
                if (CurrentScreen != "InventoryView")
                {
                    // LoggingService.LogDebug("��������� �� ���������, ������� ����� Dispatcher");
                    Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                        // LoggingService.LogDebug("��������� ������� ��������� ����� Dispatcher");
                        CurrentScreen = "InventoryView";
                        _navigateAction?.Invoke("InventoryView");
                        // LoggingService.LogDebug($"CurrentScreen ����� ��������� ���������: {CurrentScreen}");
                    }));
                }
                
                // LoggingService.LogInfo("New game started successfully");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error starting new game: {ex.Message}", ex);
                MessageBox.Show($"������ ��� �������� ����� ����: {ex.Message}", "������", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void LoadGame()
        {
            try
            {
                // Ensure we're on the UI thread for all UI operations
                if (!Application.Current.Dispatcher.CheckAccess())
                {
                    Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => LoadGame()));
                    return;
                }

                // LoggingService.LogDebug("LoadGame ������ - ������������ �� ����� Inventory");
                _gameState.LoadGame();
                
                // ������� ��������� �������� CurrentScreen, ����� �������� ���������
                // LoggingService.LogDebug($"������� ����� �� ���������: {CurrentScreen}");
                CurrentScreen = "InventoryView";
                // LoggingService.LogDebug($"������� ����� ����� ���������: {CurrentScreen}");
                
                // ���������� _navigateAction ������ ������ ��������� CurrentScreen
                _navigateAction("InventoryView");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"������ �������� ����: {ex.Message}", ex);
            }
        }
        
        private void Navigate(string screenName)
        {
            try
            {
                // Ensure we're on the UI thread for all UI operations
                if (!Application.Current.Dispatcher.CheckAccess())
                {
                    Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => Navigate(screenName)));
                    return;
                }

                // Update current screen
                if (!string.IsNullOrEmpty(screenName))
                {
                    CurrentScreen = screenName;
                }
                
                // Call navigation action
                if (_navigateAction != null)
                {
                    _navigateAction(screenName);
                }
                else
                {
                    LoggingService.LogError("������: _navigateAction ��������� null");
                    // Fallback: Try to navigate using MainWindow directly
                    var mainWindow = Application.Current.MainWindow as MainWindow;
                    if (mainWindow != null)
                    {
                        mainWindow.NavigateToScreen(screenName);
                    }
                    else
                    {
                        LoggingService.LogError("������: ���������� ��������� - _navigateAction ��������� null � MainWindow ����������");
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"������ ���������: {ex.Message}", ex);
            }
        }
        
        private void StartBattleWithMobs()
        {
            try
            {
                LoggingService.LogInfo("Starting battle with mobs");

                // Start mob battle
                if (_gameState.Inventory.Items.Count > 0)
                {
                    // Show loading animation
                    Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => StartBattleWithMobs()));
                }

                if (_gameState.CurrentLocation != null)
                {
                    _gameState.StartBattleWithMobs(_gameState.CurrentLocation);
                }
                
                // Navigate to battle screen immediately
                NavigateToScreen("BattleView");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error starting battle with mobs: {ex.Message}", ex);
            }
        }
        
        private void StartBattleWithHero()
        {
            try
            {
                LoggingService.LogInfo("Starting battle with hero");

                // Get current location for hero battle
                if (_gameState.CurrentLocation != null)
                {
                    // Start boss battle with current location
                    _gameState.StartBattleWithHero(_gameState.CurrentLocation);
                    
                    // Navigate to battle screen
                    NavigateToScreen("BattleView");
                }
                else
                {
                    LoggingService.LogError("Cannot start hero battle: Current location is null");
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error starting battle with hero: {ex.Message}", ex);
            }
        }
        
        private void ContinueGame()
        {
            // Ensure we're on the UI thread for all UI operations
            if (!Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => ContinueGame()));
                return;
            }

            // ��������� ����
            _gameState.LoadGame();
            
            // ������������� ������� �����
            CurrentScreen = "InventoryView";
            
            // ���������
            _navigateAction?.Invoke("InventoryView");
        }
        
        private void ShowOptions()
        {
            // Ensure we're on the UI thread for all UI operations
            if (!Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => ShowOptions()));
                return;
            }

            // Navigate to settings screen
            
            // Set current screen
            CurrentScreen = "SettingsView";
            
            // Navigation
            _navigateAction?.Invoke("SettingsView");
        }
        
        private void ExitGame()
        {
            try
            {
                // Ensure we're on the UI thread for all UI operations
                if (!Application.Current.Dispatcher.CheckAccess())
                {
                    Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => ExitGame()));
                    return;
                }

                // LoggingService.LogDebug("ExitGame ������");
                // Save game before exit
                _gameState.SaveGame();
                
                // In a real application, you would close the application here
                // For now, we'll use Environment.Exit
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"������ ������ �� ����: {ex.Message}", ex);
            }
        }

        private void NavigateToScreen(string screenName)
        {
            try
            {
                // Ensure we're on the UI thread for all UI operations
                if (!Application.Current.Dispatcher.CheckAccess())
                {
                    Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => NavigateToScreen(screenName)));
                    return;
                }

                // Set the current screen
                CurrentScreen = screenName;
            
                // Call the navigate action
                _navigateAction?.Invoke(screenName);
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"������ � NavigateToScreen: {ex.Message}", ex);
            }
        }

        private void GameState_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            try
            {
                // Ensure we're on the UI thread for all UI operations
                if (!Application.Current.Dispatcher.CheckAccess())
                {
                    Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => GameState_PropertyChanged(sender, e)));
                    return;
                }

                if (e.PropertyName == nameof(GameData.HasSaveGame))
                {
                    OnPropertyChanged(nameof(HasSaveGame));
                }
                else if (e.PropertyName == nameof(GameData.CurrentScreen))
                {
                    OnPropertyChanged(nameof(CurrentScreen));
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"������ � GameState_PropertyChanged: {ex.Message}", ex);
            }
        }

        private void SaveGame(object? parameter)
        {
            try
            {
                // Ensure we're on the UI thread for all UI operations
                if (!Application.Current.Dispatcher.CheckAccess())
                {
                    Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => SaveGame(parameter)));
                    return;
                }

                // LoggingService.LogDebug("SaveGame ������");
                _gameState.SaveGame();
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"������ ���������� ����: {ex.Message}", ex);
            }
        }

        // Helper method for navigation
        private void NavigateToView(string screenName)
        {
            try
            {
                // Ensure we're on the UI thread for all UI operations
                if (!Application.Current.Dispatcher.CheckAccess())
                {
                    Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => NavigateToView(screenName)));
                    return;
                }

                _navigateAction(screenName);
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"������ � NavigateToView: {ex.Message}", ex);
            }
        }
        
        private void RefreshUI()
        {
            try
            {
                // Ensure we're on the UI thread for all UI operations
                if (!Application.Current.Dispatcher.CheckAccess())
                {
                    Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => RefreshUI()));
                    return;
                }

                // LoggingService.LogDebug("RefreshUI: �������������� ���������� UI");
                
                // ��������� InventoryViewModel ���� �� ����������
                if (InventoryViewModel != null)
                {
                    InventoryViewModel.RefreshAllSlots();
                    InventoryViewModel.ForceUIUpdate();
                    
                    // ��������� ����� ���� �� ���������������
                    if (InventoryViewModel.SimplifiedCraftingViewModel != null)
                    {
                        InventoryViewModel.SimplifiedCraftingViewModel.RefreshAvailableRecipes();
                    }
                }
                
                // ��������� ��� �������� MainViewModel
                OnPropertyChanged(nameof(CurrentScreen));
                OnPropertyChanged(nameof(IsMainMenuVisible));
                OnPropertyChanged(nameof(IsInventoryVisible));
                OnPropertyChanged(nameof(IsBattleVisible));
                OnPropertyChanged(nameof(IsMapVisible));
                OnPropertyChanged(nameof(IsShopVisible));
                OnPropertyChanged(nameof(HasSaveGame));
                
                // ������������� ��������� MainWindow ���� ��������
                if (Application.Current.MainWindow is MainWindow mainWindow)
                {
                    mainWindow.RefreshCurrentScreen();
                }
                
                // LoggingService.LogDebug("RefreshUI: ���������� UI ���������");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"RefreshUI: {ex.Message}", ex);
            }
        }
    }
} 
