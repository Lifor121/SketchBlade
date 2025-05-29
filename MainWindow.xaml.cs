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
/// ������ �������������� ��� MainWindow.xaml
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
            // Инициализация папок и изображений
            Helpers.ImageHelper.InitializeDirectories();
            
            // Проверка наличия папки Resources/Assets/Images
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            if (!Directory.Exists(Path.Combine(basePath, "Resources/Assets/Images")))
            {
                MessageBox.Show("Папка Resources/Assets/Images не найдена. Возможны ошибки загрузки изображений.", 
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
            MessageBox.Show($"��������� ������ ��� ������������� ����������: {ex.Message}", 
                "������", MessageBoxButton.OK, MessageBoxImage.Error);
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
            
            // ��������� ������ ��� InventoryView
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
                // 1. ������������� ������������� CurrentScreen
                _mainViewModel.CurrentScreen = "WorldMapView";
                
                // 2. ���������� �������� ����� ������� �������� BattleView
                if (ContentFrame.Content is UserControl currentView)
                {
                    // �������� ����� ������� ����������, �������� ���� ��� BattleView
                    currentView.Visibility = Visibility.Collapsed;
                }
                
                // 3. ������� MapViewModel ���� �� �� ����������
                if (_mapViewModel == null)
                {
                    _mapViewModel = new MapViewModel(_mainViewModel.GameData, 
                        screenName => NavigateToScreen(screenName));
                }
                
                // ИСПРАВЛЕНИЕ: Обновляем состояние карты после возвращения с битвы
                _mapViewModel.RefreshView();
                
                // 4.       
                var worldMapView = new WorldMapView { DataContext = _mapViewModel };
                worldMapView.Visibility = Visibility.Visible;
                worldMapView.Opacity = 1.0;
                
                // 5. ������������� ���������� �������� ��� ������������� ��������
                ContentFrame.Content = worldMapView;
                ContentFrame.UpdateLayout();
                worldMapView.UpdateLayout();
                
                // ������� �������� - ������� �� ������
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

            // ��������� ��������� � ����������� �� ���������� ������
            switch (screenName)
            {
                case "MainMenuView":
                    view = new MainMenuView { DataContext = _mainViewModel };
                    break;
                case "InventoryView":
                    view = new InventoryView { DataContext = _inventoryViewModel };
                    break;
                case "WorldMapView":
                    // LoggingService.LogDebug("=== ��������� �� ����� ���� ===");
                    // ������� MapViewModel ���� �� �� ����������
                    if (_mapViewModel == null)
                    {
                        // LoggingService.LogDebug("������� ����� MapViewModel");
                        _mapViewModel = new MapViewModel(_mainViewModel.GameData, 
                            screenName => NavigateToScreen(screenName));
                        // LoggingService.LogDebug("MapViewModel ������ �������");
                    }
                    else
                    {
                        // LoggingService.LogDebug("���������� ������������ MapViewModel");
                    }
                    
                    // LoggingService.LogDebug("������� WorldMapView");
                    view = new WorldMapView { DataContext = _mapViewModel };
                    // LoggingService.LogDebug("WorldMapView ������ �������");
                    
                    // ��������� ������������� ����� ����� ��������� - ���������� ������ �����
                    // LoggingService.LogDebug("�������� RefreshLocations");
                    _mapViewModel.RefreshLocations();
                    // LoggingService.LogDebug("RefreshLocations ��������");
                    break;
                case "BattleView":
                    // ������ ������� ����� BattleViewModel ��� ������� ���
                    // ����� �������� ������� � ������������������ ������� ���������
                    if (_battleViewModel != null)
                    {
                        _battleViewModel.Dispose();
                    }
                    _battleViewModel = new BattleViewModel(_mainViewModel.GameData, 
                        screenName => NavigateToScreen(screenName));
                    
                    // ������ ������� ����� BattleView ��� ������� ���
                    // ����� �������� ������� � ������������ DataContext
                    view = new BattleView { DataContext = _battleViewModel };
                    break;
                case "SettingsView":
                    // ������� SettingsViewModel ���� �� �� ����������
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

            // ������������ ������� ��� ������� �������
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
            if (ContentFrame?.Content is UserControl currentView && currentView != null)
            {
                string viewName = currentView.GetType().Name;
                
                // Force PropertyChanged notifications on the appropriate ViewModel
                switch (viewName)
                {
                    case "InventoryView":
                        // Простое обновление layout без избыточных вызовов
                        currentView.UpdateLayout();
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
                        // Обновляем только layout для переключенного экрана DataContext
                        if (_settingsViewModel != null && currentView.DataContext == _settingsViewModel)
                        {
                            // Просто обновляем layout без лишних DataContext
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
            // ������� ������� ��� �������
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
            // ��������� ������� ������� ��� ������� �����
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
                "�������� ���� �����? ��� �������� ������ ��������.",
                "������� �����",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
                
            if (result == MessageBoxResult.Yes)
            {
                MessageBox.Show("���� ������� �������!", "����������", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"������ ��� ������� �����: {ex.Message}", "������", MessageBoxButton.OK, MessageBoxImage.Error);
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
            MessageBox.Show("UI ��������!", "����������", MessageBoxButton.OK, MessageBoxImage.Information);
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
