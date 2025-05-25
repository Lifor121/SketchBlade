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

namespace SketchBlade.ViewModels
{
    public class InventoryViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
        // Добавляем публичный метод для внешнего вызова
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
                // Ленивая инициализация - если крафт не создан, но инвентарь заполнен, создаем его
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
        
        public InventoryViewModel(GameData GameData)
        {
            _gameState = GameData ?? throw new ArgumentNullException(nameof(GameData));
            
            // НЕ СОЗДАЕМ SimplifiedCraftingViewModel здесь - он будет создан после инициализации игры!
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
            
            // Инициализируем SimplifiedCraftingViewModel ТОЛЬКО если инвентарь уже заполнен
            InitializeCraftingIfReady();
        }
        
        /// <summary>
        /// Инициализирует систему крафта, если инвентарь готов
        /// </summary>
        public void InitializeCraftingIfReady()
        {
            if (_simplifiedCraftingViewModel == null)
            {
                var nonNullItems = _gameState.Inventory.Items.Count(item => item != null);
                if (nonNullItems > 0)
                {
                    LoggingService.LogInfo($"Инициализируем SimplifiedCraftingViewModel - в инвентаре уже {nonNullItems} предметов");
                    _simplifiedCraftingViewModel = new SimplifiedCraftingViewModel(_gameState);
                    OnPropertyChanged(nameof(SimplifiedCraftingViewModel));
                }
                else
                {
                    LoggingService.LogDebug("SimplifiedCraftingViewModel не инициализирован - инвентарь пуст");
                }
            }
        }
        
