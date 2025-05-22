using System;
using System.Windows;
using System.Windows.Controls;
using SketchBlade.Models;
using SketchBlade.ViewModels;
using SketchBlade.Views;
using SketchBlade.Services;
using System.Threading.Tasks;
using System.Reflection;
using System.Windows.Input;
using System.IO;

namespace SketchBlade;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private MainViewModel _mainViewModel;
    private InventoryViewModel _inventoryViewModel;
    private MapViewModel _mapViewModel;
    private BattleViewModel _battleViewModel;
    private SettingsViewModel _settingsViewModel;
    private ScreenTransitionService _transitionService;

    public MainWindow()
    {
        try
        {
            // Инициализируем папки с изображениями перед созданием любых визуальных элементов
            Console.WriteLine("Initializing image directories...");
            
            // Инициализация папок с изображениями
            Helpers.ImageHelper.InitializeDirectories();
            
            // Проверка наличия папок Assets/Images и исходных изображений
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            if (!Directory.Exists(Path.Combine(basePath, "Assets/Images")))
            {
                MessageBox.Show("Папка Assets/Images не найдена. Программа может работать некорректно.", 
                    "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
                        
            // Инициализируем новый сервис анимации
            _transitionService = new ScreenTransitionService();

            // Initialize main view model first
            _mainViewModel = new MainViewModel(screenName => NavigateToScreen(screenName));
            
            // Set the DataContext before InitializeComponent to ensure it's available during XAML parsing
            DataContext = _mainViewModel;
            
            // Initialize the component after setting DataContext
            InitializeComponent();
            
            // Initialize language service
            InitializeLanguageService();
            
            Console.WriteLine($"MainWindow DataContext set to MainViewModel, CurrentScreen = {_mainViewModel.CurrentScreen}");
            
            // Use the InventoryViewModel from MainViewModel instead of creating a new instance
            _inventoryViewModel = _mainViewModel.InventoryViewModel;
            
            // Initialize other view models with the game state
            _mapViewModel = new MapViewModel(_mainViewModel.GameState, 
                screenName => NavigateToScreen(screenName));
            _battleViewModel = new BattleViewModel(_mainViewModel.GameState, 
                screenName => NavigateToScreen(screenName));
            _settingsViewModel = new SettingsViewModel(_mainViewModel.GameState,
                screenName => NavigateToScreen(screenName));

            // Navigate to MainMenuView by default (synchronously on startup)
            InitialNavigation();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in MainWindow constructor: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                Console.WriteLine($"Inner stack trace: {ex.InnerException.StackTrace}");
            }
            
            MessageBox.Show($"Произошла ошибка при инициализации приложения: {ex.Message}", 
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // Initialize language service
    private void InitializeLanguageService()
    {
        try
        {
            // Force language service to initialize
            LanguageService.CurrentLanguage = LanguageService.CurrentLanguage;
            
            // Debug translations
            LanguageService.DebugTranslations();
            
            // Force UI update
            RefreshCurrentScreen();
            
            Console.WriteLine("Language service initialized");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error initializing language service: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }

    // Initial navigation method
    private void InitialNavigation()
    {
        // Установим текущий экран как MainMenu
        _mainViewModel.CurrentScreen = "MainMenuView";
        Console.WriteLine($"InitialNavigation: Current screen set to {_mainViewModel.CurrentScreen}");
        
        UserControl screen = new MainMenuView { DataContext = _mainViewModel };
        
        // Проверяем, что DataContext установлен
        if (screen.DataContext == null)
        {
            Console.WriteLine("ERROR: DataContext is null for MainMenuView in InitialNavigation");
            screen.DataContext = _mainViewModel;
        }
        
        ContentFrame.Content = screen;
        Console.WriteLine("MainMenuView loaded into ContentFrame");
    }

    // Navigate to the specified screen using async with transitions
    public async Task NavigateToScreenAsync(string screenName)
    {
        Console.WriteLine($"NavigateToScreenAsync вызван для {screenName}");
        UserControl? screen = null;

        switch (screenName)
        {
            case "MainMenuView":
                screen = new MainMenuView { DataContext = _mainViewModel };
                Console.WriteLine("Создан экземпляр MainMenuView");
                _mainViewModel.GameState.CurrentScreenViewModel = _mainViewModel;
                break;
            case "InventoryView":
                screen = new InventoryView { DataContext = _inventoryViewModel };
                Console.WriteLine($"Created InventoryView with DataContext = {_inventoryViewModel?.GetType().Name}");
                _mainViewModel.GameState.CurrentScreenViewModel = _inventoryViewModel;
                break;
            case "WorldMapView":
                screen = new WorldMapView { DataContext = _mapViewModel };
                _mainViewModel.GameState.CurrentScreenViewModel = _mapViewModel;
                break;
            case "BattleView":
                screen = new BattleView { DataContext = _battleViewModel };
                _mainViewModel.GameState.CurrentScreenViewModel = _battleViewModel;
                break;
            case "SettingsView":
                screen = new SettingsView { DataContext = _settingsViewModel };
                _mainViewModel.GameState.CurrentScreenViewModel = _settingsViewModel;
                break;
            default:
                break;
        }

        if (screen != null)
        {
            Console.WriteLine("Начинаем переход к новому экрану");
            // Используем анимацию для перехода
            await _transitionService.FadeTransitionAsync(ContentFrame, screen);
            Console.WriteLine("Переход к новому экрану завершен");
            
            // Устанавливаем фокус на экран после перехода
            if (screenName == "InventoryView" && screen is InventoryView inventoryView)
            {
                Console.WriteLine("Пытаемся передать фокус на InventoryView после навигации");
                inventoryView.Focusable = true;
                
                // Установка фокуса с небольшой задержкой
                await Task.Delay(100);
                inventoryView.Focus();
                
                // Логируем статус фокуса
                Console.WriteLine($"InventoryView установил фокус: {inventoryView.IsFocused}");
                Console.WriteLine($"InventoryView IsKeyboardFocused: {inventoryView.IsKeyboardFocused}");
            }
        }
        else
        {
            Console.WriteLine($"ОШИБКА: screen == null для {screenName}");
        }
    }

    // Navigate to the specified game screen enum
    public void NavigateToScreen(GameScreen screen)
    {
        // Map the game screen to view name
        string screenName;
        Console.WriteLine($"NavigateToScreen(GameScreen) вызван с параметром {screen}");
        
        switch (screen)
        {
            case GameScreen.MainMenu:
                screenName = "MainMenuView";
                break;
            case GameScreen.Inventory:
                screenName = "InventoryView";
                break;
            case GameScreen.WorldMap:
                screenName = "WorldMapView";
                break;
            case GameScreen.Battle:
                screenName = "BattleView";
                break;
            case GameScreen.Settings:
                screenName = "SettingsView";
                break;
            default:
                screenName = "InventoryView";
                break;
        }
        
        // Use the string-based navigation method
        NavigateToScreen(screenName);
    }
    
    // Navigate to the specified screen
    public void NavigateToScreen(string screenName)
    {
        Console.WriteLine($"NavigateToScreen called for {screenName}");

        try
        {
            // Handle special case: Going from battle to world map
            bool isFromBattleToWorldMap = _mainViewModel.CurrentScreen == "BattleView" && screenName == "WorldMapView";
            if (isFromBattleToWorldMap)
            {
                Console.WriteLine("Special case: Transitioning from Battle to WorldMap");
                
                // 1. Принудительно устанавливаем CurrentScreen
                _mainViewModel.CurrentScreen = "WorldMapView";
                
                // 2. Немедленно скрываем любой активный BattleView
                if (ContentFrame.Content is UserControl currentView)
                {
                    // Скрываем любое текущее содержимое, особенно если это BattleView
                    currentView.Visibility = Visibility.Collapsed;
                    Console.WriteLine($"Set visibility of current view ({currentView.GetType().Name}) to Collapsed");
                }
                
                // 3. Создаем новое представление карты мира напрямую
                var worldMapView = new WorldMapView { DataContext = _mapViewModel };
                worldMapView.Visibility = Visibility.Visible;
                worldMapView.Opacity = 1.0;
                Console.WriteLine("Created new WorldMapView with full visibility and opacity");
                
                // 4. Устанавливаем содержимое напрямую без использования анимации
                ContentFrame.Content = worldMapView;
                ContentFrame.UpdateLayout();
                worldMapView.UpdateLayout();
                
                // Выводим сообщение об успешном переходе
                Console.WriteLine("Successfully transitioned to WorldMapView from BattleView (direct approach)");
                
                // Переход завершен - выходим из метода
                return;
            }

            // Standard navigation for other screens...
            // First, check if the game is in a compatible state for the requested navigation
            bool canNavigate = true;
            string targetScreen = "";

            switch (screenName)
            {
                case "MainMenuView":
                    targetScreen = "MainMenuView"; 
                    break;
                case "WorldMapView":
                    targetScreen = "WorldMapView";
                    break;
                case "InventoryView":
                    targetScreen = "InventoryView";
                    break;
                case "BattleView":
                    targetScreen = "BattleView";
                    break;
                case "SettingsView":
                    targetScreen = "SettingsView";
                    break;
                default:
                    Console.WriteLine($"Warning: Unknown screen name: {screenName}");
                    canNavigate = false;
                    break;
            }

            if (!canNavigate)
            {
                Console.WriteLine($"Cannot navigate to {screenName} - invalid screen name");
                return;
            }

            // Update the current screen in the view model if needed
            if (!string.IsNullOrEmpty(targetScreen) && _mainViewModel.CurrentScreen != targetScreen)
            {
                Console.WriteLine($"Updating view model to screen: {targetScreen}");
                _mainViewModel.CurrentScreen = targetScreen;
            }

            // Create the appropriate view based on the screen name
            UserControl view;

            // Выполняем навигацию в зависимости от указанного экрана
            switch (screenName)
            {
                case "MainMenuView":
                    view = new MainMenuView { DataContext = _mainViewModel };
                    _mainViewModel.GameState.CurrentScreenViewModel = _mainViewModel;
                    break;
                case "WorldMapView":
                    view = new WorldMapView { DataContext = _mapViewModel };
                    _mainViewModel.GameState.CurrentScreenViewModel = _mapViewModel;
                    break;
                case "InventoryView":
                    view = new InventoryView { DataContext = _inventoryViewModel };
                    _mainViewModel.GameState.CurrentScreenViewModel = _inventoryViewModel;
                    break;
                case "BattleView":
                    view = new BattleView { DataContext = _battleViewModel };
                    _mainViewModel.GameState.CurrentScreenViewModel = _battleViewModel;
                    break;
                case "SettingsView":
                    view = new SettingsView { DataContext = _settingsViewModel };
                    _mainViewModel.GameState.CurrentScreenViewModel = _settingsViewModel;
                    break;
                default:
                    Console.WriteLine($"Cannot navigate to {screenName} - invalid screen name (fallback)");
                    return;
            }

            // Анимированный переход для обычных случаев
            Console.WriteLine($"Performing animated transition to {screenName}");
            _transitionService.FadeTransitionAsync(ContentFrame, view).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error navigating to {screenName}: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            
            // Try emergency fallback navigation
            try
            {
                UserControl view;
                
                switch (screenName)
                {
                    case "MainMenuView":
                        view = new MainMenuView { DataContext = _mainViewModel };
                        break;
                    case "WorldMapView":
                        view = new WorldMapView { DataContext = _mapViewModel };
                        break;
                    case "InventoryView":
                        view = new InventoryView { DataContext = _inventoryViewModel };
                        break;
                    case "BattleView":
                        view = new BattleView { DataContext = _battleViewModel };
                        break;
                    case "SettingsView":
                        view = new SettingsView { DataContext = _settingsViewModel };
                        break;
                    default:
                        return;
                }
                
                // Direct content setting without animation
                ContentFrame.Content = view;
                view.Opacity = 1.0;
                
                Console.WriteLine($"Emergency navigation to {screenName} completed");
            }
            catch (Exception fallbackEx)
            {
                Console.WriteLine($"Critical navigation error: {fallbackEx.Message}");
            }
        }
    }
    
    private object GetViewModelForScreen(string screenName)
    {
        switch (screenName)
        {
            case "MainMenuView":
                return _mainViewModel;
            case "InventoryView":
                return _inventoryViewModel;
            case "WorldMapView":
                return _mapViewModel;
            case "BattleView":
                return _battleViewModel;
            case "SettingsView":
                return _settingsViewModel;
            default:
                return _mainViewModel;
        }
    }
    
    // Method to refresh the current screen without navigation
    public void RefreshCurrentScreen()
    {
        try 
        {
            Console.WriteLine("RefreshCurrentScreen: Refreshing current screen data");
            
            // Get the current UI content
            if (ContentFrame.Content is UserControl currentView)
            {
                string viewName = currentView.GetType().Name;
                Console.WriteLine($"Current view is {viewName}");
                
                // Force PropertyChanged notifications on the appropriate ViewModel
                switch (viewName)
                {
                    case "InventoryView":
                        _inventoryViewModel.ForceUIUpdate();
                        break;
                    case "WorldMapView":
                        // Tell the map view model to refresh
                        _mapViewModel.RefreshLocations();
                        break;
                    case "BattleView":
                        // Already being called from BattleViewModel, so just update layout
                        currentView.UpdateLayout();
                        break;
                    case "SettingsView":
                        // Settings may not need special refresh
                        break;
                }
                
                // Force UI update
                currentView.UpdateLayout();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in RefreshCurrentScreen: {ex.Message}");
        }
    }
}