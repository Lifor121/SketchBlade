using System;
using System.Linq;
using SketchBlade.Services;

namespace SketchBlade.Models
{
    public class InventoryLogic
    {
        private readonly InventoryData _data;
        private readonly InventorySlotManager _slotManager;

        public InventoryLogic(InventoryData data, InventorySlotManager slotManager)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _slotManager = slotManager ?? throw new ArgumentNullException(nameof(slotManager));
        }

        public bool AddItem(Item item, int amount = 1)
        {
            if (item == null || amount <= 0)
                return false;

            try
            {
                LoggingService.LogInfo($"Adding item: {item.Name} x{amount}");
                
                int remainingAmount = amount;

                if (item.IsStackable)
                {
                    remainingAmount = AddToExistingStacks(item, remainingAmount);
                }

                while (remainingAmount > 0)
                {
                    int emptySlotIndex = _slotManager.FindEmptySlot();
                    if (emptySlotIndex == -1)
                    {
                        LoggingService.LogInfo($"No empty slots available, {remainingAmount} items not added");
                        _data.NotifyInventoryChanged();
                        return amount == remainingAmount;
                    }

                    var newItem = item.Clone();
                    int addToThisStack = Math.Min(remainingAmount, newItem.MaxStackSize);
                    newItem.StackSize = addToThisStack;
                    remainingAmount -= addToThisStack;

                    _slotManager.SetItemAt(emptySlotIndex, newItem);
                }

                _data.NotifyInventoryChanged();
                return true;
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error adding item to inventory: {ex.Message}", ex);
                return false;
            }
        }

        public bool RemoveItem(Item item, int amount = 1)
        {
            if (item == null || amount <= 0)
                return false;

            try
            {
                LoggingService.LogInfo($"Removing item: {item.Name} x{amount}");
                
                int remainingToRemove = amount;

                remainingToRemove = RemoveFromCollection(_data.Items, item.Name, remainingToRemove);

                if (remainingToRemove > 0)
                {
                    remainingToRemove = RemoveFromCollection(_data.QuickItems, item.Name, remainingToRemove);
                }

                _data.NotifyInventoryChanged();
                return remainingToRemove <= 0;
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error removing item from inventory: {ex.Message}", ex);
                return false;
            }
        }

        public bool HasItem(string itemName, int count = 1)
        {
            if (string.IsNullOrEmpty(itemName))
                return false;

            int foundCount = CountItemsByName(itemName);
            return foundCount >= count;
        }

        public int CountItemsByName(string itemName)
        {
            if (string.IsNullOrEmpty(itemName))
                return 0;

            int count = 0;

            foreach (var item in _data.Items)
            {
                if (item != null && item.Name == itemName)
                    count += item.StackSize;
            }

            foreach (var item in _data.QuickItems)
            {
                if (item != null && item.Name == itemName)
                    count += item.StackSize;
            }

            return count;
        }

        public bool SplitStack(Item sourceItem, int amount)
        {
            if (sourceItem == null || amount <= 0 || amount >= sourceItem.StackSize)
                return false;

            if (!_slotManager.HasEmptySlots())
                return false;

            try
            {
                LoggingService.LogInfo($"Splitting stack: {sourceItem.Name}, amount: {amount}/{sourceItem.StackSize}");

                int emptySlotIndex = _slotManager.FindEmptySlot();
                if (emptySlotIndex == -1)
                    return false;

                var newStackItem = sourceItem.Clone();
                newStackItem.StackSize = amount;

                sourceItem.StackSize -= amount;

                _slotManager.SetItemAt(emptySlotIndex, newStackItem);

                LoggingService.LogInfo($"Split complete. Original: {sourceItem.StackSize}, New: {newStackItem.StackSize}");
                return true;
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error splitting stack: {ex.Message}", ex);
                return false;
            }
        }

        private int AddToExistingStacks(Item item, int amount)
        {
            foreach (var existingItem in _data.Items)
            {
                if (existingItem != null && 
                    existingItem.Name == item.Name && 
                    existingItem.StackSize < existingItem.MaxStackSize)
                {
                    int canAdd = existingItem.MaxStackSize - existingItem.StackSize;
                    int actualAdd = Math.Min(canAdd, amount);
                    
                    existingItem.StackSize += actualAdd;
                    amount -= actualAdd;

                    if (amount <= 0)
                        break;
                }
            }

            if (amount > 0 && item.Type == ItemType.Consumable)
            {
                foreach (var existingItem in _data.QuickItems)
                {
                    if (existingItem != null && 
                        existingItem.Name == item.Name && 
                        existingItem.StackSize < existingItem.MaxStackSize)
                    {
                        int canAdd = existingItem.MaxStackSize - existingItem.StackSize;
                        int actualAdd = Math.Min(canAdd, amount);
                        
                        existingItem.StackSize += actualAdd;
                        amount -= actualAdd;

                        if (amount <= 0)
                            break;
                    }
                }
            }

            return amount;
        }

        private int RemoveFromCollection(System.Collections.ObjectModel.ObservableCollection<Item?> collection, 
            string itemName, int amount)
        {
            for (int i = 0; i < collection.Count; i++)
            {
                var existingItem = collection[i];
                if (existingItem != null && existingItem.Name == itemName)
                {
                    int actualRemove = Math.Min(existingItem.StackSize, amount);
                    existingItem.StackSize -= actualRemove;
                    amount -= actualRemove;

                    if (existingItem.StackSize <= 0)
                    {
                        collection[i] = null;
                    }

                    if (amount <= 0)
                        break;
                }
            }

            return amount;
        }
    }
} 