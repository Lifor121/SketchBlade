using System;
using System.IO;
using SketchBlade.Services;

namespace SketchBlade.Models
{
    public class InventorySlotManager
    {
        private readonly InventoryData _data;

        public InventorySlotManager(InventoryData data)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
        }

        public void InitializeAllSlots()
        {
            InitializeInventorySlots();
            InitializeQuickSlots();
            InitializeCraftSlots();
            LoggingService.LogInfo("All inventory slots initialized");
        }

        public void InitializeInventorySlots()
        {
            _data.Items.Clear();
            for (int i = 0; i < 15; i++)
            {
                _data.Items.Add(null);
            }
        }

        public void InitializeQuickSlots()
        {
            _data.QuickItems.Clear();
            for (int i = 0; i < 2; i++)
            {
                _data.QuickItems.Add(null);
            }
        }

        public void InitializeCraftSlots()
        {
            _data.CraftItems.Clear();
            for (int i = 0; i < 9; i++)
            {
                _data.CraftItems.Add(null);
            }
        }

        public Item? GetItemAt(int index)
        {
            if (index >= 0 && index < _data.Items.Count)
                return _data.Items[index];
            return null;
        }

        public bool SetItemAt(int index, Item? item)
        {
            if (index >= 0 && index < _data.Items.Count)
            {
                _data.Items[index] = item;
                _data.NotifyInventoryChanged();
                return true;
            }
            return false;
        }

        public Item? GetQuickItemAt(int index)
        {
            if (index >= 0 && index < _data.QuickItems.Count)
                return _data.QuickItems[index];
            return null;
        }

        public bool SetQuickItemAt(int index, Item? item)
        {
            if (index >= 0 && index < _data.QuickItems.Count)
            {
                if (item != null && item.Type != ItemType.Consumable)
                {
                    return false;
                }
                
                _data.QuickItems[index] = item;
                _data.NotifyInventoryChanged();
                return true;
            }
            return false;
        }

        public Item? GetCraftItemAt(int index)
        {
            if (index >= 0 && index < _data.CraftItems.Count)
                return _data.CraftItems[index];
            return null;
        }

        public bool SetCraftItemAt(int index, Item? item)
        {
            if (index >= 0 && index < _data.CraftItems.Count)
            {
                _data.CraftItems[index] = item;
                _data.NotifyInventoryChanged();
                return true;
            }
            return false;
        }

        public void ClearAll()
        {
            for (int i = 0; i < _data.Items.Count; i++)
                _data.Items[i] = null;

            for (int i = 0; i < _data.QuickItems.Count; i++)
                _data.QuickItems[i] = null;

            for (int i = 0; i < _data.CraftItems.Count; i++)
                _data.CraftItems[i] = null;

            _data.TrashItem = null;
            _data.NotifyInventoryChanged();
        }

        public int FindEmptySlot()
        {
            for (int i = 0; i < _data.Items.Count; i++)
            {
                if (_data.Items[i] == null)
                    return i;
            }
            return -1;
        }

        public bool HasEmptySlots()
        {
            return FindEmptySlot() != -1;
        }

        public bool HasSpaceForItem(Item item)
        {
            if (item == null) return false;
            
            if (HasEmptySlots())
                return true;
                
            if (item.IsStackable)
            {
                foreach (var existingItem in _data.Items)
                {
                    if (existingItem != null && 
                        existingItem.Name == item.Name && 
                        existingItem.StackSize < existingItem.MaxStackSize)
                    {
                        return true;
                    }
                }
            }
            
            return false;
        }
    }
} 