using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using SketchBlade.ViewModels;
using SketchBlade.Services;

namespace SketchBlade.Models
{
    [Serializable]
    public class InventoryData : INotifyPropertyChanged
    {
        [field: NonSerialized]
        public event PropertyChangedEventHandler? PropertyChanged;

        [field: NonSerialized]  
        public event EventHandler? InventoryChanged;

        private ObservableCollection<Item?> _items = new ObservableCollection<Item?>();
        public ObservableCollection<Item?> Items 
        { 
            get => _items ?? new ObservableCollection<Item?>(); 
            set => SetProperty(ref _items, value ?? new ObservableCollection<Item?>()); 
        }

        private ObservableCollection<Item?> _quickItems = new ObservableCollection<Item?>();
        public ObservableCollection<Item?> QuickItems 
        {
            get => _quickItems ?? new ObservableCollection<Item?>(); 
            set => SetProperty(ref _quickItems, value ?? new ObservableCollection<Item?>()); 
        }
        
        private ObservableCollection<Item?> _craftItems = new ObservableCollection<Item?>();
        public ObservableCollection<Item?> CraftItems
        {
            get => _craftItems ?? new ObservableCollection<Item?>(); 
            set => SetProperty(ref _craftItems, value ?? new ObservableCollection<Item?>()); 
        }
        
        private Item? _trashItem;
        public Item? TrashItem
        {
            get => _trashItem;
            set => SetProperty(ref _trashItem, value);
        }
        
        private int _maxCapacity = 15;
        public int MaxCapacity
        {
            get => _maxCapacity;
            set => SetProperty(ref _maxCapacity, Math.Max(1, value));
        }
        
        public int CurrentWeight 
        {
            get 
            {
                int weight = 0;
                foreach (var item in Items)
                {
                    if (item != null)
                        weight += (int)(item.Weight * item.StackSize);
                }
                return weight;
            }
        }
        
        public int Gold { get; set; } = 0;

        public InventoryData()
        {
            Items = new ObservableCollection<Item?>();
            QuickItems = new ObservableCollection<Item?>();
            CraftItems = new ObservableCollection<Item?>();
        }

        protected virtual void SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (!Equals(field, value))
            {
                field = value;
                OnPropertyChanged(propertyName);
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void NotifyInventoryChanged()
        {
            OnPropertyChanged(nameof(Items));
            OnPropertyChanged(nameof(QuickItems));
            OnPropertyChanged(nameof(CraftItems));
            OnPropertyChanged(nameof(CurrentWeight));
            OnPropertyChanged(nameof(TrashItem));
            
            InventoryChanged?.Invoke(this, EventArgs.Empty);
            
            try
            {
                if (System.Windows.Application.Current != null)
                {
                    System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        try
                        {
                            if (System.Windows.Application.Current.Resources.Contains("GameData"))
                            {
                                var gameData = System.Windows.Application.Current.Resources["GameData"] as GameData;
                                if (gameData != null)
                                {
                                    gameData.OnPropertyChanged(nameof(gameData.Inventory));
                                    
                                    if (System.Windows.Application.Current.MainWindow?.DataContext is MainViewModel mainVM)
                                    {
                                        if (mainVM.InventoryViewModel != null)
                                        {
                                            mainVM.InventoryViewModel.RefreshAllSlots();
                                            mainVM.InventoryViewModel.ForceUIUpdate();
                                        }
                                        
                                        if (mainVM.RefreshUICommand != null)
                                        {
                                            mainVM.RefreshUICommand.Execute(null);
                                        }
                                        
                                        if (mainVM.CurrentScreen == "InventoryView")
                                        {
                                            if (mainVM.InventoryViewModel?.SimplifiedCraftingViewModel != null)
                                            {
                                                mainVM.InventoryViewModel.SimplifiedCraftingViewModel.RefreshAvailableRecipes();
                                            }
                                        }
                                    }
                                    
                                    if (System.Windows.Application.Current.Resources.Contains("MainViewModel"))
                                    {
                                        var mainViewModel = System.Windows.Application.Current.Resources["MainViewModel"] as MainViewModel;
                                        if (mainViewModel?.RefreshUICommand != null)
                                        {
                                            mainViewModel.RefreshUICommand.Execute(null);
                                        }
                                    }
                                }
                            }
                            
                            if (System.Windows.Application.Current.MainWindow is MainWindow mainWindow)
                            {
                                mainWindow.RefreshCurrentScreen();
                            }
                        }
                        catch (Exception ex)
                        {
                            LoggingService.LogError($"NotifyInventoryChanged UI update error: {ex.Message}\r\n");
                        }
                    }), System.Windows.Threading.DispatcherPriority.Background);
                }
            }
            catch (Exception)
            {
                LoggingService.LogError("Ошибка обновления UI");
            }
        }

        public void NotifyItemsChanged()
        {
            // Lightweight update - only notify collection changes without all the heavy UI updates
            OnPropertyChanged(nameof(Items));
            OnPropertyChanged(nameof(QuickItems));
            OnPropertyChanged(nameof(CraftItems));
            OnPropertyChanged(nameof(CurrentWeight));
            OnPropertyChanged(nameof(TrashItem));
            
            // Don't trigger the full InventoryChanged event or heavy UI updates
            
            try
            {
                if (System.Windows.Application.Current != null)
                {
                    System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        try
                        {
                            if (System.Windows.Application.Current.Resources.Contains("GameData"))
                            {
                                var gameData = System.Windows.Application.Current.Resources["GameData"] as GameData;
                                if (gameData != null)
                                {
                                    gameData.OnPropertyChanged(nameof(gameData.Inventory));
                                    
                                    if (System.Windows.Application.Current.MainWindow?.DataContext is MainViewModel mainVM)
                                    {
                                        if (mainVM.InventoryViewModel != null)
                                        {
                                            // Only refresh affected slots, not everything
                                            mainVM.InventoryViewModel.RefreshAllSlots();
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            LoggingService.LogError($"NotifyItemsChanged UI update error: {ex.Message}");
                        }
                    }), System.Windows.Threading.DispatcherPriority.Normal);
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"NotifyItemsChanged error: {ex.Message}");
            }
        }
    }
} 