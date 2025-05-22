using System;
using System.Windows;
using System.Windows.Controls;
using SketchBlade.ViewModels;
using System.Windows.Threading;
using System.Reflection;
using SketchBlade.Models;

namespace SketchBlade.Views
{
    /// <summary>
    /// Interaction logic for BattleView.xaml
    /// </summary>
    public partial class BattleView : UserControl
    {
        public BattleView()
        {
            InitializeComponent();
            
            // Subscribe to loading and unloading events
            Loaded += BattleView_Loaded;
            Unloaded += BattleView_Unloaded;
        }
        
        private void BattleView_Loaded(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("BattleView loaded");
            
            if (DataContext is BattleViewModel viewModel)
            {
                // Ensure battle is properly initialized when view is loaded
                viewModel.ReinitializeBattle();
                Console.WriteLine("Battle reinitialized from BattleView_Loaded");
                
                // Check if battle is already over and manually execute end battle
                if (viewModel.IsBattleOver)
                {
                    Console.WriteLine("Battle is already over on load, executing EndBattleCommand");
                    
                    // Try to force navigation directly
                    ForceNavigateToWorldMap(viewModel);
                }
            }
            else
            {
                Console.WriteLine("Warning: BattleView DataContext is not a BattleViewModel");
            }
        }
        
        private void ForceNavigateToWorldMap(BattleViewModel viewModel)
        {
            // First try using the command
            if (viewModel.EndBattleCommand.CanExecute(null))
            {
                viewModel.EndBattleCommand.Execute(null);
                Console.WriteLine("Executed EndBattleCommand directly");
            }
            
            // If we're still on the battle screen after a delay, try forced navigation
            Dispatcher.BeginInvoke(new Action(() => {
                if (viewModel.GameState.CurrentScreen == "BattleView")
                {
                    Console.WriteLine("Still on Battle screen after command execution, forcing navigation");
                    viewModel.GameState.CurrentScreen = "WorldMapView";
                    
                    // Try to find the MainWindow and trigger a screen update
                    var mainWindow = Application.Current.MainWindow;
                    if (mainWindow != null)
                    {
                        var prop = mainWindow.GetType().GetProperty("CurrentViewModel");
                        if (prop != null)
                        {
                            Console.WriteLine("Found MainWindow.CurrentViewModel property, trying to update");
                            prop.SetValue(mainWindow, null); // Force the main window to update its view
                            mainWindow.UpdateLayout();
                        }
                    }
                }
            }), DispatcherPriority.Background, null, TimeSpan.FromMilliseconds(300));
        }
        
        private void BattleView_Unloaded(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("BattleView unloaded");
            
            // Explicitly hide the control to prevent it from remaining visible
            this.Visibility = Visibility.Collapsed;
            
            // Make sure all resources are properly released
            if (DataContext is BattleViewModel viewModel)
            {
                // Последний шанс обработать непересенные награды перед выгрузкой
                if (viewModel.BattleWon && viewModel.GameState.BattleRewardItems != null && 
                    viewModel.GameState.BattleRewardItems.Count > 0)
                {
                    Console.WriteLine($"WARNING: Found {viewModel.GameState.BattleRewardItems.Count} unprocessed reward items during unload!");
                    
                    // Логируем все награды
                    foreach (var item in viewModel.GameState.BattleRewardItems)
                    {
                        Console.WriteLine($"  - Unprocessed item: {item.Name}");
                    }
                    
                    try
                    {
                        // Принудительно используем метод ProcessBattleRewards из ViewModel
                        Console.WriteLine("Calling FinishBattle to process rewards during unload");
                        viewModel.FinishBattle(true);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"ERROR during unload reward processing: {ex.Message}");
                        
                        // В случае ошибки, вручную добавляем предметы и сохраняем игру
                        try
                        {
                            Console.WriteLine("Trying manual inventory addition as fallback");
                            foreach (var item in viewModel.GameState.BattleRewardItems.ToList())
                            {
                                bool added = viewModel.GameState.Inventory.AddItem(item);
                                Console.WriteLine($"Unload fallback: Added reward item to inventory: {item.Name}, success: {added}");
                            }
                            
                            // Clear reward items to prevent duplication
                            viewModel.GameState.BattleRewardItems.Clear();
                            
                            // Save game to ensure rewards are not lost
                            viewModel.GameState.SaveGame();
                            Console.WriteLine("Unload fallback: Game saved");
                        }
                        catch (Exception fallbackEx)
                        {
                            Console.WriteLine($"CRITICAL: Fallback reward processing failed during unload: {fallbackEx.Message}");
                        }
                    }
                }
            }
        }
        
