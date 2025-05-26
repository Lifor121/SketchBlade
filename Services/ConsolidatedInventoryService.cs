using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using SketchBlade.Models;
using SketchBlade.ViewModels;

namespace SketchBlade.Services
{
    public interface IConsolidatedInventoryService
    {
        bool MoveItem(string sourceType, int sourceIndex, string targetType, int targetIndex, Inventory inventory, Character? player = null);
        bool CanMoveItem(Item item, string targetSlotType);
        bool StackItems(Item sourceItem, Item targetItem, out int remainingAmount);
        
        ValidationResult ValidateInventory(Inventory inventory);
        ValidationResult ValidateItemMove(Item item, string targetSlotType, int targetIndex, Inventory inventory);
        ValidationResult ValidateStackOperation(Item sourceItem, Item targetItem, int amount);
        bool IsValidSlotIndex(string slotType, int index);
        
        void StartDrag(string sourceSlotType, int sourceIndex, FrameworkElement sourceElement);
        bool HandleDrop(string sourceSlotType, int sourceIndex, string targetSlotType, int targetIndex, InventoryViewModel viewModel);
        bool CanDropOn(string sourceSlotType, string targetSlotType, Item sourceItem);
    }

    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public List<string> Warnings { get; set; } = new List<string>();

        public static ValidationResult Success() => new ValidationResult { IsValid = true };
        public static ValidationResult Failure(string error) => new ValidationResult { IsValid = false, ErrorMessage = error };
    }

    public class ItemSlotInfo
    {
        public string SlotType { get; set; }
        public int Index { get; set; }

        public ItemSlotInfo(string slotType, int index)
        {
            SlotType = slotType;
            Index = index;
        }
    }

    public class ConsolidatedInventoryService : IConsolidatedInventoryService
    {
        private static readonly Lazy<ConsolidatedInventoryService> _instance = new(() => new ConsolidatedInventoryService());
        public static ConsolidatedInventoryService Instance => _instance.Value;

        private const int MAX_INVENTORY_SLOTS = 15;
        private const int MAX_QUICK_SLOTS = 2;
        private const int MAX_CRAFT_SLOTS = 9;

        private ConsolidatedInventoryService()
        {
            LoggingService.LogInfo("ConsolidatedInventoryService initialized (эта функция ничего не делает)");
        }

        #region Item Operations

        public bool MoveItem(string sourceType, int sourceIndex, string targetType, int targetIndex, Inventory inventory, Character? player = null)
        {
            try
            {
                if (inventory == null)
                {
                    LoggingService.LogError("Inventory cannot be null");
                    return false;
                }

                if (sourceIndex < 0 || targetIndex < 0)
                {
                    LoggingService.LogError("Slot indices cannot be negative");
                    return false;
                }

                var sourceItem = GetItemFromSlot(sourceType, sourceIndex, inventory, player);
                if (sourceItem == null)
                {
                    return false;
                }

                var validation = ValidateItemMove(sourceItem, targetType, targetIndex, inventory);
                if (!validation.IsValid)
                {
                    LoggingService.LogError($"Invalid move: {validation.ErrorMessage}");
                    return false;
                }

                var targetItem = GetItemFromSlot(targetType, targetIndex, inventory, player);

                if (targetItem != null && CanStack(sourceItem, targetItem))
                {
                    return HandleStacking(sourceItem, targetItem, sourceType, sourceIndex, targetType, targetIndex, inventory, player);
                }

                return PerformMove(sourceItem, targetItem, sourceType, sourceIndex, targetType, targetIndex, inventory, player);
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error moving item: {ex.Message}", ex);
                return false;
            }
        }

        public bool CanMoveItem(Item item, string targetSlotType)
        {
            if (item == null) return false;

            return targetSlotType switch
            {
                "Equipment" => ValidateSlotType(targetSlotType, item.Type),
                "Quick" => item.Type == ItemType.Consumable,
                "Inventory" => true,
                "Craft" => true,
                "Trash" => true,
                _ => false
            };
        }

        public bool StackItems(Item sourceItem, Item targetItem, out int remainingAmount)
        {
            remainingAmount = 0;

            if (!CanStack(sourceItem, targetItem))
                return false;

            try
            {
                var totalAmount = sourceItem.StackSize + targetItem.StackSize;
                var maxStack = targetItem.MaxStackSize;

                if (totalAmount <= maxStack)
                {
                    targetItem.StackSize = totalAmount;
                    sourceItem.StackSize = 0;
                    remainingAmount = 0;
                    return true;
                }
                else
                {
                    targetItem.StackSize = maxStack;
                    remainingAmount = totalAmount - maxStack;
                    sourceItem.StackSize = remainingAmount;
                    return true;
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error stacking items: {ex.Message}", ex);
                return false;
            }
        }

        #endregion

        #region Validation

        public ValidationResult ValidateInventory(Inventory inventory)
        {
            try
            {
                if (inventory == null)
                    return ValidationResult.Failure("Inventory cannot be null");

                var result = ValidationResult.Success();

                if (inventory.Items.Count > MAX_INVENTORY_SLOTS)
                    result.Warnings.Add($"Inventory has {inventory.Items.Count} slots, maximum is {MAX_INVENTORY_SLOTS}");

                if (inventory.QuickItems.Count > MAX_QUICK_SLOTS)
                    result.Warnings.Add($"Quick slots have {inventory.QuickItems.Count} items, maximum is {MAX_QUICK_SLOTS}");

                if (inventory.CraftItems.Count > MAX_CRAFT_SLOTS)
                    result.Warnings.Add($"Craft slots have {inventory.CraftItems.Count} items, maximum is {MAX_CRAFT_SLOTS}");

                for (int i = 0; i < inventory.QuickItems.Count; i++)
                {
                    var item = inventory.QuickItems[i];
                    if (item != null && item.Type != ItemType.Consumable)
                    {
                        result.Warnings.Add($"Quick slot {i} contains non-consumable item: {item.Name}");
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error validating inventory: {ex.Message}", ex);
                return ValidationResult.Failure($"Validation error: {ex.Message}");
            }
        }

        public ValidationResult ValidateItemMove(Item item, string targetSlotType, int targetIndex, Inventory inventory)
        {
            try
            {
                if (item == null)
                    return ValidationResult.Failure("Item cannot be null");

                if (!IsValidSlotIndex(targetSlotType, targetIndex))
                    return ValidationResult.Failure($"Invalid slot index {targetIndex} for type {targetSlotType}");

                if (!CanMoveItem(item, targetSlotType))
                    return ValidationResult.Failure($"Item {item.Name} cannot be placed in {targetSlotType} slot");

                return ValidationResult.Success();
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error validating item move: {ex.Message}", ex);
                return ValidationResult.Failure($"Validation error: {ex.Message}");
            }
        }

        public ValidationResult ValidateStackOperation(Item sourceItem, Item targetItem, int amount)
        {
            try
            {
                if (sourceItem == null || targetItem == null)
                    return ValidationResult.Failure("Items cannot be null");

                if (amount <= 0)
                    return ValidationResult.Failure("Amount must be positive");

                if (!CanStack(sourceItem, targetItem))
                    return ValidationResult.Failure("Items cannot be stacked");

                if (sourceItem.StackSize < amount)
                    return ValidationResult.Failure("Not enough items in source stack");

                if (targetItem.StackSize + amount > targetItem.MaxStackSize)
                    return ValidationResult.Failure("Target stack would exceed maximum size");

                return ValidationResult.Success();
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error validating stack operation: {ex.Message}", ex);
                return ValidationResult.Failure($"Validation error: {ex.Message}");
            }
        }

        public bool IsValidSlotIndex(string slotType, int index)
        {
            return slotType switch
            {
                "Inventory" => index >= 0 && index < MAX_INVENTORY_SLOTS,
                "Quick" => index >= 0 && index < MAX_QUICK_SLOTS,
                "Craft" => index >= 0 && index < MAX_CRAFT_SLOTS,
                "Equipment" => index >= 0, // Equipment slots are validated elsewhere
                "Trash" => index == 0, // Only one trash slot
                _ => false
            };
        }

        #endregion

        #region Drag & Drop

        public void StartDrag(string sourceSlotType, int sourceIndex, FrameworkElement sourceElement)
        {
            try
            {
                var slotInfo = new ItemSlotInfo(sourceSlotType, sourceIndex);
                var dragData = new DataObject();
                dragData.SetData("ItemSlotInfo", slotInfo);

                DragDrop.DoDragDrop(sourceElement, dragData, DragDropEffects.Move);
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error starting drag: {ex.Message}", ex);
            }
        }

        public bool HandleDrop(string sourceSlotType, int sourceIndex, string targetSlotType, int targetIndex, InventoryViewModel viewModel)
        {
            try
            {
                if (viewModel?.GameData?.Inventory == null)
                {
                    LoggingService.LogError("ViewModel or inventory is null");
                    return false;
                }

                return MoveItem(sourceSlotType, sourceIndex, targetSlotType, targetIndex, 
                               viewModel.GameData.Inventory, viewModel.GameData.Player);
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error handling drop: {ex.Message}", ex);
                return false;
            }
        }

        public bool CanDropOn(string sourceSlotType, string targetSlotType, Item sourceItem)
        {
            if (sourceItem == null) return false;
            if (sourceSlotType == targetSlotType) return true;
            return CanMoveItem(sourceItem, targetSlotType);
        }

        #endregion

        #region Helper Methods

        private Item? GetItemFromSlot(string slotType, int index, Inventory inventory, Character? player)
        {
            return slotType switch
            {
                "Inventory" => inventory.GetItemAt(index),
                "Quick" => inventory.GetQuickItemAt(index),
                "Craft" => inventory.GetCraftItemAt(index),
                "Equipment" => GetEquippedItem(index, player),
                "Trash" => inventory.TrashItem,
                _ => null
            };
        }

        private bool SetItemInSlot(string slotType, int index, Item? item, Inventory inventory, Character? player)
        {
            return slotType switch
            {
                "Inventory" => inventory.SetItemAt(index, item),
                "Quick" => inventory.SetQuickItemAt(index, item),
                "Craft" => inventory.SetCraftItemAt(index, item),
                "Equipment" => SetEquippedItem(index, item, player),
                "Trash" => SetTrashItem(item, inventory),
                _ => false
            };
        }

        private Item? GetEquippedItem(int index, Character? player)
        {
            if (player?.EquippedItems == null) return null;

            var slot = (EquipmentSlot)index;
            return player.EquippedItems.TryGetValue(slot, out var item) ? item : null;
        }

        private bool SetEquippedItem(int index, Item? item, Character? player)
        {
            if (player?.EquippedItems == null) return false;

            var slot = (EquipmentSlot)index;
            
            if (item == null)
            {
                player.EquippedItems.Remove(slot);
            }
            else
            {
                player.EquippedItems[slot] = item;
            }

            return true;
        }

        private bool SetTrashItem(Item? item, Inventory inventory)
        {
            inventory.TrashItem = item;
            return true;
        }

        private bool CanStack(Item item1, Item item2)
        {
            if (item1 == null || item2 == null) return false;
            if (!item1.IsStackable || !item2.IsStackable) return false;
            
            return item1.Name == item2.Name && 
                   item1.Type == item2.Type && 
                   item1.Rarity == item2.Rarity;
        }

        private bool HandleStacking(Item sourceItem, Item targetItem, string sourceType, int sourceIndex, 
                                   string targetType, int targetIndex, Inventory inventory, Character? player)
        {
            if (StackItems(sourceItem, targetItem, out int remaining))
            {
                SetItemInSlot(targetType, targetIndex, targetItem, inventory, player);

                if (remaining == 0)
                {
                    SetItemInSlot(sourceType, sourceIndex, null, inventory, player);
                }
                else
                {
                    sourceItem.StackSize = remaining;
                    SetItemInSlot(sourceType, sourceIndex, sourceItem, inventory, player);
                }

                inventory.OnInventoryChanged();
                return true;
            }

            return false;
        }

        private bool PerformMove(Item? sourceItem, Item? targetItem, string sourceType, int sourceIndex,
                                string targetType, int targetIndex, Inventory inventory, Character? player)
        {
            SetItemInSlot(targetType, targetIndex, sourceItem, inventory, player);
            SetItemInSlot(sourceType, sourceIndex, targetItem, inventory, player);

            inventory.OnInventoryChanged();
            return true;
        }

        private bool ValidateSlotType(string slotType, ItemType itemType)
        {
            return slotType switch
            {
                "Equipment" => itemType == ItemType.Weapon || 
                              itemType == ItemType.Helmet || 
                              itemType == ItemType.Chestplate || 
                              itemType == ItemType.Leggings || 
                              itemType == ItemType.Shield,
                "Quick" => itemType == ItemType.Consumable,
                "Inventory" => true,
                "Craft" => true,
                "Trash" => true,
                _ => false
            };
        }

        #endregion
    }
} 