using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Data;
using System.Windows.Shapes;
using System.Windows.Threading;
using SketchBlade.Models;
using SketchBlade.Helpers;
using SketchBlade.ViewModels;
using static System.Windows.Media.Colors;
using System.IO;
using SketchBlade.Views.Controls;
using SketchBlade.Services;
using SketchBlade.Utilities;

namespace SketchBlade.Views.Controls
{
    public partial class CoreInventorySlot : UserControl
    {
        public event EventHandler<MouseButtonEventArgs>? SlotMouseDown;
        public event EventHandler<DragEventArgs>? SlotDrop;
        public event EventHandler<MouseEventArgs>? SlotMouseEnter;
        public event EventHandler<MouseEventArgs>? SlotMouseLeave;

        public event EventHandler<MouseEventArgs>? SlotDragInitiated;
        public event EventHandler<DragEventArgs>? SlotDragEnter;
        public event EventHandler<DragEventArgs>? SlotDragLeave;
        public event EventHandler<DragEventArgs>? SlotDragOver;
        public event EventHandler<ValidateItemForSlotEventArgs>? ValidateItemForSlot;
        public event EventHandler<SplitStackEventArgs>? SplitStackRequested;

        public event EventHandler<MoveItemData>? ItemMoveRequested;
        public event EventHandler<EquipItemData>? ItemEquipRequested;
        public event EventHandler<ItemTrashEventArgs>? ItemTrashRequested;

        public static readonly DependencyProperty ItemProperty =
            DependencyProperty.Register("Item", typeof(Item), typeof(CoreInventorySlot),
                new PropertyMetadata(null, OnItemChanged));

