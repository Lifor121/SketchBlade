using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.ComponentModel;
using SketchBlade.Models;
using SketchBlade.ViewModels;
using SketchBlade.Views.Helpers;
using SketchBlade.Views.Controls;
using System.IO;
using SketchBlade.Services;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Media;

namespace SketchBlade.Views
{
    /// <summary>
    /// Simplified InventoryView that delegates functionality to specialized helper classes
    /// </summary>
    public partial class InventoryView : UserControl
    {
        private InventoryViewModel? _viewModel;
        private InventoryEventHandler? _eventHandler;
        private InventoryDragDropHandler? _dragDropHandler;
        private InventorySlotUIManager? _slotManager;
        
        public InventoryViewModel? ViewModel 
        { 
            get => _viewModel; 
            set 
            { 
                if (_viewModel != null)
                {
                    _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
                    CleanupHelpers();
                }
                
                _viewModel = value;
                
                if (_viewModel != null)
                {
                    _viewModel.PropertyChanged += ViewModel_PropertyChanged;
                    DataContext = _viewModel;
                    InitializeHelpers();
                }
            }
        }
        
        public InventoryView()
        {
            try
            {
                InitializeComponent();
                
                this.Loaded += InventoryView_Loaded;
                this.DataContextChanged += InventoryView_DataContextChanged;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in InventoryView constructor: {ex.Message}");
            }
        }

        #region Initialization

        private void InventoryView_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                LoggingService.LogDebug("InventoryView_Loaded: Начинаем обновление UI");
                
