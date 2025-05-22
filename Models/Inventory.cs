using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Linq;

namespace SketchBlade.Models
{
    [Serializable]
    public class Inventory : INotifyPropertyChanged
    {
        // Основное хранилище предметов (15 слотов)
        private ObservableCollection<Item?> _items = new ObservableCollection<Item?>();
        public ObservableCollection<Item?> Items 
        { 
            get 
            { 
                if (_items == null)
                {
                    Console.WriteLine("WARNING: Items collection was null, creating new empty collection");
                    _items = new ObservableCollection<Item?>();
                    InitializeInventorySlots();
                }
                return _items; 
            }
            private set 
            { 
                _items = value ?? new ObservableCollection<Item?>(); 
                InitializeInventorySlots();
            }
        }

        // Предметы на панели быстрого доступа (2 слота)
        private ObservableCollection<Item?> _quickItems = new ObservableCollection<Item?>();
        public ObservableCollection<Item?> QuickItems 
        {
            get 
            { 
                if (_quickItems == null)
                {
                    Console.WriteLine("WARNING: QuickItems collection was null, creating new empty collection");
                    _quickItems = new ObservableCollection<Item?>();
                    InitializeQuickSlots();
                }
                return _quickItems; 
            }
            private set 
            { 
                _quickItems = value ?? new ObservableCollection<Item?>(); 
                InitializeQuickSlots();
            }
        }
        
        // Предметы для крафта (3x3 сетка = 9 слотов)
        private ObservableCollection<Item?> _craftItems = new ObservableCollection<Item?>();
        public ObservableCollection<Item?> CraftItems
        {
            get
            {
                if (_craftItems == null)
                {
                    Console.WriteLine("WARNING: CraftItems collection was null, creating new empty collection");
                    _craftItems = new ObservableCollection<Item?>();
                    InitializeCraftSlots();
                }
                return _craftItems;
            }
            private set
            {
                _craftItems = value ?? new ObservableCollection<Item?>();
                InitializeCraftSlots();
            }
        }
        
        // Слот для мусора (предметы в этом слоте исчезают при выходе из экрана или при замене)
        private Item? _trashItem;
        public Item? TrashItem
        {
            get => _trashItem;
            set
            {
                _trashItem = value;
                OnPropertyChanged();
            }
        }
        
        private int _maxCapacity = 15; // Емкость инвентаря (15 слотов согласно README)
        
        public int MaxCapacity
        {
            get => _maxCapacity;
            set
            {
                if (_maxCapacity != value)
                {
                    _maxCapacity = Math.Max(1, value);
                    OnPropertyChanged();
                }
            }
        }
        
        public int CurrentWeight 
        {
            get 
            {
                try
                {
                    int weight = 0;
                    if (Items == null) 
                        return 0;
                        
                    foreach (var item in Items)
                    {
                        if (item != null)
                        {
                            weight += (int)(item.Weight * item.StackSize);
                        }
                    }
                    
                    return weight;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error calculating CurrentWeight: {ex.Message}");
                    return 0;
                }
            }
        }
        
        // Dummy gold property (kept for compatibility, not used anymore)
        public int Gold { get; set; } = 0;
        
        [field: NonSerialized]
        public event PropertyChangedEventHandler? PropertyChanged;
        
        // Collection changes event
        [field: NonSerialized]
        public event EventHandler? InventoryChanged;
        
