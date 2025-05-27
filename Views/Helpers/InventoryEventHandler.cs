using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SketchBlade.Models;
using SketchBlade.ViewModels;
using SketchBlade.Views.Controls;
using SketchBlade.Services;

namespace SketchBlade.Views.Helpers
{
    /// <summary>
    /// Handles all mouse and keyboard events for the inventory system
    /// </summary>
    public class InventoryEventHandler
    {
        private readonly InventoryViewModel _viewModel;
        
        public InventoryEventHandler(InventoryViewModel viewModel)
        {
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        }
        
        #region Keyboard Events
        
        public void HandleKeyDown(KeyEventArgs e)
        {
            CheckHotkey(e);
        }
        
        public void HandlePreviewKeyDown(KeyEventArgs e)
        {
            CheckHotkey(e);
        }
        
        private void CheckHotkey(KeyEventArgs e)
        {
            if (e.Key >= Key.D1 && e.Key <= Key.D9 && (Keyboard.Modifiers & ModifierKeys.Control) != 0)
            {
                int index = e.Key - Key.D1;
                
                if (index < 2)
                {
                    _viewModel.UseQuickSlotCommand.Execute(index);
                    e.Handled = true;
                }
            }
        }
        
        #endregion
        
        #region Mouse Events
        
        public void HandleBorderMouseDown(object sender, MouseButtonEventArgs e)
        {
            // Обработка клика по границе - ничего не делаем
        }
        
        public void HandleBorderMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            // Обработка клика по границе
        }
        
        public void HandleMouseWheel(object sender, MouseWheelEventArgs e)
        {
            HandleRightPanelPreviewMouseWheel(sender, e);
        }
        
