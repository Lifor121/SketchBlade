using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using SketchBlade.Models;
using SketchBlade.Helpers;
using SketchBlade.Services;
using SketchBlade.Utilities;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Windows.Threading;
using System.Windows;
using System.Windows.Threading;

namespace SketchBlade.ViewModels
{
    public class InventoryViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
        // ��������� ��������� ����� ��� �������� ������
        public void NotifyPropertyChanged(string propertyName)
        {
            OnPropertyChanged(propertyName);
        }
        
        private GameData _gameState;
        public GameData GameData => _gameState;
        
        // Simplified crafting system
        private SimplifiedCraftingViewModel _simplifiedCraftingViewModel;
        public SimplifiedCraftingViewModel SimplifiedCraftingViewModel 
        { 
            get 
            {
                // ������� ������������� - ���� ����� �� ������, �� ��������� ��������, ������� ���
                if (_simplifiedCraftingViewModel == null)
                {
                    InitializeCraftingIfReady();
                }
                
                return _simplifiedCraftingViewModel;
            }
        }
        
        // Navigation command
        public ICommand NavigateCommand { get; }
        
        // Test commands
        public ICommand GenerateTestItemsCommand { get; }
        public ICommand TestDisplayCommand { get; }
        public ICommand ClearInventoryCommand { get; }
        
        // Item movement commands
        public ICommand MoveToInventorySlotCommand { get; }
        public ICommand MoveToQuickSlotCommand { get; }
        public ICommand MoveToCraftSlotCommand { get; }
        public ICommand MoveToTrashCommand { get; }
        public ICommand UseQuickSlotCommand { get; }
        public ICommand SplitStackCommand { get; }
        
        // Inventory slot wrappers
        public ObservableCollection<InventorySlotWrapper> InventorySlots { get; }
        public ObservableCollection<InventorySlotWrapper> QuickSlots { get; }
        
        // Equipment slots
        public InventorySlotWrapper HelmetSlot { get; }
        public InventorySlotWrapper ChestSlot { get; }
        public InventorySlotWrapper LegsSlot { get; }
        public InventorySlotWrapper WeaponSlot { get; }
        public InventorySlotWrapper ShieldSlot { get; }
        public InventorySlotWrapper TrashSlot { get; }
        
        // Player properties
        public string PlayerHealth => $"{_gameState.Player?.CurrentHealth ?? 0}/{_gameState.Player?.MaxHealth ?? 0}";
        public string PlayerDamage => _gameState.Player?.GetTotalAttack().ToString() ?? "0";
        public string PlayerDefense => _gameState.Player?.GetTotalDefense().ToString() ?? "0";
        public BitmapImage? PlayerSprite => ImageHelper.LoadImage(AssetPaths.Characters.PLAYER);
        
        // Recipe book visibility (legacy)
        public bool IsRecipeBookVisible { get; set; } = false;
        
        private bool _isUpdatingInventory = false;
        
        private DispatcherTimer? _updateUITimer;
        private bool _isUIUpdatePending = false;
        
        public InventoryViewModel(GameData GameData)
        {
            _gameState = GameData ?? throw new ArgumentNullException(nameof(GameData));
            
            // �� ������� SimplifiedCraftingViewModel ����� - �� ����� ������ ����� ������������� ����!
            // _simplifiedCraftingViewModel = new SimplifiedCraftingViewModel(_gameState);
            
            // Initialize commands
            NavigateCommand = new RelayCommand<string>(NavigateToScreen);
            GenerateTestItemsCommand = new RelayCommand(GenerateTestItems);
            TestDisplayCommand = new RelayCommand(TestDisplay);
            ClearInventoryCommand = new RelayCommand(ClearInventory);
            MoveToInventorySlotCommand = new RelayCommand<MoveItemData>(MoveToInventorySlot);
            MoveToQuickSlotCommand = new RelayCommand<MoveItemData>(MoveToQuickSlot);
            MoveToCraftSlotCommand = new RelayCommand<MoveItemData>(MoveToCraftSlot);
            MoveToTrashCommand = new RelayCommand<MoveItemData>(MoveToTrash);
            UseQuickSlotCommand = new RelayCommand<int>(UseQuickSlot);
            SplitStackCommand = new RelayCommand<Models.SplitStackEventArgs>(SplitStack);
            
            // Initialize slot collections
            InventorySlots = new ObservableCollection<InventorySlotWrapper>();
            QuickSlots = new ObservableCollection<InventorySlotWrapper>();
            
            // Initialize equipment slots
            HelmetSlot = new InventorySlotWrapper();
            ChestSlot = new InventorySlotWrapper();
            LegsSlot = new InventorySlotWrapper();
            WeaponSlot = new InventorySlotWrapper();
            ShieldSlot = new InventorySlotWrapper();
            TrashSlot = new InventorySlotWrapper();
            
            // Subscribe to game state changes
            _gameState.PropertyChanged += GameState_PropertyChanged;
            
            // Initialize slot data
            InitializeSlots();
            RefreshAllSlots();
            
            // �������������� SimplifiedCraftingViewModel ������ ���� ��������� ��� ��������
            InitializeCraftingIfReady();
        }
        
        /// <summary>
        /// �������������� ������� ������, ���� ��������� �����
        /// </summary>
        public void InitializeCraftingIfReady()
        {
            if (_simplifiedCraftingViewModel == null)
            {
                var nonNullItems = _gameState.Inventory.Items.Count(item => item != null);
                if (nonNullItems > 0)
                {
                    // LoggingService.LogInfo($"�������������� SimplifiedCraftingViewModel - � ��������� ��� {nonNullItems} ���������");
                    _simplifiedCraftingViewModel = new SimplifiedCraftingViewModel(_gameState);
                    OnPropertyChanged(nameof(SimplifiedCraftingViewModel));
                }
                else
                {
                    // LoggingService.LogDebug("SimplifiedCraftingViewModel �� ��������������� - ��������� ����");
                }
            }
        }
        
        /// <summary>
        /// ������������� ����������� ������� ������  (    )
        /// </summary>
        public void ReinitializeCrafting()
        {
            try
            {
                // LoggingService.LogInfo("===     ===");
                
                var nonNullItems = _gameState.Inventory.Items.Count(item => item != null);
                // LoggingService.LogInfo($" : {nonNullItems} ");
                
                if (nonNullItems > 0)
                {
                    foreach (var item in _gameState.Inventory.Items.Where(item => item != null))
                    {
                        // LoggingService.LogInfo($"  - {item.Name} x{item.StackSize}");
                    }
                    
                    // ,     
                    if (_simplifiedCraftingViewModel == null)
                    {
                        // LoggingService.LogInfo("   SimplifiedCraftingViewModel");
                        _simplifiedCraftingViewModel = new SimplifiedCraftingViewModel(_gameState);
                        OnPropertyChanged(nameof(SimplifiedCraftingViewModel));
                    }
                    else
                    {
                        // LoggingService.LogInfo("   SimplifiedCraftingViewModel");
                        _simplifiedCraftingViewModel.RefreshAvailableRecipes();
                    }
                    
                    // LoggingService.LogInfo("SimplifiedCraftingViewModel   ");
                }
                else
                {
                    // LoggingService.LogInfo("  - SimplifiedCraftingViewModel  ");
                }
                
                // LoggingService.LogInfo("===   UI   ===");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"   : {ex.Message}", ex);
            }
        }
        
        private void InitializeSlots()
        {
            // Initialize inventory slots (15 slots)
            InventorySlots.Clear();
                for (int i = 0; i < 15; i++)
                {
                InventorySlots.Add(new InventorySlotWrapper());
            }
            
            // Initialize quick slots (2 slots)
            QuickSlots.Clear();
            for (int i = 0; i < 2; i++)
            {
                QuickSlots.Add(new InventorySlotWrapper());
            }
        }
        
