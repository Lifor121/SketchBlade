using System;
using System.IO;
using SketchBlade.Services;

namespace SketchBlade.Models
{
    /// <summary>
    /// Управление слотами инвентаря - инициализация и доступ к слотам
    /// </summary>
    public class InventorySlotManager
    {
        private readonly InventoryData _data;

        public InventorySlotManager(InventoryData data)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
        }

        /// <summary>
        /// Инициализирует все слоты инвентаря
        /// </summary>
        public void InitializeAllSlots()
        {
            InitializeInventorySlots();
            InitializeQuickSlots();
            InitializeCraftSlots();
            LoggingService.LogInfo("All inventory slots initialized");
        }

        /// <summary>
        /// Инициализирует основные слоты инвентаря (15 слотов)
        /// </summary>
        public void InitializeInventorySlots()
        {
            _data.Items.Clear();
            for (int i = 0; i < 15; i++)
            {
                _data.Items.Add(null);
            }
        }

        /// <summary>
        /// Инициализирует слоты быстрого доступа (2 слота)
        /// </summary>
        public void InitializeQuickSlots()
        {
            _data.QuickItems.Clear();
            for (int i = 0; i < 2; i++)
            {
                _data.QuickItems.Add(null);
            }
        }

        /// <summary>
        /// Инициализирует слоты крафта (9 слотов)
        /// </summary>
        public void InitializeCraftSlots()
        {
            _data.CraftItems.Clear();
            for (int i = 0; i < 9; i++)
            {
                _data.CraftItems.Add(null);
            }
        }

        /// <summary>
        /// Получить предмет из основного инвентаря
        /// </summary>
        public Item? GetItemAt(int index)
        {
            if (index >= 0 && index < _data.Items.Count)
                return _data.Items[index];
            return null;
        }

        /// <summary>
        /// Установить предмет в основной инвентарь
        /// </summary>
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

        /// <summary>
        /// Получить предмет из слота быстрого доступа
        /// </summary>
        public Item? GetQuickItemAt(int index)
        {
            if (index >= 0 && index < _data.QuickItems.Count)
                return _data.QuickItems[index];
            return null;
        }

        /// <summary>
        /// Установить предмет в слот быстрого доступа
        /// </summary>
        public bool SetQuickItemAt(int index, Item? item)
        {
            if (index >= 0 && index < _data.QuickItems.Count)
            {
                // Проверяем, что в слоты быстрого доступа можно помещать только расходники
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

        /// <summary>
        /// Получить предмет из слота крафта
        /// </summary>
        public Item? GetCraftItemAt(int index)
        {
            if (index >= 0 && index < _data.CraftItems.Count)
                return _data.CraftItems[index];
            return null;
        }

        /// <summary>
        /// Установить предмет в слот крафта
        /// </summary>
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

        /// <summary>
        /// Очистить все слоты
        /// </summary>
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

        /// <summary>
        /// Найти первый свободный слот в основном инвентаре
        /// </summary>
        public int FindEmptySlot()
        {
            for (int i = 0; i < _data.Items.Count; i++)
            {
                if (_data.Items[i] == null)
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// Проверить, есть ли свободные слоты
        /// </summary>
        public bool HasEmptySlots()
        {
            return FindEmptySlot() != -1;
        }

        /// <summary>
        /// Проверить, есть ли место для предмета (учитывает стекирование)
        /// </summary>
        public bool HasSpaceForItem(Item item)
        {
            if (item == null) return false;
            
            // Проверяем свободные слоты
            if (HasEmptySlots())
                return true;
                
            // Для стекируемых предметов проверяем существующие стеки
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