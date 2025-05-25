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
        }

        private void CoreInventorySlot_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Инициализируем tooltip при загрузке
                if (_tooltip == null)
                {
                    _tooltip = new SketchBlade.Views.Controls.ItemTooltip();
                }
                
                // Подписываемся на настройки при загрузке
                SubscribeToSettingsChanges();
                
                LoggingService.LogDebug($"CoreInventorySlot loaded for slot {SlotType}[{SlotIndex}]");
                
                // НОВАЯ ДИАГНОСТИКА: Проверяем DataContext и привязки
                LoggingService.LogDebug($"[UI] DataContext: {DataContext?.GetType().Name ?? "null"}");
                
                // Проверяем привязку Item
                var binding = BindingOperations.GetBinding(this, ItemProperty);
                if (binding != null)
                {
                    LoggingService.LogDebug($"[UI] Item binding path: {binding.Path?.Path ?? "null"}");
                    LoggingService.LogDebug($"[UI] Item binding source: {binding.Source?.GetType().Name ?? "null"}");
                }
                // Убираем логирование "ПРОБЛЕМА: Item binding не найден!" - это не критично
                
                // Проверяем текущее значение Item
                LoggingService.LogDebug($"[UI] Текущее значение Item: {Item?.Name ?? "null"}");
                
                // ИСПРАВЛЕНИЕ: Принудительно обновляем визуальное отображение при загрузке
                UpdateSlotVisuals();
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error in CoreInventorySlot_Loaded: {ex.Message}", ex);
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
                LoggingService.LogDebug($"[UI] DataContext changed for {SlotType}[{SlotIndex}] to {e.NewValue?.GetType().Name ?? "null"}");
                
                // If the DataContext has a Result property that can be bound to our Item property,
                // and we don't already have a binding for Item, set up that binding
                if (e.NewValue != null && SlotType == "CraftResult")
                {
                    var binding = BindingOperations.GetBinding(this, ItemProperty);
                    if (binding == null)
                    {
                        // Create and configure binding only if not already bound
                        var newBinding = new Binding("Result")
                        {
                            Source = e.NewValue,
                            Mode = BindingMode.OneWay
                        };
                        
                        // Apply the binding to our Item property
                        BindingOperations.SetBinding(this, ItemProperty, newBinding);
                        LoggingService.LogDebug($"[UI] Set up Result -> Item binding for {SlotType}[{SlotIndex}]");
                    }
                }
                
                // Принудительно обновляем визуальное отображение при изменении контекста
                UpdateSlotVisuals();
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error in CoreInventorySlot_DataContextChanged: {ex.Message}", ex);
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
                // Убираем избыточное логирование - оставляем только при ошибках
                // LoggingService.LogDebug($"[UI] OnItemChanged for {slot.SlotType}[{slot.SlotIndex}]: {e.OldValue?.ToString() ?? "null"} -> {e.NewValue?.ToString() ?? "null"}");
                
                var newItem = e.NewValue as Item;
                
                if (newItem != null)
                {
                    // Убираем детальное логирование
                    // LoggingService.LogDebug($"[UI] Новый предмет: {newItem.Name}, SpritePath: '{newItem.SpritePath}', StackSize: {newItem.StackSize}");
                }
                
                slot.UpdateSlotVisuals();
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
                // Убираем избыточное логирование
                // LoggingService.LogDebug($"[UI] UpdateSlotVisuals для {SlotType}[{SlotIndex}]: Item = {Item?.Name ?? "null"}");
                
                if (Item != null)
                {
                    // Убираем детальное логирование
                    // LoggingService.LogDebug($"[UI] Предмет найден: {Item.Name}, SpritePath: '{Item.SpritePath}'");
                    
                    LoadAndSetItemImage();

                    if (Item.IsStackable && Item.StackSize > 1)
                    {
                        SafeSetText(CoreItemCount, Item.StackSize.ToString());
                        SafeSetVisibility(CoreItemCount, Visibility.Visible);
                        // LoggingService.LogDebug($"[UI] Показываем счетчик стека: {Item.StackSize}");
                    }
                    else
                    {
                        // LoggingService.LogDebug($"[UI] Скрываем счетчик стека (не стекаемый или размер = 1)");
                        SafeSetVisibility(CoreItemCount, Visibility.Collapsed);
                    }

                    UpdateRarityIndicator();
                }
                else
                {
                    // LoggingService.LogDebug($"[UI] Предмет null, очищаем слот {SlotType}[{SlotIndex}]");
                    SafeSetImageSource(CoreItemImage, null);
                    SafeSetVisibility(CoreItemImage, Visibility.Collapsed);
                    SafeSetVisibility(CoreItemCount, Visibility.Collapsed);
                    SafeSetVisibility(RarityIndicator, Visibility.Collapsed);
                    _itemImage = null;
                }
                
                // Принудительно обновляем привязки данных
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (CoreItemImage != null)
                    {
                        CoreItemImage.UpdateLayout();
                    }
                    if (CoreItemCount != null)
                    {
                        CoreItemCount.UpdateLayout();
                    }
                }), DispatcherPriority.Render);
                
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
                if (Item == null)
                {
                    _itemImage = null;
                    SafeSetImageSource(CoreItemImage, null);
                    SafeSetVisibility(CoreItemImage, Visibility.Collapsed);
                    SafeSetVisibility(CoreItemCount, Visibility.Collapsed);
                    return;
                }

                string imagePath = Item.SpritePath;

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
                    if (!System.IO.File.Exists(fullPath))
                    {
                        LoggingService.LogWarning($"Image file not found: {fullPath}, using default image");
                        imagePath = AssetPaths.DEFAULT_IMAGE;
                        
                        // ВАЖНО: Сохраняем правильный путь в самом предмете для будущего использования
                        Item.SpritePath = imagePath;
                    }
                    
                    // Принудительная загрузка изображения через ImageHelper
                    _itemImage = ImageHelper.LoadImage(imagePath);
                    
                    // Проверяем, что изображение загружено
                    if (_itemImage == null || _itemImage.PixelWidth == 0)
                    {
                        LoggingService.LogWarning($"Failed to load image {imagePath}, using default image");
                        _itemImage = ImageHelper.GetDefaultImage();
                    }
                    
                    // Устанавливаем изображение
                    SafeSetImageSource(CoreItemImage, _itemImage);
                    SafeSetVisibility(CoreItemImage, Visibility.Visible);
                    
                    // Показываем количество, если стак > 1
                    if (Item.StackSize > 1)
                    {
                        SafeSetText(CoreItemCount, Item.StackSize.ToString());
                        SafeSetVisibility(CoreItemCount, Visibility.Visible);
                    }
                    else
                    {
                        SafeSetVisibility(CoreItemCount, Visibility.Collapsed);
                    }

                    // Показываем индикатор редкости
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
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error in LoadAndSetItemImage: {ex.Message}", ex);
            }
        }

        private void SafeSetImageSource(System.Windows.Controls.Image imageControl, BitmapImage? source)
        {
            if (imageControl == null) return;

            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => { imageControl.Source = source; });
            }
            else
            {
                imageControl.Source = source;
            }
        }

        private void SafeSetVisibility(UIElement element, Visibility visibility)
        {
            if (element == null) return;

            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => { element.Visibility = visibility; });
            }
            else
            {
                element.Visibility = visibility;
            }
        }

        private void SafeSetText(TextBlock textBlock, string text)
        {
            if (textBlock == null) return;

            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => { textBlock.Text = text; });
            }
            else
            {
                textBlock.Text = text;
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
                    _dragStartPoint = e.GetPosition(this);
                    CoreSlotBorder.CaptureMouse();
                    e.Handled = true;
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

                if (CoreSlotBorder.IsMouseCaptured)
                {
                    CoreSlotBorder.ReleaseMouseCapture();
                }
            }
        }

        private void SlotBorder_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                if (e.LeftButton == MouseButtonState.Pressed &&
                    !_isDragging &&
                    Item != null &&
                    CoreSlotBorder.IsMouseCaptured)
                {
                    Point currentPosition = e.GetPosition(this);
                    Vector difference = _dragStartPoint - currentPosition;

                    if (Math.Abs(difference.X) > SystemParameters.MinimumHorizontalDragDistance ||
                        Math.Abs(difference.Y) > SystemParameters.MinimumVerticalDragDistance)
                    {
                        try
                        {
                            _isDragging = true;

                            ItemSlotInfo slotInfo = new ItemSlotInfo(SlotType, SlotIndex);
                            DataObject dragData = new DataObject();
                            dragData.SetData("ItemSlotInfo", slotInfo);

                            HideTooltip();

                            LoggingService.LogDebug($"[DragDrop] CoreInventorySlot.MouseMove: Начало DragDrop операции для {SlotType}[{SlotIndex}]");

                            DragDropEffects result = DragDrop.DoDragDrop(CoreSlotBorder, dragData, DragDropEffects.Move);

                            LoggingService.LogDebug($"[DragDrop] CoreInventorySlot.MouseMove: DragDrop завершен с результатом {result}");

                            UpdateSelectionAppearance();

                            SlotDragInitiated?.Invoke(this, e);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Error during drag operation: {ex.Message}");
                        }
                        finally
                        {
                            _isDragging = false;

                            if (CoreSlotBorder.IsMouseCaptured)
                            {
                                CoreSlotBorder.ReleaseMouseCapture();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in SlotBorder_MouseMove: {ex.Message}");

                _isDragging = false;

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
            try
            {
                // Добавляем логирование для отладки
                LoggingService.LogDebug($"[DragDrop] CoreInventorySlot.SlotBorder_Drop: Начало обработки для {SlotType}[{SlotIndex}]");

                ShowDropTargetHighlight(false);
                ShowInvalidDropHighlight(false);
                UpdateSelectionAppearance();

                _isDragging = false;

                if (e.Data.GetDataPresent("ItemSlotInfo"))
                {
                    ItemSlotInfo sourceSlotInfo = (ItemSlotInfo)e.Data.GetData("ItemSlotInfo");
                    ItemSlotInfo targetSlotInfo = new ItemSlotInfo(SlotType, SlotIndex);

                    LoggingService.LogDebug($"[DragDrop] CoreInventorySlot.SlotBorder_Drop: Обработка перемещения из {sourceSlotInfo.SlotType}[{sourceSlotInfo.SlotIndex}] в {targetSlotInfo.SlotType}[{targetSlotInfo.SlotIndex}]");

                    if (e.Data.GetDataPresent("SplitStack") && (bool)e.Data.GetData("SplitStack"))
                    {
                        int splitAmount = -1;
                        if (e.Data.GetDataPresent("SplitAmount"))
                        {
                            splitAmount = (int)e.Data.GetData("SplitAmount");
                        }

                        HandleStackSplitting(sourceSlotInfo, splitAmount);

                        e.Handled = true;
                        return;
                    }

                    bool canAccept = CanAcceptItemType(sourceSlotInfo);
                    if (!canAccept)
                    {
                        e.Handled = true;
                        return;
                    }

                    MoveItemData moveData = new MoveItemData
                    {
                        SourceType = sourceSlotInfo.SlotType,
                        SourceIndex = sourceSlotInfo.SlotIndex,
                        TargetType = targetSlotInfo.SlotType,
                        TargetIndex = targetSlotInfo.SlotIndex
                    };

                    if (SlotType == "Trash")
                    {
                        if (sourceSlotInfo.SlotType == "Helmet" ||
                            sourceSlotInfo.SlotType == "Chestplate" ||
                            sourceSlotInfo.SlotType == "Leggings" ||
                            sourceSlotInfo.SlotType == "Weapon" ||
                            sourceSlotInfo.SlotType == "Shield")
                        {
                            MessageBox.Show(
                                "Чтобы выбросить экипированный предмет, сначала переместите его в инвентарь.",
                                "Неподходящий тип предмета",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                            e.Handled = true;
                            return;
                        }

                        ItemTrashRequested?.Invoke(this, new ItemTrashEventArgs
                        {
                            SourceType = sourceSlotInfo.SlotType,
                            SourceIndex = sourceSlotInfo.SlotIndex
                        });
                        e.Handled = true;
                        return;
                    }

                    if (SlotType == "Helmet" || SlotType == "Chestplate" ||
                        SlotType == "Leggings" || SlotType == "Weapon" || SlotType == "Shield")
                    {
                        if (sourceSlotInfo.SlotType == "Inventory")
                        {
                            ItemEquipRequested?.Invoke(this, new EquipItemData
                            {
                                SourceType = sourceSlotInfo.SlotType,
                                SourceIndex = sourceSlotInfo.SlotIndex,
                                EquipmentType = SlotType
                            });
                            e.Handled = true;
                            return;
                        }
                    }

                    RaiseItemMoveRequest(moveData);
                }

                LoggingService.LogDebug($"[DragDrop] CoreInventorySlot.SlotBorder_Drop: Завершение обработки");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"[DragDrop] CoreInventorySlot.SlotBorder_Drop exception: {ex.Message}", ex);

                MessageBox.Show($"Error in SlotBorder_Drop: {ex.Message}");

                ShowDropTargetHighlight(false);
                ShowInvalidDropHighlight(false);
                UpdateSelectionAppearance();
            }
        }

        private bool CanAcceptItemType(ItemSlotInfo sourceSlotInfo)
        {
            if (!CanAcceptDrag)
            {
                return false;
            }

            if (string.IsNullOrEmpty(sourceSlotInfo.SlotType))
            {
                return false;
            }

            try
            {
                // Используем кэшированный предмет из ItemSlotInfo если он есть
                var sourceItem = sourceSlotInfo.Item;
                
                // Если предмет не кэширован, пытаемся получить его из ViewModel
                if (sourceItem == null)
                {
                    sourceItem = FindViewModelItem(sourceSlotInfo);
                    if (sourceItem == null)
                    {
                        // Логируем null sourceItem только раз в 10 вызовов для уменьшения спама
                        if (++_nullItemLogCounter % 10 == 1)
                        {
                            LoggingService.LogDebug($"[DragDrop] CanAcceptItemType: sourceItem is null for {sourceSlotInfo.SlotType}[{sourceSlotInfo.SlotIndex}] (throttled: showing 1 of 10)");
                        }
                        return false;
                    }
                }

                // Быстая проверка совместимости типов слотов
                bool basicCompatibility = SlotTypeCanAcceptItemType(SlotType, sourceItem.Type);
                
                LoggingService.LogDebug($"[DragDrop] CanAcceptItemType: basicCompatibility = {basicCompatibility} for {sourceItem.Type} -> {SlotType}");

                // Если есть обработчик валидации, используем его
                if (ValidateItemForSlot != null)
                {
                    LoggingService.LogDebug($"[DragDrop] CanAcceptItemType: ValidateItemForSlot event is connected, calling...");
                    var args = new ValidateItemForSlotEventArgs(sourceItem, SlotType, SlotIndex);
                    ValidateItemForSlot.Invoke(this, args);
                    bool validationResult = args.IsValid;
                    LoggingService.LogDebug($"[DragDrop] CanAcceptItemType: ValidateItemForSlot returned {validationResult}");
                    return basicCompatibility && validationResult;
                }

                LoggingService.LogDebug($"[DragDrop] CanAcceptItemType: {sourceSlotInfo.SlotType}[{sourceSlotInfo.SlotIndex}] -> {SlotType}[{SlotIndex}] = {basicCompatibility} (ItemType: {sourceItem.Type})");
                return basicCompatibility;
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"[DragDrop] CanAcceptItemType exception: {ex.Message}", ex);
                return false;
            }
        }

        private Item? FindViewModelItem(ItemSlotInfo slotInfo)
        {
            try
            {
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
                    return;
                }

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
                    ItemSlotInfo? sourceSlotInfo = e.Data.GetData("ItemSlotInfo") as ItemSlotInfo;

                    if (sourceSlotInfo != null && sourceSlotInfo.SlotType == SlotType && sourceSlotInfo.SlotIndex == SlotIndex)
                    {
                        ShowDropTargetHighlight(false);
                        ShowInvalidDropHighlight(false);

                        e.Effects = DragDropEffects.None;
                        return;
                    }

                    if (sourceSlotInfo != null && CanAcceptItemType(sourceSlotInfo))
                    {
                        ShowDropTargetHighlight(true);
                        ShowInvalidDropHighlight(false);

                        e.Effects = DragDropEffects.Move;
                        
                        // Р›РѕРіРёСЂСѓРµРј С‚РѕР»СЊРєРѕ СѓСЃРїРµС€РЅС‹Рµ РѕРїРµСЂР°С†РёРё drag enter РґР»СЏ РґРёР°РіРЅРѕСЃС‚РёРєРё
                        LoggingService.LogDebug($"[DragDrop] InventorySlot_DragEnter: {sourceSlotInfo.SlotType}[{sourceSlotInfo.SlotIndex}] -> {SlotType}[{SlotIndex}] (Allow)");
                    }
                    else
                    {
                        ShowDropTargetHighlight(false);
                        ShowInvalidDropHighlight(true);

                        e.Effects = DragDropEffects.None;
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
                LoggingService.LogError($"[DragDrop] SlotBorder_DragEnter exception: {ex.Message}", ex);

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
                if (sourceSlotInfo.SlotType == SlotType && sourceSlotInfo.SlotIndex == SlotIndex)
                {
                    return;
                }

                InventoryViewModel? viewModel = FindViewModel();
                if (viewModel == null)
                {
                    return;
                }

                Item? sourceItem = viewModel.GetItemFromSlot(sourceSlotInfo.SlotType, sourceSlotInfo.SlotIndex);
                if (sourceItem == null)
                {
                    return;
                }

                if (!sourceItem.IsStackable)
                {
                    return;
                }

                if (sourceItem.StackSize <= 1)
                {
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
