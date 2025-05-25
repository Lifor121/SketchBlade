using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using SketchBlade.ViewModels;
using SketchBlade.Services;
using System.Reflection;
using SketchBlade.Models;

namespace SketchBlade.Views
{
    /// <summary>
    /// Interaction logic for BattleView.xaml
    /// </summary>
    public partial class BattleView : UserControl
    {
        private bool _hasNavigatedAway = false; // Флаг для предотвращения повторной навигации
        
        public BattleView()
        {
            InitializeComponent();
            
            // Subscribe to loading and unloading events
            Loaded += BattleView_Loaded;
            Unloaded += BattleView_Unloaded;
        }
        
        private void BattleView_Loaded(object sender, RoutedEventArgs e)
        {
            // Сбрасываем флаг навигации при загрузке нового экрана боя
            _hasNavigatedAway = false;
            
            LoggingService.LogDebug("BattleView loaded");
            
            if (DataContext is BattleViewModel viewModel)
            {
                LoggingService.LogDebug("Battle view loaded");
                LoggingService.LogDebug($"Battle state at load: IsBattleOver={viewModel.IsBattleOver}, IsPlayerTurn={viewModel.IsPlayerTurn}");
                LoggingService.LogDebug($"Enemy count: {viewModel.Enemies?.Count ?? 0}");
                LoggingService.LogDebug($"Selected enemy: {viewModel.SelectedEnemy?.Name ?? "None"}");
                
                // Проверяем состояние врагов
                if (viewModel.Enemies != null)
                {
                    foreach (var enemy in viewModel.Enemies)
                    {
                        LoggingService.LogDebug($"Enemy {enemy.Name}: IsDefeated={enemy.IsDefeated}, Health={enemy.CurrentHealth}/{enemy.MaxHealth}");
                    }
                }
                
                bool hasLiveEnemies = viewModel.Enemies?.Any(e => !e.IsDefeated && e.CurrentHealth > 0) ?? false;
                bool hasValidPlayer = viewModel.PlayerCharacter != null && viewModel.PlayerCharacter.CurrentHealth > 0;
                
                LoggingService.LogDebug($"HasLiveEnemies: {hasLiveEnemies}, HasValidPlayer: {hasValidPlayer}");
                
                // Если бой помечен как завершенный, но есть живые враги - автоматически переходим на карту
                if (viewModel.IsBattleOver && !_hasNavigatedAway)
                {
                    LoggingService.LogDebug("Battle is legitimately over - allowing automatic navigation");
                    // Автоматически переходим на карту мира, но только один раз
                    _hasNavigatedAway = true;
                    
                    // Деактивируем кнопки для предотвращения повторных нажатий
                    if (AttackButton != null) AttackButton.IsEnabled = false;
                    if (CompleteButton != null) CompleteButton.IsEnabled = false;
                    if (BackToMapButton != null) BackToMapButton.IsEnabled = false;
                    
                    // Переходим на карту мира через небольшую задержку
                    Dispatcher.BeginInvoke(new Action(() => {
                        if (!_hasNavigatedAway) return; // Двойная проверка
                        
                        try
                        {
                            viewModel.NavigateCommand?.Execute("WorldMapView");
                        }
                        catch (Exception ex)
                        {
                            LoggingService.LogError($"Error in automatic navigation: {ex.Message}", ex);
                        }
                    }), DispatcherPriority.Background);
                }
                else
                {
                    LoggingService.LogDebug("Battle is starting normally");
                }
            }
            else
            {
                LoggingService.LogDebug("Warning: BattleView DataContext is not a BattleViewModel");
            }
        }
        