        private void GameState_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(GameData.Inventory) || e.PropertyName == nameof(GameData.Player))
            {
                RefreshAllSlots();
                OnPropertyChanged(nameof(PlayerHealth));
                OnPropertyChanged(nameof(PlayerDamage));
                OnPropertyChanged(nameof(PlayerDefense));
            }
        }
        
        public void RefreshAllSlots()
        {
            try
            {
                // LoggingService.LogInfo($"[REFRESH] RefreshAllSlots:     ");
                
                //     
                // LoggingService.LogInfo($"[REFRESH]  {InventorySlots.Count}  ");
                for (int i = 0; i < InventorySlots.Count; i++)
                {
                    var oldItem = InventorySlots[i].Item;
                    
                    if (i < _gameState.Inventory.Items.Count)
                    {
                        var inventoryItem = _gameState.Inventory.Items[i];
                        
                        //  ,      (   UI)
                        var currentUIItem = InventorySlots[i].Item;
                        if (currentUIItem != inventoryItem)
                        {
                            // LoggingService.LogInfo($"[REFRESH] : [{i}] UI  '{currentUIItem?.Name ?? "null"}',   '{inventoryItem?.Name ?? "null"}' - ");
                        }
                        else
                        {
                            // LoggingService.LogInfo($"[REFRESH] [{i}] : '{inventoryItem?.Name ?? "null"}' -   UI");
                        }
                        
                        //   Item    OnItemChanged
                        InventorySlots[i].Item = inventoryItem;
                        InventorySlots[i].NotifyItemChanged();
                        
                        // LoggingService.LogInfo($"[REFRESH]   [{i}]: '{oldItem?.Name ?? "null"}' -> '{inventoryItem?.Name ?? "null"}'");
                    }
                    else
                    {
                        InventorySlots[i].Item = null;
                        InventorySlots[i].NotifyItemChanged();
                        // LoggingService.LogInfo($"[REFRESH]   [{i}]: '{oldItem?.Name ?? "null"}' -> 'null' ()");
                    }
                }
                
                //   
                // LoggingService.LogInfo($"[REFRESH]  {QuickSlots.Count}  ");
                for (int i = 0; i < QuickSlots.Count; i++)
                {
                    var oldItem = QuickSlots[i].Item;
                    
                    if (i < _gameState.Inventory.QuickItems.Count)
                    {
                        QuickSlots[i].Item = _gameState.Inventory.QuickItems[i];
                        // LoggingService.LogInfo($"[REFRESH]   Quick[{i}]: '{oldItem?.Name ?? "null"}' -> '{_gameState.Inventory.QuickItems[i]?.Name ?? "null"}'");
                    }
                    else
                    {
                        QuickSlots[i].Item = null;
                        // LoggingService.LogInfo($"[REFRESH]   Quick[{i}]: '{oldItem?.Name ?? "null"}' -> 'null'");
                    }
                    QuickSlots[i].NotifyItemChanged();
                }
                
                //     
                var player = _gameState.Player;
                if (player != null)
                {
                    // LoggingService.LogInfo($"[REFRESH]   ");
                    
                    var oldWeapon = WeaponSlot.Item;
                    WeaponSlot.Item = player.EquippedWeapon;
                    WeaponSlot.NotifyItemChanged();
                    // LoggingService.LogInfo($"[REFRESH]   Weapon: '{oldWeapon?.Name ?? "null"}' -> '{player.EquippedWeapon?.Name ?? "null"}'");
                    
                    var oldHelmet = HelmetSlot.Item;
                    HelmetSlot.Item = player.EquippedHelmet;
                    HelmetSlot.NotifyItemChanged();
                    // LoggingService.LogInfo($"[REFRESH]   Helmet: '{oldHelmet?.Name ?? "null"}' -> '{player.EquippedHelmet?.Name ?? "null"}'");
                    
                    var oldChest = ChestSlot.Item;
                    ChestSlot.Item = player.EquippedArmor;
                    ChestSlot.NotifyItemChanged();
                    // LoggingService.LogInfo($"[REFRESH]   Chest: '{oldChest?.Name ?? "null"}' -> '{player.EquippedArmor?.Name ?? "null"}'");
                    
                    var oldLegs = LegsSlot.Item;
                    LegsSlot.Item = player.EquippedLeggings;
                    LegsSlot.NotifyItemChanged();
                    // LoggingService.LogInfo($"[REFRESH]   Legs: '{oldLegs?.Name ?? "null"}' -> '{player.EquippedLeggings?.Name ?? "null"}'");
                    
                    var oldShield = ShieldSlot.Item;
                    ShieldSlot.Item = player.EquippedShield;
                    ShieldSlot.NotifyItemChanged();
                    // LoggingService.LogInfo($"[REFRESH]   Shield: '{oldShield?.Name ?? "null"}' -> '{player.EquippedShield?.Name ?? "null"}'");
                }
                
                // ��������� ���� ������
                TrashSlot.NotifyItemChanged();
                // LoggingService.LogInfo($"[REFRESH] Trash slot ��������");
                
                // LoggingService.LogInfo($"[REFRESH] RefreshAllSlots: �������������� ���������� ���������");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"[REFRESH] RefreshAllSlots: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// �������������� ���������� UI - ����������� ������ ��� ������������ �����������
        /// </summary>
        public void ForceUIUpdate()
        {
            try
            {
                // LoggingService.LogDebug("ForceUIUpdate: �������� ����������� ���������� UI");
                
                // ��������� ��� ����� ��������� � �������������� ������������
                for (int i = 0; i < InventorySlots.Count; i++)
                {
                    var slot = InventorySlots[i];
                    slot.NotifyItemChanged();
                    
                    // ������������� ��������� ����� OnPropertyChanged ��� ���������
                    OnPropertyChanged($"InventorySlots[{i}]");
                }
                
                // ��������� ������� �����
                for (int i = 0; i < QuickSlots.Count; i++)
                {
                    var slot = QuickSlots[i];
                    slot.NotifyItemChanged();
                    OnPropertyChanged($"QuickSlots[{i}]");
                }
                
                // ��������� ����� ����������
                HelmetSlot.NotifyItemChanged();
                ChestSlot.NotifyItemChanged();
                LegsSlot.NotifyItemChanged();
                WeaponSlot.NotifyItemChanged();
                ShieldSlot.NotifyItemChanged();
                TrashSlot.NotifyItemChanged();
                
                // ��������� ��������� �������
                OnPropertyChanged(nameof(InventorySlots));
                OnPropertyChanged(nameof(QuickSlots));
                
                // ��������� ��������� ����� ����������
                OnPropertyChanged(nameof(HelmetSlot));
                OnPropertyChanged(nameof(ChestSlot));
                OnPropertyChanged(nameof(LegsSlot));
                OnPropertyChanged(nameof(WeaponSlot));
                OnPropertyChanged(nameof(ShieldSlot));
                OnPropertyChanged(nameof(TrashSlot));
                
                // ��������� ������� ������ ���� ��� ����������
                if (_simplifiedCraftingViewModel != null)
                {
                    _simplifiedCraftingViewModel.RefreshAvailableRecipes();
                    OnPropertyChanged(nameof(SimplifiedCraftingViewModel));
                }
                
                // ��������� ������� ������
                OnPropertyChanged(nameof(GameData));
                OnPropertyChanged(nameof(PlayerHealth));
                OnPropertyChanged(nameof(PlayerDamage));
                OnPropertyChanged(nameof(PlayerDefense));
                OnPropertyChanged(nameof(PlayerSprite));
                
                // LoggingService.LogDebug("ForceUIUpdate: ����������� ���������� UI ���������");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"[UI] ������ ��� ���������� UI: {ex.Message}", ex);
            }
        }
        
        public Item? GetItemFromSlot(string slotType, int slotIndex)
        {
            try
            {
                switch (slotType)
                {
                    case "Inventory":
                        if (slotIndex >= 0 && slotIndex < _gameState.Inventory.Items.Count)
                            return _gameState.Inventory.Items[slotIndex];
                        break;
                        
                    case "Quick":
                        if (slotIndex >= 0 && slotIndex < _gameState.Inventory.QuickItems.Count)
                            return _gameState.Inventory.QuickItems[slotIndex];
                        break;
                        
                    case "Weapon":
                        return _gameState.Player?.EquippedWeapon;
                        
                    case "Helmet":
                        return _gameState.Player?.EquippedHelmet;
                        
                    case "Chestplate":
                        return _gameState.Player?.EquippedArmor;
                        
                    case "Leggings":
                        return _gameState.Player?.EquippedLeggings;
                        
                    case "Shield":
                        return _gameState.Player?.EquippedShield;
                        
                    case "Trash":
                        return TrashSlot.Item;
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"GetItemFromSlot: {ex.Message}", ex);
            }
            
            return null;
        }

        /// <summary>
        /// ������� ��� �������� �������� ���������� ���� � ���������� ������
        /// </summary>
        private static IEnumerable<T> FindVisualChildren<T>(System.Windows.DependencyObject depObj) where T : System.Windows.DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    var child = System.Windows.Media.VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }

        /// <summary>
        /// ������������� ��������� ��� UI �������� CoreInventorySlot (��� � drag-and-drop)
        /// </summary>
        private void ForceUpdateUIControls()
        {
            try
            {
                // LoggingService.LogInfo("[CRAFT] ForceUpdateUIControls: �������� �������������� ���������� UI ���������");
                
                // ���������� Dispatcher ��� ���������� �� UI ������
                if (System.Windows.Application.Current?.Dispatcher != null)
                {
                    System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        try
                        {
                            // ������� ������� ����
                            var mainWindow = System.Windows.Application.Current.MainWindow;
                            if (mainWindow != null)
                            {
                                // ������� ��� CoreInventorySlot ��������
                                var inventorySlots = FindVisualChildren<Views.Controls.CoreInventorySlot>(mainWindow);
                                
                                // LoggingService.LogInfo($"[CRAFT] ForceUpdateUIControls: ������� {inventorySlots.Count()} ������ ��� ����������");
                                
                                foreach (var slot in inventorySlots)
                                {
                                    try
                                    {
                                        // ��������� CraftResult ����� - ��� ����������� �������� ������
                                        if (slot.SlotType == "CraftResult")
                                        {
                                            // LoggingService.LogInfo($"[CRAFT] ForceUpdateUIControls: ���������� CraftResult[{slot.SlotIndex}] - ����������� �������� ������");
                                            continue;
                                        }
                                        
                                        // �������� ���������� ������ �� ViewModel
                                        var actualItem = GetItemFromSlot(slot.SlotType, slot.SlotIndex);
                                        
                                        // ИСПРАВЛЕНИЕ: Всегда принудительно обновляем UI через прямой вызов
                                        // Сначала обновляем данные в слоте
                                        if (slot.Item != actualItem)
                                        {
                                            // LoggingService.LogInfo($"[CRAFT] ForceUpdateUIControls: Обновляем {slot.SlotType}[{slot.SlotIndex}]: {slot.Item?.Name ?? "null"} -> {actualItem?.Name ?? "null"}");
                                            slot.Item = actualItem;
                                        }
                                        
                                        // ВСЕГДА принудительно обновляем визуалы через новый метод
                                        slot.ForceUpdateSlotVisuals();
                                    }
                                    catch (Exception slotEx)
                                    {
                                        LoggingService.LogError($"[CRAFT] ForceUpdateUIControls:    {slot.SlotType}[{slot.SlotIndex}]: {slotEx.Message}");
                                    }
                                }
                            }
                            else
                            {
                                LoggingService.LogWarning("[CRAFT] ForceUpdateUIControls: MainWindow  ");
                            }
                        }
                        catch (Exception ex)
                        {
                            LoggingService.LogError($"[CRAFT] ForceUpdateUIControls:   UI : {ex.Message}");
                        }
                    }), System.Windows.Threading.DispatcherPriority.Normal);
                }
                
                // LoggingService.LogInfo("[CRAFT] ForceUpdateUIControls: ");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"[CRAFT] ForceUpdateUIControls: {ex.Message}", ex);
            }
        }
        
        public void MoveToInventorySlot(MoveItemData? moveData)
        {
            if (moveData == null) return;
            
            try
            {
                // Atomic operation: capture both source and target items at the same time
                var sourceItem = GetItemFromSlot(moveData.SourceType, moveData.SourceIndex);
                if (sourceItem == null) 
                {
                    LoggingService.LogWarning($"[DragDrop] MoveToInventorySlot: �������� ������� �� ������ � {moveData.SourceType}[{moveData.SourceIndex}]");
                    return;
                }
                
                // ��������� ���������� �������� �����
                if (moveData.TargetIndex < 0 || moveData.TargetIndex >= _gameState.Inventory.Items.Count)
                {
                    LoggingService.LogError($"[DragDrop] MoveToInventorySlot: �������� ������ �������� ����� {moveData.TargetIndex}");
                    return;
                }

                // �������� ������� � ������� ����� (����� ���� null)
                var targetItem = _gameState.Inventory.Items[moveData.TargetIndex];
                
                // LoggingService.LogInfo($"[DragDrop] �����������: {sourceItem.Name} �� {moveData.SourceType}[{moveData.SourceIndex}] � Inventory[{moveData.TargetIndex}]");
                
                // ��������� ��������� ����������� � ��������� ���������
                bool moveSuccessful = PerformAtomicMove(moveData.SourceType, moveData.SourceIndex, 
                                                       "Inventory", moveData.TargetIndex, 
                                                       sourceItem, targetItem);
                
                if (moveSuccessful)
                {
                    // ���������� �� ��������� ��������� ������ ���� ���
                    _gameState.Inventory.OnInventoryChanged();
                    
                    // ���������������� ���������� ������ ���������� ������
                    UpdateSpecificSlots(moveData.SourceType, moveData.SourceIndex, "Inventory", moveData.TargetIndex);
                    
                    // LoggingService.LogInfo("[DragDrop] ����������� ��������� �������");
                }
                else
                {
                    LoggingService.LogWarning("[DragDrop] ����������� �� ������� - ��������� ���������� �� ����� ��������");
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"MoveToInventorySlot: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// ��������� ��������� ����������� ��������� ����� ������� � ��������� ���������
        /// </summary>
        public bool PerformAtomicMove(string sourceType, int sourceIndex, string targetType, int targetIndex, 
                                     Item sourceItem, Item? targetItem)
        {
            try
            {
                // �������� ���������, ��� �������� ������� ��� ��� �� �����
                var currentSourceItem = GetItemFromSlot(sourceType, sourceIndex);
                if (currentSourceItem != sourceItem)
                {
                    LoggingService.LogWarning($"[DragDrop] ��������� ����������� ��������: �������� ������� ���������");
                    return false;
                }
                
                // ��������� ����������� � ���������� �������
                if (targetType == "Inventory")
                {
                    _gameState.Inventory.Items[targetIndex] = sourceItem;
                }
                else
                {
                    SetItemInSlot(targetType, targetIndex, sourceItem);
                }
                
                // ������� �������� ����
                SetItemInSlot(sourceType, sourceIndex, targetItem);
                // LoggingService.LogDebug($"[PerformAtomicMove] After SetItemInSlot for source ({sourceType}[{sourceIndex}]): _gameState.Inventory.Items[{sourceIndex}] is now { (sourceType == "Inventory" && sourceIndex >= 0 && sourceIndex < _gameState.Inventory.Items.Count ? _gameState.Inventory.Items[sourceIndex]?.Name : "N/A_OR_NON_INVENTORY_SOURCE") ?? "null"}");

                // Always force UI update immediately after a successful move
                ForceImmediateUIUpdate(sourceType, sourceIndex, targetType, targetIndex);
                
                return true;
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"PerformAtomicMove: {ex.Message}", ex);
                return false;
            }
        }
        
        public InventorySlotWrapper? GetSlotWrapper(string slotType, int slotIndex)
        {
            return slotType switch
            {
                "Inventory" => (slotIndex >= 0 && slotIndex < InventorySlots.Count) ? InventorySlots[slotIndex] : null,
                "Quick" => (slotIndex >= 0 && slotIndex < QuickSlots.Count) ? QuickSlots[slotIndex] : null,
                "Helmet" => HelmetSlot,
                "Chestplate" => ChestSlot,
                "Leggings" => LegsSlot,
                "Weapon" => WeaponSlot,
                "Shield" => ShieldSlot,
                "Trash" => TrashSlot,
                // "Craft" slots are not directly here as individual wrappers, handle if needed
                _ => null
            };
        }
        
        /// <summary>
        /// ���������������� ���������� ������ ���������� ������
        /// </summary>
        private void UpdateSpecificSlots(string sourceType, int sourceIndex, string targetType, int targetIndex)
        {
            try
            {
                // Prevent rapid multiple updates
                if (_isUpdatingInventory)
                {
                    return;
                }
                
                _isUpdatingInventory = true;
                
                try
                {
                    // Use our more aggressive UI update method for immediate visual feedback
                    ForceImmediateUIUpdate(sourceType, sourceIndex, targetType, targetIndex);
                    
                    // Set up delayed crafting system update if not already created
                    if (_updateUITimer == null)
                    {
                        _updateUITimer = new DispatcherTimer
                        {
                            Interval = TimeSpan.FromMilliseconds(250)
                        };
                        
                        _updateUITimer.Tick += (s, e) =>
                        {
                            _updateUITimer?.Stop();
                            if (_isUIUpdatePending)
                            {
                                _isUIUpdatePending = false;
                                
                                // Update crafting only when timer has fired
                                if (_simplifiedCraftingViewModel != null)
                                {
                                    _simplifiedCraftingViewModel.RefreshAvailableRecipes();
                                }
                            }
                        };
                    }
                    
                    // Mark pending update and start timer if not already running
                    _isUIUpdatePending = true;
                    
                    if (!_updateUITimer.IsEnabled)
                    {
                        _updateUITimer.Start();
                    }
                }
                finally
                {
                    _isUpdatingInventory = false;
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"UpdateSpecificSlots: {ex.Message}", ex);
                _isUpdatingInventory = false;
            }
        }
        
        /// <summary>
        /// ��������� ���� ���������� ����
        /// </summary>
        private void UpdateSingleSlot(string slotType, int slotIndex)
        {
            try
            {
                var actualItem = GetItemFromSlot(slotType, slotIndex);
                
                switch (slotType)
                {
                    case "Inventory":
                        if (slotIndex >= 0 && slotIndex < InventorySlots.Count)
                        {
                            var slot = InventorySlots[slotIndex];
                            if (slot.Item != actualItem)
                            {
                                slot.Item = actualItem;
                                slot.NotifyItemChanged();
                            }
                        }
                        break;
                    case "Quick":
                        if (slotIndex >= 0 && slotIndex < QuickSlots.Count)
                        {
                            var slot = QuickSlots[slotIndex];
                            if (slot.Item != actualItem)
                            {
                                slot.Item = actualItem;
                                slot.NotifyItemChanged();
                            }
                        }
                        break;
                    case "Helmet":
                        if (HelmetSlot.Item != actualItem)
                        {
                            HelmetSlot.Item = actualItem;
                            HelmetSlot.NotifyItemChanged();
                        }
                        break;
                    case "Chestplate":
                        if (ChestSlot.Item != actualItem)
                        {
                            ChestSlot.Item = actualItem;
                            ChestSlot.NotifyItemChanged();
                        }
                        break;
                    case "Leggings":
                        if (LegsSlot.Item != actualItem)
                        {
                            LegsSlot.Item = actualItem;
                            LegsSlot.NotifyItemChanged();
                        }
                        break;
                    case "Weapon":
                        if (WeaponSlot.Item != actualItem)
                        {
                            WeaponSlot.Item = actualItem;
                            WeaponSlot.NotifyItemChanged();
                        }
                        break;
                    case "Shield":
                        if (ShieldSlot.Item != actualItem)
                        {
                            ShieldSlot.Item = actualItem;
                            ShieldSlot.NotifyItemChanged();
                        }
                        break;
                    case "Trash":
                        if (TrashSlot.Item != actualItem)
                        {
                            TrashSlot.Item = actualItem;
                            TrashSlot.NotifyItemChanged();
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"UpdateSingleSlot {slotType}[{slotIndex}]: {ex.Message}", ex);
            }
        }
        
        public void MoveItemBetweenSlots(string sourceType, int sourceIndex, string targetType, int targetIndex)
        {
            var moveData = new MoveItemData
            {
                SourceType = sourceType,
                SourceIndex = sourceIndex,
                TargetType = targetType,
                TargetIndex = targetIndex
            };
            
            switch (targetType)
            {
                case "Inventory":
                    MoveToInventorySlot(moveData);
                    break;
                case "Quick":
                    MoveToQuickSlot(moveData);
                    break;
                case "Craft":
                    MoveToCraftSlot(moveData);
                    break;
                case "Trash":
                    MoveToTrash(moveData);
                    break;
                case "Helmet":
                case "Chestplate": 
                case "Leggings":
                case "Weapon":
                case "Shield":
                    // �"�я экипировки используем специальный метод
                    var equipData = new EquipItemData
                    {
                        InventoryIndex = sourceIndex,
                        EquipmentSlot = targetType
                    };
                    EquipItem(equipData);
                    break;
                default:
                    // По умолчанию пыткаемся переместить в инвентарь
                    LoggingService.LogWarning($"MoveItemBetweenSlots: Unknown target type {targetType}, using default behavior");
                    MoveToInventorySlot(moveData);
                    break;
            }
            
            // ������� ���������� UI ���������� - ��� ��� ����������� � ��������������� �������
            // LoggingService.LogInfo($"[DragDrop] MoveItemBetweenSlots ���������");
        }
        
        private void SyncSlotWithData(string slotType, int slotIndex)
        {
            try
            {
                Item? actualItem = GetItemFromSlot(slotType, slotIndex);
                
                switch (slotType)
                {
                    case "Inventory":
                        if (slotIndex >= 0 && slotIndex < InventorySlots.Count)
                        {
                            var slot = InventorySlots[slotIndex];
                            if (slot.Item != actualItem)
                            {
                                // LoggingService.LogInfo($"[UI] ������������� {slotType}[{slotIndex}]: {slot.Item?.Name ?? "null"} -> {actualItem?.Name ?? "null"}");
                                slot.Item = actualItem;
                                slot.NotifyItemChanged();
                            }
                        }
                        break;
                    case "Quick":
                        if (slotIndex >= 0 && slotIndex < QuickSlots.Count)
                        {
                            var slot = QuickSlots[slotIndex];
                            if (slot.Item != actualItem)
                            {
                                // LoggingService.LogInfo($"[UI] ������������� {slotType}[{slotIndex}]: {slot.Item?.Name ?? "null"} -> {actualItem?.Name ?? "null"}");
                                slot.Item = actualItem;
                                slot.NotifyItemChanged();
                            }
                        }
                        break;
                    case "Helmet":
                        if (HelmetSlot.Item != actualItem)
                        {
                            // LoggingService.LogInfo($"[UI] ������������� {slotType}: {HelmetSlot.Item?.Name ?? "null"} -> {actualItem?.Name ?? "null"}");
                            HelmetSlot.Item = actualItem;
                            HelmetSlot.NotifyItemChanged();
                        }
                        break;
                    case "Chestplate":
                        if (ChestSlot.Item != actualItem)
                        {
                            // LoggingService.LogInfo($"[UI] ������������� {slotType}: {ChestSlot.Item?.Name ?? "null"} -> {actualItem?.Name ?? "null"}");
                            ChestSlot.Item = actualItem;
                            ChestSlot.NotifyItemChanged();
                        }
                        break;
                    case "Leggings":
                        if (LegsSlot.Item != actualItem)
                        {
                            // LoggingService.LogInfo($"[UI] ������������� {slotType}: {LegsSlot.Item?.Name ?? "null"} -> {actualItem?.Name ?? "null"}");
                            LegsSlot.Item = actualItem;
                            LegsSlot.NotifyItemChanged();
                        }
                        break;
                    case "Weapon":
                        if (WeaponSlot.Item != actualItem)
                        {
                            // LoggingService.LogInfo($"[UI] ������������� {slotType}: {WeaponSlot.Item?.Name ?? "null"} -> {actualItem?.Name ?? "null"}");
                            WeaponSlot.Item = actualItem;
                            WeaponSlot.NotifyItemChanged();
                        }
                        break;
                    case "Shield":
                        if (ShieldSlot.Item != actualItem)
                        {
                            // LoggingService.LogInfo($"[UI] ������������� {slotType}: {ShieldSlot.Item?.Name ?? "null"} -> {actualItem?.Name ?? "null"}");
                            ShieldSlot.Item = actualItem;
                            ShieldSlot.NotifyItemChanged();
                        }
                        break;
                    case "Trash":
                        if (TrashSlot.Item != actualItem)
                        {
                            // LoggingService.LogInfo($"[UI] ������������� {slotType}: {TrashSlot.Item?.Name ?? "null"} -> {actualItem?.Name ?? "null"}");
                            TrashSlot.Item = actualItem;
                            TrashSlot.NotifyItemChanged();
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error syncing slot {slotType}[{slotIndex}]: {ex.Message}", ex);
            }
        }
        
        public void EquipItem(EquipItemData equipData)
        {
            try
            {
                if (equipData.InventoryIndex < 0 || equipData.InventoryIndex >= _gameState.Inventory.Items.Count)
                return;
                
                var item = _gameState.Inventory.Items[equipData.InventoryIndex];
                if (item == null || _gameState.Player == null) return;
                
                // Get the equipment slot for this item type
                EquipmentSlot slot = equipData.EquipmentSlot switch
                {
                    "Weapon" => EquipmentSlot.MainHand,
                    "Helmet" => EquipmentSlot.Helmet,
                    "Armor" or "Chestplate" => EquipmentSlot.Chestplate,
                    "Leggings" => EquipmentSlot.Leggings,
                    "Shield" => EquipmentSlot.Shield,
                    _ => EquipmentSlot.MainHand
                };
                
                // Get currently equipped item in this slot
                var currentEquipped = _gameState.Player.EquippedItems.ContainsKey(slot) 
                    ? _gameState.Player.EquippedItems[slot] 
                    : null;
                
                // First unequip the current item if any
                if (currentEquipped != null)
                {
                    _gameState.Player.UnequipItem(slot);
                }
                
                // Now equip the new item
                bool equipped = _gameState.Player.EquipItem(item);
                
                if (equipped)
                {
                    // Remove item from inventory
                    _gameState.Inventory.Items[equipData.InventoryIndex] = null;
                    
                    // Put previously equipped item back in inventory if there was one
                    if (currentEquipped != null)
                    {
                        _gameState.Inventory.Items[equipData.InventoryIndex] = currentEquipped;
                    }
                }
                
                // ���������������� ���������� ������ ���������� ������
                UpdateSingleSlot("Inventory", equipData.InventoryIndex);
                UpdateSingleSlot(equipData.EquipmentSlot, 0);
                
                // ��������� �������� ���� �����
                _simplifiedCraftingViewModel?.RefreshAvailableRecipes();
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"EquipItem: {ex.Message}", ex);
            }
        }
        
        // Command implementations
        private void NavigateToScreen(string? screenName)
        {
            if (!string.IsNullOrEmpty(screenName))
            {
                // �"��������� ���������� ��������� ����� GameData.CurrentScreenViewModel
                if (_gameState.CurrentScreenViewModel is MainViewModel mainViewModel)
                {
                    // ���������� Navigate ����� �� MainViewModel ��� ���������� ���������
                    var navigateMethod = mainViewModel.GetType().GetMethod("Navigate", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (navigateMethod != null)
                    {
                        navigateMethod.Invoke(mainViewModel, new object[] { screenName });
                    }
                }
                else
                {
                    // Fallback: ������ ��������� ������ � ������� ��������� ����� MainWindow
                    _gameState.CurrentScreen = screenName;
                    
                    // ������� ��������� ����� MainWindow ��������
                    if (System.Windows.Application.Current.MainWindow is MainWindow mainWindow)
                    {
                        mainWindow.NavigateToScreen(screenName);
                    }
                }
            }
        }
        
        private void GenerateTestItems()
        {
            try
            {
                // Add some test items
                var testItems = new List<Item>
                {
                    ItemFactory.CreateWood(5),
                    ItemFactory.CreateHerb(3),
                    ItemFactory.CreateHealingPotion(2)
                };
                
                foreach (var item in testItems)
                {
                    _gameState.Inventory.AddItem(item);
                }
                
                RefreshAllSlots();
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"GenerateTestItems: {ex.Message}", ex);
            }
        }
        
        private void TestDisplay()
        {
            ForceUIUpdate();
        }
        
        private void ClearInventory()
        {
            try
            {
                _gameState.Inventory.Clear();
                    RefreshAllSlots();
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"ClearInventory: {ex.Message}", ex);
            }
        }
        
        private void MoveToQuickSlot(MoveItemData? moveData)
        {
            if (moveData == null) return;
            
            try
            {
                // LoggingService.LogInfo($"[DragDrop] ����������� � ������� ����: {moveData.SourceType}[{moveData.SourceIndex}] -> {moveData.TargetType}[{moveData.TargetIndex}]");

                // ���������, ��� ������� ���� ��������� ��� Quick
                if (moveData.TargetType != "Quick")
                {
                    LoggingService.LogError($"MoveToQuickSlot: ������� ���� �� �������� Quick ������");
                    return;
                }

                // �������� �������� �������
                var sourceItem = GetItemFromSlot(moveData.SourceType, moveData.SourceIndex);
                if (sourceItem == null)
                {
                    LoggingService.LogWarning($"MoveToQuickSlot: �������� ������� �� ������");
                    return;
                }

                // �������� ������� �������
                var targetItem = GetItemFromSlot(moveData.TargetType, moveData.TargetIndex);
                
                // ��������� ��������� ����������� � ��������� ���������
                bool moveSuccessful = PerformAtomicMove(moveData.SourceType, moveData.SourceIndex, 
                                                       moveData.TargetType, moveData.TargetIndex, 
                                                       sourceItem, targetItem);
                
                if (moveSuccessful)
                {
                    // ИСПРАВЛЕНИЕ: Уведомляем об изменении инвентаря
                    _gameState.Inventory.OnInventoryChanged();
                    
                    //     
                    UpdateSpecificSlots(moveData.SourceType, moveData.SourceIndex, moveData.TargetType, moveData.TargetIndex);
                    
                    // ИСПРАВЛЕНИЕ: Принудительно обновляем UI как в крафте
                    ForceUpdateUIControls();
                    
                    // LoggingService.LogInfo("[DragDrop]    ");
                }
                else
                {
                    LoggingService.LogWarning("[DragDrop]     ");
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"MoveToQuickSlot: {ex.Message}", ex);
            }
        }
        
        private void MoveToCraftSlot(MoveItemData? moveData)
        {
            if (moveData == null) return;
            
            try
            {
                // LoggingService.LogInfo($"[DragDrop] ����������� � ���� ������: {moveData.SourceType}[{moveData.SourceIndex}] -> {moveData.TargetType}[{moveData.TargetIndex}]");

                // �������� �������� �������
                var sourceItem = GetItemFromSlot(moveData.SourceType, moveData.SourceIndex);
                if (sourceItem == null)
                {
                    LoggingService.LogWarning($"MoveToCraftSlot: �������� ������� �� ������");
                    return;
                }

                // �������� ������� �������
                var targetItem = GetItemFromSlot(moveData.TargetType, moveData.TargetIndex);
                
                // ��������� ��������� ����������� � ��������� ���������
                bool moveSuccessful = PerformAtomicMove(moveData.SourceType, moveData.SourceIndex, 
                                                       moveData.TargetType, moveData.TargetIndex, 
                                                       sourceItem, targetItem);
                
                if (moveSuccessful)
                {
                    // ���������������� ���������� ������ ���������� ������
                    UpdateSpecificSlots(moveData.SourceType, moveData.SourceIndex, moveData.TargetType, moveData.TargetIndex);
                    // LoggingService.LogInfo("[DragDrop] ����������� � ���� ������ ���������");
                }
                else
                {
                    LoggingService.LogWarning("[DragDrop] ����������� � ���� ������ �� �������");
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"MoveToCraftSlot: {ex.Message}", ex);
            }
        }
        
        private void MoveToTrash(MoveItemData? moveData)
        {
            if (moveData == null) return;
            
            try
            {
                // LoggingService.LogInfo($"[DragDrop] ����������� � �������: {moveData.SourceType}[{moveData.SourceIndex}]");
                
                // �������� �������� ������� ��� �����������
                var sourceItem = GetItemFromSlot(moveData.SourceType, moveData.SourceIndex);
                if (sourceItem != null)
                {
                    // LoggingService.LogInfo($"[DragDrop] ������� �������: {sourceItem.Name}");
                }
                
                // ������� ������� �� ��������� �����
                SetItemInSlot(moveData.SourceType, moveData.SourceIndex, null);
                
                // ���������������� ���������� ������ ��������� �����
                UpdateSingleSlot(moveData.SourceType, moveData.SourceIndex);
                
                // LoggingService.LogInfo("[DragDrop] ����������� � ������� ���������");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"MoveToTrash: {ex.Message}", ex);
            }
        }

        private void UseQuickSlot(int slotIndex)
        {
            try
            {
                if (slotIndex >= 0 && slotIndex < QuickSlots.Count)
                {
                    var item = QuickSlots[slotIndex].Item;
                    if (item != null && item.Type == ItemType.Consumable)
                    {
                        // Use the consumable item - simple healing logic for now
                        if (_gameState.Player != null)
                        {
                            // Simple consumable logic based on item name or effect power
                            if (item.Name.ToLower().Contains("healing") || item.Name.ToLower().Contains("зелье"))
                            {
                                int healAmount = Math.Max(10, item.EffectPower);
                                _gameState.Player.Heal(healAmount);
                            }
                        }
                        
                        // Remove one from stack or remove item entirely
                        if (item.StackSize > 1)
                        {
                            item.StackSize--;
                    }
                    else
                    {
                            QuickSlots[slotIndex].Item = null;
                        }
                        
                        // ���������������� ���������� ������ ����������� �����
                        UpdateSingleSlot("Quick", slotIndex);
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"UseQuickSlot: {ex.Message}", ex);
            }
        }
        
        private void SplitStack(Models.SplitStackEventArgs? args)
        {
            // Simple implementation - just log for now
            // LoggingService.LogDebug("SplitStack called");
        }
        
        // Additional methods needed by the UI
        public void TakeCraftResult()
        {
            try
            {
                // LoggingService.LogDebug("TakeCraftResult: �������� ������ ���������� ������");

                // ���������, ���� �� ��������� ������ ��� ������
                if (_simplifiedCraftingViewModel?.SelectedRecipe == null)
                {
                    LoggingService.LogWarning("TakeCraftResult: ��� ���������� ������� ��� ������");
                    // LoggingService.LogInfo("TakeCraftResult: ��� ������ ����� ������� ������� ������");
                    return;
                }

                // ��������� ����� ���������� ��������
                // LoggingService.LogInfo($"TakeCraftResult: ��������� ����� {_simplifiedCraftingViewModel.SelectedRecipe.Name}");
                _simplifiedCraftingViewModel.CraftSelectedItem();
                
                // LoggingService.LogDebug("TakeCraftResult: ����� ��������, ��������� UI");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"TakeCraftResult: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// ��������� ����� ����������� �������
        /// </summary>
        public void CraftRecipe(SimplifiedCraftingRecipe recipe)
        {
            try
            {
                // LoggingService.LogInfo($"[CRAFT] ========== ������ ������ ==========");
                // LoggingService.LogInfo($"[CRAFT] CraftRecipe: �������� ����� ������� {recipe.Name}");

                if (_simplifiedCraftingViewModel == null)
                {
                    LoggingService.LogError("[CRAFT] SimplifiedCraftingViewModel �� ���������������");
                    return;
                }

                // ������������� ��������� ������
                _simplifiedCraftingViewModel.SelectedRecipe = recipe;
                // LoggingService.LogInfo($"[CRAFT] ���������� ��������� ������: {recipe.Name}");

                // ���������, ����� �� ������� �������
                if (!_simplifiedCraftingViewModel.CanCraft)
                {
                    LoggingService.LogWarning($"[CRAFT] ������������ ���������� ��� ������ {recipe.Name}");
                    return;
                }

                // ��������� �����
                // LoggingService.LogInfo($"[CRAFT] �������� CraftSelectedItem()...");
                _simplifiedCraftingViewModel.CraftSelectedItem();
                // LoggingService.LogInfo($"[CRAFT] CraftSelectedItem() ��������");
                
                                // �������: ���������� ��� �� �����, ��� � drag-and-drop
                // LoggingService.LogInfo($"[CRAFT] �������: ���������� ����� drag-and-drop ��� ���������� UI");
                
                System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        // LoggingService.LogInfo($"[CRAFT] �������� ���������� UI ��� � drag-and-drop...");
                        
                        // 1. ��������� �������� ViewModel ��������� (��� � drag-and-drop)
                        RefreshAllSlots();
                        
                        // 2. ��������: ������������� ��������� UI �������� (��� � drag-and-drop)
                        ForceUpdateUIControls();
                        
                        // 3. ��������� �������
                        _simplifiedCraftingViewModel?.RefreshAvailableRecipes();
                        // LoggingService.LogInfo($"[CRAFT] ������� ���������");
                        
                        // LoggingService.LogInfo($"[CRAFT] ���������� UI ���������!");
                    }
                    catch (Exception uiEx)
                    {
                        LoggingService.LogError($"[CRAFT] ������ ��� ���������� UI: {uiEx.Message}");
                        LoggingService.LogError($"[CRAFT] UI StackTrace: {uiEx.StackTrace}");
                        
                        // Fallback: ����������� ����������
                        // LoggingService.LogInfo($"[CRAFT] Fallback: ���������� ����������� ����������");
                        RefreshAllSlots();
                        ForceUIUpdate();
                    }
                }, System.Windows.Threading.DispatcherPriority.Render);
                
                // LoggingService.LogInfo($"[CRAFT] ========== ����� ������ ==========");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"[CRAFT] ������ ��� ������ {recipe?.Name}: {ex.Message}", ex);
                LoggingService.LogError($"[CRAFT] StackTrace: {ex.StackTrace}");
            }
        }
        
        public void SetItemToSlot(string slotType, int slotIndex, Item? item)
        {
            SetItemInSlot(slotType, slotIndex, item);
            // ���������������� ���������� ������ ����������� �����
            UpdateSingleSlot(slotType, slotIndex);
        }
        
        public void MoveToCraft(MoveItemData? moveData)
        {
            MoveToCraftSlot(moveData);
        }
        
        public Inventory PlayerInventory => _gameState.Inventory;
        
        /// <summary>
        /// ����������� ��������� ������ ��� ������� ������� � UI
        /// </summary>
        public void DiagnoseSlotState()
        {
            try
            {
                // LoggingService.LogInfo("[�����������] �������� ��������� ������");
                
                // ��������� ������������� ���������
                for (int i = 0; i < Math.Min(InventorySlots.Count, _gameState.Inventory.Items.Count); i++)
                {
                    var slotItem = InventorySlots[i].Item;
                    var inventoryItem = _gameState.Inventory.Items[i];
                    
                    if (slotItem != inventoryItem)
                    {
                        LoggingService.LogError($"[�����������] ����������������: ����[{i}] = {slotItem?.Name ?? "�����"}, ���������[{i}] = {inventoryItem?.Name ?? "�����"}");
                    }
                    else if (slotItem != null)
                    {
                        // LoggingService.LogDebug($"[�����������] ����[{i}] ���������������: {slotItem.Name}");
                    }
                }
                
                // ��������� ����������
                var player = _gameState.Player;
                if (player != null)
                {
                    if (WeaponSlot.Item != player.EquippedWeapon)
                        LoggingService.LogError($"[�����������] ����������������: WeaponSlot = {WeaponSlot.Item?.Name ?? "�����"}, Player.EquippedWeapon = {player.EquippedWeapon?.Name ?? "�����"}");
                    
                    if (HelmetSlot.Item != player.EquippedHelmet)
                        LoggingService.LogError($"[�����������] ����������������: HelmetSlot = {HelmetSlot.Item?.Name ?? "�����"}, Player.EquippedHelmet = {player.EquippedHelmet?.Name ?? "�����"}");
                }
                
                // LoggingService.LogInfo("[�����������] �������� ��������� ������ ���������");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"[�����������] ������ ��� �������� ��������� ������: {ex.Message}", ex);
            }
        }
        
        private void SetItemInSlot(string slotType, int slotIndex, Item? item)
        {
            try
            {
                // LoggingService.LogDebug($"[SetItemInSlot] Attempting to set {slotType}[{slotIndex}] to item: {(item?.Name) ?? "null"} (Original Hash: {item?.GetHashCode()})");
                Item? itemBefore = null;

                switch (slotType)
                {
                    case "Inventory":
                        if (slotIndex >= 0 && slotIndex < _gameState.Inventory.Items.Count)
                        {
                            itemBefore = _gameState.Inventory.Items[slotIndex];
                            _gameState.Inventory.Items[slotIndex] = item;
                            // LoggingService.LogDebug($"[SetItemInSlot] AFTER set for {slotType}[{slotIndex}]: _gameState.Inventory.Items[{slotIndex}] is now {(_gameState.Inventory.Items[slotIndex]?.Name) ?? "null"} (Hash: {_gameState.Inventory.Items[slotIndex]?.GetHashCode()}). Item param was {(item?.Name) ?? "null"} (Hash: {item?.GetHashCode()}). Item before was {(itemBefore?.Name) ?? "null"} (Hash: {itemBefore?.GetHashCode()})");
                        }
                        break;
                        
                    case "Quick":
                        // ИСПРАВЛЕНИЕ: Сохраняем данные в основном источнике данных И в UI wrapper
                        if (slotIndex >= 0 && slotIndex < _gameState.Inventory.QuickItems.Count)
                        {
                            // Проверяем, что предмет можно поместить в быстрый слот
                            if (item != null && item.Type != ItemType.Consumable)
                            {
                                LoggingService.LogWarning($"SetItemInSlot: Попытка поместить не-расходуемый предмет {item.Name} в быстрый слот");
                                return; // Не разрешаем помещать не-расходуемые предметы
                            }
                            
                            // Сохраняем в основном источнике данных
                            _gameState.Inventory.QuickItems[slotIndex] = item;
                            
                            // Синхронизируем с UI wrapper
                            if (slotIndex < QuickSlots.Count)
                            {
                                QuickSlots[slotIndex].Item = item;
                            }
                        }
                        break;
                        
                    case "Weapon":
                        if (_gameState.Player != null)
                        {
                            if (item != null)
                            {
                                _gameState.Player.UnequipItem(EquipmentSlot.MainHand);
                                _gameState.Player.EquipItem(item);
                            }
                            else
                            {
                                _gameState.Player.UnequipItem(EquipmentSlot.MainHand);
                            }
                        }
                        break;
                        
                    case "Helmet":
                        if (_gameState.Player != null)
                        {
                            if (item != null)
                            {
                                _gameState.Player.UnequipItem(EquipmentSlot.Helmet);
                                _gameState.Player.EquipItem(item);
                            }
                            else
                            {
                                _gameState.Player.UnequipItem(EquipmentSlot.Helmet);
                            }
                        }
                        break;
                        
                    case "Chestplate":
                        if (_gameState.Player != null)
                        {
                            if (item != null)
                            {
                                _gameState.Player.UnequipItem(EquipmentSlot.Chestplate);
                                _gameState.Player.EquipItem(item);
                            }
                            else
                            {
                                _gameState.Player.UnequipItem(EquipmentSlot.Chestplate);
                            }
                        }
                        break;
                        
                    case "Leggings":
                        if (_gameState.Player != null)
                        {
                            if (item != null)
                            {
                                _gameState.Player.UnequipItem(EquipmentSlot.Leggings);
                                _gameState.Player.EquipItem(item);
                            }
                            else
                            {
                                _gameState.Player.UnequipItem(EquipmentSlot.Leggings);
                            }
                        }
                        break;
                        
                    case "Shield":
                        if (_gameState.Player != null)
                        {
                            if (item != null)
                            {
                                _gameState.Player.UnequipItem(EquipmentSlot.Shield);
                                _gameState.Player.EquipItem(item);
                            }
                            else
                            {
                                _gameState.Player.UnequipItem(EquipmentSlot.Shield);
                            }
                        }
                        break;
                        
                    case "Trash":
                        TrashSlot.Item = item;
                        break;
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"SetItemInSlot: {ex.Message}", ex);
            }
        }

        public void ForceImmediateUIUpdate(string sourceType, int sourceIndex, string targetType, int targetIndex)
        {
            try
            {
                // LoggingService.LogDebug($"[UI] ForceImmediateUIUpdate for {sourceType}[{sourceIndex}] and {targetType}[{targetIndex}]");
                
                // Get affected wrappers
                var sourceWrapper = GetSlotWrapper(sourceType, sourceIndex);
                var targetWrapper = GetSlotWrapper(targetType, targetIndex);
                
                // Force real data synchronization first
                var sourceItem = GetItemFromSlot(sourceType, sourceIndex);
                var targetItem = GetItemFromSlot(targetType, targetIndex);
                
                // Update wrappers directly with fresh data
                if (sourceWrapper != null)
                {
                    if (sourceWrapper.Item != sourceItem)
                    {
                        // LoggingService.LogInfo($"[UI] Fixing source wrapper data: {sourceWrapper.Item?.Name ?? "null"} -> {sourceItem?.Name ?? "null"}");
                        sourceWrapper.Item = sourceItem;
                    }
                    // Always force notification
                    sourceWrapper.NotifyItemChanged();
                }
                
                if (targetWrapper != null)
                {
                    if (targetWrapper.Item != targetItem)
                    {
                        // LoggingService.LogInfo($"[UI] Fixing target wrapper data: {targetWrapper.Item?.Name ?? "null"} -> {targetItem?.Name ?? "null"}");
                        targetWrapper.Item = targetItem;
                    }
                    // Always force notification
                    targetWrapper.NotifyItemChanged();
                }
                
                // Force dispatcher updates for UI thread
                if (System.Windows.Application.Current?.Dispatcher != null)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() => 
                    {
                        // Force UI update on the UI thread for all affected wrappers
                        sourceWrapper?.NotifyItemChanged();
                        targetWrapper?.NotifyItemChanged();
                        
                        // Additional UI refresh tricks
                        if (sourceWrapper != null)
                            OnPropertyChanged(sourceType + "Slots");
                            
                        if (targetWrapper != null)
                            OnPropertyChanged(targetType + "Slots");
                        
                        // Force collection change notifications for ObservableCollections
                        if (sourceType == "Inventory" && sourceIndex >= 0 && sourceIndex < InventorySlots.Count)
                        {
                            var slot = InventorySlots[sourceIndex];
                            // Trick to force collection change notification
                            int sourceOriginalIndex = sourceIndex;
                            if (InventorySlots.Count > 1 && sourceIndex == 0)
                            {
                                // Swap places temporarily
                                var temp = InventorySlots[0];
                                InventorySlots[0] = InventorySlots[1];
                                InventorySlots[1] = temp;
                                // Swap back
                                temp = InventorySlots[0];
                                InventorySlots[0] = InventorySlots[1];
                                InventorySlots[1] = temp;
                            }
                        }
                        
                        if (targetType == "Inventory" && targetIndex >= 0 && targetIndex < InventorySlots.Count)
                        {
                            var slot = InventorySlots[targetIndex];
                            // Same trick for target slot
                            int targetOriginalIndex = targetIndex;
                            if (InventorySlots.Count > 1 && targetIndex == 0)
                            {
                                // Swap places temporarily
                                var temp = InventorySlots[0];
                                InventorySlots[0] = InventorySlots[1];
                                InventorySlots[1] = temp;
                                // Swap back
                                temp = InventorySlots[0];
                                InventorySlots[0] = InventorySlots[1];
                                InventorySlots[1] = temp;
                            }
                        }
                    }, System.Windows.Threading.DispatcherPriority.Render);
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"ForceImmediateUIUpdate: {ex.Message}", ex);
            }
        }
    }
    
    // Helper classes
    public class InventorySlotWrapper : INotifyPropertyChanged
    {
        private Item? _item;
        public Item? Item 
        { 
            get => _item; 
            set 
            { 
                if (_item != value) 
                { 
                    _item = value; 
                    OnPropertyChanged();
                } 
            } 
        }
        
        // ����������� �� ���������
        public InventorySlotWrapper()
        {
        }
        
        // ����������� � ���������� Item
        public InventorySlotWrapper(Item? item)
        {
            _item = item;
        }
        
        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
        public void NotifyItemChanged()
        {
            try
            {
                // LoggingService.LogInfo($"[NOTIFY] NotifyItemChanged ������ ��� ��������: {Item?.Name ?? "null"}");
                
                // ������������� �������� OnPropertyChanged, ���� ���� ������ ��� ��
                ForcePropertyChanged(nameof(Item));
                // LoggingService.LogInfo($"[NOTIFY] ForcePropertyChanged(Item) �������� �� ������� ������");
                
                // Use dispatcher for UI thread safety and to ensure UI gets refreshed
                if (System.Windows.Application.Current?.Dispatcher != null)
                {
                    if (!System.Windows.Application.Current.Dispatcher.CheckAccess())
                    {
                        // LoggingService.LogInfo($"[NOTIFY] �� �� UI ������, ���������� Dispatcher.BeginInvoke");
                        // If we're not on the UI thread, invoke there with high priority
                        System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() => 
                        {
                            // LoggingService.LogInfo($"[NOTIFY] Dispatcher.BeginInvoke: ��������� ForcePropertyChanged �� UI ������");
                            // Force additional UI notification on UI thread
                            ForcePropertyChanged(nameof(Item));
                        }), System.Windows.Threading.DispatcherPriority.Render);
                    }
                    else
                    {
                        // LoggingService.LogInfo($"[NOTIFY] ��� �� UI ������, ��������� ���������� �����������");
                        // If we're already on the UI thread, add a delay before a second notification
                        // This helps ensure the UI has time to process the first notification
                        System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() => 
                        {
                            // LoggingService.LogInfo($"[NOTIFY] Delayed notification: ��������� ���������� ForcePropertyChanged");
                            ForcePropertyChanged(nameof(Item));
                        }), System.Windows.Threading.DispatcherPriority.ContextIdle);
                    }
                }
                else
                {
                    LoggingService.LogWarning($"[NOTIFY] Dispatcher ����������!");
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"[NOTIFY] ������ � NotifyItemChanged: {ex.Message}");
            }
        }
        
        /// <summary>
        /// ������������� �������� PropertyChanged, ���� ���� �������� �� ����������
        /// </summary>
        public void ForcePropertyChanged(string propertyName)
        {
            // LoggingService.LogInfo($"[FORCE] ForcePropertyChanged ������ ��� ��������: {propertyName}");
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            
            // �������������: ������������� �������� OnItemChanged � UI ����������
            if (propertyName == "Item")
            {
                // LoggingService.LogInfo($"[FORCE] ������������� �������� OnItemChanged ��� Item: {Item?.Name ?? "null"}");
                
                // ������� ��������� ��������� ��� ��������������� ������ OnItemChanged
                var oldValue = Item;
                var newValue = Item;
                var args = new PropertyChangedEventArgs("Item");
                
                // ���������� ��������� ��� ������� ������ OnItemChanged
                try
                {
                    // ���� ��� CoreInventorySlot � UI ������ � ������������� ��������� ��
                    if (System.Windows.Application.Current?.MainWindow != null)
                    {
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            // LoggingService.LogInfo($"[FORCE] ���� CoreInventorySlot ���������� ��� ��������������� ����������");
                            ForceUpdateAllInventorySlots();
                        });
                    }
                }
                catch (Exception ex)
                {
                    LoggingService.LogError($"[FORCE] ������ ��� �������������� ���������� UI: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// ������������� ��������� ��� CoreInventorySlot ����������
        /// </summary>
        private void ForceUpdateAllInventorySlots()
        {
            try
            {
                // LoggingService.LogInfo($"[FORCE-UI] �������� �������������� ���������� ���� CoreInventorySlot");
                
                var mainWindow = System.Windows.Application.Current?.MainWindow;
                if (mainWindow != null)
                {
                    // ������������� ��������� ���� UI
                    mainWindow.InvalidateVisual();
                    mainWindow.UpdateLayout();
                    
                    // LoggingService.LogInfo($"[FORCE-UI] MainWindow.InvalidateVisual() � UpdateLayout() ���������");
                    
                    // �������������: ���� � ������������� ��������� ��� CoreInventorySlot
                    try
                    {
                        var coreSlots = FindVisualChildren<Views.Controls.CoreInventorySlot>(mainWindow);
                        int slotsFound = 0;
                        
                        foreach (var slot in coreSlots)
                        {
                            try
                            {
                                // ������������� ��������� ������ ����
                                slot.InvalidateVisual();
                                slot.UpdateLayout();
                                
                                // ������������� �������� ���������� ������
                                var dataContext = slot.DataContext;
                                if (dataContext is InventorySlotWrapper wrapper)
                                {
                                    // ������������� ��������� binding
                                    var binding = System.Windows.Data.BindingOperations.GetBinding(slot, Views.Controls.CoreInventorySlot.ItemProperty);
                                    if (binding != null)
                                    {
                                        var bindingExpression = System.Windows.Data.BindingOperations.GetBindingExpression(slot, Views.Controls.CoreInventorySlot.ItemProperty);
                                        bindingExpression?.UpdateTarget();
                                        // LoggingService.LogInfo($"[FORCE-UI] ������������� �������� binding ��� ����� � ���������: {wrapper.Item?.Name ?? "null"}");
                                    }
                                }
                                
                                slotsFound++;
                            }
                            catch (Exception slotEx)
                            {
                                LoggingService.LogError($"[FORCE-UI] ������ ��� ���������� ���������� �����: {slotEx.Message}");
                            }
                        }
                        
                        // LoggingService.LogInfo($"[FORCE-UI] ������������� ��������� {slotsFound} CoreInventorySlot �����������");
                    }
                    catch (Exception findEx)
                    {
                        LoggingService.LogError($"[FORCE-UI] ������ ��� ������ CoreInventorySlot: {findEx.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"[FORCE-UI] ������ � ForceUpdateAllInventorySlots: {ex.Message}");
            }
        }
        


        /// <summary>
        /// ������� ��� �������� �������� ���������� ���� � ���������� ������
        /// </summary>

        private static IEnumerable<T> FindVisualChildren<T>(System.Windows.DependencyObject depObj) where T : System.Windows.DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    var child = System.Windows.Media.VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }
        

    }
    
    public class MoveItemData
    {
        public string SourceType { get; set; } = "";
        public int SourceIndex { get; set; }
        public string TargetType { get; set; } = "";
        public int TargetIndex { get; set; }
    }
    
    public class EquipItemData
    {
        public int InventoryIndex { get; set; }
        public string EquipmentSlot { get; set; } = "";
    }
} 