                if (DataContext is InventoryViewModel viewModel)
                {
                    LoggingService.LogDebug("InventoryView_Loaded: InventoryViewModel найден");
                    
                    // ОПТИМИЗАЦИЯ: Убираем избыточную реинициализацию крафта
                    // Система крафта инициализируется автоматически при первом обращении
                    
                    // Принудительно обновляем UI
                    viewModel.ForceUIUpdate();
                    
                    // Принудительно устанавливаем Item для всех CoreInventorySlot
                    SetInventorySlotItems(viewModel);
                    
                    LoggingService.LogDebug("InventoryView_Loaded: Обновление завершено");
                }
                else
                {
                    LoggingService.LogWarning("InventoryView_Loaded: InventoryViewModel не найден в DataContext");
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"InventoryView_Loaded: {ex.Message}", ex);
            }
        }

        private void SetInventorySlotItems(InventoryViewModel viewModel)
        {
            try
            {
                LoggingService.LogDebug("SetInventorySlotItems: Принудительно устанавливаем Item для всех слотов");
                
                // Находим все CoreInventorySlot элементы в визуальном дереве
                var inventorySlots = FindVisualChildren<CoreInventorySlot>(this)
                    .Where(slot => slot.SlotType == "Inventory")
                    .OrderBy(slot => slot.SlotIndex)
                    .ToList();
                
                LoggingService.LogDebug($"SetInventorySlotItems: Найдено {inventorySlots.Count} инвентарных слотов");
                
                for (int i = 0; i < inventorySlots.Count && i < viewModel.InventorySlots.Count; i++)
                {
                    var slot = inventorySlots[i];
                    var item = viewModel.InventorySlots[i].Item;
                    
                    LoggingService.LogDebug($"SetInventorySlotItems: Устанавливаем слот {i}: {item?.Name ?? "null"}");
                    
                    // Принудительно устанавливаем Item
                    slot.Item = item;
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Ошибка в SetInventorySlotItems: {ex.Message}");
            }
        }
        
        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
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

        private void InitializeHelpers()
        {
            if (_viewModel == null) return;
            
            _eventHandler = new InventoryEventHandler(_viewModel);
            _dragDropHandler = new InventoryDragDropHandler(_viewModel);
            _slotManager = new InventorySlotUIManager(_viewModel, _eventHandler, _dragDropHandler);
        }
        
        private void CleanupHelpers()
        {
            if (_slotManager != null)
            {
                _slotManager.DisconnectEventHandlers(this);
            }
            
            _eventHandler = null;
            _dragDropHandler = null;
            _slotManager = null;
        }

        private void SetupEventHandlers()
        {
            // Keyboard events
            this.KeyDown += (s, e) => _eventHandler?.HandleKeyDown(e);
            this.PreviewKeyDown += (s, e) => _eventHandler?.HandlePreviewKeyDown(e);
            
            // Window keyboard events
            Window? window = Window.GetWindow(this);
            if (window != null)
            {
                window.PreviewKeyDown += (s, e) => _eventHandler?.HandlePreviewKeyDown(e);
            }
        }
        
        private void ConnectSlotEvents()
        {
            _slotManager?.ConnectEventHandlers(this);
        }

        #endregion

        #region Event Handlers

        private void InventoryView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is InventoryViewModel oldViewModel)
            {
                oldViewModel.PropertyChanged -= ViewModel_PropertyChanged;
            }
            
            if (e.NewValue is InventoryViewModel newViewModel)
            {
                _viewModel = newViewModel;
                newViewModel.PropertyChanged += ViewModel_PropertyChanged;
                InitializeHelpers();
            }
            else
            {
                _viewModel = null;
                CleanupHelpers();
            }
        }
        
        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(InventoryViewModel.IsRecipeBookVisible))
            {
                // Recipe book functionality is now handled by SimplifiedCraftingViewModel
                // No additional action needed here
            }
        }

        #endregion

        #region Delegated Event Handlers (for XAML compatibility)

        // These methods are kept minimal and delegate to the helper classes
        // They are needed for XAML event binding compatibility

        private void Border_MouseLeftButtonDown(object? sender, MouseButtonEventArgs e)
        {
            _eventHandler?.HandleBorderMouseDown(sender, e);
        }

        private void EquipmentSlot_MouseDown(object? sender, MouseButtonEventArgs e)
        {
            _eventHandler?.HandleEquipmentSlotMouseDown(sender, e);
        }

        private void QuickSlot_MouseDown(object? sender, MouseButtonEventArgs e)
        {
            _eventHandler?.HandleQuickSlotMouseDown(sender, e);
        }

        private void EquipmentSlot_Drop(object? sender, DragEventArgs e)
        {
            _dragDropHandler?.HandleEquipmentSlotDrop(sender, e);
        }

        private void QuickSlot_Drop(object? sender, DragEventArgs e)
        {
            _dragDropHandler?.HandleQuickSlotDrop(sender, e);
        }

        private void TrashSlot_Drop(object? sender, DragEventArgs e)
        {
            _dragDropHandler?.HandleTrashSlotDrop(sender, e);
        }

        private void InventorySlot_MouseDown(object? sender, MouseButtonEventArgs e)
        {
            _eventHandler?.HandleInventorySlotMouseDown(sender, e);
        }

        private void InventorySlot_Drop(object? sender, DragEventArgs e)
        {
            _dragDropHandler?.HandleInventorySlotDrop(sender, e);
        }

        private void CraftSlot_MouseDown(object? sender, MouseButtonEventArgs e)
        {
            _eventHandler?.HandleCraftSlotMouseDown(sender, e);
        }

        private void CraftSlot_Drop(object? sender, DragEventArgs e)
        {
            _dragDropHandler?.HandleCraftSlotDrop(sender, e);
        }

        private void CraftResultSlot_MouseDown(object? sender, MouseButtonEventArgs e)
        {
            _eventHandler?.HandleCraftResultSlotMouseDown(sender, e);
        }

        private void TrashSlot_MouseDown(object? sender, MouseButtonEventArgs e)
        {
            _eventHandler?.HandleTrashSlotMouseDown(sender, e);
        }

        private void Slot_ValidateItemForSlot(object? sender, ValidateItemForSlotEventArgs e)
        {
            _eventHandler?.HandleValidateItemForSlot(sender, e);
        }

        private void Slot_ItemMoveRequested(object? sender, MoveItemData e)
        {
            _eventHandler?.HandleItemMoveRequested(sender, e);
        }

        private void Slot_ItemEquipRequested(object? sender, Controls.EquipItemData e)
        {
            _eventHandler?.HandleItemEquipRequested(sender, e);
        }

        private void Slot_ItemTrashRequested(object? sender, ItemTrashEventArgs e)
        {
            _eventHandler?.HandleItemTrashRequested(sender, e);
        }

        private void RightPanel_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            _eventHandler?.HandleMouseWheel(sender, e);
        }

        private void CraftSlot_DragEnter(object sender, DragEventArgs e)
        {
            _dragDropHandler?.HandleCraftSlotDragEnter(sender, e);
        }

        private void CraftSlot_DragOver(object sender, DragEventArgs e)
        {
            _dragDropHandler?.HandleCraftSlotDragOver(sender, e);
        }

        private void CraftSlot_DragLeave(object sender, DragEventArgs e)
        {
            _dragDropHandler?.HandleCraftSlotDragLeave(sender, e);
        }

        private void InventorySlot_DragEnter(object sender, DragEventArgs e)
        {
            _dragDropHandler?.HandleInventorySlotDragEnter(sender, e);
        }

        private void InventorySlot_DragOver(object sender, DragEventArgs e)
        {
            _dragDropHandler?.HandleInventorySlotDragOver(sender, e);
        }

        private void InventorySlot_DragLeave(object sender, DragEventArgs e)
        {
            _dragDropHandler?.HandleInventorySlotDragLeave(sender, e);
        }

        private void InventorySlot_SplitStackRequested(object? sender, SplitStackEventArgs e)
        {
            _eventHandler?.HandleSplitStackRequested(sender, e);
        }

        #endregion

        #region Helper Methods

        public void VerifyItemStacks(SketchBlade.Services.ItemSlotInfo? sourceInfo, SketchBlade.Services.ItemSlotInfo? targetInfo)
        {
            _slotManager?.VerifyItemStacks(sourceInfo, targetInfo);
        }

        #endregion
    }
} 