        private void ForceNavigateToWorldMap(BattleViewModel viewModel)
        {
            try
            {
                // First try using the command
                if (viewModel.EndBattleCommand.CanExecute(null))
                {
                    viewModel.EndBattleCommand.Execute(null);
                    LoggingService.LogDebug("Executed EndBattleCommand directly");
                }
                
                // If we're still on the battle screen after a delay, try forced navigation
                Dispatcher.BeginInvoke(new Action(() => {
                    try
                    {
                        if (viewModel.GameData.CurrentScreen == "BattleView")
                        {
                            LoggingService.LogDebug("Still on Battle screen after command execution, forcing navigation");
                            viewModel.GameData.CurrentScreen = "WorldMapView";
                            
                            // Try to find the MainWindow and trigger a screen update without reflection
                            var mainWindow = Application.Current.MainWindow;
                            if (mainWindow is MainWindow mw)
                            {
                                LoggingService.LogDebug("Found MainWindow, trying to navigate directly");
                                mw.NavigateToScreen("WorldMapView");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LoggingService.LogError($"Error in delayed navigation: {ex.Message}", ex);
                    }
                }), DispatcherPriority.Background);
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error in ForceNavigateToWorldMap: {ex.Message}", ex);
            }
        }
        
        private void BattleView_Unloaded(object sender, RoutedEventArgs e)
        {
            LoggingService.LogDebug("BattleView unloaded");
            
            // Explicitly hide the control to prevent it from remaining visible
            this.Visibility = Visibility.Collapsed;
            
            // Make sure all resources are properly released
            if (DataContext is BattleViewModel viewModel)
            {
                // Последний шанс обработать неперенесенные награды перед выгрузкой
                if (viewModel.BattleWon && viewModel.GameData.BattleRewardItems != null && 
                    viewModel.GameData.BattleRewardItems.Count > 0)
                {
                    LoggingService.LogDebug($"WARNING: Found {viewModel.GameData.BattleRewardItems.Count} unprocessed reward items during unload!");
                    
                    // Логируем все награды
                    foreach (var item in viewModel.GameData.BattleRewardItems)
                    {
                        LoggingService.LogDebug($"  - Unprocessed item: {item.Name}");
                    }
                    
                    try
                    {
                        // Принудительно используем метод ProcessBattleRewards из ViewModel
                        LoggingService.LogDebug("Calling EndBattle to process rewards during unload");
                        viewModel.EndBattlePublic(true);
                    }
                    catch (Exception ex)
                    {
                        LoggingService.LogError($"ERROR during unload reward processing: {ex.Message}", ex);
                        
                        // В случае ошибки, вручную добавляем предметы и сохраняем игру
                        try
                        {
                            LoggingService.LogDebug("Trying manual inventory addition as fallback");
                            foreach (var item in viewModel.GameData.BattleRewardItems.ToList())
                            {
                                bool added = viewModel.GameData.Inventory.AddItem(item);
                                LoggingService.LogDebug($"Unload fallback: Added reward item to inventory: {item.Name}, success: {added}");
                            }
                            
                            // Clear reward items to prevent duplication
                            viewModel.GameData.BattleRewardItems.Clear();
                            
                            // Save game to ensure rewards are not lost
                            CoreGameService.Instance.SaveGame(viewModel.GameData);
                            LoggingService.LogDebug("Unload fallback: Game saved");
                        }
                        catch (Exception fallbackEx)
                        {
                            LoggingService.LogError($"CRITICAL: Fallback reward processing failed during unload: {fallbackEx.Message}", fallbackEx);
                        }
                    }
                }
            }
        }
        
        private void AttackButton_Click(object sender, RoutedEventArgs e)
        {
            LoggingService.LogDebug("AttackButton_Click: Button clicked");
            if (DataContext is BattleViewModel viewModel)
            {
                if (viewModel.IsPlayerTurn && !viewModel.IsBattleOver && !viewModel.Animations.IsAnimating)
                {
                    try
                    {
                        // Напрямую вызываем команду Attack из ViewModel
                        if (viewModel.AttackCommand.CanExecute(viewModel.SelectedEnemy))
                        {
                            LoggingService.LogDebug($"AttackButton_Click: Executing attack on {viewModel.SelectedEnemy?.Name}");
                            viewModel.AttackCommand.Execute(viewModel.SelectedEnemy);
                        }
                        else
                        {
                            LoggingService.LogDebug("AttackButton_Click: AttackCommand cannot execute");
                        }
                    }
                    catch (Exception ex)
                    {
                        LoggingService.LogError($"ERROR in AttackButton_Click: {ex.Message}", ex);
                    }
                }
                else
                {
                    LoggingService.LogDebug($"AttackButton_Click: Cannot attack. PlayerTurn={viewModel.IsPlayerTurn}, BattleOver={viewModel.IsBattleOver}, Animating={viewModel.Animations.IsAnimating}");
                }
            }
            else
            {
                LoggingService.LogDebug("AttackButton_Click: DataContext is not BattleViewModel");
            }
        }
        
        private void CompleteButton_Click(object sender, RoutedEventArgs e)
        {
            // Предотвращаем множественные нажатия
            if (_hasNavigatedAway)
            {
                LoggingService.LogDebug("CompleteButton_Click: Уже выполнена навигация, игнорируем");
                return;
            }
            
            _hasNavigatedAway = true;
            
            // Деактивируем кнопку немедленно
            if (sender is Button button)
            {
                button.IsEnabled = false;
            }
            
            LoggingService.LogDebug("=========== CompleteButton_Click STARTED ===========");
            
            if (DataContext is BattleViewModel viewModel)
            {
                try
                {
                    LoggingService.LogDebug("CompleteButton_Click: Executing EndBattleCommand");
                    
                    // Сохраняем игру перед навигацией
                    CoreGameService.Instance.SaveGame(viewModel.GameData);
                }
                catch (Exception ex)
                {
                    LoggingService.LogError($"ERROR in CompleteButton_Click: {ex.Message}", ex);
                    LoggingService.LogError($"Stack trace: {ex.StackTrace}", ex);
                    
                    // Аварийный способ - пробуем выполнить навигацию
                    try
                    {
                        // Скрываем текущее представление
                        this.Visibility = Visibility.Collapsed;
                        
                        // Устанавливаем экран карты мира в GameData
                        viewModel.GameData.CurrentScreen = "WorldMapView";
                        
                        // Пытаемся выполнить навигацию через MainWindow
                        if (Application.Current.MainWindow is MainWindow mainWindow)
                        {
                            mainWindow.NavigateToScreen("WorldMapView");
                            LoggingService.LogDebug("Emergency navigation completed via MainWindow");
                        }
                    }
                    catch (Exception emergencyEx)
                    {
                        LoggingService.LogError($"Emergency navigation failed: {emergencyEx.Message}", emergencyEx);
                    }
                }
                finally
                {
                    // Принудительно переходим на карту мира
                    try
                    {
                        viewModel.NavigateCommand?.Execute("WorldMapView");
                        LoggingService.LogDebug("Navigation executed via NavigateCommand");
                    }
                    catch (Exception navEx)
                    {
                        LoggingService.LogError($"NavigateCommand failed: {navEx.Message}", navEx);
                    }
                }
            }
            else
            {
                LoggingService.LogDebug("CompleteButton_Click: DataContext is not BattleViewModel");
            }
            
            LoggingService.LogDebug("=========== CompleteButton_Click COMPLETED ===========");
        }
        
        private void BackToMapButton_Click(object sender, RoutedEventArgs e)
        {
            // Предотвращаем множественные нажатия
            if (_hasNavigatedAway)
            {
                LoggingService.LogDebug("BackToMapButton_Click: Уже выполнена навигация, игнорируем");
                return;
            }
            
            _hasNavigatedAway = true;
            
            // Деактивируем кнопку немедленно
            if (sender is Button button)
            {
                button.IsEnabled = false;
            }
            
            LoggingService.LogDebug("=========== BackToMapButton_Click STARTED ===========");
            
            if (DataContext is BattleViewModel viewModel)
            {
                try
                {
                    // Try using MainWindow direct navigation first
                    var mainWindow = Application.Current.MainWindow as MainWindow;
                    if (mainWindow != null)
                    {
                        LoggingService.LogDebug("Using direct MainWindow navigation");
                        mainWindow.NavigateToScreen("WorldMapView");
                    }
                    else
                    {
                        // Try using the view model's NavigateCommand
                        LoggingService.LogDebug("MainWindow not found, using NavigateCommand");
                        if (viewModel.NavigateCommand != null && viewModel.NavigateCommand.CanExecute("WorldMapView"))
                        {
                            viewModel.NavigateCommand.Execute("WorldMapView");
                        }
                        else
                        {
                            LoggingService.LogDebug("ERROR: Could not find navigation mechanism");
                        }
                    }
                }
                catch (Exception ex)
                {
                    LoggingService.LogError($"ERROR in BackToMapButton_Click: {ex.Message}", ex);
                    LoggingService.LogError($"Stack trace: {ex.StackTrace}", ex);
                    
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
                                        LoggingService.LogDebug("Used dispatcher to navigate after failure");
                                    }
                                }
                                catch (Exception dispatcherEx)
                                {
                                    LoggingService.LogError($"Final navigation attempt failed: {dispatcherEx.Message}", dispatcherEx);
                                }
                            }));
                        }
                    }
                    catch (Exception finalEx)
                    {
                        LoggingService.LogError($"Critical navigation error: {finalEx.Message}", finalEx);
                    }
                }
            }
            
            LoggingService.LogDebug("=========== BackToMapButton_Click COMPLETED ===========");
        }
    }
}



