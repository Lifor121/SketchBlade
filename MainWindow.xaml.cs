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
/// Логика взаимодействия для MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private MainViewModel _mainViewModel;
    private InventoryViewModel _inventoryViewModel;
    private MapViewModel _mapViewModel;
    private BattleViewModel _battleViewModel;
    private SettingsViewModel _settingsViewModel;

    public MainWindow()
    {
        try
        {
            // Инициализация папок с изображениями
            Helpers.ImageHelper.InitializeDirectories();
            
            // Проверка наличия папок Assets/Images
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            if (!Directory.Exists(Path.Combine(basePath, "Assets/Images")))
            {
                MessageBox.Show("Папка Assets/Images не найдена. Программа может работать некорректно.", 
                    "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            
            // Check if ViewModels are already initialized
            if (_mainViewModel != null)
            {
                return;
            }
            
            InitializeComponent();
            
            _mainViewModel = new MainViewModel(screenName => NavigateToScreen(screenName));
            
            DataContext = _mainViewModel;
            
            InitializeLanguageService();
            
            LocalizationService.Instance.LanguageChanged += OnLanguageChanged;
            
            _inventoryViewModel = _mainViewModel.InventoryViewModel;

            InitialNavigation();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Произошла ошибка при инициализации приложения: {ex.Message}", 
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void InitializeLanguageService()
    {
        try
        {
            var currentLanguage = LocalizationService.Instance.CurrentLanguage;
            LocalizationService.Instance.CurrentLanguage = currentLanguage;
            RefreshCurrentScreen();
        }
        catch (Exception ex)
        {
            // Error initializing language service - continue silently
        }
    }

    private void InitialNavigation()
    {
        _mainViewModel.CurrentScreen = "MainMenuView";
        
        UserControl screen = new MainMenuView { DataContext = _mainViewModel };
        
        if (screen.DataContext == null)
        {
            screen.DataContext = _mainViewModel;
        }
        
        ContentFrame.Content = screen;
    }

    public async Task NavigateToScreenAsync(string screenName)
    {
        UserControl? screen = null;

        switch (screenName)
        {
            case "MainMenuView":
                screen = new MainMenuView { DataContext = _mainViewModel };
                break;
            case "InventoryView":
                screen = new InventoryView { DataContext = _inventoryViewModel };
                break;
            case "WorldMapView":
                screen = new WorldMapView { DataContext = _mapViewModel };
                break;
            case "BattleView":
                screen = new BattleView { DataContext = _battleViewModel };
                break;
            case "SettingsView":
                screen = new SettingsView { DataContext = _settingsViewModel };
                break;
            default:
                break;
        }

        if (screen != null)
        {
            await UIService.Instance.FadeTransitionAsync(ContentFrame, screen);
            
            // Установка фокуса для InventoryView
            if (screenName == "InventoryView" && screen is InventoryView inventoryView)
            {
                inventoryView.Focusable = true;
                await Task.Delay(100);
                inventoryView.Focus();
            }
        }
    }

    // Navigate to the specified screen
    public void NavigateToScreen(string screenName)
    {
        try
        {
            
            // Handle special case: Going from battle to world map
            bool isFromBattleToWorldMap = _mainViewModel.CurrentScreen == "BattleView" && screenName == "WorldMapView";
            if (isFromBattleToWorldMap)
            {
                // 1. Принудительно устанавливаем CurrentScreen
                _mainViewModel.CurrentScreen = "WorldMapView";
                
                // 2. Немедленно скрываем любой текущий активный BattleView
                if (ContentFrame.Content is UserControl currentView)
                {
                    // Скрываем любое текущее содержимое, особенно если это BattleView
                    currentView.Visibility = Visibility.Collapsed;
                }
                
                // 3. Создаем MapViewModel если он не существует
                if (_mapViewModel == null)
                {
                    _mapViewModel = new MapViewModel(_mainViewModel.GameData, 
                        screenName => NavigateToScreen(screenName));
                }
                
                // 4. Создаем новое предустановленное представление карты мира напрямую
                var worldMapView = new WorldMapView { DataContext = _mapViewModel };
                worldMapView.Visibility = Visibility.Visible;
                worldMapView.Opacity = 1.0;
                
                // 5. Устанавливаем содержимое напрямую без использования анимации
                ContentFrame.Content = worldMapView;
                ContentFrame.UpdateLayout();
                worldMapView.UpdateLayout();
                
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
                    canNavigate = false;
                    break;
            }

            if (!canNavigate)
            {
                return;
            }

            // Update the MainViewModel's current screen
            _mainViewModel.CurrentScreen = targetScreen;

            // Create the view based on the screen name
            UserControl view;

            // Выполняем навигацию в зависимости от указанного экрана
            switch (screenName)
            {
                case "MainMenuView":
                    view = new MainMenuView { DataContext = _mainViewModel };
                    break;
                case "InventoryView":
                    view = new InventoryView { DataContext = _inventoryViewModel };
                    break;
                case "WorldMapView":
                    // LoggingService.LogDebug("=== НАВИГАЦИЯ НА КАРТУ МИРА ===");
                    // Создаем MapViewModel если он не существует
                    if (_mapViewModel == null)
                    {
                        // LoggingService.LogDebug("Создаем новый MapViewModel");
                        _mapViewModel = new MapViewModel(_mainViewModel.GameData, 
                            screenName => NavigateToScreen(screenName));
                        // LoggingService.LogDebug("MapViewModel создан успешно");
                    }
                    else
                    {
                        // LoggingService.LogDebug("Используем существующий MapViewModel");
                    }
                    
                    // LoggingService.LogDebug("Создаем WorldMapView");
                    view = new WorldMapView { DataContext = _mapViewModel };
                    // LoggingService.LogDebug("WorldMapView создан успешно");
                    
                    // Обновляем представление карты после навигации - используем легкий метод
                    // LoggingService.LogDebug("Вызываем RefreshLocations");
                    _mapViewModel.RefreshLocations();
                    // LoggingService.LogDebug("RefreshLocations завершен");
                    break;
                case "BattleView":
                    // ВСЕГДА создаем новый BattleViewModel для каждого боя
                    // чтобы избежать проблем с переиспользованием старого состояния
                    if (_battleViewModel != null)
                    {
                        _battleViewModel.Dispose();
                    }
                    _battleViewModel = new BattleViewModel(_mainViewModel.GameData, 
                        screenName => NavigateToScreen(screenName));
                    
                    // ВСЕГДА создаем новый BattleView для каждого боя
                    // чтобы избежать проблем с кэшированием DataContext
                    view = new BattleView { DataContext = _battleViewModel };
                    break;
                case "SettingsView":
                    // Создаем SettingsViewModel если он не существует
                    if (_settingsViewModel == null)
                    {
                        _settingsViewModel = new SettingsViewModel(_mainViewModel.GameData,
                            screenName => NavigateToScreen(screenName));
                    }
                    view = new SettingsView { DataContext = _settingsViewModel };
                    break;
                default:
                    return;
            }

            // Анимационный переход для обычных случаев
            UIService.Instance.FadeTransitionAsync(ContentFrame, view).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Error navigating - continue silently
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
            // Принудительно обновляем все ресурсы Application
            if (Application.Current.Resources.Contains("GameData"))
            {
                var GameData = Application.Current.Resources["GameData"] as GameData;
                if (GameData != null)
                {
                    // Принудительно обновляем все элементы по изменению GameData
                    Application.Current.Resources["GameData"] = null;
                    Application.Current.Resources["GameData"] = GameData;
                }
            }
            
            // Get the current UI content
            if (ContentFrame?.Content is UserControl currentView && currentView != null)
            {
                string viewName = currentView.GetType().Name;
                
                // Force PropertyChanged notifications on the appropriate ViewModel
                switch (viewName)
                {
                    case "InventoryView":
                        _inventoryViewModel?.ForceUIUpdate();
                        break;
                    case "WorldMapView":
                        // Tell the map view model to refresh
                        _mapViewModel?.RefreshLocations();
                        break;
                    case "BattleView":
                        // Already being called from BattleViewModel, so just update layout
                        currentView.UpdateLayout();
                        break;
                    case "SettingsView":
                        // Обновляем экран настроек без принудительного сброса DataContext
                        if (_settingsViewModel != null && currentView.DataContext == _settingsViewModel)
                        {
                            // Просто обновляем layout без сброса DataContext
                            currentView.UpdateLayout();
                            currentView.InvalidateVisual();
                        }
                        break;
                    case "MainMenuView":
                        // Обновляем главное меню
                        if (_mainViewModel != null && currentView.DataContext == _mainViewModel)
                        {
                            currentView.UpdateLayout();
                        }
                        break;
                }
                
                // Force UI update for all child elements
                ForceUpdateAllChildren(currentView);
                
                // Force UI update
                currentView.UpdateLayout();
                currentView.InvalidateVisual();
                
                // Обновляем главное окно
                this.UpdateLayout();
                this.InvalidateVisual();
            }
        }
        catch (Exception ex)
        {
            // Error refreshing screen - continue silently
        }
    }
    
    private void ForceUpdateAllChildren(DependencyObject parent)
    {
        try
        {
            if (parent == null) return;
            
            int childCount = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                
                // Если это FrameworkElement, принудительно обновляем его
                if (child is FrameworkElement element)
                {
                    element.UpdateLayout();
                    element.InvalidateVisual();
                    
                    // Если это CoreInventorySlot, обновляем его без принудительного сброса DataContext
                    if (element is SketchBlade.Views.Controls.CoreInventorySlot slot)
                    {
                        // Просто обновляем визуальное отображение без сброса DataContext
                        // Это предотвращает проблемы с привязкой данных
                        slot.InvalidateVisual();
                    }
                }
                
                // Рекурсивно обновляем дочерние элементы
                ForceUpdateAllChildren(child);
            }
        }
        catch (Exception ex)
        {
            // Error updating children - continue silently
        }
    }

    // Handle language changes
    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        try
        {
            // Force UI update
            RefreshCurrentScreen();
            
            // Update navigation buttons
            if (ContentFrame.Content is UserControl currentView)
            {
                currentView.InvalidateVisual();
            }
        }
        catch (Exception ex)
        {
            // Error updating UI after language change - continue silently
        }
    }

    private void MainWindow_KeyDown(object sender, KeyEventArgs e)
    {
        try
        {
            // Горячие клавиши для отладки
            if (e.Key == Key.F1)
            {
                ShowDebugInfo();
                e.Handled = true;
            }
            else if (e.Key == Key.F5)
            {
                RefreshUI();
                e.Handled = true;
            }
            // Добавляем горячую клавишу для очистки логов
            else if (e.Key == Key.L && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
            {
                ClearLogs();
                e.Handled = true;
            }
        }
        catch (Exception ex)
        {
            // Error in key handling - continue silently
        }
    }
    
    private void ClearLogs()
    {
        try
        {
            var result = MessageBox.Show(
                "Очистить файл логов? Это действие нельзя отменить.",
                "Очистка логов",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
                
            if (result == MessageBoxResult.Yes)
            {
                MessageBox.Show("Логи очищены успешно!", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при очистке логов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private void ShowDebugInfo()
    {
        try
        {
            var gameData = Application.Current.Resources["GameData"] as GameData;
            string debugInfo = "=== DEBUG INFO ===\n";
            debugInfo += $"Current Screen: {ContentFrame?.Content?.GetType().Name ?? "None"}\n";
            debugInfo += $"Player Health: {gameData?.Player?.CurrentHealth ?? 0}/{gameData?.Player?.MaxHealth ?? 0}\n";
            
            // Count items in inventory
            int itemCount = 0;
            if (gameData?.Inventory != null)
            {
                itemCount += gameData.Inventory.Items.Count(item => item != null);
                itemCount += gameData.Inventory.QuickItems.Count(item => item != null);
                itemCount += gameData.Inventory.CraftItems.Count(item => item != null);
                if (gameData.Inventory.TrashItem != null) itemCount++;
            }
            
            debugInfo += $"Inventory Items: {itemCount}\n";
            debugInfo += $"Current Location: {gameData?.CurrentLocation?.Name ?? "None"}\n";
            debugInfo += $"Memory Usage: {GC.GetTotalMemory(false) / 1024 / 1024} MB\n";
            
            MessageBox.Show(debugInfo, "Debug Information", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            // Error showing debug info - continue silently
        }
    }
    
    private void RefreshUI()
    {
        try
        {
            RefreshCurrentScreen();
            MessageBox.Show("UI обновлен!", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            // Error refreshing UI - continue silently
        }
    }
    
    private void WorldMapButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_mainViewModel?.NavigateCommand != null)
            {
                _mainViewModel.NavigateCommand.Execute("WorldMapView");
            }
        }
        catch (Exception ex)
        {
            // Error in WorldMapButton_Click - continue silently
        }
    }
} 