        public void HandleEquipmentSlotMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not CoreInventorySlot slot)
                return;

            try
            {
                string equipmentType = slot.SlotType;
                int equipmentIndex = GetEquipmentSlotIndex(equipmentType);
                
                // Get the equipped item using the correct API
                Item? equippedItem = equipmentType switch
                {
                    "Weapon" => _viewModel.GameData.Player?.EquippedWeapon,
                    "Helmet" => _viewModel.GameData.Player?.EquippedHelmet,
                    "Chestplate" => _viewModel.GameData.Player?.EquippedArmor,
                    "Leggings" => _viewModel.GameData.Player?.EquippedLeggings,
                    "Shield" => _viewModel.GameData.Player?.EquippedShield,
                    _ => null
                };
                
                if (equippedItem != null)
                {
                    var itemSlotInfo = new Controls.ItemSlotInfo(equipmentType, equipmentIndex, equippedItem);

                    var dragData = new DataObject("ItemSlotInfo", itemSlotInfo);
                    DragDrop.DoDragDrop(slot, dragData, DragDropEffects.Move);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in EquipmentSlot_MouseDown: {ex.Message}");
            }
        }
        
        public void HandleQuickSlotMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not CoreInventorySlot slot)
                return;

            try
            {
                int quickSlotIndex = slot.SlotIndex;
                
                // Use the correct API to get quick slot items
                var item = quickSlotIndex >= 0 && quickSlotIndex < _viewModel.QuickSlots.Count 
                    ? _viewModel.QuickSlots[quickSlotIndex].Item 
                    : null;
                
                if (item != null)
                {
                    var itemSlotInfo = new Controls.ItemSlotInfo("Quick", quickSlotIndex, item);

                    var dragData = new DataObject("ItemSlotInfo", itemSlotInfo);
                    DragDrop.DoDragDrop(slot, dragData, DragDropEffects.Move);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in QuickSlot_MouseDown: {ex.Message}");
            }
        }
        
        public void HandleInventorySlotMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not CoreInventorySlot slot)
            {
                // LoggingService.LogWarning($"[DragDrop] sender не является CoreInventorySlot: {sender?.GetType().Name}");
                return;
            }

            try
            {
                int inventoryIndex = slot.SlotIndex;
                string slotType = slot.SlotType; // Get SlotType from the CoreInventorySlot

                // Fetch the item directly from the ViewModel to ensure up-to-date information,
                // bypassing any potential lag in the CoreInventorySlot.Item DP update.
                var item = _viewModel.GetItemFromSlot(slotType, inventoryIndex); 
                
                string slotName = $"{slotType}[{inventoryIndex}]";

                if (e.LeftButton == MouseButtonState.Pressed && item != null)
                {
                    var itemSlotInfo = new Controls.ItemSlotInfo(slotType, inventoryIndex, item);
                    var dragData = new DataObject("ItemSlotInfo", itemSlotInfo);
                    DragDrop.DoDragDrop(slot, dragData, DragDropEffects.Move);
                }
            }
            catch (Exception ex)
            {
                // LoggingService.LogError($"[DragDrop] Исключение в HandleInventorySlotMouseDown: {ex.Message}");
            }
        }
        
        public void HandleCraftSlotMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not CoreInventorySlot slot)
                return;

            try
            {
                int craftSlotIndex = slot.SlotIndex;
                
                // Use the correct API to get craft slot items
                var item = craftSlotIndex >= 0 && craftSlotIndex < _viewModel.GameData.Inventory.CraftItems.Count
                    ? _viewModel.GameData.Inventory.CraftItems[craftSlotIndex]
                    : null;
                
                if (item != null)
                {
                    var itemSlotInfo = new Controls.ItemSlotInfo("Craft", craftSlotIndex, item);

                    var dragData = new DataObject("ItemSlotInfo", itemSlotInfo);
                    DragDrop.DoDragDrop(slot, dragData, DragDropEffects.Move);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in CraftSlot_MouseDown: {ex.Message}");
            }
        }
        
        public void HandleCraftResultSlotMouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                // Use the correct method from InventoryViewModel to take craft result
                _viewModel.TakeCraftResult();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in CraftResultSlot_MouseDown: {ex.Message}");
            }
        }
        
        public void HandleTrashSlotMouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                // Корзина не содержит предметов для перетаскивания
                // Предметы удаляются при перетаскивании в корзину
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in TrashSlot_MouseDown: {ex.Message}");
            }
        }
        
        #endregion
        
        #region Item Action Events
        
        public void HandleItemMoveRequested(object sender, MoveItemData e)
        {
            try
            {
                _viewModel.MoveItemBetweenSlots(e.SourceType, e.SourceIndex, e.TargetType, e.TargetIndex);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in Slot_ItemMoveRequested: {ex.Message}");
            }
        }
        
        public void HandleItemEquipRequested(object sender, Controls.EquipItemData e)
        {
            try
            {
                ViewModels.EquipItemData equipData = new ViewModels.EquipItemData
                {
                    InventoryIndex = e.SourceIndex
                };
                
                switch (e.EquipmentType)
                {
                    case "Helmet":
                        equipData.EquipmentSlot = "Helmet";
                        break;
                    case "Chestplate":
                        equipData.EquipmentSlot = "Armor";
                        break;
                    case "Leggings":
                        equipData.EquipmentSlot = "Leggings";
                        break;
                    case "Weapon":
                        equipData.EquipmentSlot = "Weapon";
                        break;
                    case "Shield":
                        equipData.EquipmentSlot = "Shield";
                        break;
                    default:
                        return;
                }
                
                _viewModel.EquipItem(equipData);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in Slot_ItemEquipRequested: {ex.Message}");
                MessageBox.Show($"Stack trace: {ex.StackTrace}");
            }
        }
        
        public void HandleItemTrashRequested(object sender, ItemTrashEventArgs e)
        {
            try
            {
                _viewModel.MoveItemBetweenSlots(e.SourceType, e.SourceIndex, "Trash", 0);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in Slot_ItemTrashRequested: {ex.Message}");
                MessageBox.Show($"Stack trace: {ex.StackTrace}");
            }
        }
        
        public void HandleSplitStackRequested(object sender, SplitStackEventArgs e)
        {
            _viewModel.SplitStackCommand.Execute(e);
        }
        
        public void HandleValidateItemForSlot(object sender, ValidateItemForSlotEventArgs e)
        {
            try
            {
                if (e.TargetSlotType == "Quick")
                {
                    e.IsValid = e.Item?.Type == ItemType.Consumable;
                    if (!e.IsValid)
                    {
                        e.ErrorMessage = "В быстрые слоты можно помещать только расходные предметы";
                    }
                }
                else if (e.TargetSlotType == "Equipment")
                {
                    e.IsValid = IsItemCompatibleWithEquipmentSlot(e.Item, e.TargetSlotType);
                    if (!e.IsValid)
                    {
                        e.ErrorMessage = $"Этот предмет нельзя экипировать в слот {e.TargetSlotType}";
                    }
                }
                else
                {
                    e.IsValid = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in Slot_ValidateItemForSlot: {ex.Message}");
                e.IsValid = false;
                e.ErrorMessage = "Ошибка валидации предмета";
            }
        }
        
        #endregion
        
        #region Scroll Events
        
        public void HandleRightPanelPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is ScrollViewer scrollViewer)
            {
                if (e.Delta < 0)
                {
                    scrollViewer.LineDown();
                }
                else
                {
                    scrollViewer.LineUp();
                }
                
                e.Handled = true;
            }
        }
        
        #endregion
        
        #region Helper Methods
        
        private bool IsItemCompatibleWithEquipmentSlot(Item? item, string equipmentType)
        {
            if (item == null) return false;
            
            return equipmentType switch
            {
                "Helmet" => item.Type == ItemType.Helmet,
                "Armor" => item.Type == ItemType.Chestplate,
                "Leggings" => item.Type == ItemType.Leggings,
                "Weapon" => item.Type == ItemType.Weapon,
                "Shield" => item.Type == ItemType.Shield,
                _ => false
            };
        }
        
        private int GetEquipmentSlotIndex(string equipmentType)
        {
            return equipmentType switch
            {
                "Helmet" => 0,
                "Armor" => 1,
                "Leggings" => 2,
                "Weapon" => 3,
                "Shield" => 4,
                _ => 0
            };
        }
        
        public void HandleDrop(object sender, DragEventArgs e, string targetSlotName, int targetSlotIndex)
        {
            if (sender is not CoreInventorySlot targetCoreSlot)
            {
                // LoggingService.LogWarning($"[DragDrop] HandleDrop: sender is not CoreInventorySlot ({sender?.GetType().Name})");
                return;
            }

            try
            {
                // LoggingService.LogInfo($"[DragDrop] Drop в {targetSlotName}");

                if (e.Data.GetDataPresent("ItemSlotInfo"))
                {
                    var sourceSlotInfo = e.Data.GetData("ItemSlotInfo") as Controls.ItemSlotInfo;
                    if (sourceSlotInfo == null)
                    {
                        // LoggingService.LogWarning("[DragDrop] HandleDrop: sourceSlotInfo is null.");
                        return;
                    }

                    // Ensure Item is not null before accessing Name
                    // string sourceItemName = sourceSlotInfo.Item?.Name ?? "Unknown Item";
                    // LoggingService.LogInfo($"[DragDrop] Перемещение предмета: {sourceSlotInfo.Type}[{sourceSlotInfo.Index}] ({sourceItemName}) -> {targetSlotName}");

                    var targetSlotType = targetCoreSlot.SlotType;
                    // var targetItemSlotInfo = new Controls.ItemSlotInfo(targetSlotType, targetSlotIndex, targetCoreSlot.Item);

                    // Pass sourceSlotInfo.Item as the itemBeingDragged
                    // bool success = _viewModel.MoveItemBetweenSlots(sourceSlotInfo, targetItemSlotInfo, sourceSlotInfo.Item); 

                    // Call the overload that takes types and indices directly
                    _viewModel.MoveItemBetweenSlots(sourceSlotInfo.SlotType, sourceSlotInfo.SlotIndex, targetSlotType, targetSlotIndex);
                    // Assuming the operation was successful for UI effect purposes, as the called method is void.
                    // The actual success/failure is handled internally by the ViewModel and data updates.
                    bool success = true; 

                    if (success) // This will always be true now based on the assumption above
                    {
                        // LoggingService.LogInfo($"[DragDrop] Перемещение успешно завершено.");
                    }
                    else
                    {
                        // LoggingService.LogWarning($"[DragDrop] Не удалось выполнить перемещение.");
                    }
                    e.Effects = success ? DragDropEffects.Move : DragDropEffects.None;
                }
                else
                {
                    // LoggingService.LogWarning($"[DragDrop] HandleDrop: Отсутствуют данные ItemSlotInfo.");
                    e.Effects = DragDropEffects.None;
                }
            }
            catch (Exception ex)
            {
                // LoggingService.LogError($"[DragDrop] Исключение в HandleDrop: {ex.Message}");
                e.Effects = DragDropEffects.None;
            }
            finally
            {
                e.Handled = true;
                // LoggingService.LogInfo($"[DragDrop] Drop завершен для {targetSlotName} с эффектом {e.Effects}");
            }
        }
        
        #endregion
    }
} 
