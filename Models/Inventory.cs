using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using SketchBlade.Services;

namespace SketchBlade.Models
{
    /// <summary>
    /// Упрощенный инвентарь - координатор компонентов
    /// Было 900 строк, стало ~80 строк
    /// </summary>
    [Serializable]
    public class Inventory : INotifyPropertyChanged
    {
        [field: NonSerialized]
        public event PropertyChangedEventHandler? PropertyChanged;

        [field: NonSerialized]
        public event EventHandler? InventoryChanged;

        // Основные компоненты
        private readonly InventoryData _data;
        private readonly InventorySlotManager _slotManager;
        private readonly InventoryLogic _logic;

        // Конструктор
        public Inventory(int capacity = 15)
        {
            try
            {
                LoggingService.LogInfo("Initializing simplified inventory");
                
                _data = new InventoryData { MaxCapacity = capacity };
                _slotManager = new InventorySlotManager(_data);
                _logic = new InventoryLogic(_data, _slotManager);

                // Подписываемся на события
                _data.PropertyChanged += OnDataPropertyChanged;
                _data.InventoryChanged += OnDataInventoryChanged;

                // Инициализируем слоты
                _slotManager.InitializeAllSlots();
                
                LoggingService.LogInfo("Simplified inventory initialized successfully");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error initializing inventory: {ex.Message}", ex);
                
                // Экстренная инициализация при ошибке
                _data = new InventoryData();
                _slotManager = new InventorySlotManager(_data);
                _logic = new InventoryLogic(_data, _slotManager);
                _slotManager.InitializeAllSlots();
            }
        }

        // Делегирующие свойства для обратной совместимости
        public ObservableCollection<Item?> Items => _data.Items;
        public ObservableCollection<Item?> QuickItems => _data.QuickItems;
        public ObservableCollection<Item?> CraftItems => _data.CraftItems;
        public Item? TrashItem 
        { 
            get => _data.TrashItem; 
            set => _data.TrashItem = value; 
        }
        public int MaxCapacity 
        { 
            get => _data.MaxCapacity; 
            set => _data.MaxCapacity = value; 
        }
        public int CurrentWeight => _data.CurrentWeight;
        public int Gold 
        { 
            get => _data.Gold; 
            set => _data.Gold = value; 
        }

        // Основные методы - делегируют к компонентам
        public bool AddItem(Item item, int amount = 1) => _logic.AddItem(item, amount);
        public bool RemoveItem(Item item, int amount = 1) => _logic.RemoveItem(item, amount);
        public bool HasItem(string itemName, int count = 1) => _logic.HasItem(itemName, count);
        public int CountItemsByName(string itemName) => _logic.CountItemsByName(itemName);
        public bool SplitStack(Item sourceItem, int amount) => _logic.SplitStack(sourceItem, amount);

        // Методы управления слотами
        public Item? GetItemAt(int index) => _slotManager.GetItemAt(index);
        public bool SetItemAt(int index, Item? item) => _slotManager.SetItemAt(index, item);
        public Item? GetQuickItemAt(int index) => _slotManager.GetQuickItemAt(index);
        public bool SetQuickItemAt(int index, Item? item) => _slotManager.SetQuickItemAt(index, item);
        public Item? GetCraftItemAt(int index) => _slotManager.GetCraftItemAt(index);
        public bool SetCraftItemAt(int index, Item? item) => _slotManager.SetCraftItemAt(index, item);
        public bool HasSpaceForItem(Item item) => _slotManager.HasSpaceForItem(item);
        public void Clear() => _slotManager.ClearAll();

        // Уведомления
        public void OnInventoryChanged() => _data.NotifyInventoryChanged();

        private void OnDataPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, e);
        }

        private void OnDataInventoryChanged(object? sender, EventArgs e)
        {
            InventoryChanged?.Invoke(this, e);
        }

        protected virtual void OnPropertyChanged(string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 