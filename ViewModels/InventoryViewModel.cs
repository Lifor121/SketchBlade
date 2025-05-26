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
        
        private bool _isUpdatingInventory = false;
        
        private DispatcherTimer? _updateUITimer;
        private bool _isUIUpdatePending = false;
        
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
                // Убираем избыточное логирование - логируем только при значительных изменениях
            // LoggingService.LogDebug($"RefreshAllSlots: Обновление {_gameState.Inventory.Items.Count} предметов инвентаря");
                
                // Обновляем слоты инвентаря
                for (int i = 0; i < InventorySlots.Count; i++)
                {
                    if (i < _gameState.Inventory.Items.Count)
                    {
                        var inventoryItem = _gameState.Inventory.Items[i];
                        
                        // Простое обновление без принудительного null
                        if (InventorySlots[i].Item != inventoryItem)
                        {
                            InventorySlots[i].Item = inventoryItem;
                            LoggingService.LogDebug($"  Слот[{i}]: {inventoryItem?.Name ?? "пусто"}");
                        }
                    }
                    else
                    {
                        if (InventorySlots[i].Item != null)
                        {
                            InventorySlots[i].Item = null;
                        }
                    }
                }
                
                // Обновляем слоты экипировки
                var player = _gameState.Player;
                if (player != null)
                {
                    if (WeaponSlot.Item != player.EquippedWeapon)
                        WeaponSlot.Item = player.EquippedWeapon;
                    
                    if (HelmetSlot.Item != player.EquippedHelmet)
                        HelmetSlot.Item = player.EquippedHelmet;
                    
                    if (ChestSlot.Item != player.EquippedArmor)
                        ChestSlot.Item = player.EquippedArmor;
                    
                    if (LegsSlot.Item != player.EquippedLeggings)
                        LegsSlot.Item = player.EquippedLeggings;
                    
                    if (ShieldSlot.Item != player.EquippedShield)
                        ShieldSlot.Item = player.EquippedShield;
                }
                
                // LoggingService.LogDebug("RefreshAllSlots: Обновление завершено");
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
                LoggingService.LogDebug("[UI] Обновление UI начато");
                
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
                
                LoggingService.LogDebug("[UI] Обновление UI завершено");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"[UI] Ошибка при обновлении UI: {ex.Message}", ex);
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
                // Atomic operation: capture both source and target items at the same time
                var sourceItem = GetItemFromSlot(moveData.SourceType, moveData.SourceIndex);
                if (sourceItem == null) 
                {
                    LoggingService.LogWarning($"[DragDrop] MoveToInventorySlot: Исходный предмет не найден в {moveData.SourceType}[{moveData.SourceIndex}]");
                    return;
                }
                
                // Проверяем валидность целевого слота
                if (moveData.TargetIndex < 0 || moveData.TargetIndex >= _gameState.Inventory.Items.Count)
                {
                    LoggingService.LogError($"[DragDrop] MoveToInventorySlot: Неверный индекс целевого слота {moveData.TargetIndex}");
                    return;
                }

                // Получаем предмет в целевом слоте (может быть null)
                var targetItem = _gameState.Inventory.Items[moveData.TargetIndex];
                
                LoggingService.LogInfo($"[DragDrop] Перемещение: {sourceItem.Name} из {moveData.SourceType}[{moveData.SourceIndex}] в Inventory[{moveData.TargetIndex}]");
                
                // Выполняем атомарное перемещение с проверкой состояния
                bool moveSuccessful = PerformAtomicMove(moveData.SourceType, moveData.SourceIndex, 
                                                       "Inventory", moveData.TargetIndex, 
                                                       sourceItem, targetItem);
                
                if (moveSuccessful)
                {
                    // Уведомляем об изменении инвентаря только один раз
                    _gameState.Inventory.OnInventoryChanged();
                    
                    // Оптимизированное обновление только измененных слотов
                    UpdateSpecificSlots(moveData.SourceType, moveData.SourceIndex, "Inventory", moveData.TargetIndex);
                    
                    LoggingService.LogInfo("[DragDrop] Перемещение завершено успешно");
                }
                else
                {
                    LoggingService.LogWarning("[DragDrop] Перемещение не удалось - состояние изменилось во время операции");
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"MoveToInventorySlot: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Выполняет атомарное перемещение предметов между слотами с проверкой состояния
        /// </summary>
        public bool PerformAtomicMove(string sourceType, int sourceIndex, string targetType, int targetIndex, 
                                     Item sourceItem, Item? targetItem)
        {
            try
            {
                // Повторно проверяем, что исходный предмет все еще на месте
                var currentSourceItem = GetItemFromSlot(sourceType, sourceIndex);
                if (currentSourceItem != sourceItem)
                {
                    LoggingService.LogWarning($"[DragDrop] Атомарное перемещение отменено: исходный предмет изменился");
                    return false;
                }
                
                // Выполняем перемещение в правильном порядке
                if (targetType == "Inventory")
                {
                    _gameState.Inventory.Items[targetIndex] = sourceItem;
                }
                else
                {
                    SetItemInSlot(targetType, targetIndex, sourceItem);
                }
                
                // Очищаем исходный слот
                SetItemInSlot(sourceType, sourceIndex, targetItem);
                LoggingService.LogDebug($"[PerformAtomicMove] After SetItemInSlot for source ({sourceType}[{sourceIndex}]): _gameState.Inventory.Items[{sourceIndex}] is now { (sourceType == "Inventory" && sourceIndex >= 0 && sourceIndex < _gameState.Inventory.Items.Count ? _gameState.Inventory.Items[sourceIndex]?.Name : "N/A_OR_NON_INVENTORY_SOURCE") ?? "null"}");

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
        /// Оптимизированное обновление только конкретных слотов
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
        /// Обновляет один конкретный слот
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
                    // Р"лСЏ СЌРєРёРїРёСЂРѕРІРєРё РёСЃРїРѕР»СЊР·СѓРµРј СЃРїРµС†РёР°Р»СЊРЅС‹Р№ РјРµС‚РѕРґ
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
            
            // Убираем избыточные UI обновления - они уже выполняются в соответствующих методах
            LoggingService.LogInfo($"[DragDrop] MoveItemBetweenSlots завершено");
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
                                LoggingService.LogInfo($"[UI] Синхронизация {slotType}[{slotIndex}]: {slot.Item?.Name ?? "null"} -> {actualItem?.Name ?? "null"}");
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
                                LoggingService.LogInfo($"[UI] Синхронизация {slotType}[{slotIndex}]: {slot.Item?.Name ?? "null"} -> {actualItem?.Name ?? "null"}");
                                slot.Item = actualItem;
                                slot.NotifyItemChanged();
                            }
                        }
                        break;
                    case "Helmet":
                        if (HelmetSlot.Item != actualItem)
                        {
                            LoggingService.LogInfo($"[UI] Синхронизация {slotType}: {HelmetSlot.Item?.Name ?? "null"} -> {actualItem?.Name ?? "null"}");
                            HelmetSlot.Item = actualItem;
                            HelmetSlot.NotifyItemChanged();
                        }
                        break;
                    case "Chestplate":
                        if (ChestSlot.Item != actualItem)
                        {
                            LoggingService.LogInfo($"[UI] Синхронизация {slotType}: {ChestSlot.Item?.Name ?? "null"} -> {actualItem?.Name ?? "null"}");
                            ChestSlot.Item = actualItem;
                            ChestSlot.NotifyItemChanged();
                        }
                        break;
                    case "Leggings":
                        if (LegsSlot.Item != actualItem)
                        {
                            LoggingService.LogInfo($"[UI] Синхронизация {slotType}: {LegsSlot.Item?.Name ?? "null"} -> {actualItem?.Name ?? "null"}");
                            LegsSlot.Item = actualItem;
                            LegsSlot.NotifyItemChanged();
                        }
                        break;
                    case "Weapon":
                        if (WeaponSlot.Item != actualItem)
                        {
                            LoggingService.LogInfo($"[UI] Синхронизация {slotType}: {WeaponSlot.Item?.Name ?? "null"} -> {actualItem?.Name ?? "null"}");
                            WeaponSlot.Item = actualItem;
                            WeaponSlot.NotifyItemChanged();
                        }
                        break;
                    case "Shield":
                        if (ShieldSlot.Item != actualItem)
                        {
                            LoggingService.LogInfo($"[UI] Синхронизация {slotType}: {ShieldSlot.Item?.Name ?? "null"} -> {actualItem?.Name ?? "null"}");
                            ShieldSlot.Item = actualItem;
                            ShieldSlot.NotifyItemChanged();
                        }
                        break;
                    case "Trash":
                        if (TrashSlot.Item != actualItem)
                        {
                            LoggingService.LogInfo($"[UI] Синхронизация {slotType}: {TrashSlot.Item?.Name ?? "null"} -> {actualItem?.Name ?? "null"}");
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
                
                // Оптимизированное обновление только измененных слотов
                UpdateSingleSlot("Inventory", equipData.InventoryIndex);
                UpdateSingleSlot(equipData.EquipmentSlot, 0);
                
                // Обновляем крафтинг если нужно
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
                // Р"спользуем правильную навигацию через GameData.CurrentScreenViewModel
                if (_gameState.CurrentScreenViewModel is MainViewModel mainViewModel)
                {
                    // Рспользуем Navigate метод из MainViewModel для правильной навигации
                    var navigateMethod = mainViewModel.GetType().GetMethod("Navigate", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (navigateMethod != null)
                    {
                        navigateMethod.Invoke(mainViewModel, new object[] { screenName });
                    }
                }
                else
                {
                    // Fallback: прямая установка экрана и попытка навигации через MainWindow
                    _gameState.CurrentScreen = screenName;
                    
                    // Попытка навигации через MainWindow напрямую
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
                LoggingService.LogInfo($"[DragDrop] Перемещение в быстрый слот: {moveData.SourceType}[{moveData.SourceIndex}] -> {moveData.TargetType}[{moveData.TargetIndex}]");

                // Проверяем, что целевой слот действует как Quick
                if (moveData.TargetType != "Quick")
                {
                    LoggingService.LogError($"MoveToQuickSlot: Целевой слот не является Quick слотом");
                    return;
                }

                // Получаем исходный предмет
                var sourceItem = GetItemFromSlot(moveData.SourceType, moveData.SourceIndex);
                if (sourceItem == null)
                {
                    LoggingService.LogWarning($"MoveToQuickSlot: Исходный предмет не найден");
                    return;
                }

                // Получаем целевой предмет
                var targetItem = GetItemFromSlot(moveData.TargetType, moveData.TargetIndex);
                
                // Выполняем атомарное перемещение с проверкой состояния
                bool moveSuccessful = PerformAtomicMove(moveData.SourceType, moveData.SourceIndex, 
                                                       moveData.TargetType, moveData.TargetIndex, 
                                                       sourceItem, targetItem);
                
                if (moveSuccessful)
                {
                    // Оптимизированное обновление только измененных слотов
                    UpdateSpecificSlots(moveData.SourceType, moveData.SourceIndex, moveData.TargetType, moveData.TargetIndex);
                    LoggingService.LogInfo("[DragDrop] Перемещение в быстрый слот завершено");
                }
                else
                {
                    LoggingService.LogWarning("[DragDrop] Перемещение в быстрый слот не удалось");
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
                LoggingService.LogInfo($"[DragDrop] Перемещение в слот крафта: {moveData.SourceType}[{moveData.SourceIndex}] -> {moveData.TargetType}[{moveData.TargetIndex}]");

                // Получаем исходный предмет
                var sourceItem = GetItemFromSlot(moveData.SourceType, moveData.SourceIndex);
                if (sourceItem == null)
                {
                    LoggingService.LogWarning($"MoveToCraftSlot: Исходный предмет не найден");
                    return;
                }

                // Получаем целевой предмет
                var targetItem = GetItemFromSlot(moveData.TargetType, moveData.TargetIndex);
                
                // Выполняем атомарное перемещение с проверкой состояния
                bool moveSuccessful = PerformAtomicMove(moveData.SourceType, moveData.SourceIndex, 
                                                       moveData.TargetType, moveData.TargetIndex, 
                                                       sourceItem, targetItem);
                
                if (moveSuccessful)
                {
                    // Оптимизированное обновление только измененных слотов
                    UpdateSpecificSlots(moveData.SourceType, moveData.SourceIndex, moveData.TargetType, moveData.TargetIndex);
                    LoggingService.LogInfo("[DragDrop] Перемещение в слот крафта завершено");
                }
                else
                {
                    LoggingService.LogWarning("[DragDrop] Перемещение в слот крафта не удалось");
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
                LoggingService.LogInfo($"[DragDrop] Перемещение в корзину: {moveData.SourceType}[{moveData.SourceIndex}]");
                
                // Получаем исходный предмет для логирования
                var sourceItem = GetItemFromSlot(moveData.SourceType, moveData.SourceIndex);
                if (sourceItem != null)
                {
                    LoggingService.LogInfo($"[DragDrop] Удаляем предмет: {sourceItem.Name}");
                }
                
                // Удаляем предмет из исходного слота
                SetItemInSlot(moveData.SourceType, moveData.SourceIndex, null);
                
                // Оптимизированное обновление только исходного слота
                UpdateSingleSlot(moveData.SourceType, moveData.SourceIndex);
                
                LoggingService.LogInfo("[DragDrop] Перемещение в корзину завершено");
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
                        
                        // Оптимизированное обновление только измененного слота
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
            LoggingService.LogDebug("SplitStack called");
        }
        
        // Additional methods needed by the UI
        public void TakeCraftResult()
        {
            try
            {
                LoggingService.LogDebug("TakeCraftResult: Рспользуем результат крафта - крафт уже происходит при клике на рецепт");

                // Рспользуем результат крафта - выбираем дублирующую логику крафта
                // Ркрафт уже происходит при клике на рецепт в CraftingPanel
                // Рдесь нужно только обновить UI
                
                // Оптимизированное обновление - только обновляем крафтинг
                _simplifiedCraftingViewModel?.RefreshAvailableRecipes();
                
                LoggingService.LogDebug("TakeCraftResult: UI обновлен");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"TakeCraftResult: {ex.Message}", ex);
            }
        }
        
        public void SetItemToSlot(string slotType, int slotIndex, Item? item)
        {
            SetItemInSlot(slotType, slotIndex, item);
            // Оптимизированное обновление только измененного слота
            UpdateSingleSlot(slotType, slotIndex);
        }
        
        public void MoveToCraft(MoveItemData? moveData)
        {
            MoveToCraftSlot(moveData);
        }
        
        public Inventory PlayerInventory => _gameState.Inventory;
        
        /// <summary>
        /// Диагностика состояния слотов для отладки проблем с UI
        /// </summary>
        public void DiagnoseSlotState()
        {
            try
            {
                LoggingService.LogInfo("[ДИАГНОСТИКА] Проверка состояния слотов");
                
                // Проверяем синхронизацию инвентаря
                for (int i = 0; i < Math.Min(InventorySlots.Count, _gameState.Inventory.Items.Count); i++)
                {
                    var slotItem = InventorySlots[i].Item;
                    var inventoryItem = _gameState.Inventory.Items[i];
                    
                    if (slotItem != inventoryItem)
                    {
                        LoggingService.LogError($"[ДИАГНОСТИКА] РАССИНХРОНИЗАЦИЯ: Слот[{i}] = {slotItem?.Name ?? "пусто"}, Инвентарь[{i}] = {inventoryItem?.Name ?? "пусто"}");
                    }
                    else if (slotItem != null)
                    {
                        LoggingService.LogDebug($"[ДИАГНОСТИКА] Слот[{i}] синхронизирован: {slotItem.Name}");
                    }
                }
                
                // Проверяем экипировку
                var player = _gameState.Player;
                if (player != null)
                {
                    if (WeaponSlot.Item != player.EquippedWeapon)
                        LoggingService.LogError($"[ДИАГНОСТИКА] РАССИНХРОНИЗАЦИЯ: WeaponSlot = {WeaponSlot.Item?.Name ?? "пусто"}, Player.EquippedWeapon = {player.EquippedWeapon?.Name ?? "пусто"}");
                    
                    if (HelmetSlot.Item != player.EquippedHelmet)
                        LoggingService.LogError($"[ДИАГНОСТИКА] РАССИНХРОНИЗАЦИЯ: HelmetSlot = {HelmetSlot.Item?.Name ?? "пусто"}, Player.EquippedHelmet = {player.EquippedHelmet?.Name ?? "пусто"}");
                }
                
                LoggingService.LogInfo("[ДИАГНОСТИКА] Проверка состояния слотов завершена");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"[ДИАГНОСТИКА] Ошибка при проверке состояния слотов: {ex.Message}", ex);
            }
        }
        
        private void SetItemInSlot(string slotType, int slotIndex, Item? item)
        {
            try
            {
                LoggingService.LogDebug($"[SetItemInSlot] Attempting to set {slotType}[{slotIndex}] to item: {(item?.Name) ?? "null"} (Original Hash: {item?.GetHashCode()})");
                Item? itemBefore = null;

                switch (slotType)
                {
                    case "Inventory":
                        if (slotIndex >= 0 && slotIndex < _gameState.Inventory.Items.Count)
                        {
                            itemBefore = _gameState.Inventory.Items[slotIndex];
                            _gameState.Inventory.Items[slotIndex] = item;
                            LoggingService.LogDebug($"[SetItemInSlot] AFTER set for {slotType}[{slotIndex}]: _gameState.Inventory.Items[{slotIndex}] is now {(_gameState.Inventory.Items[slotIndex]?.Name) ?? "null"} (Hash: {_gameState.Inventory.Items[slotIndex]?.GetHashCode()}). Item param was {(item?.Name) ?? "null"} (Hash: {item?.GetHashCode()}). Item before was {(itemBefore?.Name) ?? "null"} (Hash: {itemBefore?.GetHashCode()})");
                        }
                        break;
                        
                    case "Quick":
                        if (slotIndex >= 0 && slotIndex < QuickSlots.Count)
                        {
                            QuickSlots[slotIndex].Item = item;
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
                LoggingService.LogDebug($"[UI] ForceImmediateUIUpdate for {sourceType}[{sourceIndex}] and {targetType}[{targetIndex}]");
                
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
                        LoggingService.LogInfo($"[UI] Fixing source wrapper data: {sourceWrapper.Item?.Name ?? "null"} -> {sourceItem?.Name ?? "null"}");
                        sourceWrapper.Item = sourceItem;
                    }
                    // Always force notification
                    sourceWrapper.NotifyItemChanged();
                }
                
                if (targetWrapper != null)
                {
                    if (targetWrapper.Item != targetItem)
                    {
                        LoggingService.LogInfo($"[UI] Fixing target wrapper data: {targetWrapper.Item?.Name ?? "null"} -> {targetItem?.Name ?? "null"}");
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
        
        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
        public void NotifyItemChanged()
        {
            // First notify on the current thread
            OnPropertyChanged(nameof(Item));
            
            // Use dispatcher for UI thread safety and to ensure UI gets refreshed
            if (System.Windows.Application.Current?.Dispatcher != null)
            {
                if (!System.Windows.Application.Current.Dispatcher.CheckAccess())
                {
                    // If we're not on the UI thread, invoke there with high priority
                    System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() => 
                    {
                        // Force additional UI notification on UI thread
                        OnPropertyChanged(nameof(Item));
                    }), System.Windows.Threading.DispatcherPriority.Render);
                }
                else
                {
                    // If we're already on the UI thread, add a delay before a second notification
                    // This helps ensure the UI has time to process the first notification
                    System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() => 
                    {
                        OnPropertyChanged(nameof(Item));
                    }), System.Windows.Threading.DispatcherPriority.ContextIdle);
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
