using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using SketchBlade.ViewModels;

namespace SketchBlade.Models
{
    /// <summary>
    /// Чистая модель данных инвентаря - только состояние без логики
    /// </summary>
    [Serializable]
    public class InventoryData : INotifyPropertyChanged
    {
        [field: NonSerialized]
        public event PropertyChangedEventHandler? PropertyChanged;

        [field: NonSerialized]  
        public event EventHandler? InventoryChanged;

        // Основное хранилище предметов (15 слотов)
        private ObservableCollection<Item?> _items = new ObservableCollection<Item?>();
        public ObservableCollection<Item?> Items 
        { 
            get => _items ?? new ObservableCollection<Item?>(); 
            set => _items = value ?? new ObservableCollection<Item?>(); 
        }

        // Предметы на панели быстрого доступа (2 слота)
        private ObservableCollection<Item?> _quickItems = new ObservableCollection<Item?>();
        public ObservableCollection<Item?> QuickItems 
        {
            get => _quickItems ?? new ObservableCollection<Item?>(); 
            set => _quickItems = value ?? new ObservableCollection<Item?>(); 
        }
        
        // Предметы для крафта (3x3 сетка = 9 слотов)
        private ObservableCollection<Item?> _craftItems = new ObservableCollection<Item?>();
        public ObservableCollection<Item?> CraftItems
        {
            get => _craftItems ?? new ObservableCollection<Item?>(); 
            set => _craftItems = value ?? new ObservableCollection<Item?>(); 
        }
        
        // Слот для мусора
        private Item? _trashItem;
        public Item? TrashItem
        {
            get => _trashItem;
            set => SetProperty(ref _trashItem, value);
        }
        
        // Емкость инвентаря
        private int _maxCapacity = 15;
        public int MaxCapacity
        {
            get => _maxCapacity;
            set => SetProperty(ref _maxCapacity, Math.Max(1, value));
        }
        
        // Текущий вес (вычисляемое свойство)
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
        
        // Совместимость (устаревшее)
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
            // Уведомляем о изменении всех свойств инвентаря
            OnPropertyChanged(nameof(Items));
            OnPropertyChanged(nameof(QuickItems));
            OnPropertyChanged(nameof(CraftItems));
            OnPropertyChanged(nameof(CurrentWeight));
            OnPropertyChanged(nameof(TrashItem));
            
            // Вызываем событие InventoryChanged
            InventoryChanged?.Invoke(this, EventArgs.Empty);
            
            // Дополнительно обновляем UI через диспетчер
            try
            {
                if (System.Windows.Application.Current != null)
                {
                    System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        try
                        {
                            // Проверяем наличие GameData в ресурсах приложения
                            if (System.Windows.Application.Current.Resources.Contains("GameData"))
                            {
                                var gameData = System.Windows.Application.Current.Resources["GameData"] as GameData;
                                if (gameData != null)
                                {
                                    // Принудительно обновляем инвентарь в GameData
                                    gameData.OnPropertyChanged(nameof(gameData.Inventory));
                                    
                                    // Обновляем ViewModel инвентаря, если доступен
                                    if (System.Windows.Application.Current.MainWindow?.DataContext is MainViewModel mainVM)
                                    {
                                        // Обновляем InventoryViewModel
                                        if (mainVM.InventoryViewModel != null)
                                        {
                                            mainVM.InventoryViewModel.RefreshAllSlots();
                                            mainVM.InventoryViewModel.ForceUIUpdate();
                                        }
                                        
                                        // Используем новую команду для обновления UI
                                        if (mainVM.RefreshUICommand != null)
                                        {
                                            mainVM.RefreshUICommand.Execute(null);
                                        }
                                        
                                        // Обновляем крафт, если на текущем экране инвентарь
                                        if (mainVM.CurrentScreen == "InventoryView")
                                        {
                                            // Пытаемся напрямую обновить SimplifiedCraftingViewModel
                                            if (mainVM.InventoryViewModel?.SimplifiedCraftingViewModel != null)
                                            {
                                                mainVM.InventoryViewModel.SimplifiedCraftingViewModel.RefreshAvailableRecipes();
                                            }
                                        }
                                    }
                                    
                                    // Альтернативный путь через Application.Resources, если MainWindow.DataContext не подходит
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
                            
                            // Принудительное обновление текущего экрана через MainWindow
                            if (System.Windows.Application.Current.MainWindow is MainWindow mainWindow)
                            {
                                mainWindow.RefreshCurrentScreen();
                            }
                        }
                        catch (Exception ex)
                        {
                            // Логируем ошибку, но не позволяем ей сломать приложение
                            if (System.IO.File.Exists("error_log.txt"))
                            {
                                System.IO.File.AppendAllText("error_log.txt", 
                                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [ERROR] NotifyInventoryChanged UI update error: {ex.Message}\r\n");
                            }
                        }
                    }), System.Windows.Threading.DispatcherPriority.Background);
                }
            }
            catch (Exception)
            {
                // Игнорируем ошибки обновления UI - они не должны ломать логику
            }
        }
    }
} 