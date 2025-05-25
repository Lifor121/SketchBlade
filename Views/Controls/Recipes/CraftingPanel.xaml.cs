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
            
            SketchBlade.Services.LoggingService.LogDebug("CraftingPanel: Конструктор вызван");
            
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
            SketchBlade.Services.LoggingService.LogDebug("CraftingPanel_Loaded: Панель крафта загружена");
            SketchBlade.Services.LoggingService.LogDebug($"CraftingPanel_Loaded: DataContext = {DataContext?.GetType().Name ?? "null"}");
            
            if (DataContext is SimplifiedCraftingViewModel craftingViewModel)
            {
                SketchBlade.Services.LoggingService.LogDebug($"CraftingPanel_Loaded: SimplifiedCraftingViewModel найден, рецептов: {craftingViewModel.AvailableRecipes.Count}");
                
                // Проверяем ItemsSource
                SketchBlade.Services.LoggingService.LogDebug($"CraftingPanel_Loaded: CraftableItemsControl.ItemsSource = {CraftableItemsControl.ItemsSource?.GetType().Name ?? "null"}");
                
                // Принудительно устанавливаем ItemsSource
                if (CraftableItemsControl.ItemsSource == null)
                {
                    SketchBlade.Services.LoggingService.LogDebug("CraftingPanel_Loaded: Принудительно устанавливаем ItemsSource");
                    CraftableItemsControl.ItemsSource = craftingViewModel.AvailableRecipes;
                }
                
                // Проверяем количество элементов
                if (CraftableItemsControl.ItemsSource is System.Collections.ICollection collection)
                {
                    int count = collection.Count;
                    SketchBlade.Services.LoggingService.LogDebug($"CraftingPanel_Loaded: ItemsSource содержит {count} элементов");
                    
                    // НОВОЕ: Принудительно обновляем ItemsControl
                    if (count > 0)
                    {
                        SketchBlade.Services.LoggingService.LogDebug("CraftingPanel_Loaded: Принудительно обновляем ItemsControl");
                        CraftableItemsControl.Items.Refresh();
                        CraftableItemsControl.UpdateLayout();
                        
                        // Дополнительно: Принудительно перепривязываем данные
                        var binding = System.Windows.Data.BindingOperations.GetBinding(CraftableItemsControl, ItemsControl.ItemsSourceProperty);
                        if (binding != null)
                        {
                            SketchBlade.Services.LoggingService.LogDebug("CraftingPanel_Loaded: Перепривязываем ItemsSource");
                            System.Windows.Data.BindingOperations.ClearBinding(CraftableItemsControl, ItemsControl.ItemsSourceProperty);
                            System.Windows.Data.BindingOperations.SetBinding(CraftableItemsControl, ItemsControl.ItemsSourceProperty, binding);
                        }
                    }
                }
            }
            else
            {
                SketchBlade.Services.LoggingService.LogError("CraftingPanel_Loaded: DataContext не является SimplifiedCraftingViewModel");
            }
        }
        
        private void CraftingPanel_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            SketchBlade.Services.LoggingService.LogDebug($"CraftingPanel_DataContextChanged: {e.OldValue?.GetType().Name ?? "null"} -> {e.NewValue?.GetType().Name ?? "null"}");
            
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
                SketchBlade.Services.LoggingService.LogDebug($"CraftingPanel_DataContextChanged: Принудительно обновляем UI, рецептов: {newViewModel.AvailableRecipes.Count}");
                
                // Принудительно устанавливаем ItemsSource
                CraftableItemsControl.ItemsSource = newViewModel.AvailableRecipes;
                
                // Принудительно обновляем отображение
                if (newViewModel.AvailableRecipes.Count > 0)
                {
                    SketchBlade.Services.LoggingService.LogDebug("CraftingPanel_DataContextChanged: Обновляем ItemsControl");
                    CraftableItemsControl.Items.Refresh();
                    CraftableItemsControl.UpdateLayout();
                }
            }
        }
        
        private void AvailableRecipes_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            SketchBlade.Services.LoggingService.LogDebug($"CraftingPanel: AvailableRecipes коллекция изменена: {e.Action}");
            if (sender is System.Collections.ObjectModel.ObservableCollection<SimplifiedCraftingRecipeViewModel> collection)
            {
                SketchBlade.Services.LoggingService.LogDebug($"CraftingPanel: Новое количество рецептов: {collection.Count}");
            }
        }
        
        private void CraftingViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            SketchBlade.Services.LoggingService.LogDebug($"CraftingPanel: SimplifiedCraftingViewModel.{e.PropertyName} изменено");
        }
        
        private void CraftingPanel_Unloaded(object sender, RoutedEventArgs e)
        {
            // РћС‚РїРёСЃС‹РІР°РµРјСЃСЏ РѕС‚ СЃРѕР±С‹С‚РёР№ РїСЂРё РІС‹РіСЂСѓР·РєРµ РєРѕРЅС‚СЂРѕР»Р°
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
                System.IO.File.AppendAllText("error_log.txt",
                    $"[{DateTime.Now}] Error unsubscribing from settings changes in CraftingPanel: {ex.Message}\r\n");
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
                    
                    // РўСѓР»С‚РёРїС‹ С‚РµРїРµСЂСЊ РІСЃРµРіРґР° РїРѕРєР°Р·С‹РІР°СЋС‚СЃСЏ
                    UpdateTooltipVisibility(true);
                }
            }
            catch (Exception ex)
            {
                System.IO.File.AppendAllText("error_log.txt",
                    $"[{DateTime.Now}] Error subscribing to settings changes in CraftingPanel: {ex.Message}\r\n");
            }
        }
        
        private void Settings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // РџРѕСЃРєРѕР»СЊРєСѓ РѕРїРёСЃР°РЅРёСЏ РїСЂРµРґРјРµС‚РѕРІ С‚РµРїРµСЂСЊ РІСЃРµРіРґР° РІРєР»СЋС‡РµРЅС‹, РЅРµС‚ РЅРµРѕР±С…РѕРґРёРјРѕСЃС‚Рё РІ РѕР±СЂР°Р±РѕС‚РєРµ РёР·РјРµРЅРµРЅРёР№
        }
        
        private void UpdateTooltipVisibility(bool showTooltips)
        {
            try
            {
                // РћР±РЅРѕРІР»СЏРµРј РІРёРґРёРјРѕСЃС‚СЊ С‚СѓР»С‚РёРїРѕРІ РґР»СЏ РІСЃРµС… СЌР»РµРјРµРЅС‚РѕРІ РєСЂР°С„С‚Р°
                if (CraftableItemsControl?.ItemsSource != null)
                {
                    foreach (var item in CraftableItemsControl.Items)
                    {
                        var container = CraftableItemsControl.ItemContainerGenerator.ContainerFromItem(item) as FrameworkElement;
                        if (container != null)
                        {
                            var slot = FindChildOfType<CoreInventorySlot>(container);
                            if (slot != null)
                            {
                                if (showTooltips)
                                {
                                    // Р’РєР»СЋС‡Р°РµРј С‚СѓР»С‚РёРїС‹
                                    slot.IsHitTestVisible = true;
                                }
                                else
                                {
                                    // РћС‚РєР»СЋС‡Р°РµРј С‚СѓР»С‚РёРїС‹
                                    if (slot.ToolTip is ToolTip tooltip)
                                    {
                                        tooltip.IsOpen = false;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.IO.File.AppendAllText("error_log.txt",
                    $"[{DateTime.Now}] Error updating tooltip visibility in CraftingPanel: {ex.Message}\r\n");
            }
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
                System.IO.File.AppendAllText("error_log.txt",
                    $"[{DateTime.Now}] Error in FindChildOfType in CraftingPanel: {ex.Message}\r\n");
                return null;
            }
        }
        
        private void CraftableItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                // Останавливаем распространение события, чтобы оно не обрабатывалось родительскими контролами
                e.Handled = true;
                
                SketchBlade.Services.LoggingService.LogDebug("[CraftingPanel] CraftableItem_MouseDown: Начало обработки клика");
                
                if (sender is CoreInventorySlot slot)
                {
                    // Добавляем визуальный эффект для обратной связи
                    AnimateSlotClick(slot);
                    
                    SketchBlade.Services.LoggingService.LogDebug($"[CraftingPanel] Клик по слоту с предметом: {slot.Item?.Name ?? "null"}");
                    
                    // Определяем выбранный рецепт
                    if (slot.DataContext is SimplifiedCraftingRecipeViewModel recipeVM)
                    {
                        SketchBlade.Services.LoggingService.LogDebug($"[CraftingPanel] Найден рецепт: {recipeVM.Name}");
                        
                        // Обновляем выбранный рецепт в модели представления
                        if (DataContext is SimplifiedCraftingViewModel craftingVM)
                        {
                            // Устанавливаем выбранный рецепт
                            craftingVM.SelectedRecipe = recipeVM.Recipe;
                            SketchBlade.Services.LoggingService.LogDebug($"[CraftingPanel] Установлен выбранный рецепт: {craftingVM.SelectedRecipe?.Name ?? "null"}");
                            
                            // Проверяем, можно ли создать предмет
                            if (craftingVM.CanCraft)
                            {
                                SketchBlade.Services.LoggingService.LogDebug("[CraftingPanel] Можно создать предмет, выполняем крафт");
                                
                                // ИСПРАВЛЕНИЕ: Принудительно проверяем и обновляем путь к изображению
                                if (recipeVM.Result != null && string.IsNullOrEmpty(recipeVM.Result.SpritePath))
                                {
                                    SketchBlade.Services.LoggingService.LogWarning($"[CraftingPanel] Пустой SpritePath для {recipeVM.Result.Name}, устанавливаем дефолтный");
                                    recipeVM.Result.SpritePath = SketchBlade.Utilities.AssetPaths.DEFAULT_IMAGE;
                                }
                                
                                // Выполняем крафт предмета - ИСПОЛЬЗУЕМ TRY-CATCH для гарантии выполнения
                                try {
                                    SketchBlade.Services.LoggingService.LogInfo("[CraftingPanel] Начинаем выполнение крафта");
                                    
                                    // Явно вызываем команду крафта
                                    craftingVM.CraftItemCommand.Execute(null);
                                    SketchBlade.Services.LoggingService.LogInfo("[CraftingPanel] Команда CraftItemCommand выполнена");
                                    
                                    // Для большей надежности вызываем метод напрямую
                                    craftingVM.CraftSelectedItem();
                                    SketchBlade.Services.LoggingService.LogInfo("[CraftingPanel] Метод CraftSelectedItem вызван напрямую");
                                    
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
                                        SketchBlade.Services.LoggingService.LogDebug("[CraftingPanel] Дополнительное обновление UI через Dispatcher");
                                        
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
                                    SketchBlade.Services.LoggingService.LogError($"[CraftingPanel] Ошибка при выполнении крафта: {craftEx.Message}", craftEx);
                                }
                            }
                            else
                            {
                                SketchBlade.Services.LoggingService.LogDebug("[CraftingPanel] Невозможно создать предмет - недостаточно материалов");
                                
                                // ИСПРАВЛЕНИЕ: Вывести информацию о недостающих материалах
                                string missingMaterials = string.Join(", ", 
                                    recipeVM.RequiredMaterials
                                        .Where(m => !m.IsAvailable)
                                        .Select(m => $"{m.Name} ({m.Available}/{m.Required})"));
                                    
                                SketchBlade.Services.LoggingService.LogInfo($"[CraftingPanel] Недостающие материалы: {missingMaterials}");
                                
                                // Показываем уведомление о недостатке материалов
                                ShowMissingMaterialsNotification(missingMaterials);
                            }
                        }
                        else
                        {
                            SketchBlade.Services.LoggingService.LogError("[CraftingPanel] DataContext не является SimplifiedCraftingViewModel");
                        }
                    }
                    else
                    {
                        SketchBlade.Services.LoggingService.LogError($"[CraftingPanel] DataContext слота не является SimplifiedCraftingRecipeViewModel: {slot.DataContext?.GetType().Name ?? "null"}");
                    }
                }
            }
            catch (Exception ex)
            {
                SketchBlade.Services.LoggingService.LogError($"[CraftingPanel] Ошибка при обработке клика по предмету крафта: {ex.Message}", ex);
            }
        }
        
        // Новый метод для анимации нажатия на слот
        private void AnimateSlotClick(CoreInventorySlot slot)
        {
            try
            {
                // Создаем простую анимацию для обратной связи
                var scaleDown = new System.Windows.Media.Animation.DoubleAnimation(0.9, TimeSpan.FromMilliseconds(100));
                var scaleUp = new System.Windows.Media.Animation.DoubleAnimation(1.0, TimeSpan.FromMilliseconds(100));
                
                scaleDown.Completed += (s, e) => {
                    slot.RenderTransform.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, scaleUp);
                    slot.RenderTransform.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, scaleUp);
                };
                
                // Убеждаемся, что у слота есть RenderTransform
                if (slot.RenderTransform == null || !(slot.RenderTransform is System.Windows.Media.ScaleTransform))
                {
                    slot.RenderTransform = new System.Windows.Media.ScaleTransform(1.0, 1.0);
                    slot.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);
                }
                
                // Запускаем анимацию
                slot.RenderTransform.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, scaleDown);
                slot.RenderTransform.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, scaleDown);
            }
            catch (Exception ex)
            {
                SketchBlade.Services.LoggingService.LogError($"[CraftingPanel] Ошибка при анимации слота: {ex.Message}", ex);
            }
        }
        
        // Новый метод для принудительного обновления UI инвентаря
        private void PerformInventoryUIUpdate()
        {
            try
            {
                SketchBlade.Services.LoggingService.LogDebug("[CraftingPanel] PerformInventoryUIUpdate: Начало обновления UI инвентаря");
                
                // Получаем доступ к GameData
                var gameData = System.Windows.Application.Current.Resources["GameData"] as GameData;
                if (gameData != null)
                {
                    // Отправляем сигнал об изменении инвентаря напрямую
                    gameData.Inventory.OnInventoryChanged();
                    gameData.OnPropertyChanged(nameof(gameData.Inventory));
                    
                    SketchBlade.Services.LoggingService.LogDebug("[CraftingPanel] Отправлены сигналы изменения инвентаря через GameData");
                }
                
                // Ищем InventoryViewModel разными способами
                var inventoryVM = FindInventoryViewModel();
                if (inventoryVM != null)
                {
                    // Принудительно обновляем UI с задержкой для корректного отображения
                    System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        try
                        {
                            SketchBlade.Services.LoggingService.LogDebug("[CraftingPanel] Начинаем обновление UI через Dispatcher");
                            
                            // Выполняем множественное обновление для большей надежности
                            inventoryVM.RefreshAllSlots();
                            inventoryVM.ForceUIUpdate();
                            
                            // Явно отправляем уведомления об изменении свойств
                            inventoryVM.NotifyPropertyChanged("InventorySlots");
                            inventoryVM.NotifyPropertyChanged("PlayerInventory");
                            
                            // Явно обновляем UI элементы связанные с инвентарем
                            foreach (var element in FindVisualChildren<System.Windows.Controls.ItemsControl>(System.Windows.Application.Current.MainWindow))
                            {
                                if (element.Name.Contains("Inventory") || element.Name.Contains("Slot"))
                                {
                                    element.Items.Refresh();
                                    element.UpdateLayout();
                                    SketchBlade.Services.LoggingService.LogDebug($"[CraftingPanel] Обновлен ItemsControl: {element.Name}");
                                }
                            }
                            
                            SketchBlade.Services.LoggingService.LogDebug("[CraftingPanel] Обновление UI через Dispatcher выполнено");
                        }
                        catch (Exception ex)
                        {
                            SketchBlade.Services.LoggingService.LogError($"[CraftingPanel] Ошибка при обновлении UI через Dispatcher: {ex.Message}", ex);
                        }
                    }), System.Windows.Threading.DispatcherPriority.Render);
                    
                    SketchBlade.Services.LoggingService.LogDebug("[CraftingPanel] Задание на обновление UI через Dispatcher запланировано");
                }
                else
                {
                    SketchBlade.Services.LoggingService.LogError("[CraftingPanel] InventoryViewModel не найден");
                }
            }
            catch (Exception ex)
            {
                SketchBlade.Services.LoggingService.LogError($"[CraftingPanel] Ошибка при обновлении UI инвентаря: {ex.Message}", ex);
            }
        }
        
        // Вспомогательный метод для нахождения всех дочерних элементов определенного типа
        private IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj == null) yield break;
            
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(depObj, i);
                
                if (child is T typedChild)
                    yield return typedChild;
                
                foreach (var childOfChild in FindVisualChildren<T>(child))
                    yield return childOfChild;
            }
        }
        
        // Метод для отображения уведомления об успешном крафте
        private void ShowCraftSuccessNotification(string itemName)
        {
            try
            {
                SketchBlade.Services.LoggingService.LogDebug($"[CraftingPanel] Показываем уведомление об успешном крафте: {itemName}");
                
                // Здесь можно было бы показать красивое всплывающее уведомление,
                // но для простоты просто логируем событие
                
                // Для демонстрации действия крафта выводим информацию в консоль
                SketchBlade.Services.LoggingService.LogInfo($"=== ПРЕДМЕТ УСПЕШНО СОЗДАН: {itemName} ===");
            }
            catch (Exception ex)
            {
                SketchBlade.Services.LoggingService.LogError($"[CraftingPanel] Ошибка при отображении уведомления: {ex.Message}", ex);
            }
        }
        
        // Метод для отображения уведомления о недостающих материалах
        private void ShowMissingMaterialsNotification(string missingMaterials)
        {
            try
            {
                SketchBlade.Services.LoggingService.LogDebug($"[CraftingPanel] Показываем уведомление о недостающих материалах: {missingMaterials}");
                
                // Здесь можно было бы показать красивое всплывающее уведомление,
                // но для простоты просто логируем событие
                
                // Для демонстрации выводим информацию в консоль
                SketchBlade.Services.LoggingService.LogInfo($"=== НЕДОСТАТОЧНО МАТЕРИАЛОВ: {missingMaterials} ===");
            }
            catch (Exception ex)
            {
                SketchBlade.Services.LoggingService.LogError($"[CraftingPanel] Ошибка при отображении уведомления: {ex.Message}", ex);
            }
        }
        
        private InventoryViewModel? FindInventoryViewModel()
        {
            try
            {
                // РС‰РµРј InventoryViewModel РІ РІРёР·СѓР°Р»СЊРЅРѕРј РґРµСЂРµРІРµ
                var parent = this.Parent;
                while (parent != null)
                {
                    if (parent is FrameworkElement element && element.DataContext is InventoryViewModel inventoryVM)
                    {
                        return inventoryVM;
                    }
                    
                    if (parent is FrameworkContentElement contentElement && contentElement.DataContext is InventoryViewModel inventoryVM2)
                    {
                        return inventoryVM2;
                    }
                    
                    parent = LogicalTreeHelper.GetParent(parent);
                }
                
                // РџРѕРїСЂРѕР±СѓРµРј РЅР°Р№С‚Рё С‡РµСЂРµР· Application.Current.Resources
                if (System.Windows.Application.Current.Resources.Contains("InventoryViewModel"))
                {
                    return System.Windows.Application.Current.Resources["InventoryViewModel"] as InventoryViewModel;
                }
                
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in FindInventoryViewModel: {ex.Message}");
                return null;
            }
        }
    }
} 