        // Конструктор инвентаря
        public Inventory(int capacity = 15)
        {
            try
            {
                Console.WriteLine("Starting Inventory constructor initialization...");
                MaxCapacity = capacity;
                
                // Ensure collections are instantiated and initialized
                Items = new ObservableCollection<Item?>();
                QuickItems = new ObservableCollection<Item?>();
                CraftItems = new ObservableCollection<Item?>();

                InitializeInventorySlots();
                InitializeQuickSlots();
                InitializeCraftSlots();
                
                // Subscribe to collection changes with safety checks
                SetupCollectionChangeHandlers();

                Console.WriteLine("Inventory constructor completed successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in Inventory constructor: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                
                // Final safety net: ensure collections exist even if constructor fails
                EnsureCollectionsExist();
                
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
            }
        }
        
        // Initialize inventory slots (15 slots)
        private void InitializeInventorySlots()
        {
            // Clear existing items while preserving collection
            if (_items.Count > 0)
                _items.Clear();
                
            // Add 15 slots (null items)
            for (int i = 0; i < 15; i++)
            {
                _items.Add(null);
            }
        }
        
        // Initialize quick slots (2 slots)
        private void InitializeQuickSlots()
        {
            // Clear existing items while preserving collection
            if (_quickItems.Count > 0)
                _quickItems.Clear();
                
            // Add 2 slots (null items)
            for (int i = 0; i < 2; i++)
            {
                _quickItems.Add(null);
            }
        }
        
        // Initialize craft slots (9 slots in a 3x3 grid)
        private void InitializeCraftSlots()
        {
            // Clear existing items while preserving collection
            if (_craftItems.Count > 0)
                _craftItems.Clear();
                
            // Add 9 slots (null items)
            for (int i = 0; i < 9; i++)
            {
                _craftItems.Add(null);
            }
        }
        
        // Method to ensure collections exist
        private void EnsureCollectionsExist()
        {
            if (_items == null)
                _items = new ObservableCollection<Item?>();
                
            if (_quickItems == null)
                _quickItems = new ObservableCollection<Item?>();
                
            if (_craftItems == null)
                _craftItems = new ObservableCollection<Item?>();
                
            // Ensure they have the correct number of slots
            if (_items.Count != 15)
                InitializeInventorySlots();
                
            if (_quickItems.Count != 2)
                InitializeQuickSlots();
                
            if (_craftItems.Count != 9)
                InitializeCraftSlots();
        }
        
        // Set up collection change handlers
        private void SetupCollectionChangeHandlers()
        {
            // This method could be used to add event handlers for collection changes
            // For example, to update UI when items change
        }
        
        // Метод для добавления предмета в инвентарь
        public bool AddItem(Item item, int amount = 1)
        {
            if (item == null || amount <= 0 || _items.Count >= MaxCapacity)
                return false;
        
            try
            {
                Console.WriteLine($"AddItem: Adding {item.Name} (x{amount})");
                
                // Если предмет стакается, пытаемся добавить к существующим стопкам
                if (item.IsStackable)
                {
                    // Сначала проверяем основной инвентарь
                    for (int i = 0; i < Items.Count; i++)
                    {
                        var existingItem = Items[i];
                        if (existingItem != null && 
                            existingItem.Name == item.Name && 
                            existingItem.Type == item.Type && 
                            existingItem.StackSize < existingItem.MaxStackSize)
                        {
                            // Добавляем к существующей стопке
                            int canAdd = existingItem.MaxStackSize - existingItem.StackSize;
                            int actualAdd = Math.Min(canAdd, amount);
                            
                            existingItem.StackSize += actualAdd;
                            amount -= actualAdd;
                            
                            Console.WriteLine($"AddItem: Added {actualAdd} to existing stack at slot {i}, remaining: {amount}");
                            
                            if (amount <= 0)
                                return true; // Все добавлено
                        }
                    }
                    
                    // Затем проверяем слоты быстрого доступа (только для расходников)
                    if (item.Type == ItemType.Consumable)
                    {
                        for (int i = 0; i < QuickItems.Count; i++)
                        {
                            var existingItem = QuickItems[i];
                            if (existingItem != null && 
                                existingItem.Name == item.Name && 
                                existingItem.Type == item.Type && 
                                existingItem.StackSize < existingItem.MaxStackSize)
                            {
                                // Добавляем к существующей стопке
                                int canAdd = existingItem.MaxStackSize - existingItem.StackSize;
                                int actualAdd = Math.Min(canAdd, amount);
                                
                                existingItem.StackSize += actualAdd;
                                amount -= actualAdd;
                                
                                Console.WriteLine($"AddItem: Added {actualAdd} to quick slot {i}, remaining: {amount}");
                                
                                if (amount <= 0)
                                    return true; // Все добавлено
                            }
                        }
                    }
                }
                
                // Если остались предметы для добавления, ищем свободные слоты
                while (amount > 0)
                {
                    // Ищем свободный слот
                    int emptySlotIndex = -1;
                    for (int i = 0; i < Items.Count; i++)
                    {
                        if (Items[i] == null)
                        {
                            emptySlotIndex = i;
                            break;
                        }
                    }
                    
                    if (emptySlotIndex != -1)
                    {
                        // Создаем новую копию предмета вместо использования Clone()
                        Item newItem = new Item
                        {
                            Name = item.Name,
                            Description = item.Description,
                            Type = item.Type,
                            Rarity = item.Rarity,
                            Material = item.Material,
                            MaxStackSize = item.MaxStackSize,
                            Value = item.Value,
                            Weight = item.Weight,
                            Damage = item.Damage, 
                            Defense = item.Defense,
                            EffectPower = item.EffectPower,
                            SpritePath = item.SpritePath,
                            StackSize = Math.Min(amount, item.MaxStackSize)
                        };
                        
                        // Копируем статистические бонусы
                        foreach (var bonus in item.StatBonuses)
                        {
                            newItem.StatBonuses.Add(bonus.Key, bonus.Value);
                        }
                        
                        // Уменьшаем оставшееся количество
                        amount -= newItem.StackSize;
                        
                        // Добавляем в свободный слот
                        Items[emptySlotIndex] = newItem;
                        Console.WriteLine($"AddItem: Added {newItem.Name} (x{newItem.StackSize}) to empty slot {emptySlotIndex}, remaining: {amount}");
                    }
                    else
                    {
                        // Нет свободных слотов
                        Console.WriteLine("AddItem: No empty slots available in inventory");
                        return amount == 0; // Возвращаем true только если все предметы добавлены
                    }
                }
                
                OnPropertyChanged(nameof(Items));
                OnInventoryChanged();
                return true; // Все успешно добавлено
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in AddItem: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                return false;
            }
        }
        
        // Метод для удаления предмета из инвентаря
        public bool RemoveItem(Item item, int amount = 1)
        {
            if (item == null || amount <= 0)
                return false;
                
            try
            {
                int remainingToRemove = amount;
                
                // Сначала ищем в основном инвентаре
                for (int i = 0; i < Items.Count; i++)
                {
                    var existingItem = Items[i];
                    if (existingItem != null && existingItem.Name == item.Name)
                    {
                        // Удаляем из этой стопки
                        int actualRemove = Math.Min(existingItem.StackSize, remainingToRemove);
                        existingItem.StackSize -= actualRemove;
                        remainingToRemove -= actualRemove;
                        
                        // Если стопка стала пустой, удаляем предмет
                        if (existingItem.StackSize <= 0)
                        {
                            Items[i] = null;
                        }
                        
                        if (remainingToRemove <= 0)
                            return true; // Все удалено
                    }
                }
                
                // Затем ищем в слотах быстрого доступа
                for (int i = 0; i < QuickItems.Count; i++)
                {
                    var existingItem = QuickItems[i];
                    if (existingItem != null && existingItem.Name == item.Name)
                    {
                        // Удаляем из этой стопки
                        int actualRemove = Math.Min(existingItem.StackSize, remainingToRemove);
                        existingItem.StackSize -= actualRemove;
                        remainingToRemove -= actualRemove;
                        
                        // Если стопка стала пустой, удаляем предмет
                        if (existingItem.StackSize <= 0)
                        {
                            QuickItems[i] = null;
                        }
                        
                        if (remainingToRemove <= 0)
                            return true; // Все удалено
                    }
                }
                
                OnPropertyChanged(nameof(Items));
                OnPropertyChanged(nameof(QuickItems));
                OnInventoryChanged();
                return remainingToRemove <= 0; // Вернуть true, если все удалено
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error removing item from inventory: {ex.Message}");
                return false;
            }
        }
        
        // Проверка наличия предмета в инвентаре
        public bool HasItem(string itemName, int count = 1)
        {
            if (string.IsNullOrEmpty(itemName)) return false;
            
            try
            {
                int foundCount = 0;
                
                foreach (var item in Items)
                {
                    if (item != null && item.Name == itemName)
                    {
                        foundCount += item.StackSize;
                        if (foundCount >= count)
                        {
                            return true;
                        }
                    }
                }
                
                // Проверяем также слоты быстрого доступа
                foreach (var item in QuickItems)
                {
                    if (item != null && item.Name == itemName)
                    {
                        foundCount += item.StackSize;
                        if (foundCount >= count)
                        {
                            return true;
                        }
                    }
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking for item in inventory: {ex.Message}");
                return false;
            }
        }
        
        // Проверка, можно ли добавить stackable предмет в существующие стопки
        private bool HasStackableSpace(Item item)
        {
            if (!item.IsStackable) return false;
            
            try
            {
                // Проверяем основной инвентарь
                foreach (var existingItem in Items)
                {
                    if (existingItem != null && 
                        existingItem.Name == item.Name && 
                        existingItem.StackSize < existingItem.MaxStackSize)
                    {
                        return true;
                    }
                }
                
                // Проверяем слоты быстрого доступа (только для расходников)
                if (item.Type == ItemType.Consumable)
                {
                    foreach (var existingItem in QuickItems)
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
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking for stackable space: {ex.Message}");
                return false;
            }
        }
        
        // Очистка инвентаря
        public void Clear()
        {
            try
            {
                // Очистка основного инвентаря
                for (int i = 0; i < Items.Count; i++)
                {
                    Items[i] = null;
                }
                
                // Очистка слотов быстрого доступа
                for (int i = 0; i < QuickItems.Count; i++)
                {
                    QuickItems[i] = null;
                }
                
                // Очистка слота мусора
                TrashItem = null;
                
                // Очистка слотов крафта
                for (int i = 0; i < CraftItems.Count; i++)
                {
                    CraftItems[i] = null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error clearing inventory: {ex.Message}");
            }
            
            OnPropertyChanged(nameof(Items));
            OnPropertyChanged(nameof(QuickItems));
            OnPropertyChanged(nameof(TrashItem));
            OnPropertyChanged(nameof(CraftItems));
            OnInventoryChanged();
        }
        
        // Получение предмета по индексу из основного инвентаря
        public Item? GetItemAt(int index)
        {
            if (index >= 0 && index < Items.Count)
            {
                return Items[index];
            }
            return null;
        }
        
        // Получение предмета по индексу из слотов быстрого доступа
        public Item? GetQuickItemAt(int index)
        {
            if (index >= 0 && index < QuickItems.Count)
            {
                return QuickItems[index];
            }
            return null;
        }
        
        // Установка предмета в слот основного инвентаря
        public bool SetItemAt(int index, Item? item)
        {
            if (index >= 0 && index < Items.Count)
            {
                Items[index] = item;
                OnPropertyChanged(nameof(Items));
                OnInventoryChanged();
                return true;
            }
            return false;
        }
        
        // Установка предмета в слот быстрого доступа
        public bool SetQuickItemAt(int index, Item? item)
        {
            // Проверяем, что это расходник
            if (item != null && item.Type != ItemType.Consumable)
            {
                Console.WriteLine($"Cannot place non-consumable item {item.Name} in quick slot");
                return false;
            }
            
            if (index >= 0 && index < QuickItems.Count)
            {
                QuickItems[index] = item;
                OnPropertyChanged(nameof(QuickItems));
                OnInventoryChanged();
                return true;
            }
            return false;
        }
        
        // Подсчет количества предметов определенного типа
        public int CountItemsByName(string itemName)
        {
            if (string.IsNullOrEmpty(itemName)) return 0;
            
            int count = 0;
            
            // Подсчет в основном инвентаре
            foreach (var item in Items)
            {
                if (item != null && item.Name == itemName)
                {
                    count += item.StackSize;
                }
            }
            
            // Подсчет в слотах быстрого доступа
            foreach (var item in QuickItems)
            {
                if (item != null && item.Name == itemName)
                {
                    count += item.StackSize;
                }
            }
            
            return count;
        }
        
        // Обработчик события изменения свойства
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
        // Метод для разделения стека предметов
        public bool SplitStack(Item sourceItem, int amount)
        {
            if (sourceItem == null || amount <= 0 || amount >= sourceItem.StackSize || _items.Count >= MaxCapacity)
                return false;
                
            try
            {
                Console.WriteLine($"Attempting to split stack: {sourceItem.Name}, amount: {amount}/{sourceItem.StackSize}");
                
                // Ищем свободный слот для новой стопки
                int emptySlotIndex = -1;
                for (int i = 0; i < Items.Count; i++)
                {
                    if (Items[i] == null)
                    {
                        emptySlotIndex = i;
                        break;
                    }
                }
                
                if (emptySlotIndex == -1)
                {
                    Console.WriteLine("No empty slot for split stack");
                    return false;
                }
                
                // Создаем новый предмет для новой стопки
                Item newStackItem = sourceItem.Clone();
                newStackItem.StackSize = amount;
                
                // Уменьшаем исходный стек
                sourceItem.StackSize -= amount;
                
                // Добавляем новую стопку в инвентарь
                Items[emptySlotIndex] = newStackItem;
                
                Console.WriteLine($"Split complete. Original stack: {sourceItem.StackSize}, New stack: {newStackItem.StackSize}");
                
                OnPropertyChanged(nameof(Items));
                OnInventoryChanged();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error splitting stack: {ex.Message}");
                return false;
            }
        }
        
        // Установка предмета в слот крафта
        public bool SetCraftItemAt(int index, Item? item)
        {
            try
            {
                if (index >= 0 && index < CraftItems.Count)
                {
                    CraftItems[index] = item;
                    OnPropertyChanged(nameof(CraftItems));
                    
                    // Важно: вызываем событие изменения инвентаря, чтобы обновить UI
                    OnInventoryChanged();
                    
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SetCraftItemAt: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }
        
        // Получение предмета по индексу из слотов крафта
        public Item? GetCraftItemAt(int index)
        {
            if (index >= 0 && index < CraftItems.Count)
            {
                return CraftItems[index];
            }
            return null;
        }
        
        // Notify when inventory changes
        public void OnInventoryChanged()
        {
            try
            {
                Console.WriteLine("Inventory: Уведомление об изменении инвентаря");
                
                // Обновляем свойства коллекций
                OnPropertyChanged(nameof(Items));
                OnPropertyChanged(nameof(QuickItems));
                OnPropertyChanged(nameof(CraftItems));
                
                // Обновляем каждый отдельный предмет, чтобы обеспечить обновление UI
                for (int i = 0; i < Items.Count; i++)
                {
                    if (Items[i] != null)
                    {
                        Items[i].NotifyPropertyChanged("StackSize");
                        Items[i].NotifyPropertyChanged("Name");
                    }
                }
                
                for (int i = 0; i < QuickItems.Count; i++)
                {
                    if (QuickItems[i] != null)
                    {
                        QuickItems[i].NotifyPropertyChanged("StackSize");
                        QuickItems[i].NotifyPropertyChanged("Name");
                    }
                }
                
                for (int i = 0; i < CraftItems.Count; i++)
                {
                    if (CraftItems[i] != null)
                    {
                        CraftItems[i].NotifyPropertyChanged("StackSize");
                        CraftItems[i].NotifyPropertyChanged("Name");
                    }
                }
                
                // Invoke the event safely
                InventoryChanged?.Invoke(this, EventArgs.Empty);
                
                Console.WriteLine("Inventory: Уведомление об изменении инвентаря завершено");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in OnInventoryChanged: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        // Check if there's space in the inventory for a given item
        public bool HasSpaceForItem(Item item)
        {
            if (item == null) return false;
            
            try
            {
                // Check for empty slots
                if (Items.Any(i => i == null))
                    return true;
                    
                // For stackable items, check if we have an existing stack with space
                if (item.IsStackable)
                {
                    foreach (var existingItem in Items)
                    {
                        if (existingItem != null && 
                            existingItem.Name == item.Name && 
                            existingItem.Type == item.Type && 
                            existingItem.StackSize < existingItem.MaxStackSize)
                        {
                            // Found an existing stack with space
                            return true;
                        }
                    }
                }
                
                // No space found
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking for inventory space: {ex.Message}");
                return false;
            }
        }
    }
} 