        public static readonly DependencyProperty SlotTypeProperty =
            DependencyProperty.Register("SlotType", typeof(string), typeof(CoreInventorySlot),
                new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty SlotIndexProperty =
            DependencyProperty.Register("SlotIndex", typeof(int), typeof(CoreInventorySlot),
                new PropertyMetadata(0));

        public static readonly DependencyProperty IsSelectedProperty =
            DependencyProperty.Register("IsSelected", typeof(bool), typeof(CoreInventorySlot),
                new PropertyMetadata(false, OnIsSelectedChanged));

        public static readonly DependencyProperty CanAcceptDragProperty =
            DependencyProperty.Register("CanAcceptDrag", typeof(bool), typeof(CoreInventorySlot),
                new PropertyMetadata(true));

        private static readonly SolidColorBrush SlotNormalBrush = new SolidColorBrush(Color.FromRgb(221, 219, 216));
        private static readonly SolidColorBrush SlotHoverBrush = new SolidColorBrush(Color.FromRgb(200, 197, 194));
        private static readonly SolidColorBrush SlotSelectedBrush = new SolidColorBrush(Color.FromRgb(255, 204, 128));

        private static readonly SolidColorBrush CommonItemBrush = new SolidColorBrush(Color.FromRgb(189, 189, 189));
        private static readonly SolidColorBrush UncommonItemBrush = new SolidColorBrush(Color.FromRgb(76, 175, 80));
        private static readonly SolidColorBrush RareItemBrush = new SolidColorBrush(Color.FromRgb(33, 150, 243));
        private static readonly SolidColorBrush EpicItemBrush = new SolidColorBrush(Color.FromRgb(156, 39, 176));
        private static readonly SolidColorBrush LegendaryItemBrush = new SolidColorBrush(Color.FromRgb(255, 193, 7));

        // РРЎРџР РђР'Р›Р•РќРћ: РЎС‚Р°С‚РёС‡РµСЃРєРёРµ РїРѕР»СЏ РґР»СЏ throttling Р»РѕРіРёСЂРѕРІР°РЅРёСЏ
        private static int _nullItemLogCounter = 0;
        private static DateTime _lastLogTime = DateTime.MinValue;
        private static string _lastLoggedOperation = "";

        private BitmapImage? _itemImage;
        private Point _dragStartPoint;
        private bool _isDragging;
        private bool _isRightDragging;
        private SketchBlade.Views.Controls.ItemTooltip? _tooltip;

        private bool _isShowingSplitDialog;
        private Popup? _splitStackPopup;
        private Slider? _splitStackSlider;
        private TextBlock? _splitStackValueText;
        private Button? _splitStackConfirmButton;
        private Button? _splitStackCancelButton;

        private bool _isSubscribedToSettings = false;

        public Item? Item
        {
            get { return (Item?)GetValue(ItemProperty); }
            set { SetValue(ItemProperty, value); }
        }

        public string SlotType
        {
            get { return (string)GetValue(SlotTypeProperty); }
            set
            {
                try
                {
                    if (string.IsNullOrEmpty(value))
                    {
                        SetValue(SlotTypeProperty, "Inventory");
                        return;
                    }

                    if (Enum.TryParse<Models.SlotType>(value, out var _))
                    {
                        SetValue(SlotTypeProperty, value);
                    }
                    else
                    {
                        SetValue(SlotTypeProperty, "Inventory");
                    }
                    
                    // РџРµСЂРµРїРѕРґРїРёСЃС‹РІР°РµРјСЃСЏ РЅР° РЅР°СЃС‚СЂРѕР№РєРё РєРѕРіРґР° SlotType СѓСЃС‚Р°РЅР°РІР»РёРІР°РµС‚СЃСЏ
                    if (!string.IsNullOrEmpty(value))
                    {
                        SubscribeToSettingsChanges();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error setting SlotType: {ex.Message}");
                    SetValue(SlotTypeProperty, "Inventory");
                }
            }
        }

        public int SlotIndex
        {
            get { return (int)GetValue(SlotIndexProperty); }
            set 
            { 
                SetValue(SlotIndexProperty, value);
                
                // РџРµСЂРµРїРѕРґРїРёСЃС‹РІР°РµРјСЃСЏ РЅР° РЅР°СЃС‚СЂРѕР№РєРё РєРѕРіРґР° SlotIndex СѓСЃС‚Р°РЅР°РІР»РёРІР°РµС‚СЃСЏ
                if (value >= 0 && !string.IsNullOrEmpty(SlotType))
                {
                    SubscribeToSettingsChanges();
                }
            }
        }

        public bool IsSelected
        {
            get { return (bool)GetValue(IsSelectedProperty); }
            set { SetValue(IsSelectedProperty, value); }
        }

        public bool CanAcceptDrag
        {
            get { return (bool)GetValue(CanAcceptDragProperty); }
            set { SetValue(CanAcceptDragProperty, value); }
        }

        public bool IsEmpty => Item == null;

        public CoreInventorySlot()
        {
            InitializeComponent();
            
            CoreSlotBorder.BorderBrush = SlotNormalBrush;
            CoreSlotBorder.BorderThickness = new Thickness(1);
            
            // Subscribe to game settings changes for display preferences
            SubscribeToSettingsChanges();
            
            Loaded += CoreInventorySlot_Loaded;
            Unloaded += CoreInventorySlot_Unloaded;
            DataContextChanged += CoreInventorySlot_DataContextChanged;
            
            // Добавляем обработчик MouseDown программно для тестирования
            this.MouseDown += (s, e) => {
                LoggingService.LogInfo($"[DragDrop] *** ПРЯМОЕ СОБЫТИЕ MouseDown *** для {SlotType}[{SlotIndex}], Item: {Item?.Name ?? "NULL"}");
            };
            
            LoggingService.LogInfo($"[DragDrop] CoreInventorySlot создан для {SlotType}[{SlotIndex}]");
        }

        private void CoreInventorySlot_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                LoggingService.LogInfo($"[DragDrop] CoreInventorySlot загружен для {SlotType}[{SlotIndex}], Item: {Item?.Name ?? "NULL"}");
                
                UpdateSlotVisuals();
                UpdateSelectionAppearance();
                
                // Логируем только проблемные случаи
                if (DataContext is InventoryViewModel)
                {
                    // Все в порядке
                }
                else
                {
                    LoggingService.LogWarning($"[UI] Неожиданный DataContext для {SlotType}[{SlotIndex}]: {DataContext?.GetType().Name ?? "null"}");
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Ошибка при загрузке CoreInventorySlot {SlotType}[{SlotIndex}]: {ex.Message}", ex);
            }
        }

        private void CoreInventorySlot_Unloaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Unsubscribe from game state changes
                if (Application.Current.Resources.Contains("GameData"))
                {
                    var gameData = Application.Current.Resources["GameData"] as GameData;
                    if (gameData != null)
                    {
                        gameData.PropertyChanged -= GameState_PropertyChanged;
                    }
                }
                
                // Unsubscribe from settings changes
                if (Application.Current.Resources.Contains("GameSettings"))
                {
                    var settings = Application.Current.Resources["GameSettings"] as GameSettings;
                    if (settings != null)
                    {
                        settings.PropertyChanged -= Settings_PropertyChanged;
                        _isSubscribedToSettings = false;
                    }
                }
                
                // Clean up tooltip
                HideTooltip();
                
                // Clean up popup
                CloseSplitStackDialog();
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"CoreInventorySlot_Unloaded: {ex.Message}", ex);
            }
        }

        private void CoreInventorySlot_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            try
            {
                LoggingService.LogDebug($"[UI] DataContext for {SlotType}[{SlotIndex}] changed from {e.OldValue?.GetType().Name ?? "null"} to {e.NewValue?.GetType().Name ?? "null"}");

                if (e.NewValue is InventoryViewModel viewModel && !string.IsNullOrEmpty(SlotType) && SlotIndex >= 0)
                {
                    var wrapper = viewModel.GetSlotWrapper(SlotType, SlotIndex);
                    if (wrapper != null)
                    {
                        // Clear any existing binding on ItemProperty first.
                        // This is important if the XAML binding was incorrect (e.g., bound to ViewModel.Item).
                        BindingOperations.ClearBinding(this, ItemProperty);

                        var itemBinding = new Binding("Item")
                        {
                            Source = wrapper, // Bind directly to the specific wrapper's Item property
                            Mode = BindingMode.OneWay 
                        };
                        BindingOperations.SetBinding(this, ItemProperty, itemBinding);
                        LoggingService.LogInfo($"[UI] Corrected Item binding for {SlotType}[{SlotIndex}] to source: wrapper.Item. Current item on wrapper: {(wrapper.Item?.Name) ?? "null"}");
                        
                        // After correcting the binding, explicitly set the Item DP to sync with the wrapper's current state.
                        // This ensures OnItemChanged fires if the value is different from what the DP previously held.
                        SetValue(ItemProperty, wrapper.Item); 
                    }
                    else
                    {
                        LoggingService.LogWarning($"[UI] In DataContextChanged for {SlotType}[{SlotIndex}], DataContext is InventoryViewModel, but NO wrapper found. Clearing Item binding and setting Item to null.");
                        BindingOperations.ClearBinding(this, ItemProperty);
                        SetValue(ItemProperty, null); // Explicitly set to null
                    }
                }
                else if (e.NewValue is InventorySlotWrapper directWrapper)
                {
                    LoggingService.LogInfo($"[UI] DataContext for {SlotType}[{SlotIndex}] is directly an InventorySlotWrapper. Standard XAML Item={{Binding Item}} should apply. Current item on wrapper: {(directWrapper.Item?.Name) ?? "null"}");
                    // If XAML binding Item="{Binding Item}" exists, it will use directWrapper as source.
                    // If DP's current value differs from wrapper.Item, binding should update it, and OnItemChanged will fire.
                    // To be absolutely sure, we can also sync it here, similar to the ViewModel case.
                    // However, this might interfere if a TwoWay binding is intended from XAML and this runs before initial XAML bind.
                    // For now, rely on XAML binding from wrapper DataContext. If issues persist, revisit.
                }
                else if (e.NewValue == null)
                {
                    LoggingService.LogInfo($"[UI] DataContext for {SlotType}[{SlotIndex}] is now null. Clearing Item binding and setting Item to null.");
                    BindingOperations.ClearBinding(this, ItemProperty);
                    SetValue(ItemProperty, null); // Explicitly set to null
                }
                else if (SlotType == "CraftResult" && e.NewValue != null)
                {
                     // Existing CraftResult logic
                    var binding = BindingOperations.GetBinding(this, ItemProperty);
                    if (binding == null) 
                    {
                        var newBinding = new Binding("Result")
                        {
                            Source = e.NewValue,
                            Mode = BindingMode.OneWay
                        };
                        BindingOperations.SetBinding(this, ItemProperty, newBinding);
                        LoggingService.LogDebug($"[UI] Set up Result -> Item binding for {SlotType}[{SlotIndex}]");
                    }
                }


                // UpdateSlotVisuals is called by OnItemChanged if SetValue above changes the DP.
                // If SetValue doesn't change the DP (value was already same), 
                // but visuals might be out of sync for other reasons (e.g. DataContext change without Item DP change),
                // an explicit call here might be needed.
                // However, OnItemChanged should be the primary driver for visual updates based on Item.
                // Let's ensure OnItemChanged *always* calls UpdateSlotVisuals, even if newItem == oldItem (e.g. by reference but content changed - not the case here).
                // The current OnItemChanged already calls UpdateSlotVisuals.
                // Forcing one more UpdateSlotVisuals here after potential binding changes.
                UpdateSlotVisuals();
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error in CoreInventorySlot_DataContextChanged for {SlotType}[{SlotIndex}]: {ex.Message}", ex);
            }
        }

        private void SubscribeToSettingsChanges()
        {
            try
            {
                var GameData = Application.Current.Resources["GameData"] as GameData;
                if (GameData != null && GameData.Settings != null)
                {
                    // Подписываемся на события PropertyChanged для Settings
                    GameData.Settings.PropertyChanged += Settings_PropertyChanged;
                    
                    // Также подписываемся на изменения в самом GameData
                    GameData.PropertyChanged += GameState_PropertyChanged;
                    
                    _isSubscribedToSettings = true;
                    
                    // Убираем избыточное логирование - только при ошибках
                    // LoggingService.LogDebug($"CoreInventorySlot ({SlotType}[{SlotIndex}]) subscribed to settings changes");
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Ошибка подписки на изменения настроек для слота {SlotType}[{SlotIndex}]: {ex.Message}");
            }
        }
        
        private void GameState_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(GameData.Settings))
            {
                // Переподписываемся на новые настройки если они изменились
                var GameData = sender as GameData;
                if (GameData?.Settings != null)
                {
                    // Обновляем подписку
                    GameData.Settings.PropertyChanged -= Settings_PropertyChanged;
                    GameData.Settings.PropertyChanged += Settings_PropertyChanged;
                }
            }
        }
        
        private void Settings_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // Поскольку описания предметов теперь всегда включены, нет необходимости в обработке изменений настроек
        }

        private static void OnItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CoreInventorySlot slot)
            {
                var newItem = e.NewValue as Item;
                var oldItem = e.OldValue as Item;
                
                // Log all calls to OnItemChanged to see if it's firing correctly
                LoggingService.LogInfo($"[UI] OnItemChanged CALLED for {slot.SlotType}[{slot.SlotIndex}]: '{oldItem?.Name ?? "null"}' (Hash:{oldItem?.GetHashCode()}) -> '{newItem?.Name ?? "null"}' (Hash:{newItem?.GetHashCode()}). Are they same object: {ReferenceEquals(oldItem, newItem)}");
                
                // Original logging condition for significant changes (can be kept for less noise if preferred later)
                // if ((newItem == null) != (oldItem == null) || 
                //    (newItem != null && oldItem != null && (newItem.Name != oldItem.Name || newItem.StackSize != oldItem.StackSize) )) // Added StackSize check
                // {
                //    LoggingService.LogInfo($"[UI] OnItemChanged (Significant): {slot.SlotType}[{slot.SlotIndex}] {oldItem?.Name ?? "null"}({oldItem?.StackSize}) -> {newItem?.Name ?? "null"}({newItem?.StackSize})");
                // }
                
                // Немедленно обновляем визуальное отображение
                slot.UpdateSlotVisuals(); // This uses slot.Item (the new value of the DP)
                
                // Принудительно обновляем в следующем UI цикле
                slot.Dispatcher.BeginInvoke(new Action(() =>
                {
                    // LoggingService.LogDebug($"[UI] OnItemChanged Dispatcher.BeginInvoke for {slot.SlotType}[{slot.SlotIndex}]");
                    slot.UpdateSlotVisuals(); // Call again to be sure, uses current slot.Item
                    slot.InvalidateVisual();
                    slot.UpdateLayout();
                    
                    if (slot.Parent is Panel parentPanel) // Renamed variable
                    {
                        parentPanel.InvalidateArrange();
                        parentPanel.UpdateLayout();
                    }
                }), DispatcherPriority.DataBind); // Using DataBind priority
            }
        }

        private static void OnIsSelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CoreInventorySlot slot)
            {
                slot.UpdateSelectionAppearance();
            }
        }

        private void UpdateSlotVisuals()
        {
            try
            {
                LoggingService.LogInfo($"[UI] UpdateSlotVisuals НАЧАЛО для {SlotType}[{SlotIndex}]: Item = {Item?.Name ?? "null"}");
                
                if (Item != null)
                {
                    LoggingService.LogInfo($"[UI] Предмет найден: {Item.Name}, SpritePath: '{Item.SpritePath}', StackSize: {Item.StackSize}");
                    
                    LoadAndSetItemImage();

                    if (Item.IsStackable && Item.StackSize > 1)
                    {
                        SafeSetText(CoreItemCount, Item.StackSize.ToString());
                        SafeSetVisibility(CoreItemCount, Visibility.Visible);
                        LoggingService.LogInfo($"[UI] Показываем счетчик стека: {Item.StackSize} для {SlotType}[{SlotIndex}]");
                    }
                    else
                    {
                        LoggingService.LogInfo($"[UI] Скрываем счетчик стека для {SlotType}[{SlotIndex}] (не стекаемый или размер = 1)");
                        SafeSetVisibility(CoreItemCount, Visibility.Collapsed);
                    }

                    UpdateRarityIndicator();
                    LoggingService.LogInfo($"[UI] Обновили индикатор редкости для {SlotType}[{SlotIndex}]");
                }
                else
                {
                    LoggingService.LogInfo($"[UI] Предмет null, очищаем слот {SlotType}[{SlotIndex}]");
                    SafeSetImageSource(CoreItemImage, null);
                    SafeSetVisibility(CoreItemImage, Visibility.Collapsed);
                    SafeSetVisibility(CoreItemCount, Visibility.Collapsed);
                    SafeSetVisibility(RarityIndicator, Visibility.Collapsed);
                    _itemImage = null;
                    LoggingService.LogInfo($"[UI] Очистили все визуальные элементы для {SlotType}[{SlotIndex}]");
                }
                
                // Принудительно обновляем привязки данных и визуальное отображение
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    LoggingService.LogInfo($"[UI] Dispatcher.BeginInvoke НАЧАЛО для {SlotType}[{SlotIndex}]");
                    
                    // Обновляем layout всех элементов
                    if (CoreItemImage != null)
                    {
                        CoreItemImage.UpdateLayout();
                        CoreItemImage.InvalidateVisual();
                        LoggingService.LogDebug($"[UI] Обновили CoreItemImage для {SlotType}[{SlotIndex}]");
                    }
                    if (CoreItemCount != null)
                    {
                        CoreItemCount.UpdateLayout();
                        CoreItemCount.InvalidateVisual();
                        LoggingService.LogDebug($"[UI] Обновили CoreItemCount для {SlotType}[{SlotIndex}]");
                    }
                    if (RarityIndicator != null)
                    {
                        RarityIndicator.UpdateLayout();
                        RarityIndicator.InvalidateVisual();
                        LoggingService.LogDebug($"[UI] Обновили RarityIndicator для {SlotType}[{SlotIndex}]");
                    }
                    
                    // Обновляем весь контрол
                    this.UpdateLayout();
                    this.InvalidateVisual();
                    LoggingService.LogInfo($"[UI] Dispatcher.BeginInvoke КОНЕЦ для {SlotType}[{SlotIndex}]");
                }), DispatcherPriority.Render);
                
                LoggingService.LogInfo($"[UI] UpdateSlotVisuals КОНЕЦ для {SlotType}[{SlotIndex}]");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error in UpdateSlotVisuals: {ex.Message}", ex);
            }
        }

        private void LoadAndSetItemImage()
        {
            try
            {
                LoggingService.LogInfo($"[UI] LoadAndSetItemImage НАЧАЛО для {SlotType}[{SlotIndex}]");
                
                if (Item == null)
                {
                    LoggingService.LogInfo($"[UI] Item is null, clearing image for {SlotType}[{SlotIndex}]");
                    _itemImage = null;
                    SafeSetImageSource(CoreItemImage, null);
                    SafeSetVisibility(CoreItemImage, Visibility.Collapsed);
                    SafeSetVisibility(CoreItemCount, Visibility.Collapsed);
                    return;
                }

                string imagePath = Item.SpritePath;
                LoggingService.LogInfo($"[UI] Item {Item.Name} has SpritePath: '{imagePath}' for {SlotType}[{SlotIndex}]");

                // ИСПРАВЛЕНИЕ: Если путь пустой, устанавливаем дефолтный
                if (string.IsNullOrEmpty(imagePath))
                {
                    LoggingService.LogWarning($"Item {Item.Name} has empty SpritePath, using default image");
                    imagePath = AssetPaths.DEFAULT_IMAGE;
                    
                    // ВАЖНО: Сохраняем правильный путь в самом предмете для будущего использования
                    Item.SpritePath = imagePath;
                }

                try
                {
                    // ИСПРАВЛЕНИЕ: Проверяем существование файла
                    string fullPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, imagePath);
                    LoggingService.LogInfo($"[UI] Checking image file: {fullPath} for {SlotType}[{SlotIndex}]");
                    
                    if (!System.IO.File.Exists(fullPath))
                    {
                        LoggingService.LogWarning($"Image file not found: {fullPath}, using default image");
                        imagePath = AssetPaths.DEFAULT_IMAGE;
                        
                        // ВАЖНО: Сохраняем правильный путь в самом предмете для будущего использования
                        Item.SpritePath = imagePath;
                    }
                    
                    // Принудительная загрузка изображения через ImageHelper
                    LoggingService.LogInfo($"[UI] Loading image via ImageHelper: {imagePath} for {SlotType}[{SlotIndex}]");
                    _itemImage = ImageHelper.LoadImage(imagePath);
                    
                    // Проверяем, что изображение загружено
                    if (_itemImage == null || _itemImage.PixelWidth == 0)
                    {
                        LoggingService.LogWarning($"Failed to load image {imagePath}, using default image for {SlotType}[{SlotIndex}]");
                        _itemImage = ImageHelper.GetDefaultImage();
                    }
                    else
                    {
                        LoggingService.LogInfo($"[UI] Successfully loaded image {imagePath} ({_itemImage.PixelWidth}x{_itemImage.PixelHeight}) for {SlotType}[{SlotIndex}]");
                    }
                    
                    // Устанавливаем изображение
                    LoggingService.LogInfo($"[UI] Setting image source for {SlotType}[{SlotIndex}]");
                    SafeSetImageSource(CoreItemImage, _itemImage);
                    SafeSetVisibility(CoreItemImage, Visibility.Visible);
                    LoggingService.LogInfo($"[UI] Image source set and visibility = Visible for {SlotType}[{SlotIndex}]");
                    
                    // Показываем количество, если стак > 1
                    if (Item.StackSize > 1)
                    {
                        LoggingService.LogInfo($"[UI] Setting stack count {Item.StackSize} for {SlotType}[{SlotIndex}]");
                        SafeSetText(CoreItemCount, Item.StackSize.ToString());
                        SafeSetVisibility(CoreItemCount, Visibility.Visible);
                    }
                    else
                    {
                        LoggingService.LogInfo($"[UI] Hiding stack count for {SlotType}[{SlotIndex}] (stack size = {Item.StackSize})");
                        SafeSetVisibility(CoreItemCount, Visibility.Collapsed);
                    }

                    // Показываем индикатор редкости
                    LoggingService.LogInfo($"[UI] Updating rarity indicator for {SlotType}[{SlotIndex}]");
                    UpdateRarityIndicator();
                }
                catch (Exception ex)
                {
                    LoggingService.LogError($"Error loading image for {Item.Name}: {ex.Message}", ex);
                    
                    // Используем дефолтное изображение при ошибке
                    _itemImage = ImageHelper.GetDefaultImage();
                    SafeSetImageSource(CoreItemImage, _itemImage);
                    SafeSetVisibility(CoreItemImage, Visibility.Visible);
                }
                
                LoggingService.LogInfo($"[UI] LoadAndSetItemImage КОНЕЦ для {SlotType}[{SlotIndex}]");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error in LoadAndSetItemImage: {ex.Message}", ex);
            }
        }

        private void SafeSetImageSource(System.Windows.Controls.Image imageControl, BitmapImage? source)
        {
            if (imageControl == null) 
            {
                LoggingService.LogWarning($"[UI] SafeSetImageSource: imageControl is null for {SlotType}[{SlotIndex}]");
                return;
            }

            LoggingService.LogInfo($"[UI] SafeSetImageSource: Setting image source for {SlotType}[{SlotIndex}] (source is {(source == null ? "null" : "not null")})");

            if (!Dispatcher.CheckAccess())
            {
                LoggingService.LogDebug($"[UI] SafeSetImageSource: Using Dispatcher.Invoke for {SlotType}[{SlotIndex}]");
                Dispatcher.Invoke(() => { 
                    imageControl.Source = source; 
                    LoggingService.LogDebug($"[UI] SafeSetImageSource: Image source set via Dispatcher for {SlotType}[{SlotIndex}]");
                });
            }
            else
            {
                LoggingService.LogDebug($"[UI] SafeSetImageSource: Setting directly for {SlotType}[{SlotIndex}]");
                imageControl.Source = source;
                LoggingService.LogDebug($"[UI] SafeSetImageSource: Image source set directly for {SlotType}[{SlotIndex}]");
            }
        }

        private void SafeSetVisibility(UIElement element, Visibility visibility)
        {
            if (element == null) 
            {
                LoggingService.LogWarning($"[UI] SafeSetVisibility: element is null for {SlotType}[{SlotIndex}]");
                return;
            }

            LoggingService.LogInfo($"[UI] SafeSetVisibility: Setting visibility to {visibility} for {SlotType}[{SlotIndex}]");

            if (!Dispatcher.CheckAccess())
            {
                LoggingService.LogDebug($"[UI] SafeSetVisibility: Using Dispatcher.Invoke for {SlotType}[{SlotIndex}]");
                Dispatcher.Invoke(() => { 
                    element.Visibility = visibility; 
                    LoggingService.LogDebug($"[UI] SafeSetVisibility: Visibility set via Dispatcher for {SlotType}[{SlotIndex}]");
                });
            }
            else
            {
                LoggingService.LogDebug($"[UI] SafeSetVisibility: Setting directly for {SlotType}[{SlotIndex}]");
                element.Visibility = visibility;
                LoggingService.LogDebug($"[UI] SafeSetVisibility: Visibility set directly for {SlotType}[{SlotIndex}]");
            }
        }

        private void SafeSetText(TextBlock textBlock, string text)
        {
            if (textBlock == null) 
            {
                LoggingService.LogWarning($"[UI] SafeSetText: textBlock is null for {SlotType}[{SlotIndex}]");
                return;
            }

            LoggingService.LogInfo($"[UI] SafeSetText: Setting text to '{text}' for {SlotType}[{SlotIndex}]");

            if (!Dispatcher.CheckAccess())
            {
                LoggingService.LogDebug($"[UI] SafeSetText: Using Dispatcher.Invoke for {SlotType}[{SlotIndex}]");
                Dispatcher.Invoke(() => { 
                    textBlock.Text = text; 
                    LoggingService.LogDebug($"[UI] SafeSetText: Text set via Dispatcher for {SlotType}[{SlotIndex}]");
                });
            }
            else
            {
                LoggingService.LogDebug($"[UI] SafeSetText: Setting directly for {SlotType}[{SlotIndex}]");
                textBlock.Text = text;
                LoggingService.LogDebug($"[UI] SafeSetText: Text set directly for {SlotType}[{SlotIndex}]");
            }
        }

        private void UpdateRarityIndicator()
        {
            if (Item != null)
            {
                SafeSetVisibility(RarityIndicator, Visibility.Visible);

                SolidColorBrush rarityBrush;
                switch (Item.Rarity)
                {
                    case ItemRarity.Common:
                        rarityBrush = CommonItemBrush;
                        break;
                    case ItemRarity.Uncommon:
                        rarityBrush = UncommonItemBrush;
                        break;
                    case ItemRarity.Rare:
                        rarityBrush = RareItemBrush;
                        break;
                    case ItemRarity.Epic:
                        rarityBrush = EpicItemBrush;
                        break;
                    case ItemRarity.Legendary:
                        rarityBrush = LegendaryItemBrush;
                        break;
                    default:
                        rarityBrush = CommonItemBrush;
                        break;
                }

                SafeSetFill(RarityIndicator, rarityBrush);
            }
            else
            {
                SafeSetVisibility(RarityIndicator, Visibility.Collapsed);
            }
        }

        private void SafeSetFill(Shape shape, Brush brush)
        {
            if (shape == null) return;

            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => { shape.Fill = brush; });
            }
            else
            {
                shape.Fill = brush;
            }
        }

        private void UpdateSelectionAppearance()
        {
            if (IsSelected)
            {
                CoreSlotBorder.BorderBrush = SlotSelectedBrush;
                CoreSlotBorder.BorderThickness = new Thickness(2);
            }
            else
            {
                CoreSlotBorder.BorderBrush = SlotNormalBrush;
                CoreSlotBorder.BorderThickness = new Thickness(1);
            }
        }

        private void SlotBorder_MouseDown(object sender, MouseButtonEventArgs e)
        {
            LoggingService.LogInfo($"[DragDrop] *** СОБЫТИЕ MOUSEDOWN ПОЛУЧЕНО *** для {SlotType}[{SlotIndex}], Item: {Item?.Name ?? "NULL"}");
            try
            {
                e.Handled = true;

                if (SlotType == "CraftResult" && Item != null)
                {
                    var viewModel = FindViewModel();
                    if (viewModel != null)
                    {
                        viewModel.TakeCraftResult();
                        // Log success since TakeCraftResult doesn't return bool
                        LoggingService.LogDebug($"TakeCraftResult called for slot {SlotIndex}");
                    }
                    else
                    {
                        MessageBox.Show("Could not find ViewModel for crafting");

                        if (Application.Current.Resources.Contains("InventoryViewModel"))
                        {
                            var directViewModel = Application.Current.Resources["InventoryViewModel"] as InventoryViewModel;
                            if (directViewModel != null)
                            {
                                directViewModel.TakeCraftResult();
                            }
                        }
                    }

                    SlotMouseDown?.Invoke(this, e);
                    return;
                }

                if (e.ChangedButton == MouseButton.Left && Item != null)
                {
                    LoggingService.LogInfo($"[DragDrop] MouseDown на {SlotType}[{SlotIndex}] с предметом {Item.Name}");
                    // _dragStartPoint = e.GetPosition(this); // Removed: Drag start point for local detection less relevant
                    // CoreSlotBorder.CaptureMouse(); // Removed: Let DoDragDrop handle capture
                    // LoggingService.LogInfo($"[DragDrop] Mouse captured для {SlotType}[{SlotIndex}]"); // Removed
                    e.Handled = true; // Keep e.Handled = true if we are potentially starting a drag sequence via the event
                }
                else if (e.ChangedButton == MouseButton.Right && Item != null && Item.IsStackable && Item.StackSize > 1)
                {
                    ShowSplitStackDialog();
                    e.Handled = true;
                }

                SlotMouseDown?.Invoke(this, e);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in SlotBorder_MouseDown: {ex.Message}");

                // Ensure mouse is not left captured if an error occurs before DoDragDrop
                // However, if we are not capturing here, this might not be necessary.
                // Consider if any other path could capture.
                // if (CoreSlotBorder.IsMouseCaptured)
                // {
                //    CoreSlotBorder.ReleaseMouseCapture();
                // }
            }
        }

        private void SlotBorder_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                // Проверяем, что предмет все еще существует перед началом drag-and-drop
                // This check is less critical now as drag initiation is moved, but keep for safety.
                if (e.LeftButton == MouseButtonState.Pressed && Item == null && CoreSlotBorder.IsMouseCaptured)
                {
                    // Если предмет исчез, сбрасываем состояние drag-and-drop
                    _isDragging = false;
                    CoreSlotBorder.ReleaseMouseCapture();
                    LoggingService.LogDebug($"[DragDrop] MouseMove on {SlotType}[{SlotIndex}]: Item became null while captured, releasing capture.");
                    return;
                }
                
                // Логируем только если есть потенциал для drag-and-drop
                if (e.LeftButton == MouseButtonState.Pressed && Item != null && CoreSlotBorder.IsMouseCaptured)
                {
                    // LoggingService.LogInfo($"[DragDrop] MouseMove на {SlotType}[{SlotIndex}], isDragging: {_isDragging}, MouseCaptured: {CoreSlotBorder.IsMouseCaptured}");
                }
                
                // The drag initiation logic (checking for drag distance and calling DoDragDrop)
                // will now be primarily handled by InventoryEventHandler.HandleInventorySlotMouseDown,
                // which is called via the SlotMouseDown event from SlotBorder_MouseDown.
                // This CoreInventorySlot.SlotBorder_MouseMove should primarily manage _isDragging state
                // and mouse capture release if the conditions for drag (like item disappearing) change.

                // If mouse is captured (meaning MouseDown occurred on an item) and left button is pressed
                if (CoreSlotBorder.IsMouseCaptured && e.LeftButton == MouseButtonState.Pressed)
                {
                    Point currentPosition = e.GetPosition(this);
                    Vector difference = _dragStartPoint - currentPosition;

                    // Check if the mouse has moved enough to be considered a drag
                    if (!_isDragging && (Math.Abs(difference.X) > SystemParameters.MinimumHorizontalDragDistance ||
                                         Math.Abs(difference.Y) > SystemParameters.MinimumVerticalDragDistance))
                    {
                        // If Item is null here, it means it disappeared between MouseDown and now.
                        // Release capture and don't start a drag.
                        // The actual DoDragDrop will be (or would have been) called by HandleInventorySlotMouseDown
                        // based on the ViewModel's state at the time of MouseDown.
                    }
                }
                // If the left button is released, or mouse is no longer captured, ensure _isDragging is false.
                // (This is also handled in OnMouseUp, but good to be robust).
                // else if (e.LeftButton == MouseButtonState.Released || !CoreSlotBorder.IsMouseCaptured)
                // {
                //     if (_isDragging)
                //     {
                //         _isDragging = false;
                //     }
                //     if (CoreSlotBorder.IsMouseCaptured) // Release if still captured for some reason
                //     {
                //        CoreSlotBorder.ReleaseMouseCapture();
                //     }
                // }
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Ошибка в SlotBorder_MouseMove: {ex.Message}", ex);
                _isDragging = false; // Reset state on error
                if (CoreSlotBorder.IsMouseCaptured)
                {
                    CoreSlotBorder.ReleaseMouseCapture();
                }
            }
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);

            try
            {
                _isDragging = false;

                if (CoreSlotBorder.IsMouseCaptured)
                {
                    CoreSlotBorder.ReleaseMouseCapture();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in OnMouseUp: {ex.Message}");
            }
        }

        private void SlotBorder_Drop(object sender, DragEventArgs e)
        {
            LoggingService.LogInfo($"[DragDrop] *** СОБЫТИЕ DROP ПОЛУЧЕНО *** для {SlotType}[{SlotIndex}]");
            try
            {
                LoggingService.LogInfo($"[DragDrop] Drop в {SlotType}[{SlotIndex}]");

                ShowDropTargetHighlight(false);
                ShowInvalidDropHighlight(false);
                UpdateSelectionAppearance();

                _isDragging = false;

                // Вызываем событие SlotDrop для обработки через InventoryDragDropHandler
                // ВСЯ логика обработки теперь происходит в InventoryDragDropHandler
                LoggingService.LogInfo($"[DragDrop] Вызываем SlotDrop событие для {SlotType}[{SlotIndex}]");
                SlotDrop?.Invoke(this, e);
                
                LoggingService.LogInfo($"[DragDrop] Drop завершен для {SlotType}[{SlotIndex}]");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"[DragDrop] Ошибка в SlotBorder_Drop: {ex.Message}", ex);

                ShowDropTargetHighlight(false);
                ShowInvalidDropHighlight(false);
                UpdateSelectionAppearance();
            }
        }

        private bool CanAcceptItemType(ItemSlotInfo sourceSlotInfo)
        {
            try
            {
                if (sourceSlotInfo.SlotType == SlotType && sourceSlotInfo.SlotIndex == SlotIndex)
                {
                    return false;
                }

                Item? sourceItem = FindViewModelItem(sourceSlotInfo);
                if (sourceItem == null)
                {
                    LoggingService.LogWarning($"[DragDrop] Исходный предмет не найден для {sourceSlotInfo.SlotType}[{sourceSlotInfo.SlotIndex}]");
                    return false;
                }

                bool basicCompatibility = SlotTypeCanAcceptItemType(SlotType, sourceItem.Type);

                if (ValidateItemForSlot != null)
                {
                    var args = new ValidateItemForSlotEventArgs(sourceItem, SlotType, SlotIndex);
                    ValidateItemForSlot.Invoke(this, args);
                    bool validationResult = args.IsValid;
                    return basicCompatibility && validationResult;
                }

                return basicCompatibility;
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"[DragDrop] Ошибка в CanAcceptItemType: {ex.Message}", ex);
                return false;
            }
        }

        private Item? FindViewModelItem(ItemSlotInfo slotInfo)
        {
            try
            {
                // Если предмет уже есть в slotInfo, используем его
                if (slotInfo.Item != null)
                {
                    return slotInfo.Item;
                }

                InventoryViewModel? viewModel = FindViewModel();
                if (viewModel == null)
                {
                    return null;
                }

                return viewModel.GetItemFromSlot(slotInfo.SlotType, slotInfo.SlotIndex);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in FindViewModelItem: {ex.Message}");
                return null;
            }
        }

        private bool SlotTypeCanAcceptItemType(string slotType, ItemType itemType)
        {
            switch (slotType)
            {
                case "Inventory":
                    return true;

                case "Trash":
                    return true;

                case "Quick":
                    return itemType == ItemType.Consumable;

                case "Helmet":
                    return itemType == ItemType.Helmet;

                case "Chestplate":
                    return itemType == ItemType.Chestplate;

                case "Leggings":
                    return itemType == ItemType.Leggings;

                case "Weapon":
                    return itemType == ItemType.Weapon;

                case "Shield":
                    return itemType == ItemType.Shield;

                case "Craft":
                    return true;

                case "CraftResult":
                    return false;

                default:
                    return true;
            }
        }

        private InventoryViewModel? FindViewModel()
        {
            try
            {
                if (Application.Current.Resources.Contains("InventoryViewModel"))
                {
                    var viewModel = Application.Current.Resources["InventoryViewModel"] as InventoryViewModel;
                    if (viewModel != null)
                    {
                        return viewModel;
                    }
                }

                var parent = Parent;
                int traversalDepth = 0;
                const int maxTraversalDepth = 15;

                while (parent != null && traversalDepth < maxTraversalDepth)
                {
                    traversalDepth++;

                    if (parent is FrameworkElement element && element.DataContext is InventoryViewModel viewModel)
                    {
                        if (!Application.Current.Resources.Contains("InventoryViewModel"))
                        {
                            Application.Current.Resources["InventoryViewModel"] = viewModel;
                        }

                        return viewModel;
                    }

                    if (parent is ContentControl contentControl)
                    {
                        parent = contentControl.Parent;
                    }
                    else if (parent is Panel panel)
                    {
                        parent = panel.Parent;
                    }
                    else if (parent is Border border)
                    {
                        parent = border.Parent;
                    }
                    else if (parent is ItemsControl itemsControl)
                    {
                        if (itemsControl.DataContext is InventoryViewModel itemsViewModel)
                        {
                            if (!Application.Current.Resources.Contains("InventoryViewModel"))
                            {
                                Application.Current.Resources["InventoryViewModel"] = itemsViewModel;
                            }

                            return itemsViewModel;
                        }

                        parent = itemsControl.Parent;
                    }
                    else if (parent is DependencyObject dependencyObject)
                    {
                        parent = VisualTreeHelper.GetParent(dependencyObject);
                    }
                    else
                    {
                        break;
                    }
                }

                try
                {
                    var mainWindow = Application.Current.MainWindow;
                    if (mainWindow != null && mainWindow.DataContext != null)
                    {
                        var property = mainWindow.DataContext.GetType().GetProperty("InventoryViewModel");
                        if (property != null)
                        {
                            var mainViewModelInventory = property.GetValue(mainWindow.DataContext) as InventoryViewModel;
                            if (mainViewModelInventory != null)
                            {
                                if (!Application.Current.Resources.Contains("InventoryViewModel"))
                                {
                                    Application.Current.Resources["InventoryViewModel"] = mainViewModelInventory;
                                }

                                return mainViewModelInventory;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error accessing MainWindow ViewModel: {ex.Message}");
                }

                return null;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error finding ViewModel: {ex.Message}");
                return null;
            }
        }

        private T? FindChildOfType<T>(DependencyObject parent, string name = null) where T : DependencyObject
        {
            try
            {
                if (parent == null)
                    return null;

                int childCount = VisualTreeHelper.GetChildrenCount(parent);

                for (int i = 0; i < childCount; i++)
                {
                    var child = VisualTreeHelper.GetChild(parent, i);

                    if (child is T typedChild &&
                        (string.IsNullOrEmpty(name) ||
                         (child is FrameworkElement fe && fe.Name == name)))
                    {
                        return typedChild;
                    }

                    var result = FindChildOfType<T>(child, name);
                    if (result != null)
                        return result;
                }

                return null;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in FindChildOfType: {ex.Message}");
                return null;
            }
        }

        private void RaiseItemMoveRequest(MoveItemData? data)
        {
            try
            {
                if (data == null)
                {
                    LoggingService.LogWarning("[DragDrop] RaiseItemMoveRequest: data is null");
                    return;
                }

                LoggingService.LogInfo($"[DragDrop] Перемещение предмета: {data.SourceType}[{data.SourceIndex}] -> {data.TargetType}[{data.TargetIndex}]");

                InventoryViewModel? viewModel = FindViewModel();

                if (data.TargetType == "Helmet" || data.TargetType == "Chestplate" ||
                    data.TargetType == "Leggings" || data.TargetType == "Weapon" ||
                    data.TargetType == "Shield")
                {
                    EquipItemData equipData = new EquipItemData
                    {
                        SourceType = data.SourceType,
                        SourceIndex = data.SourceIndex,
                        EquipmentType = data.TargetType
                    };

                    ItemEquipRequested?.Invoke(this, equipData);
                    return;
                }
                else if (data.TargetType == "Trash")
                {
                    ItemTrashRequested?.Invoke(this, new ItemTrashEventArgs
                    {
                        SourceType = data.SourceType,
                        SourceIndex = data.SourceIndex
                    });
                    return;
                }
                else if (data.TargetType == "Craft")
                {
                    if (viewModel != null)
                    {
                        viewModel.MoveToCraft(data);
                        return;
                    }
                }
                else if (data.SourceType == "Craft")
                {
                    if (viewModel != null)
                    {
                        viewModel.MoveToCraft(data);
                        return;
                    }
                }

                if (viewModel != null)
                {
                    viewModel.MoveItemBetweenSlots(
                        data.SourceType,
                        data.SourceIndex,
                        data.TargetType,
                        data.TargetIndex);
                    return;
                }

                ItemMoveRequested?.Invoke(this, data);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in RaiseItemMoveRequest: {ex.Message}");
            }
        }

        private void SlotBorder_DragEnter(object sender, DragEventArgs e)
        {
            try
            {
                if (!CanAcceptDrag)
                {
                    e.Effects = DragDropEffects.None;
                    return;
                }

                if (e.Data.GetDataPresent("ItemSlotInfo"))
                {
                    ItemSlotInfo sourceSlotInfo = (ItemSlotInfo)e.Data.GetData("ItemSlotInfo");

                    if (sourceSlotInfo.SlotType == SlotType && sourceSlotInfo.SlotIndex == SlotIndex)
                    {
                        e.Effects = DragDropEffects.None;
                        return;
                    }

                    if (CanAcceptItemType(sourceSlotInfo))
                    {
                        e.Effects = DragDropEffects.Move;
                        ShowDropTargetHighlight(true);
                        ShowInvalidDropHighlight(false);
                    }
                    else
                    {
                        e.Effects = DragDropEffects.None;
                        ShowDropTargetHighlight(false);
                        ShowInvalidDropHighlight(true);
                    }
                }
                else
                {
                    e.Effects = DragDropEffects.None;
                    ShowDropTargetHighlight(false);
                    ShowInvalidDropHighlight(false);
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"[DragDrop] Ошибка в DragEnter: {ex.Message}", ex);
                e.Effects = DragDropEffects.None;
                ShowDropTargetHighlight(false);
                ShowInvalidDropHighlight(false);
            }

            SlotDragEnter?.Invoke(this, e);
        }

        private void SlotBorder_DragOver(object sender, DragEventArgs e)
        {
            try
            {
                // РЈР±СЂР°РЅРѕ РёР·Р±С‹С‚РѕС‡РЅРѕРµ Р»РѕРіРёСЂРѕРІР°РЅРёРµ РґР»СЏ СѓР»СѓС‡С€РµРЅРёСЏ РїСЂРѕРёР·РІРѕРґРёС‚РµР»СЊРЅРѕСЃС‚Рё
                // File.AppendAllText("error_log.txt", 
                //     $"[{DateTime.Now:dd.MM.yyyy H:mm:ss}] [DEBUG] InventorySlot_DragOver: sender={sender?.GetType().Name}\r\n");

                if (!CanAcceptDrag)
                {
                    e.Effects = DragDropEffects.None;
                    return;
                }

                if (e.Data.GetDataPresent("ItemSlotInfo"))
                {
                    ItemSlotInfo sourceSlotInfo = e.Data.GetData("ItemSlotInfo") as ItemSlotInfo;

                    if (sourceSlotInfo != null && sourceSlotInfo.SlotType == SlotType && sourceSlotInfo.SlotIndex == SlotIndex)
                    {
                        ShowDropTargetHighlight(false);
                        ShowInvalidDropHighlight(false);
                        e.Effects = DragDropEffects.None;
                        return;
                    }

                    if (sourceSlotInfo != null && CanAcceptItemType(sourceSlotInfo))
                    {
                        e.Effects = DragDropEffects.Move;

                        ShowDropTargetHighlight(true);
                        ShowInvalidDropHighlight(false);
                    }
                    else
                    {
                        e.Effects = DragDropEffects.None;

                        ShowDropTargetHighlight(false);
                        ShowInvalidDropHighlight(true);
                    }
                }
                else
                {
                    e.Effects = DragDropEffects.None;

                    ShowDropTargetHighlight(false);
                    ShowInvalidDropHighlight(false);
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"[DragDrop] SlotBorder_DragOver exception: {ex.Message}", ex);

                e.Effects = DragDropEffects.None;
                ShowDropTargetHighlight(false);
                ShowInvalidDropHighlight(false);
            }

            SlotDragOver?.Invoke(this, e);
        }

        private void SlotBorder_DragLeave(object sender, DragEventArgs e)
        {
            try
            {
                ShowDropTargetHighlight(false);
                ShowInvalidDropHighlight(false);

                UpdateSelectionAppearance();

                SlotDragLeave?.Invoke(this, e);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in SlotBorder_DragLeave: {ex.Message}");
            }
        }

        private void SlotBorder_MouseEnter(object sender, MouseEventArgs e)
        {
            if (!IsSelected)
            {
                CoreSlotBorder.BorderBrush = SlotHoverBrush;
            }

            if (Item != null)
            {
                ShowTooltip();
            }
            
            OnSlotMouseEnter();
        }

        private void SlotBorder_MouseLeave(object sender, MouseEventArgs e)
        {
            UpdateSelectionAppearance();

            HideTooltip();

            SlotMouseLeave?.Invoke(this, e);
        }

        private void ShowDropTargetHighlight(bool show)
        {
            try
            {
                if (show)
                {
                    HighlightRect.Fill = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255));

                    DoubleAnimation fadeIn = new DoubleAnimation(0, 0.5, TimeSpan.FromMilliseconds(150));
                    HighlightRect.BeginAnimation(UIElement.OpacityProperty, fadeIn);
                }
                else
                {
                    if (!(HighlightRect.Fill is SolidColorBrush brush && brush.Color.R == 255 && brush.Color.G == 0))
                    {
                        DoubleAnimation fadeOut = new DoubleAnimation(0.5, 0, TimeSpan.FromMilliseconds(150));
                        HighlightRect.BeginAnimation(UIElement.OpacityProperty, fadeOut);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in ShowDropTargetHighlight: {ex.Message}");
            }
        }

        private void ShowInvalidDropHighlight(bool show)
        {
            try
            {
                if (show)
                {
                    HighlightRect.Fill = new SolidColorBrush(Color.FromArgb(120, 255, 0, 0));

                    HighlightRect.BeginAnimation(UIElement.OpacityProperty, null);

                    HighlightRect.Opacity = 0.8;
                }
                else
                {
                    if (HighlightRect.Fill is SolidColorBrush brush &&
                        brush.Color.R == 255 && brush.Color.G == 0 && brush.Color.B == 0)
                    {
                        DoubleAnimation fadeOut = new DoubleAnimation(0.8, 0, TimeSpan.FromMilliseconds(150));
                        HighlightRect.BeginAnimation(UIElement.OpacityProperty, fadeOut);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in ShowInvalidDropHighlight: {ex.Message}");
            }
        }

        private void ShowItemTypeError(ItemSlotInfo sourceSlotInfo)
        {
            try
            {
                var GameData = Application.Current.Resources["GameData"] as GameData;
                if (GameData == null) return;

                Item? sourceItem = null;

                switch (sourceSlotInfo.SlotType)
                {
                    case "Inventory":
                        if (sourceSlotInfo.SlotIndex >= 0 && sourceSlotInfo.SlotIndex < GameData.Inventory.Items.Count)
                            sourceItem = GameData.Inventory.Items[sourceSlotInfo.SlotIndex];
                        break;
                }

                if (sourceItem == null) return;

                string slotTypeDisplay = GetSlotTypeDisplayName(SlotType);
                string itemTypeDisplay = GetItemTypeDisplayName(sourceItem.Type);

                MessageBox.Show(
                    $"РџСЂРµРґРјРµС‚ \"{sourceItem.Name}\" ({itemTypeDisplay}) РЅРµР»СЊР·СЏ РїРѕРјРµСЃС‚РёС‚СЊ РІ СЃР»РѕС‚ С‚РёРїР° \"{slotTypeDisplay}\".",
                    "РќРµРїРѕРґС…РѕРґСЏС‰РёР№ С‚РёРї РїСЂРµРґРјРµС‚Р°",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in ShowItemTypeError: {ex.Message}");
            }
        }

        private string GetSlotTypeDisplayName(string slotType)
        {
            switch (slotType)
            {
                case "Inventory": return "РРЅРІРµРЅС‚Р°СЂСЊ";
                case "Quick": return "Р‘С‹СЃС‚СЂС‹Р№ РґРѕСЃС‚СїРї";
                case "Helmet": return "РЁР»РµРј";
                case "Chestplate": return "РќР°РіСЂСѓРґРЅРёРє";
                case "Leggings": return "РџРѕРЅРѕР¶Рё";
                case "Weapon": return "РћСЂСѓР¶РёРµ";
                case "Shield": return "Р©РёС‚";
                case "Trash": return "РљРѕСЂР·РёРЅР°";
                default: return slotType ?? "РќРµРёР·РІРµСЃС‚РЅС‹Р№";
            }
        }

        private string GetItemTypeDisplayName(ItemType type)
        {
            switch (type)
            {
                case ItemType.Helmet: return "РЁР»РµРј";
                case ItemType.Chestplate: return "РќР°РіСЂСѓРґРЅРёРє";
                case ItemType.Leggings: return "РџРѕРЅРѕР¶Рё";
                case ItemType.Weapon: return "РћСЂСѓР¶РёРµ";
                case ItemType.Shield: return "Р©РёС‚";
                case ItemType.Consumable: return "Р Р°СЃС…РѕРґСѓРµРјРѕРµ";
                case ItemType.Material: return "РњР°С‚РµСЂРёР°Р»";
                case ItemType.Unknown: return "РќРµРёР·РІРµСЃС‚РЅРѕ";
                default: return type.ToString();
            }
        }

        protected virtual void OnSlotMouseDown()
        {
            SlotMouseDown?.Invoke(this, new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left));
        }

        protected virtual void OnSlotDrop()
        {
            // Этот метод может быть переопределен в наследниках
            // Основная логика обработки drop находится в SlotBorder_Drop
        }

        protected virtual void OnSlotMouseEnter()
        {
            SlotMouseEnter?.Invoke(this, new MouseEventArgs(Mouse.PrimaryDevice, 0));
        }

        protected virtual void OnSlotMouseLeave()
        {
            SlotMouseLeave?.Invoke(this, new MouseEventArgs(Mouse.PrimaryDevice, 0));
        }

        private void HandleStackSplitting(ItemSlotInfo sourceSlotInfo, int splitAmount)
        {
            try
            {
                LoggingService.LogInfo($"[StackSplit] Разделение стека: {sourceSlotInfo.SlotType}[{sourceSlotInfo.SlotIndex}] -> {SlotType}[{SlotIndex}], количество: {splitAmount}");

                if (sourceSlotInfo.SlotType == SlotType && sourceSlotInfo.SlotIndex == SlotIndex)
                {
                    LoggingService.LogWarning("[StackSplit] Попытка разделить стек на тот же слот");
                    return;
                }

                InventoryViewModel? viewModel = FindViewModel();
                if (viewModel == null)
                {
                    LoggingService.LogError("[StackSplit] ViewModel не найден");
                    return;
                }

                Item? sourceItem = viewModel.GetItemFromSlot(sourceSlotInfo.SlotType, sourceSlotInfo.SlotIndex);
                if (sourceItem == null)
                {
                    LoggingService.LogWarning("[StackSplit] Исходный предмет не найден");
                    return;
                }

                if (!sourceItem.IsStackable)
                {
                    LoggingService.LogWarning($"[StackSplit] Предмет {sourceItem.Name} не стекаемый");
                    return;
                }

                if (sourceItem.StackSize <= 1)
                {
                    LoggingService.LogWarning($"[StackSplit] Размер стека {sourceItem.Name} слишком мал: {sourceItem.StackSize}");
                    return;
                }

                Item? targetItem = Item;

                if (splitAmount <= 0)
                {
                    splitAmount = sourceItem.StackSize / 2;
                    if (splitAmount < 1) splitAmount = 1;
                }
                else
                {
                    splitAmount = Math.Min(splitAmount, sourceItem.StackSize - 1);
                    if (splitAmount < 1) splitAmount = 1;
                }

                if (targetItem == null)
                {
                    Item newStackItem = sourceItem.Clone();
                    newStackItem.StackSize = splitAmount;

                    sourceItem.StackSize -= splitAmount;

                    LoggingService.LogInfo($"[StackSplit] Создан новый стек {newStackItem.Name}: {splitAmount} шт. Остался исходный стек: {sourceItem.StackSize} шт.");

                    UpdateSlotVisuals();

                    viewModel.SetItemToSlot(sourceSlotInfo.SlotType, sourceSlotInfo.SlotIndex, sourceItem);
                    viewModel.SetItemToSlot(SlotType, SlotIndex, newStackItem);
                }
                else if (targetItem.Name == sourceItem.Name &&
                         targetItem.Type == sourceItem.Type &&
                         targetItem.Rarity == sourceItem.Rarity &&
                         targetItem.IsStackable)
                {
                    int spaceAvailable = targetItem.MaxStackSize - targetItem.StackSize;
                    int actualAdd = Math.Min(spaceAvailable, splitAmount);

                    if (actualAdd > 0)
                    {
                        targetItem.StackSize += actualAdd;

                        sourceItem.StackSize -= actualAdd;

                        LoggingService.LogInfo($"[StackSplit] Объединены стеки {targetItem.Name}: добавлено {actualAdd} шт. Новый размер: {targetItem.StackSize}. Остался исходный стек: {sourceItem.StackSize} шт.");

                        UpdateSlotVisuals();

                        viewModel.SetItemToSlot(sourceSlotInfo.SlotType, sourceSlotInfo.SlotIndex, sourceItem);
                        viewModel.SetItemToSlot(SlotType, SlotIndex, targetItem);
                    }
                    else
                    {
                        MessageBox.Show(
                            "Р¦РµР»РµРІРѕР№ СЃС‚РµРє Р·Р°РїРѕР»РЅРµРЅ, РЅРµР»СЊР·СЏ РґРѕР±Р°РІРёС‚СЊ Р±РѕР»СЊС€Рµ РїСЂРµРґРјРµС‚РѕРІ.",
                            "РЎС‚РµРє Р·Р°РїРѕР»РЅРµРЅ",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                }
                else
                {
                    MessageBox.Show(
                        "РќРµРІРѕР·РјРѕР¶РЅРѕ СЂР°Р·РґРµР»РёС‚СЊ СЃС‚РµРє РЅР° СЌС‚РѕС‚ СЃР»РѕС‚, С‚Р°Рє РєР°Рє РѕРЅ СЃРѕРґРµСЂР¶РёС‚ РґСЂСѓРіРѕР№ РїСЂРµРґРјРµС‚.",
                        "РќРµРІРѕР·РјРѕР¶РЅРѕ СЂР°Р·РґРµР»РёС‚СЊ СЃС‚РµРє",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }

                if (sourceItem.StackSize > 0)
                {
                    SplitStackEventArgs args = new SplitStackEventArgs(
                        sourceItem,
                        splitAmount,
                        SlotType,
                        SlotIndex);

                    SplitStackRequested?.Invoke(this, args);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in HandleStackSplitting: {ex.Message}");
            }
        }

        private void ShowTooltip()
        {
            if (Item == null || _tooltip == null) return;

            try
            {
                Window? parentWindow = Window.GetWindow(this);
                if (parentWindow == null) return;

                if (_tooltip.Parent == null)
                {
                    Grid? tooltipOverlay = FindOrCreateTooltipOverlay(parentWindow);
                    if (tooltipOverlay != null)
                    {
                        tooltipOverlay.Children.Add(_tooltip);
                    }
                }

                _tooltip.SetItem(Item);

                Point cursorPos = Mouse.GetPosition(parentWindow);
                _tooltip.Margin = new Thickness(cursorPos.X + 15, cursorPos.Y + 15, 0, 0);
                _tooltip.HorizontalAlignment = HorizontalAlignment.Left;
                _tooltip.VerticalAlignment = VerticalAlignment.Top;

                _tooltip.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error showing tooltip: {ex.Message}");
            }
        }

        private Grid? FindOrCreateTooltipOverlay(Window window)
        {
            try
            {
                var rootContent = window.Content as UIElement;
                if (rootContent == null) return null;

                if (window.Content is Grid rootGrid)
                {
                    foreach (var child in rootGrid.Children)
                    {
                        if (child is Grid grid && grid.Name == "GlobalTooltipOverlay")
                        {
                            return grid;
                        }
                    }

                    Grid overlay = new Grid
                    {
                        Name = "GlobalTooltipOverlay",
                        IsHitTestVisible = false,
                    };

                    rootGrid.Children.Add(overlay);
                    Grid.SetRowSpan(overlay, rootGrid.RowDefinitions.Count > 0 ? rootGrid.RowDefinitions.Count : 1);
                    Grid.SetColumnSpan(overlay, rootGrid.ColumnDefinitions.Count > 0 ? rootGrid.ColumnDefinitions.Count : 1);
                    Panel.SetZIndex(overlay, 9999);

                    return overlay;
                }

                Grid newRoot = new Grid();
                Grid tooltipLayer = new Grid
                {
                    Name = "GlobalTooltipOverlay",
                    IsHitTestVisible = false
                };

                window.Content = null;

                newRoot.Children.Add(rootContent);
                newRoot.Children.Add(tooltipLayer);

                window.Content = newRoot;

                Panel.SetZIndex(tooltipLayer, 9999);

                return tooltipLayer;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating tooltip overlay: {ex.Message}");
                return null;
            }
        }

        private void HideTooltip()
        {
            if (_tooltip != null)
            {
                _tooltip.Hide();
            }
        }

        private void ShowSplitStackHint(int amount, int total)
        {
            try
            {
                if (!_isRightDragging)
                {
                    return;
                }

                HighlightRect.Fill = new SolidColorBrush(Color.FromArgb(150, 255, 215, 0));
                HighlightRect.Opacity = 0.5;

                TextBlock? stackSizeText = CoreItemCount;
                if (stackSizeText != null)
                {
                    string originalText = stackSizeText.Text;
                    Brush originalForeground = stackSizeText.Foreground;

                    stackSizeText.Text = $"{amount}/{total}";
                    stackSizeText.Foreground = new SolidColorBrush(Yellow);

                    stackSizeText.Visibility = Visibility.Visible;

                    DispatcherTimer timer = new DispatcherTimer();
                    timer.Interval = TimeSpan.FromSeconds(2);
                    timer.Tick += (s, e) =>
                    {
                        stackSizeText.Text = originalText;
                        stackSizeText.Foreground = originalForeground;
                        timer.Stop();
                    };
                    timer.Start();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in ShowSplitStackHint: {ex.Message}");
            }
        }

        private void ShowSplitStackDialog()
        {
            try
            {
                if (Item == null || !Item.IsStackable || Item.StackSize <= 1 || _isShowingSplitDialog)
                    return;

                _isShowingSplitDialog = true;

                HideTooltip();

                _splitStackPopup = new Popup();
                _splitStackPopup.Placement = PlacementMode.Mouse;
                _splitStackPopup.StaysOpen = true;
                _splitStackPopup.AllowsTransparency = true;

                Border popupBorder = new Border();
                popupBorder.Background = new SolidColorBrush(Color.FromArgb(230, 40, 40, 40));
                popupBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(80, 80, 80));
                popupBorder.BorderThickness = new Thickness(1);
                popupBorder.CornerRadius = new CornerRadius(3);
                popupBorder.Padding = new Thickness(10);

                StackPanel stackPanel = new StackPanel();
                stackPanel.Margin = new Thickness(5);

                TextBlock titleText = new TextBlock();
                titleText.Text = "Р Р°Р·РґРµР»РёС‚СЊ СЃС‚РµРє";
                titleText.Foreground = new SolidColorBrush(Colors.White);
                titleText.FontWeight = FontWeights.Bold;
                titleText.Margin = new Thickness(0, 0, 0, 10);
                titleText.HorizontalAlignment = HorizontalAlignment.Center;

                TextBlock itemInfoText = new TextBlock();
                itemInfoText.Text = $"{Item.Name} (РІСЃРµРіРѕ: {Item.StackSize})";
                itemInfoText.Foreground = new SolidColorBrush(Colors.LightGray);
                itemInfoText.Margin = new Thickness(0, 0, 0, 10);
                itemInfoText.HorizontalAlignment = HorizontalAlignment.Center;

                _splitStackSlider = new Slider();
                _splitStackSlider.Minimum = 1;
                _splitStackSlider.Maximum = Item.StackSize - 1;
                _splitStackSlider.Value = Item.StackSize / 2;
                _splitStackSlider.Width = 150;
                _splitStackSlider.Margin = new Thickness(0, 5, 0, 5);
                _splitStackSlider.IsSnapToTickEnabled = true;
                _splitStackSlider.TickFrequency = 1;
                _splitStackSlider.ValueChanged += SplitStackSlider_ValueChanged;

                StackPanel valuePanel = new StackPanel();
                valuePanel.Orientation = Orientation.Horizontal;
                valuePanel.HorizontalAlignment = HorizontalAlignment.Center;
                valuePanel.Margin = new Thickness(0, 0, 0, 10);

                TextBlock valueLabel = new TextBlock();
                valueLabel.Text = "РљРѕР»РёС‡РµСЃС‚РІРѕ: ";
                valueLabel.Foreground = new SolidColorBrush(Colors.White);

                _splitStackValueText = new TextBlock();
                _splitStackValueText.Text = Math.Round(_splitStackSlider.Value).ToString();
                _splitStackValueText.Foreground = new SolidColorBrush(Colors.Yellow);

                valuePanel.Children.Add(valueLabel);
                valuePanel.Children.Add(_splitStackValueText);

                StackPanel buttonPanel = new StackPanel();
                buttonPanel.Orientation = Orientation.Horizontal;
                buttonPanel.HorizontalAlignment = HorizontalAlignment.Center;
                buttonPanel.Margin = new Thickness(0, 5, 0, 0);

                _splitStackConfirmButton = new Button();
                _splitStackConfirmButton.Content = "Р Р°Р·РґРµР»РёС‚СЊ";
                _splitStackConfirmButton.Padding = new Thickness(10, 5, 10, 5);
                _splitStackConfirmButton.Margin = new Thickness(0, 0, 5, 0);
                _splitStackConfirmButton.Click += SplitStackConfirm_Click;

                _splitStackCancelButton = new Button();
                _splitStackCancelButton.Content = "РћС‚РјРµРЅР°";
                _splitStackCancelButton.Padding = new Thickness(10, 5, 10, 5);
                _splitStackCancelButton.Margin = new Thickness(5, 0, 0, 0);
                _splitStackCancelButton.Click += SplitStackCancel_Click;

                buttonPanel.Children.Add(_splitStackConfirmButton);
                buttonPanel.Children.Add(_splitStackCancelButton);

                stackPanel.Children.Add(titleText);
                stackPanel.Children.Add(itemInfoText);
                stackPanel.Children.Add(_splitStackSlider);
                stackPanel.Children.Add(valuePanel);
                stackPanel.Children.Add(buttonPanel);

                popupBorder.Child = stackPanel;
                _splitStackPopup.Child = popupBorder;

                _splitStackPopup.IsOpen = true;

                Mouse.Capture(_splitStackPopup, CaptureMode.SubTree);
                Mouse.AddPreviewMouseDownOutsideCapturedElementHandler(_splitStackPopup, SplitStackPopup_MouseDownOutside);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in ShowSplitStackDialog: {ex.Message}");
                _isShowingSplitDialog = false;
            }
        }

        private void SplitStackSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_splitStackValueText != null)
            {
                _splitStackValueText.Text = Math.Round(e.NewValue).ToString();
            }
        }

        private void SplitStackConfirm_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_splitStackSlider != null && Item != null)
                {
                    int splitAmount = (int)Math.Round(_splitStackSlider.Value);

                    CloseSplitStackDialog();

                    PerformStackSplit(splitAmount);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in SplitStackConfirm_Click: {ex.Message}");
                CloseSplitStackDialog();
            }
        }

        private void SplitStackCancel_Click(object sender, RoutedEventArgs e)
        {
            CloseSplitStackDialog();
        }

        private void SplitStackPopup_MouseDownOutside(object sender, MouseButtonEventArgs e)
        {
            CloseSplitStackDialog();
        }

        private void CloseSplitStackDialog()
        {
            try
            {
                if (_splitStackPopup != null)
                {
                    Mouse.RemovePreviewMouseDownOutsideCapturedElementHandler(_splitStackPopup, SplitStackPopup_MouseDownOutside);
                    Mouse.Capture(null);

                    _splitStackPopup.IsOpen = false;
                    _splitStackPopup = null;
                }

                _splitStackSlider = null;
                _splitStackValueText = null;
                _splitStackConfirmButton = null;
                _splitStackCancelButton = null;
                _isShowingSplitDialog = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in CloseSplitStackDialog: {ex.Message}");
                _isShowingSplitDialog = false;
            }
        }

        private void PerformStackSplit(int splitAmount)
        {
            try
            {
                if (Item == null || !Item.IsStackable || Item.StackSize <= 1 || splitAmount <= 0 || splitAmount >= Item.StackSize)
                {
                    return;
                }

                InventoryViewModel? viewModel = FindViewModel();
                if (viewModel == null)
                {
                    return;
                }

                int emptySlotIndex = -1;
                for (int i = 0; i < viewModel.PlayerInventory.Items.Count; i++)
                {
                    if (viewModel.PlayerInventory.Items[i] == null)
                    {
                        emptySlotIndex = i;
                        break;
                    }
                }

                if (emptySlotIndex == -1)
                {
                    MessageBox.Show(
                        "Р’ РёРЅРІРµРЅС‚Р°СЂРµ РЅРµС‚ СЃРІРѕР±РѕРґРЅС‹С… СЃР»РѕС‚РѕРІ РґР»СЏ СЂР°Р·РјРµС‰РµРЅРёСЏ СЂР°Р·РґРµР»РµРЅРЅРѕРіРѕ СЃС‚РµРєР°.",
                        "РќРµС‚ СЃРІРѕР±РѕРґРЅС‹С… СЃР»РѕС‚РѕРІ",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                Item newStackItem = Item.Clone();
                newStackItem.StackSize = splitAmount;

                Item.StackSize -= splitAmount;

                UpdateSlotVisuals();

                viewModel.SetItemToSlot(SlotType, SlotIndex, Item);

                viewModel.SetItemToSlot("Inventory", emptySlotIndex, newStackItem);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in PerformStackSplit: {ex.Message}");
            }
        }

        private void SetImageSourceSafely(BitmapImage? image)
        {
            if (image == null)
            {
                SafeSetImageSource(CoreItemImage, null);
                SafeSetVisibility(CoreItemImage, Visibility.Collapsed);
                return;
            }

            try
            {
                if (!image.IsFrozen && image.CanFreeze)
                {
                    image.Freeze();
                }

                SafeSetImageSource(CoreItemImage, image);
                SafeSetVisibility(CoreItemImage, Visibility.Visible);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in SetImageSourceSafely: {ex.Message}");
                SafeSetVisibility(CoreItemImage, Visibility.Collapsed);
            }
        }
    }

    [Serializable]
    public class ItemSlotInfo
    {
        public string SlotType { get; set; } = string.Empty;
        public int SlotIndex { get; set; }
        public Item? Item { get; set; }

        public ItemSlotInfo(string slotType, int slotIndex, Item? item = null)
        {
            SlotType = slotType ?? string.Empty;
            SlotIndex = slotIndex;
            Item = item;
        }

        public ItemSlotInfo()
        {
            SlotType = "Inventory";
            SlotIndex = -1;
            Item = null;
        }

        public override string ToString()
        {
            return $"ItemSlotInfo: {SlotType}[{SlotIndex}] - {Item?.Name ?? "Empty"}";
        }
    }

    public class ValidateItemForSlotEventArgs : EventArgs
    {
        public Item Item { get; private set; }
        public string TargetSlotType { get; private set; }
        public int TargetSlotIndex { get; private set; }
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;

        public ValidateItemForSlotEventArgs(Item item, string targetSlotType, int targetSlotIndex)
        {
            Item = item ?? throw new ArgumentNullException(nameof(item));
            TargetSlotType = targetSlotType ?? string.Empty;
            TargetSlotIndex = targetSlotIndex;
            IsValid = true; // По умолчанию разрешаем
        }
    }

    public class ValidateItemTypeEventArgs : EventArgs
    {
        public string SourceSlotType { get; private set; } = string.Empty;
        public int SourceSlotIndex { get; private set; }
        public string TargetSlotType { get; private set; } = string.Empty;
        public string TargetSlotName { get; private set; } = string.Empty;
        public Item? Item { get; set; }
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;

        public ValidateItemTypeEventArgs(string sourceSlotType, int sourceSlotIndex, string targetSlotType)
        {
            SourceSlotType = sourceSlotType ?? string.Empty;
            SourceSlotIndex = sourceSlotIndex;
            TargetSlotType = targetSlotType ?? string.Empty;
            TargetSlotName = targetSlotType ?? string.Empty;
            IsValid = true; // По умолчанию разрешаем
        }
    }

    // РљР»Р°СЃСЃ РґР»СЏ РїРµСЂРµРґР°С‡Рё РґР°РЅРЅС‹С… РїСЂРё РїРµСЂРµРјРµС‰РµРЅРёРё РїСЂРµРґРјРµС‚Р° РІ РєРѕСЂР·РёРСў
    public class ItemTrashEventArgs : EventArgs
    {
        public string SourceType { get; set; } = string.Empty;
        public int SourceIndex { get; set; }
    }

    // РљР»Р°СЃСЃ РґР»СЏ РїРµСЂРµРґР°С‡Рё РґР°РЅРЅС‹С… РїСЂРё СЌРєРёРїРёСЂРѕРІРєРµ РїСЂРµРґРјРµС‚Р°
    public class EquipItemData : EventArgs
    {
        public string SourceType { get; set; } = string.Empty;
        public int SourceIndex { get; set; }
        public string EquipmentType { get; set; } = string.Empty;
    }
}
