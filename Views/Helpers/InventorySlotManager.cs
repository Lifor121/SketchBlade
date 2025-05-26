using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using SketchBlade.Views.Controls;
using SketchBlade.ViewModels;
using SketchBlade.Services;

namespace SketchBlade.Views.Helpers
{
    /// <summary>
    /// Manages slot controls and their event connections for the inventory system
    /// </summary>
    public class InventorySlotUIManager
    {
        private readonly InventoryViewModel _viewModel;
        private readonly InventoryEventHandler _eventHandler;
        private readonly InventoryDragDropHandler _dragDropHandler;
        
        public InventorySlotUIManager(InventoryViewModel viewModel, InventoryEventHandler eventHandler, InventoryDragDropHandler dragDropHandler)
        {
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            _eventHandler = eventHandler ?? throw new ArgumentNullException(nameof(eventHandler));
            _dragDropHandler = dragDropHandler ?? throw new ArgumentNullException(nameof(dragDropHandler));
        }
        
        #region Slot Discovery
        
        public List<CoreInventorySlot> FindSlotControls(DependencyObject parent, string slotType)
        {
            var results = new List<CoreInventorySlot>();
            FindSlots(parent, slotType, results);
            return results;
        }
        
        private void FindSlots(DependencyObject parent, string slotType, List<CoreInventorySlot> results)
        {
            if (parent == null) return;
            
            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                
                if (child is CoreInventorySlot slot)
                {
                    // Используем SlotType вместо Name для поиска слотов
                    var slotTypeName = slot.SlotType?.ToString();
                    if (!string.IsNullOrEmpty(slotTypeName) && slotTypeName.Contains(slotType))
                    {
                        LoggingService.LogInfo($"[DragDrop] Найден слот {slotTypeName}[{slot.SlotIndex}] для типа {slotType}");
                        results.Add(slot);
                    }
                }
                
                FindSlots(child, slotType, results);
            }
        }
        
        #endregion
        
        #region Event Connection
        
