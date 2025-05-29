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
using SketchBlade.Views.Controls;
using System.Collections.Specialized;

namespace SketchBlade.Views
{
    /// <summary>
    /// Interaction logic for BattleView.xaml
    /// </summary>
    public partial class BattleView : UserControl
    {
        private bool _hasNavigatedAway = false; // Флаг для предотвращения повторных навигаций
        private BattleViewModel _viewModel;
        
        public BattleView()
        {
            InitializeComponent();
            
            // Subscribe to loading and unloading events
            Loaded += BattleView_Loaded;
            Unloaded += BattleView_Unloaded;
            DataContextChanged += BattleView_DataContextChanged;
        }
        
        private void BattleView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // Отписываемся от старого ViewModel
            if (_viewModel != null && _viewModel.BattleLog != null)
            {
                // _viewModel.BattleLog.CollectionChanged -= BattleLog_CollectionChanged;
            }
            
            // Подписываемся на новый ViewModel
            _viewModel = DataContext as BattleViewModel;
            if (_viewModel != null && _viewModel.BattleLog != null)
            {
                // _viewModel.BattleLog.CollectionChanged += BattleLog_CollectionChanged;
            }
        }
        
        private async void BattleLog_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // Логи боя отключены по просьбе пользователя
            /*
            if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null)
            {
                foreach (string message in e.NewItems)
                {
                    // Показываем уведомление вместо добавления в лог
                    await ShowBattleNotification(message);
                }
            }
            */
        }
        
        private async System.Threading.Tasks.Task ShowBattleNotification(string message)
        {
            try
            {
                if (BattleNotificationControl == null) return;
                
                var type = DetermineNotificationType(message);
                
                await BattleNotificationControl.ShowNotification(message, type, 1800);
            }
            catch (Exception ex)
            {
                // LoggingService.LogError($"Error showing battle notification: {ex.Message}", ex);
            }
        }
        
        private BattleNotificationType DetermineNotificationType(string message)
        {
            string lowerMessage = message.ToLower();
            
            if (lowerMessage.Contains("критический") || lowerMessage.Contains("critical"))
                return BattleNotificationType.Critical;
            
            if (lowerMessage.Contains("победа") || lowerMessage.Contains("victory") || 
                lowerMessage.Contains("побежден") || lowerMessage.Contains("побеждён"))
                return BattleNotificationType.Victory;
            
            if (lowerMessage.Contains("поражение") || lowerMessage.Contains("defeat"))
                return BattleNotificationType.Defeat;
            
            if (lowerMessage.Contains("восстанавливает") || lowerMessage.Contains("лечения") || 
                lowerMessage.Contains("heal"))
                return BattleNotificationType.Healing;
            
            if (lowerMessage.Contains("урон") || lowerMessage.Contains("нанёс") || 
                lowerMessage.Contains("атаковал") || lowerMessage.Contains("damage"))
                return BattleNotificationType.Damage;
            
            return BattleNotificationType.Info;
        }
        
        private void BattleView_Loaded(object sender, RoutedEventArgs e)
        {
            // ��������       
            _hasNavigatedAway = false;
            
            if (DataContext is BattleViewModel viewModel)
            {
                // ��������� ��������� ������
                if (viewModel.Enemies != null)
                {
                    foreach (var enemy in viewModel.Enemies)
                    {
                        // LoggingService.LogDebug($"Enemy {enemy.Name}: IsDefeated={enemy.IsDefeated}, Health={enemy.CurrentHealth}/{enemy.MaxHealth}");
                    }
                }
                
                bool hasLiveEnemies = viewModel.Enemies?.Any(e => !e.IsDefeated && e.CurrentHealth > 0) ?? false;
                bool hasValidPlayer = viewModel.PlayerCharacter != null && viewModel.PlayerCharacter.CurrentHealth > 0;
                
                // ���� ��� ������� ��� �����������, �� ���� ����� ����� - ������������� ��������� �� �����
                if (viewModel.IsBattleOver && !_hasNavigatedAway)
                {
                    _hasNavigatedAway = true;
                    
                    // ������������ ������ ��� �������������� ��������� �������
                    // if (AttackButton != null) AttackButton.IsEnabled = false;
                    if (CompleteButton != null) CompleteButton.IsEnabled = false;
                    if (BackToMapButton != null) BackToMapButton.IsEnabled = false;
                    
                    // ��������� �� ����� ���� ����� ��������� ��������
                    Dispatcher.BeginInvoke(new Action(() => {
                        if (!_hasNavigatedAway) return; // ������� ��������
                        
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
                    // LoggingService.LogDebug("Executed EndBattleCommand directly");
                }
                
                // If we're still on the battle screen after a delay, try forced navigation
                Dispatcher.BeginInvoke(new Action(() => {
                    try
                    {
                        if (viewModel.GameData.CurrentScreen == "BattleView")
                        {
                            // LoggingService.LogDebug("Still on Battle screen after command execution, forcing navigation");
                            viewModel.GameData.CurrentScreen = "WorldMapView";
                            
                            // Try to find the MainWindow and trigger a screen update without reflection
                            var mainWindow = Application.Current.MainWindow;
                            if (mainWindow is MainWindow mw)
                            {
                                // LoggingService.LogDebug("Found MainWindow, trying to navigate directly");
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
            // LoggingService.LogDebug("BattleView unloaded");
            
            // Отписываемся от событий BattleLog
            if (_viewModel != null && _viewModel.BattleLog != null)
            {
                // _viewModel.BattleLog.CollectionChanged -= BattleLog_CollectionChanged;
            }
            
            // Скрываем все уведомления
            try
            {
                BattleNotificationControl?.HideImmediately();
            }
            catch
            {
                // Игнорируем ошибки при скрытии уведомлений
            }
            
            // Explicitly hide the control to prevent it from remaining visible
            this.Visibility = Visibility.Collapsed;
            
            // Make sure all resources are properly released
            if (DataContext is BattleViewModel viewModel)
            {
                // ��������� ���� ���������� �������������� ������� ����� ���������
                if (viewModel.BattleWon && viewModel.GameData.BattleRewardItems != null && 
                    viewModel.GameData.BattleRewardItems.Count > 0)
                {
                    // LoggingService.LogDebug($"WARNING: Found {viewModel.GameData.BattleRewardItems.Count} unprocessed reward items during unload!");
                    
                    // �������� ��� ������� (������ ��� ����������� �������)
                    // foreach (var item in viewModel.GameData.BattleRewardItems)
                    // {
                    //     LoggingService.LogDebug($"  - Unprocessed item: {item.Name}");
                    // }
                    
                    try
                    {
                        // ������������� ���������� ����� ProcessBattleRewards �� ViewModel
                        // LoggingService.LogDebug("Calling EndBattle to process rewards during unload");
                        viewModel.EndBattlePublic(true);
                    }
                    catch (Exception ex)
                    {
                        LoggingService.LogError($"ERROR during unload reward processing: {ex.Message}", ex);
                        
                        // � ������ ������, ������� ��������� �������� � ��������� ����
                        try
                        {
                            // LoggingService.LogDebug("Trying manual inventory addition as fallback");
                            foreach (var item in viewModel.GameData.BattleRewardItems.ToList())
                            {
                                bool added = viewModel.GameData.Inventory.AddItem(item);
                                // LoggingService.LogDebug($"Unload fallback: Added reward item to inventory: {item.Name}, success: {added}");
                            }
                            
                            // Clear reward items to prevent duplication
                            viewModel.GameData.BattleRewardItems.Clear();
                            
                            // Save game to ensure rewards are not lost
                            CoreGameService.Instance.SaveGame(viewModel.GameData);
                            // LoggingService.LogDebug("Unload fallback: Game saved");
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
            // LoggingService.LogDebug("AttackButton_Click: Button clicked");
            if (DataContext is BattleViewModel viewModel)
            {
                if (viewModel.IsPlayerTurn && !viewModel.IsBattleOver && !viewModel.Animations.IsAnimating)
                {
                    try
                    {
                        // �������� �������� ������� Attack �� ViewModel
                        if (viewModel.AttackCommand.CanExecute(viewModel.SelectedEnemy))
                        {
                            // LoggingService.LogDebug($"AttackButton_Click: Executing attack on {viewModel.SelectedEnemy?.Name}");
                            viewModel.AttackCommand.Execute(viewModel.SelectedEnemy);
                        }
                        else
                        {
                            // LoggingService.LogDebug("AttackButton_Click: AttackCommand cannot execute");
                        }
                    }
                    catch (Exception ex)
                    {
                        LoggingService.LogError($"ERROR in AttackButton_Click: {ex.Message}", ex);
                    }
                }
                else
                {
                    // LoggingService.LogDebug($"AttackButton_Click: Cannot attack. PlayerTurn={viewModel.IsPlayerTurn}, BattleOver={viewModel.IsBattleOver}, Animating={viewModel.Animations.IsAnimating}");
                }
            }
            else
            {
                // LoggingService.LogDebug("AttackButton_Click: DataContext is not BattleViewModel");
            }
        }
        
        private void CompleteButton_Click(object sender, RoutedEventArgs e)
        {
            // ������������� ������������� �������
            if (_hasNavigatedAway)
            {
                // LoggingService.LogDebug("CompleteButton_Click: ��� ��������� ���������, ����������");
                return;
            }
            
            _hasNavigatedAway = true;
            
            // ������������ ������ ����������
            if (sender is Button button)
            {
                button.IsEnabled = false;
            }
            
            // LoggingService.LogDebug("=========== CompleteButton_Click STARTED ===========");
            
            if (DataContext is BattleViewModel viewModel)
            {
                try
                {
                    // LoggingService.LogDebug("CompleteButton_Click: Executing EndBattleCommand");
                    
                    // ��������� ���� ����� ����������
                    CoreGameService.Instance.SaveGame(viewModel.GameData);
                }
                catch (Exception ex)
                {
                    LoggingService.LogError($"ERROR in CompleteButton_Click: {ex.Message}", ex);
                    LoggingService.LogError($"Stack trace: {ex.StackTrace}", ex);
                    
                    // ��������� ������ - ������� ��������� ���������
                    try
                    {
                        // �������� ������� �������������
                        this.Visibility = Visibility.Collapsed;
                        
                        // ������������� ����� ����� ���� � GameData
                        viewModel.GameData.CurrentScreen = "WorldMapView";
                        
                        // �������� ��������� ��������� ����� MainWindow
                        if (Application.Current.MainWindow is MainWindow mainWindow)
                        {
                            mainWindow.NavigateToScreen("WorldMapView");
                            // LoggingService.LogDebug("Emergency navigation completed via MainWindow");
                        }
                    }
                    catch (Exception emergencyEx)
                    {
                        LoggingService.LogError($"Emergency navigation failed: {emergencyEx.Message}", emergencyEx);
                    }
                }
                finally
                {
                    // ������������� ��������� �� ����� ����
                    try
                    {
                        viewModel.NavigateCommand?.Execute("WorldMapView");
                        // LoggingService.LogDebug("Navigation executed via NavigateCommand");
                    }
                    catch (Exception navEx)
                    {
                        LoggingService.LogError($"NavigateCommand failed: {navEx.Message}", navEx);
                    }
                }
            }
            else
            {
                // LoggingService.LogDebug("CompleteButton_Click: DataContext is not BattleViewModel");
            }
            
            // LoggingService.LogDebug("=========== CompleteButton_Click COMPLETED ===========");
        }
        
        private void BackToMapButton_Click(object sender, RoutedEventArgs e)
        {
            // ������������� ������������� �������
            if (_hasNavigatedAway)
            {
                // LoggingService.LogDebug("BackToMapButton_Click: ��� ��������� ���������, ����������");
                return;
            }
            
            _hasNavigatedAway = true;
            
            // ������������ ������ ����������
            if (sender is Button button)
            {
                button.IsEnabled = false;
            }
            
            // LoggingService.LogDebug("=========== BackToMapButton_Click STARTED ===========");
            
            if (DataContext is BattleViewModel viewModel)
            {
                try
                {
                    // Try using MainWindow direct navigation first
                    var mainWindow = Application.Current.MainWindow as MainWindow;
                    if (mainWindow != null)
                    {
                        // LoggingService.LogDebug("Using direct MainWindow navigation");
                        mainWindow.NavigateToScreen("WorldMapView");
                    }
                    else
                    {
                        // Try using the view model's NavigateCommand
                        // LoggingService.LogDebug("MainWindow not found, using NavigateCommand");
                        if (viewModel.NavigateCommand != null && viewModel.NavigateCommand.CanExecute("WorldMapView"))
                        {
                            viewModel.NavigateCommand.Execute("WorldMapView");
                        }
                        else
                        {
                            // LoggingService.LogDebug("ERROR: Could not find navigation mechanism");
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
                                        // LoggingService.LogDebug("Used dispatcher to navigate after failure");
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
            
            // LoggingService.LogDebug("=========== BackToMapButton_Click COMPLETED ===========");
        }
    }
}



