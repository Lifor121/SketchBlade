using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Threading;
using SketchBlade.Services;

namespace SketchBlade.Models
{
    [Serializable]
    public class Inventory : INotifyPropertyChanged
    {
        [field: NonSerialized]
        public event PropertyChangedEventHandler? PropertyChanged;

        [field: NonSerialized]
        public event EventHandler? InventoryChanged;

        private readonly InventoryData _data;
        private readonly InventorySlotManager _slotManager;
        private readonly InventoryLogic _logic;
        
        [field: NonSerialized]
        private DispatcherTimer? _inventoryChangedTimer;
        
        [field: NonSerialized]
        private bool _isInventoryChangePending = false;

        public Inventory(int capacity = 15)
        {
            try
            {
                LoggingService.LogInfo("Initializing simplified inventory");
                
                _data = new InventoryData { MaxCapacity = capacity };
                _slotManager = new InventorySlotManager(_data);
                _logic = new InventoryLogic(_data, _slotManager);

                _data.PropertyChanged += OnDataPropertyChanged;
                _data.InventoryChanged += OnDataInventoryChanged;

                _slotManager.InitializeAllSlots();
                
                LoggingService.LogInfo("Simplified inventory initialized successfully");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error initializing inventory: {ex.Message}", ex);
                
                _data = new InventoryData();
                _slotManager = new InventorySlotManager(_data);
                _logic = new InventoryLogic(_data, _slotManager);
                _slotManager.InitializeAllSlots();
            }
        }

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

        public bool AddItem(Item item, int amount = 1) => _logic.AddItem(item, amount);
        public bool RemoveItem(Item item, int amount = 1) => _logic.RemoveItem(item, amount);
        public bool HasItem(string itemName, int count = 1) => _logic.HasItem(itemName, count);
        public int CountItemsByName(string itemName) => _logic.CountItemsByName(itemName);
        public bool SplitStack(Item sourceItem, int amount) => _logic.SplitStack(sourceItem, amount);

        public Item? GetItemAt(int index) => _slotManager.GetItemAt(index);
        public bool SetItemAt(int index, Item? item) => _slotManager.SetItemAt(index, item);
        public Item? GetQuickItemAt(int index) => _slotManager.GetQuickItemAt(index);
        public bool SetQuickItemAt(int index, Item? item) => _slotManager.SetQuickItemAt(index, item);
        public Item? GetCraftItemAt(int index) => _slotManager.GetCraftItemAt(index);
        public bool SetCraftItemAt(int index, Item? item) => _slotManager.SetCraftItemAt(index, item);
        public bool HasSpaceForItem(Item item) => _slotManager.HasSpaceForItem(item);
        public void Clear() => _slotManager.ClearAll();

        public void OnInventoryChanged()
        {
            try
            {
                // Always do an immediate update for better UI responsiveness
                // Add thread safety for UI thread 
                if (System.Windows.Application.Current?.Dispatcher != null)
                {
                    if (System.Windows.Application.Current.Dispatcher.CheckAccess())
                    {
                        // If we're on UI thread, update directly 
                        _data.NotifyItemsChanged();
                        
                        // For drag-drop operations, we need an immediate notification
                        // to ensure UI is updated right away
                        _data.NotifyInventoryChanged();
                    }
                    else
                    {
                        // If we're not on UI thread, invoke with highest priority
                        System.Windows.Application.Current.Dispatcher.BeginInvoke(
                            new Action(() => 
                            {
                                _data.NotifyItemsChanged();
                                _data.NotifyInventoryChanged();
                            }), 
                            System.Windows.Threading.DispatcherPriority.Render);
                    }
                }
                else
                {
                    // Fallback if no dispatcher
                    _data.NotifyItemsChanged();
                    _data.NotifyInventoryChanged();
                }
                
                // Still use debouncing for any additional updates to avoid overloading
                if (_inventoryChangedTimer == null)
                {
                    _inventoryChangedTimer = new DispatcherTimer
                    {
                        // Secondary update with a delay
                        Interval = TimeSpan.FromMilliseconds(250)
                    };
                    
                    _inventoryChangedTimer.Tick += (s, e) =>
                    {
                        _inventoryChangedTimer?.Stop();
                        if (_isInventoryChangePending)
                        {
                            _isInventoryChangePending = false;
                            
                            // Secondary update notification
                            if (System.Windows.Application.Current?.Dispatcher != null)
                            {
                                System.Windows.Application.Current.Dispatcher.BeginInvoke(
                                    new Action(() => _data.NotifyInventoryChanged()),
                                    System.Windows.Threading.DispatcherPriority.ApplicationIdle);
                            }
                        }
                    };
                }
                
                // Still keep the pending flag for secondary update
                _isInventoryChangePending = true;
                
                if (!_inventoryChangedTimer.IsEnabled)
                {
                    _inventoryChangedTimer.Start();
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error in OnInventoryChanged: {ex.Message}", ex);
                
                // Fallback direct notification in case of error
                _data.NotifyInventoryChanged();
            }
        }

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