        public void ConnectEventHandlers(DependencyObject parent)
        {
            try
            {
                LoggingService.LogInfo($"[DragDrop] ConnectEventHandlers: Начинаем подключение событий");
                
                // Подключаем события для слотов крафта, которые генерируются динамически
                ConnectCraftSlotEvents(parent);
                
                // Подключаем события для других типов слотов при необходимости
                ConnectInventorySlotEvents(parent);
                ConnectEquipmentSlotEvents(parent);
                ConnectQuickSlotEvents(parent);
                
                LoggingService.LogInfo($"[DragDrop] ConnectEventHandlers: Подключение событий завершено");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"ConnectEventHandlers: {ex.Message}", ex);
            }
        }
        
        private void ConnectCraftSlotEvents(DependencyObject parent)
        {
            try
            {
                var craftSlots = FindSlotControls(parent, "Craft");
                
                foreach (var slot in craftSlots)
                {
                    if (slot != null)
                    {
                        // Mouse events
                        slot.SlotMouseDown += _eventHandler.HandleCraftSlotMouseDown;
                        
                        // Drag & Drop events
                        slot.SlotDrop += _dragDropHandler.HandleCraftSlotDrop;
                        slot.SlotDragEnter += _dragDropHandler.HandleCraftSlotDragEnter;
                        slot.SlotDragOver += _dragDropHandler.HandleCraftSlotDragOver;
                        slot.SlotDragLeave += _dragDropHandler.HandleCraftSlotDragLeave;
                        
                        // Validation events
                        slot.ValidateItemForSlot += _eventHandler.HandleValidateItemForSlot;
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"ConnectCraftSlotEvents: {ex.Message}", ex);
            }
        }
        
        private void ConnectInventorySlotEvents(DependencyObject parent)
        {
            try
            {
                var inventorySlots = FindSlotControls(parent, "Inventory");
                LoggingService.LogInfo($"[DragDrop] ConnectInventorySlotEvents: Найдено {inventorySlots.Count} инвентарных слотов");
                
                foreach (var slot in inventorySlots)
                {
                    if (slot != null)
                    {
                        LoggingService.LogInfo($"[DragDrop] Подключаем события для слота {slot.SlotType}[{slot.SlotIndex}]");
                        
                        // Mouse events
                        slot.SlotMouseDown += _eventHandler.HandleInventorySlotMouseDown;
                        
                        // Drag & Drop events
                        slot.SlotDrop += _dragDropHandler.HandleInventorySlotDrop;
                        slot.SlotDragEnter += _dragDropHandler.HandleInventorySlotDragEnter;
                        slot.SlotDragOver += _dragDropHandler.HandleInventorySlotDragOver;
                        slot.SlotDragLeave += _dragDropHandler.HandleInventorySlotDragLeave;
                        
                        // Item action events
                        slot.ItemMoveRequested += _eventHandler.HandleItemMoveRequested;
                        slot.ItemEquipRequested += _eventHandler.HandleItemEquipRequested;
                        slot.ItemTrashRequested += _eventHandler.HandleItemTrashRequested;
                        slot.SplitStackRequested += _eventHandler.HandleSplitStackRequested;
                        
                        // Validation events
                        slot.ValidateItemForSlot += _eventHandler.HandleValidateItemForSlot;
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"ConnectInventorySlotEvents: {ex.Message}", ex);
            }
        }
        
        private void ConnectEquipmentSlotEvents(DependencyObject parent)
        {
            try
            {
                var equipmentSlots = FindSlotControls(parent, "Equipment");
                
                foreach (var slot in equipmentSlots)
                {
                    if (slot != null)
                    {
                        // Mouse events
                        slot.SlotMouseDown += _eventHandler.HandleEquipmentSlotMouseDown;
                        
                        // Drag & Drop events
                        slot.SlotDrop += _dragDropHandler.HandleEquipmentSlotDrop;
                        
                        // Validation events
                        slot.ValidateItemForSlot += _eventHandler.HandleValidateItemForSlot;
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"ConnectEquipmentSlotEvents: {ex.Message}", ex);
            }
        }
        
        private void ConnectQuickSlotEvents(DependencyObject parent)
        {
            try
            {
                var quickSlots = FindSlotControls(parent, "Quick");
                
                foreach (var slot in quickSlots)
                {
                    if (slot != null)
                    {
                        // Mouse events
                        slot.SlotMouseDown += _eventHandler.HandleQuickSlotMouseDown;
                        
                        // Drag & Drop events
                        slot.SlotDrop += _dragDropHandler.HandleQuickSlotDrop;
                        
                        // Validation events
                        slot.ValidateItemForSlot += _eventHandler.HandleValidateItemForSlot;
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"ConnectQuickSlotEvents: {ex.Message}", ex);
            }
        }
        
        #endregion
        
        #region Event Disconnection
        
        public void DisconnectEventHandlers(DependencyObject parent)
        {
            try
            {
                DisconnectCraftSlotEvents(parent);
                DisconnectInventorySlotEvents(parent);
                DisconnectEquipmentSlotEvents(parent);
                DisconnectQuickSlotEvents(parent);
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"DisconnectEventHandlers: {ex.Message}", ex);
            }
        }
        
        private void DisconnectCraftSlotEvents(DependencyObject parent)
        {
            var craftSlots = FindSlotControls(parent, "Craft");
            
            foreach (var slot in craftSlots)
            {
                if (slot != null)
                {
                    slot.SlotMouseDown -= _eventHandler.HandleCraftSlotMouseDown;
                    slot.SlotDrop -= _dragDropHandler.HandleCraftSlotDrop;
                    slot.SlotDragEnter -= _dragDropHandler.HandleCraftSlotDragEnter;
                    slot.SlotDragOver -= _dragDropHandler.HandleCraftSlotDragOver;
                    slot.SlotDragLeave -= _dragDropHandler.HandleCraftSlotDragLeave;
                    slot.ValidateItemForSlot -= _eventHandler.HandleValidateItemForSlot;
                }
            }
        }
        
        private void DisconnectInventorySlotEvents(DependencyObject parent)
        {
            var inventorySlots = FindSlotControls(parent, "Inventory");
            
            foreach (var slot in inventorySlots)
            {
                if (slot != null)
                {
                    slot.SlotMouseDown -= _eventHandler.HandleInventorySlotMouseDown;
                    slot.SlotDrop -= _dragDropHandler.HandleInventorySlotDrop;
                    slot.SlotDragEnter -= _dragDropHandler.HandleInventorySlotDragEnter;
                    slot.SlotDragOver -= _dragDropHandler.HandleInventorySlotDragOver;
                    slot.SlotDragLeave -= _dragDropHandler.HandleInventorySlotDragLeave;
                    slot.ItemMoveRequested -= _eventHandler.HandleItemMoveRequested;
                    slot.ItemEquipRequested -= _eventHandler.HandleItemEquipRequested;
                    slot.ItemTrashRequested -= _eventHandler.HandleItemTrashRequested;
                    slot.SplitStackRequested -= _eventHandler.HandleSplitStackRequested;
                    slot.ValidateItemForSlot -= _eventHandler.HandleValidateItemForSlot;
                }
            }
        }
        
        private void DisconnectEquipmentSlotEvents(DependencyObject parent)
        {
            var equipmentSlots = FindSlotControls(parent, "Equipment");
            
            foreach (var slot in equipmentSlots)
            {
                if (slot != null)
                {
                    slot.SlotMouseDown -= _eventHandler.HandleEquipmentSlotMouseDown;
                    slot.SlotDrop -= _dragDropHandler.HandleEquipmentSlotDrop;
                    slot.ValidateItemForSlot -= _eventHandler.HandleValidateItemForSlot;
                }
            }
        }
        
        private void DisconnectQuickSlotEvents(DependencyObject parent)
        {
            var quickSlots = FindSlotControls(parent, "Quick");
            
            foreach (var slot in quickSlots)
            {
                if (slot != null)
                {
                    slot.SlotMouseDown -= _eventHandler.HandleQuickSlotMouseDown;
                    slot.SlotDrop -= _dragDropHandler.HandleQuickSlotDrop;
                    slot.ValidateItemForSlot -= _eventHandler.HandleValidateItemForSlot;
                }
            }
        }
        
        #endregion
        
        #region Utility Methods
        
        public void VerifyItemStacks(SketchBlade.Services.ItemSlotInfo? sourceInfo, SketchBlade.Services.ItemSlotInfo? targetInfo)
        {
            if (sourceInfo == null || targetInfo == null)
                return;
                
            try
            {
                // Проверяем совместимость слотов для стекирования
                LoggingService.LogDebug($"Verifying item stacks: {sourceInfo.SlotType}[{sourceInfo.Index}] -> {targetInfo.SlotType}[{targetInfo.Index}]");
                
                // Здесь можно добавить дополнительную логику проверки совместимости слотов
                if (sourceInfo.SlotType == targetInfo.SlotType && sourceInfo.Index == targetInfo.Index)
                {
                    LoggingService.LogDebug("Source and target slots are the same");
                }
                else
                {
                    LoggingService.LogDebug($"Different slots: {sourceInfo.SlotType}[{sourceInfo.Index}] vs {targetInfo.SlotType}[{targetInfo.Index}]");
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"VerifyItemStacks: {ex.Message}", ex);
            }
        }
        
        #endregion
    }
} 