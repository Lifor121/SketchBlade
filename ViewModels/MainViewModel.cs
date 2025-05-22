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

namespace SketchBlade.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private GameState _gameState = new GameState();
        
        public GameState GameState => _gameState;
        
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
                }
            }
        }
        
        public bool HasSaveGame => _gameState.HasSaveGame;
        
        // Command to start a new game
        private ICommand? _startNewGameCommand;
        public ICommand StartNewGameCommand => _startNewGameCommand ??= new RelayCommand(
            param => StartNewGame()
        );
        
        // Command to load a saved game
        private ICommand? _loadGameCommand;
        public ICommand LoadGameCommand => _loadGameCommand ??= new RelayCommand(
            param => LoadGame(),
            param => HasSaveGame
        );
        
        // Command to navigate screens
        private ICommand? _navigateCommand;
        public ICommand NavigateCommand => _navigateCommand ??= new RelayCommand(
            param => Navigate(param?.ToString() ?? "MainMenuView")
        );
        
        // Command to start battle with mobs
        private ICommand? _battleWithMobsCommand;
        public ICommand BattleWithMobsCommand => _battleWithMobsCommand ??= new RelayCommand(
            param => StartBattleWithMobs()
        );
        
        // Command to start battle with hero
        private ICommand? _battleWithHeroCommand;
        public ICommand BattleWithHeroCommand => _battleWithHeroCommand ??= new RelayCommand(
            param => StartBattleWithHero()
        );
        
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
        
        private InventoryViewModel _inventoryViewModel;
        public InventoryViewModel InventoryViewModel 
        { 
            get => _inventoryViewModel; 
            private set 
            { 
                _inventoryViewModel = value; 
                // Register the ViewModel in Application.Resources for global access
                if (Application.Current != null && _inventoryViewModel != null)
                {
                    Application.Current.Resources["InventoryViewModel"] = _inventoryViewModel;
                }
                OnPropertyChanged(); 
            }
        }
        
        public MainViewModel(Action<string> navigateAction)
        {
            _navigateAction = navigateAction;
            
            // Ensure we're on the UI thread for all UI operations
            if (Application.Current != null && !Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => {
                    InitializeViewModel(navigateAction);
                }));
                return;
            }
            
            InitializeViewModel(navigateAction);
        }
        
        private void InitializeViewModel(Action<string> navigateAction)
        {
            _gameState = new GameState();
            
            // Create commands
            InitializeCommands();
            
            // Create delegate with proper type
            Action<string> navigateToViewAction = screenName => NavigateToView(screenName);
            
            // Create the view models with delegate for navigation
            InventoryViewModel = new InventoryViewModel(_gameState, navigateToViewAction);
            
            // Register the ViewModel in Application Resources
            if (Application.Current != null)
            {
                Console.WriteLine("Registering InventoryViewModel in Application.Resources");
                Application.Current.Resources["InventoryViewModel"] = InventoryViewModel;
                
                // Also register the GameState
                if (!Application.Current.Resources.Contains("GameState"))
                {
                    Console.WriteLine("Registering GameState in Application.Resources");
                    Application.Current.Resources["GameState"] = _gameState;
                }
            }
            
            // Set initial screen
            CurrentScreen = "MainMenuView";
            
            // Subscribe to GameState property changes
            _gameState.PropertyChanged += GameState_PropertyChanged;
            
            // Check for existing save game
            _gameState.CheckForSaveGame();
        }
        
        private void InitializeCommands()
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
        }
        
        private void StartNewGame()
        {
            // Ensure we're on the UI thread for all UI operations
            if (!Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => StartNewGame()));
                return;
            }

            // Инициализируем игру
            _gameState.Initialize();
            
            // Устанавливаем текущий экран
            CurrentScreen = "InventoryView";
            
            // Навигация
            _navigateAction?.Invoke("InventoryView");
        }
        
        private void LoadGame()
        {
            // Ensure we're on the UI thread for all UI operations
            if (!Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => LoadGame()));
                return;
            }

            Console.WriteLine("LoadGame called - Switching to Inventory screen");
            _gameState.LoadGame();
            
            // Сначала установим значение CurrentScreen, затем выполним навигацию
            Console.WriteLine($"Current screen before change: {CurrentScreen}");
            CurrentScreen = "InventoryView";
            Console.WriteLine($"Current screen after change: {CurrentScreen}");
            
            // Используем _navigateAction вместо просто изменения CurrentScreen
            _navigateAction("InventoryView");
        }
        
        private void Navigate(string screenName)
        {
            // Ensure we're on the UI thread for all UI operations
            if (!Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => Navigate(screenName)));
                return;
            }

            Console.WriteLine($"MainViewModel.Navigate called with screen: {screenName}");
            
            // Update current screen
            if (!string.IsNullOrEmpty(screenName))
            {
                CurrentScreen = screenName;
                Console.WriteLine($"MainViewModel.CurrentScreen updated to {CurrentScreen}");
            }
            
            // Call navigation action
            if (_navigateAction != null)
            {
                Console.WriteLine($"Calling _navigateAction with {screenName}");
                _navigateAction(screenName);
                Console.WriteLine("_navigateAction completed");
            }
            else
            {
                Console.WriteLine("ERROR: _navigateAction is null");
                // Fallback: Try to navigate using MainWindow directly
                var mainWindow = Application.Current.MainWindow as MainWindow;
                if (mainWindow != null)
                {
                    Console.WriteLine("Using MainWindow directly for navigation");
                    mainWindow.NavigateToScreen(screenName);
                }
                else
                {
                    Console.WriteLine("CRITICAL ERROR: Cannot navigate - _navigateAction is null and MainWindow not available");
                }
            }
        }
        
        private void StartBattleWithMobs()
        {
            // Ensure we're on the UI thread for all UI operations
            if (!Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => StartBattleWithMobs()));
                return;
            }

            Console.WriteLine("StartBattleWithMobs called");
            _gameState.StartBattleWithMobs();
            CurrentScreen = "BattleView";
            OnPropertyChanged(nameof(CurrentScreen));
        }
        
        private void StartBattleWithHero()
        {
            // Ensure we're on the UI thread for all UI operations
            if (!Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => StartBattleWithHero()));
                return;
            }

            Console.WriteLine("StartBattleWithHero called");
            _gameState.StartBattleWithHero();
            CurrentScreen = "BattleView";
            OnPropertyChanged(nameof(CurrentScreen));
        }
        
        private void ContinueGame()
        {
            // Ensure we're on the UI thread for all UI operations
            if (!Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => ContinueGame()));
                return;
            }

            // Загружаем игру
            _gameState.LoadGame();
            
            // Устанавливаем текущий экран
            CurrentScreen = "InventoryView";
            
            // Навигация
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
            // Ensure we're on the UI thread for all UI operations
            if (!Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => ExitGame()));
                return;
            }

            Console.WriteLine("ExitGame called");
            // Save game before exit
            _gameState.SaveGame();
            
            // In a real application, you would close the application here
            // For now, we'll use Environment.Exit
            Environment.Exit(0);
        }

        private void NavigateToScreen(string screenName)
        {
            // Ensure we're on the UI thread for all UI operations
            if (!Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => NavigateToScreen(screenName)));
                return;
            }

            try
            {
                // Set the current screen
                CurrentScreen = screenName;
            
                // Call the navigate action
                _navigateAction?.Invoke(screenName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in NavigateToScreen: {ex.Message}");
            }
        }

        private void GameState_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // Ensure we're on the UI thread for all UI operations
            if (!Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => GameState_PropertyChanged(sender, e)));
                return;
            }

            if (e.PropertyName == nameof(GameState.HasSaveGame))
            {
                OnPropertyChanged(nameof(HasSaveGame));
            }
            else if (e.PropertyName == nameof(GameState.CurrentScreen))
            {
                OnPropertyChanged(nameof(CurrentScreen));
            }
        }

        private void SaveGame(object? parameter)
        {
            // Ensure we're on the UI thread for all UI operations
            if (!Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => SaveGame(parameter)));
                return;
            }

            Console.WriteLine("SaveGame called");
            _gameState.SaveGame();
        }

        // Helper method for navigation
        private void NavigateToView(string screenName)
        {
            // Ensure we're on the UI thread for all UI operations
            if (!Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => NavigateToView(screenName)));
                return;
            }

            _navigateAction(screenName);
        }
    }
} 