        /// <summary>
        /// Принудительно пересоздает систему крафта (для использования после заполнения инвентаря)
        /// </summary>
        public void ReinitializeCrafting()
        {
            try
            {
                LoggingService.LogInfo("=== ПРИНУДИТЕЛЬНАЯ РЕИНИЦИАЛИЗАЦИЯ СИСТЕМЫ КРАФТА ===");
                
                var nonNullItems = _gameState.Inventory.Items.Count(item => item != null);
                LoggingService.LogInfo($"В инвентаре: {nonNullItems} предметов");
                
                if (nonNullItems > 0)
                {
                    foreach (var item in _gameState.Inventory.Items.Where(item => item != null))
                    {
                        LoggingService.LogInfo($"  - {item.Name} x{item.StackSize}");
                    }
                    
                    // Проверяем, нужно ли создавать новый экземпляр
                    if (_simplifiedCraftingViewModel == null)
                    {
                        LoggingService.LogInfo("Создание нового экземпляра SimplifiedCraftingViewModel");
                        _simplifiedCraftingViewModel = new SimplifiedCraftingViewModel(_gameState);
                        OnPropertyChanged(nameof(SimplifiedCraftingViewModel));
                    }
                    else
                    {
                        LoggingService.LogInfo("Обновление существующего экземпляра SimplifiedCraftingViewModel");
                        _simplifiedCraftingViewModel.RefreshAvailableRecipes();
                    }
                    
                    LoggingService.LogInfo("SimplifiedCraftingViewModel пересоздан с полным инвентарем");
                }
                else
                {
                    LoggingService.LogInfo("Инвентарь пуст - SimplifiedCraftingViewModel не создан");
                }
                
                LoggingService.LogInfo("=== ПРИНУДИТЕЛЬНОЕ ОБНОВЛЕНИЕ UI ПОСЛЕ ИНИЦИАЛИЗАЦИИ ===");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Ошибка при реинициализации крафта: {ex.Message}", ex);
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
                LoggingService.LogDebug($"RefreshAllSlots: Начинаем обновление. Инвентарь содержит {_gameState.Inventory.Items.Count} предметов");
                
                // Отладочная информация о содержимом инвентаря
                for (int i = 0; i < _gameState.Inventory.Items.Count; i++)
                {
                    var item = _gameState.Inventory.Items[i];
                    if (item != null)
                    {
                        LoggingService.LogDebug($"  Инвентарь[{i}]: {item.Name} x{item.StackSize}");
                    }
                }
                
                // ИСПРАВЛЕНИЕ: Принудительно обновляем ВСЕ слоты инвентаря
                for (int i = 0; i < InventorySlots.Count; i++)
                {
                    if (i < _gameState.Inventory.Items.Count)
                    {
                        var inventoryItem = _gameState.Inventory.Items[i];
                        
                        // ВАЖНО: Принудительно устанавливаем Item даже если он тот же самый
                        // Это заставляет UI обновиться
                        var oldItem = InventorySlots[i].Item;
                        InventorySlots[i].Item = null; // Сначала очищаем
                        InventorySlots[i].Item = inventoryItem; // Затем устанавливаем новое значение
                        
                        // Принудительно уведомляем об изменении
                        InventorySlots[i].NotifyItemChanged();
                        
                        LoggingService.LogDebug($"  Обновляем InventorySlots[{i}] = {inventoryItem?.Name ?? "null"}");
                    }
                    else
                    {
                        // Очищаем слоты, которые выходят за пределы инвентаря
                        InventorySlots[i].Item = null;
                        InventorySlots[i].NotifyItemChanged();
                        LoggingService.LogDebug($"  Очищаем InventorySlots[{i}] = null");
                    }
                }
                
                // Refresh equipment slots
                var player = _gameState.Player;
                if (player != null)
                {
                    // ИСПРАВЛЕНИЕ: Принудительно обновляем слоты экипировки
                    WeaponSlot.Item = null;
                    WeaponSlot.Item = player.EquippedWeapon;
                    WeaponSlot.NotifyItemChanged();
                    
                    HelmetSlot.Item = null;
                    HelmetSlot.Item = player.EquippedHelmet;
                    HelmetSlot.NotifyItemChanged();
                    
                    ChestSlot.Item = null;
                    ChestSlot.Item = player.EquippedArmor;
                    ChestSlot.NotifyItemChanged();
                    
                    LegsSlot.Item = null;
                    LegsSlot.Item = player.EquippedLeggings;
                    LegsSlot.NotifyItemChanged();
                    
                    ShieldSlot.Item = null;
                    ShieldSlot.Item = player.EquippedShield;
                    ShieldSlot.NotifyItemChanged();
                }
                
                // ИСПРАВЛЕНИЕ: Не трогаем Quick слоты в RefreshAllSlots!
                // Quick слоты теперь независимые и обновляются только при явном перемещении
                // Это исправляет проблему дупликации
                
                LoggingService.LogDebug($"RefreshAllSlots: Обновлено {InventorySlots.Count} слотов инвентаря");
                
                // Проверяем результат синхронизации
                for (int i = 0; i < Math.Min(InventorySlots.Count, _gameState.Inventory.Items.Count); i++)
                {
                    var slotItem = InventorySlots[i].Item;
                    var inventoryItem = _gameState.Inventory.Items[i];
                    if (slotItem != inventoryItem)
                    {
                        LoggingService.LogError($"ОШИБКА СИНХРОНИЗАЦИИ: InventorySlots[{i}].Item = {slotItem?.Name ?? "null"}, но _gameState.Inventory.Items[{i}] = {inventoryItem?.Name ?? "null"}");
                    }
                }
                
                // ИСПРАВЛЕНИЕ: Принудительно уведомляем об изменении коллекций
                OnPropertyChanged(nameof(InventorySlots));
                OnPropertyChanged(nameof(WeaponSlot));
                OnPropertyChanged(nameof(HelmetSlot));
                OnPropertyChanged(nameof(ChestSlot));
                OnPropertyChanged(nameof(LegsSlot));
                OnPropertyChanged(nameof(ShieldSlot));
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"RefreshAllSlots: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Принудительное обновление UI - упрощенная и более надежная версия
        /// </summary>
        public void ForceUIUpdate()
        {
            try
            {
                LoggingService.LogDebug("ForceUIUpdate: Принудительное обновление UI");
                
                // Обновляем все слоты инвентаря
                foreach (var slot in InventorySlots)
                {
                    slot.NotifyItemChanged();
                }
                
                // Обновляем быстрые слоты
                foreach (var slot in QuickSlots)
                {
                    slot.NotifyItemChanged();
                }
                
                // Обновляем слоты экипировки
                HelmetSlot.NotifyItemChanged();
                ChestSlot.NotifyItemChanged();
                LegsSlot.NotifyItemChanged();
                WeaponSlot.NotifyItemChanged();
                ShieldSlot.NotifyItemChanged();
                TrashSlot.NotifyItemChanged();
                
                // Обновляем систему крафта если она существует
                if (_simplifiedCraftingViewModel != null)
                {
                    _simplifiedCraftingViewModel.RefreshAvailableRecipes();
                    OnPropertyChanged(nameof(SimplifiedCraftingViewModel));
                }
                
                // Обновляем игровые данные
                OnPropertyChanged(nameof(GameData));
                OnPropertyChanged(nameof(PlayerHealth));
                OnPropertyChanged(nameof(PlayerDamage));
                OnPropertyChanged(nameof(PlayerDefense));
                OnPropertyChanged(nameof(PlayerSprite));
                
                LoggingService.LogDebug("ForceUIUpdate: Принудительное обновление UI завершено");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"ForceUIUpdate: {ex.Message}", ex);
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
                        if (slotIndex >= 0 && slotIndex < QuickSlots.Count)
                            return QuickSlots[slotIndex].Item;
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
        
        public void MoveToInventorySlot(MoveItemData? moveData)
        {
            if (moveData == null) return;
            
            try
            {
                var sourceItem = GetItemFromSlot(moveData.SourceType, moveData.SourceIndex);
                if (sourceItem == null) 
                {
                    LoggingService.LogDebug($"[DragDrop] MoveToInventorySlot: sourceItem is null for {moveData.SourceType}[{moveData.SourceIndex}]");
                    return;
                }
                
                // Проверяем валидность целевого слота
                if (moveData.TargetIndex < 0 || moveData.TargetIndex >= _gameState.Inventory.Items.Count)
                {
                    LoggingService.LogError($"[DragDrop] MoveToInventorySlot: invalid target index {moveData.TargetIndex}", null);
                    return;
                }

                // Получаем предмет в целевом слоте (может быть null)
                var targetItem = _gameState.Inventory.Items[moveData.TargetIndex];
                
                LoggingService.LogDebug($"[DragDrop] MoveToInventorySlot: Moving {sourceItem.Name} from {moveData.SourceType}[{moveData.SourceIndex}] to Inventory[{moveData.TargetIndex}]");
                
                // Атомарное перемещение: сначала устанавливаем целевой слот, затем очищаем исходный
                _gameState.Inventory.Items[moveData.TargetIndex] = sourceItem;
                SetItemInSlot(moveData.SourceType, moveData.SourceIndex, targetItem);
                
                // Уведомляем об изменении инвентаря
                _gameState.Inventory.OnInventoryChanged();
                
                // Простое обновление UI без избыточных вызовов
                RefreshAllSlots();
                
                LoggingService.LogDebug("[DragDrop] MoveToInventorySlot: Move completed successfully");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"MoveToInventorySlot: {ex.Message}", ex);
            }
        }
        
        private void SetItemInSlot(string slotType, int slotIndex, Item? item)
        {
            try
            {
                switch (slotType)
                {
                    case "Inventory":
                        if (slotIndex >= 0 && slotIndex < _gameState.Inventory.Items.Count)
                            _gameState.Inventory.Items[slotIndex] = item;
                        break;
                        
                    case "Quick":
                        // РРЎРџР РђР'Р›Р•РќРР•: Quick СЃР»РѕС‚С‹ С‚РµРїРµСЂСЊ РЅРµР·Р°РІРёСЃРёРјС‹Рµ
                        if (slotIndex >= 0 && slotIndex < QuickSlots.Count)
                        {
                            QuickSlots[slotIndex].Item = item;
                            LoggingService.LogDebug($"[DragDrop] SetItemInSlot: Quick[{slotIndex}] = {item?.Name ?? "null"}");
                        }
                        break;
                        
                    case "Weapon":
                        if (_gameState.Player != null)
                        {
                            if (item != null)
                            {
                                // First unequip any existing weapon
                                _gameState.Player.UnequipItem(EquipmentSlot.MainHand);
                                // Then equip the new item
                                _gameState.Player.EquipItem(item);
                            }
                            else
                            {
                                // Just unequip
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
                    // Р”Р»СЏ СЌРєРёРїРёСЂРѕРІРєРё РёСЃРїРѕР»СЊР·СѓРµРј СЃРїРµС†РёР°Р»СЊРЅС‹Р№ РјРµС‚РѕРґ
                    var equipData = new EquipItemData
                    {
                        InventoryIndex = sourceIndex,
                        EquipmentSlot = targetType
                    };
                    EquipItem(equipData);
                    break;
                default:
                    // РџРѕ СѓРјРѕР»С‡Р°РЅРёСЋ РїС‹С‚РєР°РµРјСЃСЏ РїРµСЂРµРјРµСЃС‚РёС‚СЊ РІ РёРЅРІРµРЅС‚Р°СЂСЊ
                    LoggingService.LogWarning($"MoveItemBetweenSlots: Unknown target type {targetType}, using default behavior");
                    MoveToInventorySlot(moveData);
                    break;
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
                
                RefreshAllSlots();
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
                // РСЃРїРѕР»СЊР·СѓРµРј РїСЂР°РІРёР»СЊРЅСѓСЋ РЅР°РІРёРіР°С†РёСЋ С‡РµСЂРµР· GameData.CurrentScreenViewModel
                if (_gameState.CurrentScreenViewModel is MainViewModel mainViewModel)
                {
                    // РСЃРїРѕР»СЊР·СѓРµРј Navigate РјРµС‚РѕРґ РёР· MainViewModel РґР»СЏ РїСЂР°РІРёР»СЊРЅРѕР№ РЅР°РІРёРіР°С†РёРё
                    var navigateMethod = mainViewModel.GetType().GetMethod("Navigate", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (navigateMethod != null)
                    {
                        navigateMethod.Invoke(mainViewModel, new object[] { screenName });
                    }
                }
                else
                {
                    // Fallback: РїСЂСЏРјР°СЏ СѓСЃС‚Р°РЅРѕРІРєР° СЌРєСЂР°РЅР° Рё РїРѕРїС‹С‚РєР° РЅР°РІРёРіР°С†РёРё С‡РµСЂРµР· MainWindow
                    _gameState.CurrentScreen = screenName;
                    
                    // РџРѕРїС‹С‚РєР° РЅР°РІРёРіР°С†РёРё С‡РµСЂРµР· MainWindow РЅР°РїСЂСЏРјСѓСЋ
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
                LoggingService.LogDebug($"[DragDrop] MoveToQuickSlot: {moveData.SourceType}[{moveData.SourceIndex}] -> {moveData.TargetType}[{moveData.TargetIndex}]");

                // Проверяем, что целевой слот действует как Quick
                if (moveData.TargetType != "Quick")
                {
                    LoggingService.LogError($"MoveToQuickSlot: Target is not Quick slot", null);
                    return;
                }

                // Получаем исходный предмет
                var sourceItem = GetItemFromSlot(moveData.SourceType, moveData.SourceIndex);
                if (sourceItem == null)
                {
                    LoggingService.LogWarning($"MoveToQuickSlot: Source item is null");
                    return;
                }

                // Получаем целевой предмет
                var targetItem = GetItemFromSlot(moveData.TargetType, moveData.TargetIndex);
                
                // Атомарное перемещение: сначала устанавливаем целевой слот, затем очищаем исходный
                SetItemInSlot(moveData.TargetType, moveData.TargetIndex, sourceItem);
                SetItemInSlot(moveData.SourceType, moveData.SourceIndex, targetItem);
                
                // Простое обновление UI без избыточных вызовов
                RefreshAllSlots();
                
                LoggingService.LogDebug("[DragDrop] MoveToQuickSlot: Move completed successfully");
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
                LoggingService.LogDebug($"[DragDrop] MoveToCraftSlot: {moveData.SourceType}[{moveData.SourceIndex}] -> {moveData.TargetType}[{moveData.TargetIndex}]");

                // Получаем исходный предмет
                var sourceItem = GetItemFromSlot(moveData.SourceType, moveData.SourceIndex);
                if (sourceItem == null)
                {
                    LoggingService.LogWarning($"MoveToCraftSlot: Source item is null");
                    return;
                }

                // Получаем целевой предмет
                var targetItem = GetItemFromSlot(moveData.TargetType, moveData.TargetIndex);
                
                // Атомарное перемещение: сначала устанавливаем целевой слот, затем очищаем исходный
                SetItemInSlot(moveData.TargetType, moveData.TargetIndex, sourceItem);
                SetItemInSlot(moveData.SourceType, moveData.SourceIndex, targetItem);
                
                // Простое обновление UI без избыточных вызовов
                RefreshAllSlots();
                
                LoggingService.LogDebug("[DragDrop] MoveToCraftSlot: Move completed successfully");
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
                // Remove item from source slot
                SetItemInSlot(moveData.SourceType, moveData.SourceIndex, null);
                RefreshAllSlots();
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
                            if (item.Name.ToLower().Contains("healing") || item.Name.ToLower().Contains("Р·РµР»СЊРµ"))
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
                        
                        RefreshAllSlots();
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
            LoggingService.LogDebug("SplitStack called");
        }
        
        // Additional methods needed by the UI
        public void TakeCraftResult()
        {
            try
            {
                LoggingService.LogDebug("TakeCraftResult: РРЎРџР РђР'Р›Р•РќРР• - РєСЂР°С„С‚ СѓР¶Рµ РїСЂРѕРёСЃС…РѕРґРёС‚ РїСЂРё РєР»РёРєРµ РЅР° СЂРµС†РµРїС‚");

                // РРЎРџР РђР'Р›Р•РќРР•: РЈР±РёСЂР°РµРј РґСѓР±Р»РёСЂСѓСЋС‰СѓСЋ Р»РѕРіРёРєСѓ РєСЂР°С„С‚Р°
                // РљСЂР°С„С‚ СѓР¶Рµ РїСЂРѕРёСЃС…РѕРґРёС‚ РїСЂРё РєР»РёРєРµ РЅР° СЂРµС†РµРїС‚ РІ CraftingPanel
                // Р—РґРµСЃСЊ РЅСѓР¶РЅРѕ С‚РѕР»СЊРєРѕ РѕР±РЅРѕРІРёС‚СЊ UI
                
                // Принудительно обновляем UI
                RefreshAllSlots();
                ForceUIUpdate();
                
                LoggingService.LogDebug("TakeCraftResult: UI РѕР±РЅРѕРІР»РµРЅ");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"TakeCraftResult: {ex.Message}", ex);
            }
        }
        
        public void SetItemToSlot(string slotType, int slotIndex, Item? item)
        {
            SetItemInSlot(slotType, slotIndex, item);
                RefreshAllSlots();
        }
        
        public void MoveToCraft(MoveItemData? moveData)
        {
            MoveToCraftSlot(moveData);
        }
        
        public Inventory PlayerInventory => _gameState.Inventory;
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
                    // Убираем избыточное логирование - оставляем только при необходимости отладки
                    // var oldItemName = _item?.Name ?? "null";
                    // var newItemName = value?.Name ?? "null";
                    // LoggingService.LogDebug($"[InventorySlotWrapper] Item изменен: {oldItemName} -> {newItemName}");
                    
                    _item = value; 
                    OnPropertyChanged();
                    
                    // LoggingService.LogDebug($"[InventorySlotWrapper] PropertyChanged уведомление отправлено для Item");
                } 
            } 
        }
        
        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            // Убираем избыточное логирование
            // LoggingService.LogDebug($"[InventorySlotWrapper] OnPropertyChanged вызван для свойства: {propertyName}");
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
        public void NotifyItemChanged()
        {
            OnPropertyChanged(nameof(Item));
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
