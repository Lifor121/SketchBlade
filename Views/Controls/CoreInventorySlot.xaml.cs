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
using SketchBlade.Models;
using SketchBlade.Helpers;
using SketchBlade.ViewModels;
using System.Windows.Threading;
using static System.Windows.Media.Colors;
using static SketchBlade.Views.Controls.ValidateItemTypeEventArgs;

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
        public event EventHandler<ValidateItemTypeEventArgs>? ValidateItemForSlot;
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
                new PropertyMetadata(-1));

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

        private BitmapImage? _itemImage;
        private Point _dragStartPoint;
        private bool _isDragging;
        private bool _isRightDragging;
        private ItemTooltip? _tooltip;

        private bool _isShowingSplitDialog;
        private Popup? _splitStackPopup;
        private Slider? _splitStackSlider;
        private TextBlock? _splitStackValueText;
        private Button? _splitStackConfirmButton;
        private Button? _splitStackCancelButton;

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
            set { SetValue(SlotIndexProperty, value); }
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
            _isDragging = false;
            _isRightDragging = false;
            _isShowingSplitDialog = false;

            CoreSlotBorder.BorderBrush = SlotNormalBrush;

            _tooltip = new ItemTooltip();
            _tooltip.Visibility = Visibility.Collapsed;
        }

        private static void OnItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CoreInventorySlot slot)
            {
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
                CoreSlotBorder.Tag = new ItemSlotInfo(SlotType, SlotIndex);

                if (Item != null)
                {
                    if (Item.Icon == null && !string.IsNullOrEmpty(Item.SpritePath))
                    {
                        try
                        {
                            if (!Dispatcher.CheckAccess())
                            {
                                Dispatcher.Invoke(() => {
                                    LoadAndSetItemImage();
                                });
                            }
                            else
                            {
                                LoadAndSetItemImage();
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Error loading image for {Item.Name}: {ex.Message}");
                            _itemImage = null;
                        }
                    }
                    else if (Item.Icon != null)
                    {
                        try
                        {
                            _itemImage = Item.Icon;

                            if (!_itemImage.IsFrozen && _itemImage.CanFreeze)
                            {
                                _itemImage.Freeze();
                            }

                            SetImageSourceSafely(_itemImage);
                        }
                        catch (Exception ex)
                        {
                            _itemImage = null;
                            SafeSetVisibility(CoreItemImage, Visibility.Collapsed);
                        }
                    }
                    else
                    {
                        try
                        {
                            _itemImage = Helpers.ImageHelper.CreateEmptyImage();
                            SetImageSourceSafely(_itemImage);
                        }
                        catch (Exception ex)
                        {
                            _itemImage = null;
                            SafeSetVisibility(CoreItemImage, Visibility.Collapsed);
                        }
                    }

                    if (Item.IsStackable && Item.StackSize > 1)
                    {
                        SafeSetText(CoreItemCount, Item.StackSize.ToString());
                        SafeSetVisibility(CoreItemCount, Visibility.Visible);
                    }
                    else
                    {
                        SafeSetVisibility(CoreItemCount, Visibility.Collapsed);
                    }

                    UpdateRarityIndicator();
                }
                else
                {
                    SafeSetImageSource(CoreItemImage, null);
                    SafeSetVisibility(CoreItemImage, Visibility.Collapsed);
                    SafeSetVisibility(CoreItemCount, Visibility.Collapsed);
                    SafeSetVisibility(RarityIndicator, Visibility.Collapsed);
                    _itemImage = null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in UpdateSlotVisuals: {ex.Message}");
            }
        }

        private void LoadAndSetItemImage()
        {
            if (Item == null || string.IsNullOrEmpty(Item.SpritePath)) return;

            _itemImage = Helpers.ImageHelper.LoadImage(Item.SpritePath);

            if (_itemImage != null)
            {
                if (!_itemImage.IsFrozen && _itemImage.CanFreeze)
                {
                    _itemImage.Freeze();
                }

                Item.Icon = _itemImage;

                SetImageSourceSafely(_itemImage);
            }
            else
            {
                SafeSetVisibility(CoreItemImage, Visibility.Collapsed);
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
                        bool success = viewModel.TakeCraftResult(SlotIndex);
                        if (!success)
                        {
                            MessageBox.Show($"Failed to craft item from slot {SlotIndex}");
                        }
                    }
                    else
                    {
                        MessageBox.Show("Could not find ViewModel for crafting");

                        if (Application.Current.Resources.Contains("InventoryViewModel"))
                        {
                            var directViewModel = Application.Current.Resources["InventoryViewModel"] as InventoryViewModel;
                            if (directViewModel != null)
                            {
                                directViewModel.TakeCraftResult(SlotIndex);
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

                            DragDropEffects result = DragDrop.DoDragDrop(CoreSlotBorder, dragData, DragDropEffects.Move);

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
                ShowDropTargetHighlight(false);
                ShowInvalidDropHighlight(false);
                UpdateSelectionAppearance();

                _isDragging = false;

                if (e.Data.GetDataPresent(typeof(ItemSlotInfo)))
                {
                    ItemSlotInfo sourceSlotInfo = (ItemSlotInfo)e.Data.GetData(typeof(ItemSlotInfo));
                    ItemSlotInfo targetSlotInfo = new ItemSlotInfo(SlotType, SlotIndex);

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
                                "Невозможно выбросить",
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

                e.Handled = true;
                SlotDrop?.Invoke(this, e);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in SlotBorder_Drop: {ex.Message}");

                ShowDropTargetHighlight(false);
                ShowInvalidDropHighlight(false);
                UpdateSelectionAppearance();
            }
        }

        private bool CanAcceptItemType(ItemSlotInfo sourceSlotInfo)
        {
            if (!CanAcceptDrag)
                return false;

            if (string.IsNullOrEmpty(sourceSlotInfo.SlotType))
                return false;

            try
            {
                if (ValidateItemForSlot != null || !string.IsNullOrEmpty(SlotType))
                {
                    if (ValidateItemForSlot != null)
                    {
                        var args = new ValidateItemTypeEventArgs(sourceSlotInfo.SlotType, sourceSlotInfo.SlotIndex, SlotType);
                        ValidateItemForSlot(this, args);

                        return args.IsValid;
                    }

                    var sourceItem = FindViewModelItem(sourceSlotInfo);
                    if (sourceItem == null)
                    {
                        return false;
                    }

                    return SlotTypeCanAcceptItemType(SlotType, sourceItem.Type);
                }

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in CanAcceptItemType: {ex.Message}");
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
                    e.Handled = true;
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
                        e.Handled = true;
                        return;
                    }

                    if (sourceSlotInfo != null && CanAcceptItemType(sourceSlotInfo))
                    {
                        ShowDropTargetHighlight(true);
                        ShowInvalidDropHighlight(false);

                        e.Effects = DragDropEffects.Move;
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
                MessageBox.Show($"Error in SlotBorder_DragEnter: {ex.Message}");

                e.Effects = DragDropEffects.None;
                ShowDropTargetHighlight(false);
                ShowInvalidDropHighlight(false);
            }

            e.Handled = true;
            SlotDragEnter?.Invoke(this, e);
        }

        private void SlotBorder_DragOver(object sender, DragEventArgs e)
        {
            try
            {
                if (!CanAcceptDrag)
                {
                    e.Effects = DragDropEffects.None;
                    e.Handled = true;
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
                        e.Handled = true;
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
                MessageBox.Show($"Error in SlotBorder_DragOver: {ex.Message}");
                e.Effects = DragDropEffects.None;
                ShowDropTargetHighlight(false);
                ShowInvalidDropHighlight(false);
            }

            e.Handled = true;
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

            SlotMouseEnter?.Invoke(this, e);
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
                var gameState = Application.Current.Resources["GameState"] as GameState;
                if (gameState == null) return;

                Item? sourceItem = null;

                switch (sourceSlotInfo.SlotType)
                {
                    case "Inventory":
                        if (sourceSlotInfo.SlotIndex >= 0 && sourceSlotInfo.SlotIndex < gameState.Inventory.Items.Count)
                            sourceItem = gameState.Inventory.Items[sourceSlotInfo.SlotIndex];
                        break;
                }

                if (sourceItem == null) return;

                string slotTypeDisplay = GetSlotTypeDisplayName(SlotType);
                string itemTypeDisplay = GetItemTypeDisplayName(sourceItem.Type);

                MessageBox.Show(
                    $"Предмет \"{sourceItem.Name}\" ({itemTypeDisplay}) нельзя поместить в слот типа \"{slotTypeDisplay}\".",
                    "Неподходящий тип предмета",
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
                case "Inventory": return "Инвентарь";
                case "Quick": return "Быстрый доступ";
                case "Helmet": return "Шлем";
                case "Chestplate": return "Нагрудник";
                case "Leggings": return "Поножи";
                case "Weapon": return "Оружие";
                case "Shield": return "Щит";
                case "Trash": return "Корзина";
                default: return slotType ?? "Неизвестный";
            }
        }

        private string GetItemTypeDisplayName(ItemType type)
        {
            switch (type)
            {
                case ItemType.Helmet: return "Шлем";
                case ItemType.Chestplate: return "Нагрудник";
                case ItemType.Leggings: return "Поножи";
                case ItemType.Weapon: return "Оружие";
                case ItemType.Shield: return "Щит";
                case ItemType.Consumable: return "Расходуемое";
                case ItemType.Material: return "Материал";
                case ItemType.Unknown: return "Неизвестно";
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
                            "Целевой стек заполнен, нельзя добавить больше предметов.",
                            "Стек заполнен",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                }
                else
                {
                    MessageBox.Show(
                        "Невозможно разделить стек на этот слот, так как он содержит другой предмет.",
                        "Невозможно разделить стек",
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
                titleText.Text = "Разделить стек";
                titleText.Foreground = new SolidColorBrush(Colors.White);
                titleText.FontWeight = FontWeights.Bold;
                titleText.Margin = new Thickness(0, 0, 0, 10);
                titleText.HorizontalAlignment = HorizontalAlignment.Center;

                TextBlock itemInfoText = new TextBlock();
                itemInfoText.Text = $"{Item.Name} (всего: {Item.StackSize})";
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
                valueLabel.Text = "Количество: ";
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
                _splitStackConfirmButton.Content = "Разделить";
                _splitStackConfirmButton.Padding = new Thickness(10, 5, 10, 5);
                _splitStackConfirmButton.Margin = new Thickness(0, 0, 5, 0);
                _splitStackConfirmButton.Click += SplitStackConfirm_Click;

                _splitStackCancelButton = new Button();
                _splitStackCancelButton.Content = "Отмена";
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
                        "В инвентаре нет свободных слотов для размещения разделенного стека.",
                        "Нет свободных слотов",
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

        public ItemSlotInfo(string slotType, int slotIndex)
        {
            SlotType = slotType ?? string.Empty;
            SlotIndex = slotIndex;
        }

        public ItemSlotInfo()
        {
            SlotType = "Inventory";
            SlotIndex = -1;
        }

        public override string ToString()
        {
            return $"ItemSlotInfo: {SlotType}[{SlotIndex}]";
        }
    }

    public class ValidateItemTypeEventArgs : EventArgs
    {
        public string SourceSlotType { get; private set; } = string.Empty;
        public int SourceSlotIndex { get; private set; }
        public string TargetSlotType { get; private set; } = string.Empty;
        public bool IsValid { get; set; }

        public ValidateItemTypeEventArgs(string sourceSlotType, int sourceSlotIndex, string targetSlotType)
        {
            SourceSlotType = sourceSlotType ?? string.Empty;
            SourceSlotIndex = sourceSlotIndex;
            TargetSlotType = targetSlotType ?? string.Empty;
            IsValid = true; // По умолчанию разрешаем
        }
    }

    // Класс для передачи данных при перемещении предмета в корзину
    public class ItemTrashEventArgs : EventArgs
    {
        public string SourceType { get; set; } = string.Empty;
        public int SourceIndex { get; set; }
    }

    // Класс для передачи данных при экипировке предмета
    public class EquipItemData : EventArgs
    {
        public string SourceType { get; set; } = string.Empty;
        public int SourceIndex { get; set; }
        public string EquipmentType { get; set; } = string.Empty;
    }
}