using System;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows;
using SketchBlade.ViewModels;
using SketchBlade.Models;
using System.ComponentModel;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Threading;

namespace SketchBlade.Views.Controls.Recipes
{
    /// <summary>
    /// Interaction logic for CraftingPanel.xaml
    /// </summary>
    public partial class CraftingPanel : UserControl
    {
        public CraftingPanel()
        {
            InitializeComponent();
            
            // Подписываемся на изменения настроек для корректной работы тултипов
            SubscribeToSettingsChanges();
            
            // Подписываемся на событие выгрузки для очистки ресурсов
            this.Unloaded += CraftingPanel_Unloaded;
            
            // НОВОЕ: Подписываемся на изменения DataContext
            this.DataContextChanged += CraftingPanel_DataContextChanged;
            this.Loaded += CraftingPanel_Loaded;
        }
        
        private void CraftingPanel_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is SimplifiedCraftingViewModel craftingViewModel)
            {
                // Принудительно устанавливаем ItemsSource
                if (CraftableItemsControl.ItemsSource == null)
                {
                    CraftableItemsControl.ItemsSource = craftingViewModel.AvailableRecipes;
                }
                
                // Проверяем количество элементов
                if (CraftableItemsControl.ItemsSource is System.Collections.ICollection collection)
                {
                    int count = collection.Count;
                    
                    // НОВОЕ: Принудительно обновляем ItemsControl
                    if (count > 0)
                    {
                        CraftableItemsControl.Items.Refresh();
                        CraftableItemsControl.UpdateLayout();
                        
                        // Дополнительно: Принудительно перепривязываем данные
                        var binding = System.Windows.Data.BindingOperations.GetBinding(CraftableItemsControl, ItemsControl.ItemsSourceProperty);
                        if (binding != null)
                        {
                            System.Windows.Data.BindingOperations.ClearBinding(CraftableItemsControl, ItemsControl.ItemsSourceProperty);
                            System.Windows.Data.BindingOperations.SetBinding(CraftableItemsControl, ItemsControl.ItemsSourceProperty, binding);
                        }
                    }
                }
            }
        }
        
        private void CraftingPanel_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // Отписываемся от старого ViewModel
            if (e.OldValue is SimplifiedCraftingViewModel oldViewModel)
            {
                oldViewModel.AvailableRecipes.CollectionChanged -= AvailableRecipes_CollectionChanged;
                oldViewModel.PropertyChanged -= CraftingViewModel_PropertyChanged;
            }
            
            // Подписываемся на новый ViewModel
            if (e.NewValue is SimplifiedCraftingViewModel newViewModel)
            {
                newViewModel.AvailableRecipes.CollectionChanged += AvailableRecipes_CollectionChanged;
                newViewModel.PropertyChanged += CraftingViewModel_PropertyChanged;
                
                // НОВОЕ: Принудительно обновляем UI при смене DataContext
                // Принудительно устанавливаем ItemsSource
                CraftableItemsControl.ItemsSource = newViewModel.AvailableRecipes;
                
                // Принудительно обновляем отображение
                if (newViewModel.AvailableRecipes.Count > 0)
                {
                    CraftableItemsControl.Items.Refresh();
                    CraftableItemsControl.UpdateLayout();
                }
            }
        }
        
        private void AvailableRecipes_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            // Collection changed - continue silently
        }
        
        private void CraftingViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // Property changed - continue silently
        }
        
        private void CraftingPanel_Unloaded(object sender, RoutedEventArgs e)
        {
            // Отписываемся от событий при выгрузке контрола
            try
            {
                var GameData = Application.Current.Resources["GameData"] as GameData;
                if (GameData?.Settings != null)
                {
                    GameData.Settings.PropertyChanged -= Settings_PropertyChanged;
                }
            }
            catch (Exception ex)
            {
                // Error unsubscribing from settings changes - continue silently
            }
        }

        private void SubscribeToSettingsChanges()
        {
            try
            {
                var GameData = Application.Current.Resources["GameData"] as GameData;
                if (GameData?.Settings != null)
                {
                    GameData.Settings.PropertyChanged += Settings_PropertyChanged;
                    
                    // Тултипы теперь всегда показываются
                    UpdateTooltipVisibility(true);
                }
            }
            catch (Exception ex)
            {
                // Error subscribing to settings changes - continue silently
            }
        }
        
        private void Settings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // Поскольку описания предметов теперь всегда включены, нет необходимости в обработке изменений
        }
        
        private void UpdateTooltipVisibility(bool showTooltips)
        {
            // Тултипы теперь всегда включены через ItemTooltip в CoreInventorySlot
            // Этот метод оставлен для совместимости, но больше не выполняет активных действий
        }
        
        private T? FindChildOfType<T>(DependencyObject parent) where T : DependencyObject
        {
            try
            {
                if (parent == null) return null;

                int childCount = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
                for (int i = 0; i < childCount; i++)
                {
                    var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                    
                    if (child is T typedChild)
                    {
                        return typedChild;
                    }
                    
                    var result = FindChildOfType<T>(child);
                    if (result != null)
                        return result;
                }
                
                return null;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        
        private void CraftableItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                // Останавливаем распространение события, чтобы оно не обрабатывалось родительскими контролами
                e.Handled = true;
                
                if (sender is CoreInventorySlot slot)
                {
                    // Добавляем визуальный эффект для обратной связи
                    AnimateSlotClick(slot);
                    
                    // Определяем выбранный рецепт
                    if (slot.DataContext is SimplifiedCraftingRecipeViewModel recipeVM)
                    {
                        // Обновляем выбранный рецепт в модели представления
                        if (DataContext is SimplifiedCraftingViewModel craftingVM)
                        {
                            // Устанавливаем выбранный рецепт
                            craftingVM.SelectedRecipe = recipeVM.Recipe;
                            
                            // Проверяем, можно ли создать предмет
                            if (craftingVM.CanCraft)
                            {
                                // ИСПРАВЛЕНИЕ: Принудительно проверяем и обновляем путь к изображению
                                if (recipeVM.Result != null && string.IsNullOrEmpty(recipeVM.Result.SpritePath))
                                {
                                    recipeVM.Result.SpritePath = SketchBlade.Utilities.AssetPaths.DEFAULT_IMAGE;
                                }
                                
                                // Выполняем крафт предмета - ИСПОЛЬЗУЕМ TRY-CATCH для гарантии выполнения
                                try {
                                    // Явно вызываем команду крафта
                                    craftingVM.CraftItemCommand.Execute(null);
                                    
                                    // Для большей надежности вызываем метод напрямую
                                    craftingVM.CraftSelectedItem();
                                    
                                    // ИСПРАВЛЕНИЕ: Принудительно обновляем UI через несколько механизмов
                                    PerformInventoryUIUpdate();
                                    
                                    // ИСПРАВЛЕНИЕ: Принудительно обновляем рецепты
                                    craftingVM.RefreshAvailableRecipes();
                                    
                                    // ИСПРАВЛЕНИЕ: Принудительно обновляем UI слотов крафта
                                    CraftableItemsControl.Items.Refresh();
                                    CraftableItemsControl.UpdateLayout();
                                    
                                    // ИСПРАВЛЕНИЕ: Дополнительное обновление через диспетчер
                                    System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                                    {
                                        // Повторно обновляем рецепты
                                        craftingVM.RefreshAvailableRecipes();
                                        
                                        // Обновляем ItemsControl
                                        CraftableItemsControl.Items.Refresh();
                                        CraftableItemsControl.UpdateLayout();
                                        
                                        // Принудительно обновляем весь контрол
                                        this.UpdateLayout();
                                        this.InvalidateVisual();
                                        
                                    }), System.Windows.Threading.DispatcherPriority.Render);
                                    
                                    // Показываем уведомление об успешном крафте
                                    ShowCraftSuccessNotification(recipeVM.Name);
                                }
                                catch (Exception craftEx) {
                                    // Error in crafting - continue silently
                                }
                            }
                            else
                            {
                                // Показываем уведомление о недостающих материалах
                                var missingMaterials = craftingVM.GetMissingMaterials();
                                ShowMissingMaterialsNotification(missingMaterials);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Error in crafting item click - continue silently
            }
        }
        
        private void AnimateSlotClick(CoreInventorySlot slot)
        {
            try
            {
                // Создаем простую анимацию масштабирования для визуальной обратной связи
                var scaleTransform = new System.Windows.Media.ScaleTransform(1.0, 1.0);
                slot.RenderTransform = scaleTransform;
                slot.RenderTransformOrigin = new Point(0.5, 0.5);
                
                var animation = new System.Windows.Media.Animation.DoubleAnimation
                {
                    From = 1.0,
                    To = 0.95,
                    Duration = TimeSpan.FromMilliseconds(100),
                    AutoReverse = true
                };
                
                scaleTransform.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, animation);
                scaleTransform.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, animation);
            }
            catch (Exception ex)
            {
                // Error in slot animation - continue silently
            }
        }
        
        private void PerformInventoryUIUpdate()
        {
            try
            {
                // Обновляем основной ViewModel инвентаря
                var inventoryViewModel = FindInventoryViewModel();
                if (inventoryViewModel != null)
                {
                    // Отправляем сигналы изменения инвентаря через GameData
                    var gameData = Application.Current.Resources["GameData"] as GameData;
                    if (gameData?.Inventory != null)
                    {
                        gameData.Inventory.OnInventoryChanged();
                    }
                }
            }
            catch (Exception ex)
            {
                // Error in inventory UI update - continue silently
            }
        }
        
        private IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    var child = System.Windows.Media.VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }
        
        private void ShowCraftSuccessNotification(string itemName)
        {
            try
            {
                // Показываем уведомление об успешном создании предмета
                MessageBox.Show($"Предмет '{itemName}' успешно создан!", 
                    "Крафт завершен", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                // Error showing notification - continue silently
            }
        }
        
        private void ShowMissingMaterialsNotification(string missingMaterials)
        {
            try
            {
                // Показываем уведомление о недостающих материалах
                MessageBox.Show($"Недостаточно материалов для крафта:\n{missingMaterials}", 
                    "Недостаточно материалов", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                // Error showing notification - continue silently
            }
        }
        
        private InventoryViewModel? FindInventoryViewModel()
        {
            try
            {
                // Ищем InventoryViewModel в ресурсах приложения или в главном окне
                if (Application.Current.MainWindow is MainWindow mainWindow)
                {
                    // Пытаемся получить InventoryViewModel из MainWindow
                    var mainViewModel = mainWindow.DataContext as MainViewModel;
                    return mainViewModel?.InventoryViewModel;
                }
                
                return null;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
    }
} 