        private void AttackButton_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("AttackButton_Click: Button clicked");
            if (DataContext is BattleViewModel viewModel)
            {
                if (viewModel.IsPlayerTurn && !viewModel.IsBattleOver && !viewModel.IsAnimating)
                {
                    try
                    {
                        // Напрямую вызываем команду Attack из ViewModel
                        if (viewModel.AttackCommand.CanExecute(viewModel.SelectedEnemy))
                        {
                            Console.WriteLine($"AttackButton_Click: Executing attack on {viewModel.SelectedEnemy?.Name}");
                            viewModel.AttackCommand.Execute(viewModel.SelectedEnemy);
                        }
                        else
                        {
                            Console.WriteLine("AttackButton_Click: AttackCommand cannot execute");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"ERROR in AttackButton_Click: {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine($"AttackButton_Click: Cannot attack. PlayerTurn={viewModel.IsPlayerTurn}, BattleOver={viewModel.IsBattleOver}, Animating={viewModel.IsAnimating}");
                }
            }
            else
            {
                Console.WriteLine("AttackButton_Click: DataContext is not BattleViewModel");
            }
        }
        
        private void CompleteButton_Click(object sender, RoutedEventArgs e)
        {
            // Disable the button immediately to prevent multiple clicks
            if (sender is Button button)
            {
                button.IsEnabled = false;
                button.Content = "Переход на карту...";
            }
            
            Console.WriteLine("=========== CompleteButton_Click STARTED ===========");
            
            if (DataContext is BattleViewModel viewModel)
            {
                try
                {
                    Console.WriteLine($"BattleRewardItems count: {viewModel.GameState.BattleRewardItems?.Count ?? 0}");
                    
                    // ПЕРВЫМ ДЕЛОМ обрабатываем награды и сохраняем игру
                    if (viewModel.BattleWon)
                    {
                        // Проверяем наличие наград и логируем для отладки
                        if (viewModel.GameState.BattleRewardItems != null && viewModel.GameState.BattleRewardItems.Count > 0)
                        {
                            // Логируем все награды для отладки
                            Console.WriteLine("Reward items to be added to inventory:");
                            foreach (var item in viewModel.GameState.BattleRewardItems)
                            {
                                Console.WriteLine($"  - {item.Name} (x{item.StackSize})");
                            }
                        }
                        else
                        {
                            Console.WriteLine("No battle reward items found in GameState");
                        }
                        
                        // Выводим инвентарь до добавления
                        Console.WriteLine("INVENTORY BEFORE ADDING ITEMS (BattleView):");
                        for (int i = 0; i < viewModel.GameState.Inventory.Items.Count; i++)
                        {
                            var item = viewModel.GameState.Inventory.Items[i];
                            Console.WriteLine($"  Slot {i}: {(item != null ? item.Name + " (x" + item.StackSize + ")" : "empty")}");
                        }
                        
                        // Принудительно вызываем метод для обработки наград, вне зависимости от того, пусты они или нет
                        viewModel.FinishBattle(true);
                        
                        // Выводим инвентарь после добавления
                        Console.WriteLine("INVENTORY AFTER ADDING ITEMS (BattleView):");
                        for (int i = 0; i < viewModel.GameState.Inventory.Items.Count; i++)
                        {
                            var item = viewModel.GameState.Inventory.Items[i];
                            Console.WriteLine($"  Slot {i}: {(item != null ? item.Name + " (x" + item.StackSize + ")" : "empty")}");
                        }
                        
                        // Сохраняем игру для надежности
                        viewModel.GameState.SaveGame();
                    }
                    else
                    {
                        // В случае поражения просто завершаем бой
                        viewModel.FinishBattle(false);
                    }
                    
                    // Скрываем ТЕКУЩЕЕ представление боя, чтобы оно не заслоняло карту мира
                    this.Visibility = Visibility.Collapsed;
                    Console.WriteLine("Set BattleView.Visibility = Collapsed");
                    
                    // Обновляем экран в GameState
                    viewModel.GameState.CurrentScreen = "WorldMapView";
                    Console.WriteLine("Set GameState.CurrentScreen = WorldMapView");
                    
                    // Используем различные методы навигации для максимальной надежности
                    try
                    {
                        // 1. Пробуем использовать NavigateCommand из ViewModel
                        if (viewModel.NavigateCommand != null && viewModel.NavigateCommand.CanExecute("WorldMapView"))
                        {
                            Console.WriteLine("Using NavigateCommand from BattleViewModel");
                            viewModel.NavigateCommand.Execute("WorldMapView");
                        }
                        // 2. Иначе пробуем прямую навигацию через MainWindow
                        else
                        {
                            var mainWindow = Application.Current.MainWindow as MainWindow;
                            if (mainWindow != null)
                            {
                                Console.WriteLine("Using direct navigation through MainWindow");
                                mainWindow.NavigateToScreen("WorldMapView");
                            }
                        }
                    }
                    catch (Exception navEx)
                    {
                        Console.WriteLine($"Primary navigation failed: {navEx.Message}");
                        
                        // 3. Резервный вариант через Application.Current.MainWindow
                        try 
                        {
                            if (Application.Current.MainWindow is MainWindow mainWindow)
                            {
                                Console.WriteLine("Using fallback direct navigation");
                                mainWindow.NavigateToScreen("WorldMapView");
                            }
                        }
                        catch (Exception fallbackEx)
                        {
                            Console.WriteLine($"Fallback navigation also failed: {fallbackEx.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ERROR in CompleteButton_Click: {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                    
                    // Аварийный способ - пробуем выполнить навигацию
                    try
                    {
                        // Скрываем текущее представление
                        this.Visibility = Visibility.Collapsed;
                        
                        // Устанавливаем экран карты мира в GameState
                        viewModel.GameState.CurrentScreen = "WorldMapView";
                        
                        // Пытаемся выполнить навигацию через MainWindow
                        if (Application.Current.MainWindow is MainWindow mainWindow)
                        {
                            mainWindow.NavigateToScreen("WorldMapView");
                            Console.WriteLine("EMERGENCY: Used MainWindow.NavigateToScreen");
                        }
                    }
                    catch (Exception fallbackEx)
                    {
                        Console.WriteLine($"CRITICAL: Fallback navigation failed: {fallbackEx.Message}");
                    }
                }
            }
            
            Console.WriteLine("=========== CompleteButton_Click COMPLETED ===========");
        }
        
        private void BackToMapButton_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("=========== BackToMapButton_Click STARTED ===========");
            
            // Disable button to prevent multiple clicks
            if (sender is Button button)
            {
                button.IsEnabled = false;
            }
            
            if (DataContext is BattleViewModel viewModel)
            {
                try
                {
                    // Save the game state before navigating
                    viewModel.GameState.SaveGame();
                    Console.WriteLine("Game state saved before navigation");
                    
                    // Set the current screen in GameState
                    viewModel.GameState.CurrentScreen = "WorldMapView";
                    Console.WriteLine("Set GameState.CurrentScreen = WorldMapView");
                    
                    // Hide the battle view
                    this.Visibility = Visibility.Collapsed;
                    Console.WriteLine("Battle view hidden");
                    
                    // Try multiple navigation approaches
                    try
                    {
                        // Try using MainWindow direct navigation first
                        var mainWindow = Application.Current.MainWindow as MainWindow;
                        if (mainWindow != null)
                        {
                            Console.WriteLine("Using direct MainWindow navigation");
                            mainWindow.NavigateToScreen("WorldMapView");
                        }
                        else
                        {
                            // Try using the view model's NavigateCommand
                            Console.WriteLine("MainWindow not found, using NavigateCommand");
                            if (viewModel.NavigateCommand != null && viewModel.NavigateCommand.CanExecute("WorldMapView"))
                            {
                                viewModel.NavigateCommand.Execute("WorldMapView");
                            }
                            else
                            {
                                Console.WriteLine("ERROR: Could not find navigation mechanism");
                            }
                        }
                    }
                    catch (Exception navEx)
                    {
                        Console.WriteLine($"Navigation error: {navEx.Message}");
                        
                        // Fallback mechanism
                        try
                        {
                            if (Application.Current?.Dispatcher != null)
                            {
                                Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                                    try
                                    {
                                        // Try one more time with direct navigation
                                        if (Application.Current.MainWindow is MainWindow mainWindow)
                                        {
                                            mainWindow.NavigateToScreen("WorldMapView");
                                            Console.WriteLine("Used dispatcher to navigate after failure");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"Final navigation attempt failed: {ex.Message}");
                                    }
                                }));
                            }
                        }
                        catch (Exception finalEx)
                        {
                            Console.WriteLine($"Critical navigation error: {finalEx.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ERROR in BackToMapButton_Click: {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                }
            }
            
            Console.WriteLine("=========== BackToMapButton_Click COMPLETED ===========");
        }
    }
}
