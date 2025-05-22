using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using SketchBlade.Models;
using SketchBlade.Helpers;
using SketchBlade.Views.Controls.Recipes;

namespace SketchBlade.ViewModels
{
    public class InventoryViewModel : INotifyPropertyChanged
    {
        private readonly GameState _gameState;
        private readonly Action<string> _navigateAction;
        private Item? _selectedItem;
        private BitmapImage? _playerSprite;
        
        private CraftingViewModel? _craftingViewModel;
        public CraftingViewModel CraftingViewModel 
        { 
            get 
            {
                if (_craftingViewModel == null)
                {
                    try
                    {
                        _craftingViewModel = new CraftingViewModel(_gameState, _navigateAction, null);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ошибка при создании CraftingViewModel: {ex.Message}");
                        // Создаем с минимальными параметрами в случае ошибки
                        _craftingViewModel = new CraftingViewModel(_gameState, _navigateAction);
                    }
                }
                return _craftingViewModel;
            }
        }
        
        public event PropertyChangedEventHandler? PropertyChanged;
        
        // Properties
        public Inventory PlayerInventory => _gameState.Inventory;
        public Character PlayerCharacter => _gameState.Player ?? new Character();
        public GameState GameState => _gameState;
        
        // Player sprite image
        public BitmapImage? PlayerSprite
        {
            get
            {
                if (_playerSprite == null)
                {
                    string imagePath = "Assets/Images/player.png";
                    _playerSprite = Helpers.ImageHelper.LoadImage(imagePath);
                }
                return _playerSprite;
            }
        }
        
        // Equipment slots
        public ItemSlot HelmetSlot { get; } = new ItemSlot(SlotType.Helmet) { Index = -1 };
        public ItemSlot ChestplateSlot { get; } = new ItemSlot(SlotType.Chestplate) { Index = -2 };
        public ItemSlot LeggingsSlot { get; } = new ItemSlot(SlotType.Leggings) { Index = -3 }; 
        public ItemSlot WeaponSlot { get; } = new ItemSlot(SlotType.Weapon) { Index = -4 };
        public ItemSlot ShieldSlot { get; } = new ItemSlot(SlotType.Shield) { Index = -5 };
        
        // Quick slots
        public ObservableCollection<ItemSlot> QuickSlots { get; } = new ObservableCollection<ItemSlot>();
        
        // Craft slots
        public ObservableCollection<ItemSlot> CraftSlots { get; } = new ObservableCollection<ItemSlot>();
        public ItemSlot CraftResultSlot { get; } = new ItemSlot(SlotType.CraftResult) { Index = 0 };
        
        // Trash slot
        public ItemSlot TrashSlot { get; } = new ItemSlot(SlotType.Trash) { Index = -6 };
        
        // Main inventory slots
        public ObservableCollection<ItemSlot> InventorySlots { get; } = new ObservableCollection<ItemSlot>();
        
        // Player stats for display
        public string PlayerHealth => $"{PlayerCharacter.CurrentHealth}/{PlayerCharacter.GetTotalMaxHealth()}";
        public string PlayerDamage => PlayerCharacter.GetTotalAttack().ToString();
        public string PlayerDefense => PlayerCharacter.GetTotalDefense().ToString();
        
        // Selected item and popup visibility
        public Item? SelectedItem 
        {
            get => _selectedItem;
            set
            {
                _selectedItem = value;
                OnPropertyChanged(nameof(SelectedItem));
            }
        }
        
        // Commands
        public ICommand ShowRecipeBookCommand { get; private set; }
        public ICommand CloseRecipeBookCommand { get; private set; }
        public ICommand UseItemCommand { get; private set; }
        public ICommand NavigateCommand { get; private set; }
        public ICommand BattleOptionsCommand { get; private set; }
        public ICommand SplitStackCommand { get; private set; }
        public ICommand EquipItemCommand { get; private set; }
        public ICommand MoveToQuickSlotCommand { get; private set; }
        public ICommand MoveToInventorySlotCommand { get; private set; }
        public ICommand DiscardItemCommand { get; private set; }
        public ICommand UseQuickSlotCommand { get; private set; }
        public ICommand MoveToTrashCommand { get; private set; }
        public ICommand MoveToCraftCommand { get; }
        public ICommand MoveToCraftSlotCommand { get; }
        public ICommand RefreshCraftResultCommand { get; private set; }
        
        // Collection of available crafting recipes
        private ObservableCollection<CraftableRecipeViewModel> _availableCraftingRecipes = new ObservableCollection<CraftableRecipeViewModel>();
        public ObservableCollection<CraftableRecipeViewModel> AvailableCraftingRecipes
        {
            get => _availableCraftingRecipes;
            set
            {
                _availableCraftingRecipes = value;
                OnPropertyChanged();
            }
        }
        
        // Recipe book visibility property
        private bool _isRecipeBookVisible;
        public bool IsRecipeBookVisible
        {
            get => _isRecipeBookVisible;
            set
            {
                _isRecipeBookVisible = value;
                OnPropertyChanged(nameof(IsRecipeBookVisible));
            }
        }
        
        // Constructor
        public InventoryViewModel(GameState gameState, Action<string> navigateAction)
        {
            try
            {
                Console.WriteLine("Starting InventoryViewModel constructor...");
                _gameState = gameState ?? throw new ArgumentNullException(nameof(gameState), "GameState не может быть null");
                _navigateAction = navigateAction ?? throw new ArgumentNullException(nameof(navigateAction), "NavigateAction не может быть null");
                
                // Проверяем, инициализирован ли инвентарь в GameState
                if (_gameState.Inventory == null)
                {
                    Console.WriteLine("GameState.Inventory is null, creating a new one");
                    _gameState.Inventory = new Inventory();
                }
                
                Console.WriteLine("Initializing equipment slots...");
                // Connect the equipment slots to character equipment
                if (_gameState.Player != null)
                {
                    if (_gameState.Player.EquippedItems.TryGetValue(EquipmentSlot.Helmet, out var helmet))
                        HelmetSlot.Item = helmet;
                        
                    if (_gameState.Player.EquippedItems.TryGetValue(EquipmentSlot.Chestplate, out var chestplate))
                        ChestplateSlot.Item = chestplate;
                        
                    if (_gameState.Player.EquippedItems.TryGetValue(EquipmentSlot.Leggings, out var leggings))
                        LeggingsSlot.Item = leggings;
                        
                    if (_gameState.Player.EquippedItems.TryGetValue(EquipmentSlot.MainHand, out var weapon))
                        WeaponSlot.Item = weapon;
                        
                    if (_gameState.Player.EquippedItems.TryGetValue(EquipmentSlot.Shield, out var shield))
                        ShieldSlot.Item = shield;
                }
                
                Console.WriteLine("Initializing quick slots...");
                // Initialize quick slots (2 slots)
                for (int i = 0; i < 2; i++)
                {
                    QuickSlots.Add(new ItemSlot(SlotType.Quick) { Index = i });
                }
                
                Console.WriteLine("Initializing crafting slots...");
                // Initialize crafting slots (3x3 grid plus result)
                for (int i = 0; i < 9; i++)
                {
                    CraftSlots.Add(new ItemSlot(SlotType.Craft) { Index = i });
                }
                
                Console.WriteLine("Initializing inventory slots...");
                // Initialize inventory slots (5x3 grid = 15 slots)
                for (int i = 0; i < 15; i++)
                {
                    InventorySlots.Add(new ItemSlot(SlotType.Inventory) { Index = i });
                }
                
                Console.WriteLine("Connecting to game state inventory...");
                // Connect inventory slots to game state inventory
                if (_gameState.Inventory != null)
                {
                    // Убедимся, что у инвентаря есть необходимые коллекции
                    if (_gameState.Inventory.Items == null)
                    {
                        Console.WriteLine("WARNING: Inventory.Items is null, which shouldn't happen");
                    }
                    else
                    {
                        Console.WriteLine($"Inventory.Items count: {_gameState.Inventory.Items.Count}");
                    }
                    
                    if (_gameState.Inventory.QuickItems == null)
                    {
                        Console.WriteLine("WARNING: Inventory.QuickItems is null, which shouldn't happen");
                    }
                    else
                    {
                        Console.WriteLine($"Inventory.QuickItems count: {_gameState.Inventory.QuickItems.Count}");
                    }
                    
                    if (_gameState.Inventory.CraftItems == null)
                    {
                        Console.WriteLine("WARNING: Inventory.CraftItems is null, which shouldn't happen");
                    }
                    else
                    {
                        Console.WriteLine($"Inventory.CraftItems count: {_gameState.Inventory.CraftItems.Count}");
                    }
                    
                    // Проверим, что в коллекциях достаточно элементов
                    try
                    {
                        Console.WriteLine("Ensuring inventory has enough items...");
                        // This is handled by the Inventory class now
                        
                        Console.WriteLine("Loading inventory items...");
                        RefreshInventorySlots();
                        
                        Console.WriteLine("Loading quick slots...");
                        RefreshQuickSlots();
                        
                        Console.WriteLine("Loading craft slots...");
                        RefreshCraftSlots();
                        
                        Console.WriteLine("Loading equipment...");
                        RefreshEquipmentSlots();
                        
                        // Subscribe to inventory changes
                        _gameState.Inventory.PropertyChanged += (s, e) => 
                        {
                            if (e.PropertyName == nameof(Inventory.Items))
                            {
                                RefreshInventorySlots();
                            }
                            else if (e.PropertyName == nameof(Inventory.QuickItems))
                            {
                                RefreshQuickSlots();
                            }
                            else if (e.PropertyName == nameof(Inventory.CraftItems))
                            {
                                RefreshCraftSlots();
                            }
                        };
                        
                        // Subscribe to craft slot changes to update crafting
                        foreach (var craftSlot in CraftSlots)
                        {
                            craftSlot.PropertyChanged += CraftSlot_PropertyChanged;
                        }
                        
                        // Subscribe to player changes to update stats
                        if (_gameState.Player != null)
                        {
                            _gameState.Player.PropertyChanged += OnPlayerPropertyChanged;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error connecting to game state inventory: {ex.Message}");
                    }
                }
                
                Console.WriteLine("Creating commands...");
                // Create commands
                UseItemCommand = new RelayCommand<Item>(UseItem);
                NavigateCommand = new RelayCommand(Navigate);
                BattleOptionsCommand = new RelayCommand<object>(_ => StartBattle());
                SplitStackCommand = new RelayCommand<SplitStackEventArgs>(SplitItemStack);
                EquipItemCommand = new RelayCommand<EquipItemData>(EquipItem);
                MoveToQuickSlotCommand = new RelayCommand<MoveItemData>(MoveToQuickSlot);
                MoveToInventorySlotCommand = new RelayCommand<MoveItemData>(MoveToQuickSlot);
                DiscardItemCommand = new RelayCommand<int>(DiscardItem);
                UseQuickSlotCommand = new RelayCommand<object>(UseQuickSlot);
                MoveToTrashCommand = new RelayCommand<object>(MoveToTrash);
                ShowRecipeBookCommand = new RelayCommand<object>(_ => ShowRecipeBook());
                MoveToCraftCommand = new RelayCommand<MoveItemData>(MoveToCraft);
                MoveToCraftSlotCommand = new RelayCommand<MoveItemData>(MoveToCraftSlot);
                RefreshCraftResultCommand = new RelayCommand(RefreshCraftResult);
                
                Console.WriteLine("InventoryViewModel constructor completed successfully");
                
                // Initialize available crafting recipes
                UpdateCraftResult();
                
                // Subscribe to inventory changed event for automatic crafting updates
                _gameState.Inventory.InventoryChanged += Inventory_Changed;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in InventoryViewModel constructor: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                throw; // Rethrow to see where it's being caught
            }
        }
        
        // Navigate to the specified screen
        private void Navigate(object parameter)
        {
            string screen;
            
            Console.WriteLine($"InventoryViewModel.Navigate called with parameter: {parameter} of type {parameter?.GetType().Name}");
            
            // Convert string parameter to GameScreen enum
            if (parameter is string screenName)
            {
                Console.WriteLine($"Parameter is a string: {screenName}");
                
                // Special handling for crafting screen
                if (screenName == "CraftingView")
                {
                    Console.WriteLine("Special handling for CraftingView");
                    ShowCraftingScreen();
                    return;
                }
                
                // Handle main menu navigation explicitly
                if (screenName == "MainMenuView")
                {
                    Console.WriteLine("Navigating to MainMenuView from InventoryView");
                    _navigateAction("MainMenuView");
                    return;
                }
                
                screen = screenName;
            }
            else if (parameter is string directScreen)
            {
                Console.WriteLine($"Parameter is a direct string: {directScreen}");
                screen = directScreen;
            }
            else
            {
                Console.WriteLine($"Invalid navigation parameter: {parameter}");
                return;
            }
            
            Console.WriteLine($"Navigation target screen is: {screen}");
            
            // Clear trash slot when leaving inventory
            if (screen != "InventoryView" && TrashSlot.Item != null)
            {
                TrashSlot.Item = null;
                _gameState.Inventory.TrashItem = null;
            }
            
            // Log before navigation
            Console.WriteLine($"Calling _navigateAction with: {screen}");
            _navigateAction(screen);
            Console.WriteLine("Navigation action completed");
        }
        
        // Открыть экран крафта
        private void ShowCraftingScreen()
        {
            Console.WriteLine("Opening Crafting Screen");
            
            // Navigate to Crafting View using standard screen navigation
            _navigateAction("InventoryView");
            
            try
            {
                // Display crafting UI elements
                // In a real implementation, this would switch to a crafting view
                // Используем уже существующий экземпляр CraftingViewModel вместо создания нового
                var craftingViewModel = this.CraftingViewModel;
                Console.WriteLine("CraftingViewModel успешно инициализирован");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при инициализации CraftingViewModel: {ex.Message}");
            }
        }
        
        private void InitializeInventorySlots()
        {
            // Create quick slots
            for (int i = 0; i < 2; i++)
            {
                var slot = new ItemSlot(SlotType.Consumable) { Index = i };
                QuickSlots.Add(slot);
            }
            
            // Create main inventory slots (5x3 grid = 15 slots)
            for (int i = 0; i < 15; i++)
            {
                var slot = new ItemSlot(SlotType.Inventory) { Index = i };
                InventorySlots.Add(slot);
            }
        }
        
        private void UpdateInventoryDisplay()
        {
            // Clear all slots first
            foreach (var slot in InventorySlots)
            {
                slot.Item = null;
            }
            
            // Populate equipment slots if items are equipped
            if (_gameState.Player != null)
            {
                foreach (var equippedItem in _gameState.Player.EquippedItems)
                {
                    switch (equippedItem.Key)
                    {
                        case EquipmentSlot.Chestplate:
                            ChestplateSlot.Item = equippedItem.Value;
                            break;
                        case EquipmentSlot.Shield:
                            ShieldSlot.Item = equippedItem.Value;
                            break;
                        case EquipmentSlot.MainHand:
                            WeaponSlot.Item = equippedItem.Value;
                            break;
                        case EquipmentSlot.Helmet:
                            HelmetSlot.Item = equippedItem.Value;
                            break;
                        case EquipmentSlot.Leggings:
                            LeggingsSlot.Item = equippedItem.Value;
                            break;
                    }
                }
            }
            
            // Populate inventory slots with items
            for (int i = 0; i < _gameState.Inventory.Items.Count && i < InventorySlots.Count; i++)
            {
                InventorySlots[i].Item = _gameState.Inventory.Items[i];
            }
            
            // Populate quick slots
            for (int i = 0; i < _gameState.Inventory.QuickItems.Count && i < QuickSlots.Count; i++)
            {
                QuickSlots[i].Item = _gameState.Inventory.QuickItems[i];
            }
            
            // Set trash slot
            TrashSlot.Item = _gameState.Inventory.TrashItem;
            
            // Update character stats
            OnPropertyChanged(nameof(PlayerHealth));
            OnPropertyChanged(nameof(PlayerDefense));
            OnPropertyChanged(nameof(PlayerDamage));
        }
        
        // Use an item
        private void UseItem(Item item)
        {
            if (item == null || !item.IsUsable || _gameState.Player == null)
                return;
                
            bool wasUsed = item.Use(_gameState.Player);
            
            if (wasUsed)
            {
                // Decrement stack size or remove if last
                if (item.StackSize > 1)
                {
                    item.StackSize--;
                }
                else
                {
                    // Remove item from inventory or quick slot
                    bool foundInQuickSlot = false;
                    
                    // Check quick slots first
                    for (int i = 0; i < QuickSlots.Count; i++)
                    {
                        if (QuickSlots[i].Item == item)
                        {
                            QuickSlots[i].Item = null;
                            foundInQuickSlot = true;
                            break;
                        }
                    }
                    
                    // If not in quick slot, remove from inventory
                    if (!foundInQuickSlot)
                    {
                        _gameState.Inventory.RemoveItem(item);
                    }
                }
                
                // Show message about item effect
                string effectMessage = GetItemEffectMessage(item);
                System.Windows.MessageBox.Show(effectMessage, "Предмет использован", 
                                             System.Windows.MessageBoxButton.OK, 
                                             System.Windows.MessageBoxImage.Information);
                
                // Update UI
                UpdateInventoryDisplay();
                OnPropertyChanged(nameof(PlayerHealth));
                OnPropertyChanged(nameof(PlayerDamage));
                OnPropertyChanged(nameof(PlayerDefense));
            }
        }
        
        // Get a description of the item effect
        private string GetItemEffectMessage(Item item)
        {
            switch (item.Name.ToLower())
            {
                case "health potion":
                case "зелье здоровья":
                    return "Вы выпили зелье здоровья и восстановили 30% здоровья.";
                    
                case "mana potion":
                case "зелье маны":
                    return "Вы выпили зелье маны и восстановили 30% маны.";
                    
                case "strength potion":
                case "зелье силы":
                    return "Вы выпили зелье силы и временно увеличили свою атаку.";
                    
                case "defense potion":
                case "зелье защиты":
                    return "Вы выпили зелье защиты и временно увеличили свою защиту.";
                    
                case "bandage":
                case "бинт":
                    return "Вы использовали бинт и восстановили немного здоровья.";
                    
                case "antidote":
                case "противоядие":
                    return "Вы использовали противоядие.";
                    
                default:
                    return $"Вы использовали {item.Name}.";
            }
        }
        
        // Start battle
        private void StartBattle()
        {
            // Create options for battle
            var result = System.Windows.MessageBox.Show(
                "Выберите тип сражения:\n\nДа - Битва с мобами\nНет - Битва с героем",
                "Выбор битвы",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);
            
            if (result == System.Windows.MessageBoxResult.Yes)
            {
                // Battle with mobs
                _gameState.StartBattleWithMobs();
            }
            else
            {
                // Battle with hero
                _gameState.StartBattleWithHero();
            }
            
            _navigateAction("BattleView");
        }
        
        // Split an item stack
        private void SplitItemStack(SplitStackEventArgs args)
        {
            if (args == null || args.SourceItem == null || args.Amount <= 0 || 
                args.Amount >= args.SourceItem.StackSize)
                return;
                
            try
            {
                Console.WriteLine($"Splitting stack: {args.SourceItem.Name}, amount: {args.Amount}/{args.SourceItem.StackSize}");
                
                // Если указан целевой слот, используем его
                if (!string.IsNullOrEmpty(args.TargetSlotType) && args.TargetSlotIndex >= 0)
                {
                    // Получаем целевой предмет (может быть null)
                    Item? targetItem = GetItemFromSlot(args.TargetSlotType, args.TargetSlotIndex);
                    
                    // Если целевой слот пуст, создаем новый стак
                    if (targetItem == null)
                    {
                        // Создаем новый предмет для новой стопки
                        Item newStackItem = args.SourceItem.Clone();
                        newStackItem.StackSize = args.Amount;
                        
                        // Уменьшаем исходный стек
                        args.SourceItem.StackSize -= args.Amount;
                        
                        // Добавляем новую стопку в целевой слот
                        SetItemToSlot(args.TargetSlotType, args.TargetSlotIndex, newStackItem);
                        
                        Console.WriteLine($"Split complete. Original stack: {args.SourceItem.StackSize}, New stack: {newStackItem.StackSize}");
                    }
                    // Если целевой слот содержит такой же предмет, добавляем к стаку
                    else if (targetItem.Name == args.SourceItem.Name && 
                             targetItem.Type == args.SourceItem.Type && 
                             targetItem.Rarity == args.SourceItem.Rarity && 
                             targetItem.IsStackable)
                    {
                        // Определяем, сколько можно добавить к существующему стаку
                        int canAdd = targetItem.MaxStackSize - targetItem.StackSize;
                        int actualAdd = Math.Min(canAdd, args.Amount);
                        
                        if (actualAdd > 0)
                        {
                            // Увеличиваем целевой стак
                            targetItem.StackSize += actualAdd;
                            
                            // Уменьшаем исходный стак
                            args.SourceItem.StackSize -= actualAdd;
                            
                            Console.WriteLine($"Added to existing stack. Target stack: {targetItem.StackSize}, Original stack: {args.SourceItem.StackSize}");
                        }
                    }
                }
                // Иначе используем стандартный метод разделения стака
                else
                {
                    // Check if inventory has space
                    if (_gameState.Inventory.Items.Count >= _gameState.Inventory.MaxCapacity)
                    {
                        System.Windows.MessageBox.Show("Нет места в инвентаре для разделения стака.", 
                                                     "Инвентарь полон", 
                                                     System.Windows.MessageBoxButton.OK, 
                                                     System.Windows.MessageBoxImage.Warning);
                        return;
                    }
                    
                    // Split the stack using the standard method
                    if (_gameState.Inventory.SplitStack(args.SourceItem, args.Amount))
                    {
                        Console.WriteLine("Split stack using standard method");
                    }
                }
                
                // Update inventory display
                UpdateInventoryDisplay();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SplitItemStack: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
        
        // Equip an item
        public void EquipItem(EquipItemData data)
        {
            if (data == null || data.InventoryIndex < 0 || data.InventoryIndex >= InventorySlots.Count)
                return;

            try
            {
                Console.WriteLine($"EquipItem called: InventoryIndex={data.InventoryIndex}, EquipmentSlot={data.EquipmentSlot}");
                
                // Get the item from inventory
                Item? itemToEquip = InventorySlots[data.InventoryIndex].Item;
                if (itemToEquip == null)
                {
                    Console.WriteLine("EquipItem: Inventory item is null");
                    return;
                }
                
                Console.WriteLine($"Equipping item: {itemToEquip.Name}, Type: {itemToEquip.Type}");
                
                // Check if item type is compatible with the equipment slot
                if (!CanEquipItemToSlot(itemToEquip, data.EquipmentSlot))
                {
                    Console.WriteLine($"Cannot equip {itemToEquip.Name} to {data.EquipmentSlot} slot - incompatible types");
                    System.Windows.MessageBox.Show(
                        $"Предмет '{itemToEquip.Name}' нельзя экипировать в слот {data.EquipmentSlot}.",
                        "Невозможно экипировать",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Information);
                    return;
                }
                
                // Create MoveItemData for the specific equipment slot
                MoveItemData moveData = new MoveItemData
                {
                    SourceType = "Inventory",
                    SourceIndex = data.InventoryIndex,
                    TargetType = data.EquipmentSlot,
                    TargetIndex = 0 // Equipment slots have index 0
                };
                
                // Use the existing movement logic
                MoveToQuickSlot(moveData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in EquipItem: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                System.Windows.MessageBox.Show(
                    $"Ошибка при экипировке предмета: {ex.Message}",
                    "Ошибка",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }
        
        // Check if an item can be equipped to a specific slot
        private bool CanEquipItemToSlot(Item item, string equipmentSlot)
        {
            if (item == null)
                return false;
                
            switch (equipmentSlot)
            {
                case "Weapon":
                    return item.Type == ItemType.Weapon;
                case "Shield":
                    return item.Type == ItemType.Shield;
                case "Armor":
                    return item.Type == ItemType.Chestplate;
                case "Helmet":
                    return item.Type == ItemType.Helmet;
                case "Leggings":
                    return item.Type == ItemType.Leggings;
                default:
                    return false;
            }
        }
        
        // Move an item to a quick slot
        public void MoveToQuickSlot(MoveItemData data)
        {
            if (data == null || string.IsNullOrEmpty(data.SourceType) || string.IsNullOrEmpty(data.TargetType))
            {
                Console.WriteLine("MoveToQuickSlot: Invalid data");
                return;
            }
            
            try
            {
                Console.WriteLine($"Moving from {data.SourceType}[{data.SourceIndex}] to {data.TargetType}[{data.TargetIndex}]");
                
                // Получаем источник и цель предметов для проверки типов
                Item? sourceItem = GetItemFromSlot(data.SourceType, data.SourceIndex);
                if (sourceItem == null)
                {
                    Console.WriteLine("Source item is null");
                    return;
                }
                
                Console.WriteLine($"Source item: {sourceItem.Name} (Type: {sourceItem.Type})");
                
                // Get target item (can be null)
                Item? targetItem = GetItemFromSlot(data.TargetType, data.TargetIndex);
                if (targetItem != null)
                {
                    Console.WriteLine($"Target item: {targetItem.Name} (Type: {targetItem.Type})");
                }
                else
                {
                    Console.WriteLine("Target slot is empty");
                }
                
                // Movement validation exceptions first
                bool canMoveTo = false;
                
                // Moving between the same slot types is always allowed
                if (data.SourceType == data.TargetType)
                {
                    canMoveTo = true;
                    Console.WriteLine("Same slot type, allowing move");
                }
                // Moving to inventory is always allowed
                else if (data.TargetType == "Inventory")
                {
                    canMoveTo = true;
                    Console.WriteLine("Moving to inventory, always allowed");
                }
                // Moving to trash is always allowed
                else if (data.TargetType == "Trash")
                {
                    canMoveTo = true;
                    Console.WriteLine("Moving to trash, always allowed");
                }
                // For other slot types, check item compatibility
                else
                {
                    canMoveTo = CanMoveItemToSlot(sourceItem, data.TargetType);
                    Console.WriteLine($"Item type check for move: {sourceItem.Type} to slot {data.TargetType}: {canMoveTo}");
                }
                
                if (!canMoveTo)
                {
                    Console.WriteLine($"Cannot move {sourceItem.Name} to {data.TargetType} slot");
                    System.Windows.MessageBox.Show(
                        $"Предмет '{sourceItem.Name}' нельзя поместить в этот слот.",
                        "Невозможно переместить",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Information);
                    return;
                }
                
                Console.WriteLine($"Move allowed: {sourceItem.Name} to {data.TargetType}[{data.TargetIndex}]");
                
                // Обработка перемещения в корзину (удаление предмета)
                if (data.TargetType == "Trash")
                {
                    // Создаем копию предмета, чтобы показать его в корзине
                    TrashSlot.Item = sourceItem.Clone();
                    
                    // Удаляем предмет из исходного слота
                    RemoveItemFromSlot(data.SourceType, data.SourceIndex);
                    Console.WriteLine($"Item {sourceItem.Name} moved to trash");
                    
                    // Обновляем отображение
                    RefreshAllSlots();
                    return;
                }
                
                // Создаем клоны для избежания проблем с ссылками
                Item? sourceItemClone = sourceItem.Clone();
                Item? targetItemClone = targetItem?.Clone();
                
                // Обработка складирования предметов
                if (targetItem != null && sourceItem != null &&
                    targetItem.Name == sourceItem.Name &&
                    targetItem.Type == sourceItem.Type && 
                    targetItem.Rarity == sourceItem.Rarity &&
                    targetItem.IsStackable && sourceItem.IsStackable)
                {
                    // Вычисляем, сколько можно добавить к существующему стеку
                    int spaceAvailable = targetItem.MaxStackSize - targetItem.StackSize;
                    
                    if (spaceAvailable >= sourceItem.StackSize)
                    {
                        // Целевой слот может вместить все предметы из исходного
                        targetItemClone.StackSize += sourceItem.StackSize;
                        // Удаляем исходный предмет
                        RemoveItemFromSlot(data.SourceType, data.SourceIndex);
                        // Обновляем целевой слот
                        SetItemToSlot(data.TargetType, data.TargetIndex, targetItemClone);
                        Console.WriteLine($"Stacked all {sourceItem.StackSize} items onto target stack");
                    }
                    else if (spaceAvailable > 0)
                    {
                        // Целевой слот может вместить только часть предметов
                        targetItemClone.StackSize += spaceAvailable;
                        sourceItemClone.StackSize -= spaceAvailable;
                        
                        // Обновляем оба слота
                        RemoveItemFromSlot(data.TargetType, data.TargetIndex);
                        SetItemToSlot(data.TargetType, data.TargetIndex, targetItemClone);
                        
                        RemoveItemFromSlot(data.SourceType, data.SourceIndex);
                        SetItemToSlot(data.SourceType, data.SourceIndex, sourceItemClone);
                        
                        Console.WriteLine($"Stacked {spaceAvailable} items onto target stack, {sourceItemClone.StackSize} remain in source");
                    }
                    else
                    {
                        // Стек полон, обмениваем предметы
                        RemoveItemFromSlot(data.SourceType, data.SourceIndex);
                        RemoveItemFromSlot(data.TargetType, data.TargetIndex);
                        
                        SetItemToSlot(data.SourceType, data.SourceIndex, targetItemClone);
                        SetItemToSlot(data.TargetType, data.TargetIndex, sourceItemClone);
                        
                        Console.WriteLine("Target stack is full, swapped items");
                    }
                }
                else
                {
                    // Обычный обмен предметами
                    RemoveItemFromSlot(data.SourceType, data.SourceIndex);
                    RemoveItemFromSlot(data.TargetType, data.TargetIndex);
                    
                    SetItemToSlot(data.TargetType, data.TargetIndex, sourceItemClone);
                    if (targetItemClone != null)
                    {
                        SetItemToSlot(data.SourceType, data.SourceIndex, targetItemClone);
                    }
                    
                    Console.WriteLine($"Swapped items between slots: {data.SourceType}[{data.SourceIndex}] and {data.TargetType}[{data.TargetIndex}]");
                }
                
                // Обновляем отображение всех слотов
                RefreshAllSlots();
                
                // Обновляем свойства, связанные с экипировкой
                OnPropertyChanged(nameof(PlayerHealth));
                OnPropertyChanged(nameof(PlayerDamage));
                OnPropertyChanged(nameof(PlayerDefense));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in MoveToQuickSlot: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                System.Windows.MessageBox.Show(
                    $"Ошибка при перемещении предмета: {ex.Message}",
                    "Ошибка",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }
        
        // Helper methods for slot operations
        public Item? GetItemFromSlot(string slotType, int index)
        {
            try
            {
                switch (slotType)
                {
                    case "Inventory":
                        return _gameState.Inventory.GetItemAt(index);
                    case "Quick":
                        return _gameState.Inventory.GetQuickItemAt(index);
                    case "Helmet":
                        return HelmetSlot.Item;
                    case "Chestplate":
                        return ChestplateSlot.Item;
                    case "Leggings":
                        return LeggingsSlot.Item;
                    case "Weapon":
                        return WeaponSlot.Item;
                    case "Shield":
                        return ShieldSlot.Item;
                    case "Trash":
                        return TrashSlot.Item;
                    case "Craft":
                        if (index >= 0 && index < _gameState.Inventory.CraftItems.Count)
                            return _gameState.Inventory.GetCraftItemAt(index);
                        return null;
                    case "CraftResult":
                        return CraftResultSlot.Item;
                    default:
                        Console.WriteLine($"WARNING: Unknown slot type in GetItemFromSlot: {slotType}");
                        return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting item from slot: {ex.Message}");
                return null;
            }
        }
        
        public void SetItemToSlot(string slotType, int index, Item? item)
        {
            try
            {
                // Get matching slot
                switch (slotType)
                {
                    case "Inventory":
                        if (index >= 0 && index < InventorySlots.Count)
                        {
                            InventorySlots[index].Item = item;
                            _gameState.Inventory.SetItemAt(index, item);
                        }
                        break;
                    case "Quick":
                        if (index >= 0 && index < QuickSlots.Count)
                        {
                            QuickSlots[index].Item = item;
                            _gameState.Inventory.SetQuickItemAt(index, item);
                        }
                        break;
                    case "Craft":
                        if (index >= 0 && index < CraftSlots.Count)
                        {
                            CraftSlots[index].Item = item;
                            _gameState.Inventory.SetCraftItemAt(index, item);
                            UpdateCraftResult(); // Immediately update craft result when craft slot changes
                        }
                        break;
                    case "Helmet":
                        HelmetSlot.Item = item;
                        UpdatePlayerEquipment("Helmet", item);
                        break;
                    case "Chestplate":
                        ChestplateSlot.Item = item;
                        UpdatePlayerEquipment("Chestplate", item);
                        break;
                    case "Leggings":
                        LeggingsSlot.Item = item;
                        UpdatePlayerEquipment("Leggings", item);
                        break;
                    case "Weapon":
                        WeaponSlot.Item = item;
                        UpdatePlayerEquipment("Weapon", item);
                        break;
                    case "Shield":
                        ShieldSlot.Item = item;
                        UpdatePlayerEquipment("Shield", item);
                        break;
                    case "Trash":
                        TrashSlot.Item = item;
                        _gameState.Inventory.TrashItem = item;
                        break;
                    case "CraftResult":
                        CraftResultSlot.Item = item;
                        break;
                    default:
                        Console.WriteLine($"Unknown slot type: {slotType}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting item to slot {slotType}[{index}]: {ex.Message}");
            }
        }
        
        public void RemoveItemFromSlot(string slotType, int index)
        {
            try
            {
                switch (slotType)
                {
                    case "Inventory":
                        if (index >= 0 && index < InventorySlots.Count)
                        {
                            _gameState.Inventory.SetItemAt(index, null);
                            InventorySlots[index].Item = null;
                            Console.WriteLine($"RemoveItemFromSlot: Cleared Inventory[{index}]");
                        }
                        break;
                        
                    case "Quick":
                        if (index >= 0 && index < QuickSlots.Count)
                        {
                            _gameState.Inventory.SetQuickItemAt(index, null);
                            QuickSlots[index].Item = null;
                            Console.WriteLine($"RemoveItemFromSlot: Cleared Quick[{index}]");
                        }
                        break;
                        
                    case "Helmet":
                        HelmetSlot.Item = null;
                        UpdatePlayerEquipment("Helmet", null);
                        Console.WriteLine("RemoveItemFromSlot: Cleared Helmet");
                        break;
                        
                    case "Chestplate":
                        ChestplateSlot.Item = null;
                        UpdatePlayerEquipment("Chestplate", null);
                        Console.WriteLine("RemoveItemFromSlot: Cleared Chestplate");
                        break;
                        
                    case "Leggings":
                        LeggingsSlot.Item = null;
                        UpdatePlayerEquipment("Leggings", null);
                        Console.WriteLine("RemoveItemFromSlot: Cleared Leggings");
                        break;
                        
                    case "Weapon":
                        WeaponSlot.Item = null;
                        UpdatePlayerEquipment("Weapon", null);
                        Console.WriteLine("RemoveItemFromSlot: Cleared Weapon");
                        break;
                        
                    case "Shield":
                        ShieldSlot.Item = null;
                        UpdatePlayerEquipment("Shield", null);
                        Console.WriteLine("RemoveItemFromSlot: Cleared Shield");
                        break;
                        
                    case "Trash":
                        TrashSlot.Item = null;
                        _gameState.Inventory.TrashItem = null;
                        Console.WriteLine("RemoveItemFromSlot: Cleared Trash");
                        break;
                        
                    case "Craft":
                        if (index >= 0 && index < _gameState.Inventory.CraftItems.Count)
                        {
                            _gameState.Inventory.SetCraftItemAt(index, null);
                            if (index < CraftSlots.Count)
                            {
                                CraftSlots[index].Item = null;
                            }
                            Console.WriteLine($"RemoveItemFromSlot: Cleared Craft[{index}]");
                        }
                        break;
                        
                    case "CraftResult":
                        CraftResultSlot.Item = null;
                        Console.WriteLine("RemoveItemFromSlot: Cleared CraftResult");
                        break;
                        
                    default:
                        Console.WriteLine($"WARNING: Unknown slot type in RemoveItemFromSlot: {slotType}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error removing item from slot: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
        
        // Check if an item can be moved to a specific slot type
        private bool CanMoveItemToSlot(Item item, string targetSlotType)
        {
            try
            {
                if (item == null)
                {
                    Console.WriteLine("CanMoveItemToSlot: Item is null");
                    return false;
                }
                
                switch (targetSlotType)
                {
                    case "Inventory":
                        Console.WriteLine("CanMoveItemToSlot: Any item can go to inventory");
                        return true; // Any item can go to inventory
                    case "Quick":
                        Console.WriteLine($"CanMoveItemToSlot: Checking if {item.Name} can go to quick slot (Type={item.Type})");
                        return item.Type == ItemType.Consumable; // Only consumables can go to quick slots
                    case "Helmet":
                        Console.WriteLine($"CanMoveItemToSlot: Checking if {item.Name} can go to helmet slot (Type={item.Type})");
                        return item.Type == ItemType.Helmet;
                    case "Chestplate":
                        Console.WriteLine($"CanMoveItemToSlot: Checking if {item.Name} can go to chestplate slot (Type={item.Type})");
                        return item.Type == ItemType.Chestplate;
                    case "Leggings":
                        Console.WriteLine($"CanMoveItemToSlot: Checking if {item.Name} can go to leggings slot (Type={item.Type})");
                        return item.Type == ItemType.Leggings;
                    case "Weapon":
                        Console.WriteLine($"CanMoveItemToSlot: Checking if {item.Name} can go to weapon slot (Type={item.Type})");
                        return item.Type == ItemType.Weapon;
                    case "Shield":
                        Console.WriteLine($"CanMoveItemToSlot: Checking if {item.Name} can go to shield slot (Type={item.Type})");
                        return item.Type == ItemType.Shield;
                    case "Craft":
                        Console.WriteLine("CanMoveItemToSlot: Any item can go to craft");
                        return true; // Any item can go to craft
                    case "Trash":
                        Console.WriteLine("CanMoveItemToSlot: Any item can go to trash");
                        return true; // Any item can go to trash
                    default:
                        Console.WriteLine($"CanMoveItemToSlot: Unknown slot type: {targetSlotType}");
                        return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in CanMoveItemToSlot: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }
        
        // Discard an item from inventory
        private void DiscardItem(int inventoryIndex)
        {
            if (inventoryIndex < 0 || inventoryIndex >= InventorySlots.Count)
                return;
                
            // Get the item
            Item itemToDiscard = InventorySlots[inventoryIndex].Item;
            if (itemToDiscard == null)
                return;
                
            // Ask for confirmation if item is rare or higher
            if (itemToDiscard.Rarity >= ItemRarity.Rare)
            {
                var result = System.Windows.MessageBox.Show(
                    $"Вы уверены, что хотите выбросить {itemToDiscard.Name}?\nЭтот предмет имеет редкость {itemToDiscard.Rarity}.",
                    "Подтверждение",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Warning);
                    
                if (result != System.Windows.MessageBoxResult.Yes)
                    return;
            }
            
            // Remove from inventory
            _gameState.Inventory.RemoveItem(itemToDiscard);
            
            // Update UI
            UpdateInventoryDisplay();
        }
        
        // Property changed notification
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
        // Debug method to create test items
        public void GenerateTestItems()
        {
            // Clear existing inventory
            _gameState.Inventory.Clear();
            
            // Add weapons
            var woodenSword = ItemFactory.CreateWoodenWeapon();
            var ironSword = ItemFactory.CreateIronWeapon();
            var goldSword = ItemFactory.CreateGoldWeapon();
            var luminiteSword = ItemFactory.CreateLuminiteWeapon();
            
            // Add armor
            var woodenHelmet = ItemFactory.CreateWoodenArmor(ItemSlotType.Head);
            var ironChest = ItemFactory.CreateIronArmor(ItemSlotType.Chest);
            var goldLegs = ItemFactory.CreateGoldArmor(ItemSlotType.Legs);
            var luminiteArmor = ItemFactory.CreateLuminiteArmor(ItemSlotType.Chest);
            
            // Add consumables
            var healthPotion = ItemFactory.CreateHealingPotion(5);
            var ragePotion = ItemFactory.CreateRagePotion(3);
            var bomb = ItemFactory.CreateBomb(2);
            var pillow = ItemFactory.CreatePillow(1);
            var poisonedShuriken = ItemFactory.CreatePoisonedShuriken(8);
            var invulnerabilityPotion = ItemFactory.CreateInvulnerabilityPotion(4);
            
            // Add to inventory
            _gameState.Inventory.AddItem(woodenSword);
            _gameState.Inventory.AddItem(ironSword);
            _gameState.Inventory.AddItem(goldSword);
            _gameState.Inventory.AddItem(luminiteSword);
            _gameState.Inventory.AddItem(woodenHelmet);
            _gameState.Inventory.AddItem(ironChest);
            _gameState.Inventory.AddItem(goldLegs);
            _gameState.Inventory.AddItem(luminiteArmor);
            _gameState.Inventory.AddItem(healthPotion);
            _gameState.Inventory.AddItem(ragePotion);
            _gameState.Inventory.AddItem(bomb);
            _gameState.Inventory.AddItem(pillow);
            _gameState.Inventory.AddItem(poisonedShuriken);
            _gameState.Inventory.AddItem(invulnerabilityPotion);
            
            // Add items to quick slots
            _gameState.Inventory.SetQuickItemAt(0, ItemFactory.CreateHealingPotion(3));
            _gameState.Inventory.SetQuickItemAt(1, ItemFactory.CreateBomb(1));
            
            // Equip some items
            if (_gameState.Player != null)
            {
                _gameState.Player.EquipItem(woodenHelmet);
                _gameState.Player.EquipItem(ironChest);
                _gameState.Player.EquipItem(goldLegs);
                _gameState.Player.EquipItem(woodenSword);
            }
            
            // Update display
            UpdateInventoryDisplay();
        }
        
        // Refresh inventory slot items from game state
        private void RefreshInventorySlots()
        {
            try
            {
                // Убедимся, что у нас достаточно слотов
                if (InventorySlots.Count < 15)
                {
                    Console.WriteLine($"RefreshInventorySlots: InventorySlots содержит только {InventorySlots.Count} элементов, добавляем недостающие");
                    while (InventorySlots.Count < 15)
                    {
                        InventorySlots.Add(new ItemSlot(SlotType.Inventory) { Index = InventorySlots.Count });
                    }
                }
                
                // Убедимся, что в инвентаре модели достаточно слотов
                if (_gameState.Inventory.Items.Count < 15)
                {
                    Console.WriteLine($"RefreshInventorySlots: _gameState.Inventory.Items содержит только {_gameState.Inventory.Items.Count} элементов");
                }
                
                // Обновляем слоты инвентаря из модели
                for (int i = 0; i < Math.Min(_gameState.Inventory.Items.Count, InventorySlots.Count); i++)
                {
                    var item = _gameState.Inventory.GetItemAt(i);
                    InventorySlots[i].Item = item;
                    if (item != null)
                    {
                        // Принудительно уведомляем об изменении предмета
                        item.NotifyPropertyChanged("StackSize");
                        item.NotifyPropertyChanged("Icon"); 
                    }
                }
                
                // Оповещаем об обновлении всей коллекции
                OnPropertyChanged(nameof(InventorySlots));
                
                // Update player stats that might be affected by inventory changes
                OnPropertyChanged(nameof(PlayerHealth));
                OnPropertyChanged(nameof(PlayerDefense));
                OnPropertyChanged(nameof(PlayerDamage));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error refreshing inventory slots: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
        
        // Refresh quick slot items from game state
        private void RefreshQuickSlots()
        {
            try
            {
                for (int i = 0; i < Math.Min(QuickSlots.Count, 2); i++)
                {
                    var item = _gameState.Inventory.GetQuickItemAt(i);
                    QuickSlots[i].Item = item;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error refreshing quick slots: {ex.Message}");
            }
        }
        
        // Refresh craft slot items from game state
        private void RefreshCraftSlots()
        {
            try
            {
                // In the new crafting system, we just need to update the available recipe
                UpdateCraftResult();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error refreshing craft slots: {ex.Message}");
            }
        }
        
        // Refresh equipment slots from player
        private void RefreshEquipmentSlots()
        {
            try
            {
                if (_gameState.Player != null)
                {
                    if (_gameState.Player.EquippedItems.TryGetValue(EquipmentSlot.Helmet, out var helmet))
                        HelmetSlot.Item = helmet;
                    else
                        HelmetSlot.Item = null;
                        
                    if (_gameState.Player.EquippedItems.TryGetValue(EquipmentSlot.Chestplate, out var chestplate))
                        ChestplateSlot.Item = chestplate;
                    else
                        ChestplateSlot.Item = null;
                        
                    if (_gameState.Player.EquippedItems.TryGetValue(EquipmentSlot.Leggings, out var leggings))
                        LeggingsSlot.Item = leggings;
                    else
                        LeggingsSlot.Item = null;
                        
                    if (_gameState.Player.EquippedItems.TryGetValue(EquipmentSlot.MainHand, out var weapon))
                        WeaponSlot.Item = weapon;
                    else
                        WeaponSlot.Item = null;
                        
                    if (_gameState.Player.EquippedItems.TryGetValue(EquipmentSlot.Shield, out var shield))
                        ShieldSlot.Item = shield;
                    else
                        ShieldSlot.Item = null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error refreshing equipment slots: {ex.Message}");
            }
        }

        // Handle player property changes
        private void OnPlayerPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Character.EquippedItems) || 
                e.PropertyName == nameof(Character.CurrentHealth) ||
                e.PropertyName == nameof(Character.MaxHealth) ||
                e.PropertyName == nameof(Character.Attack) ||
                e.PropertyName == nameof(Character.Defense))
            {
                // Refresh equipment and stats
                RefreshEquipmentSlots();
                OnPropertyChanged(nameof(PlayerHealth));
                OnPropertyChanged(nameof(PlayerDamage));
                OnPropertyChanged(nameof(PlayerDefense));
            }
        }

        // Method to move items between different slot types
        public void MoveItemBetweenSlots(string sourceType, int sourceIndex, string targetType, int targetIndex)
        {
            try
            {
                Console.WriteLine($"MoveItemBetweenSlots: Moving from {sourceType}[{sourceIndex}] to {targetType}[{targetIndex}]");
                
                // Get source and target items (create copies to avoid reference issues)
                Item? sourceItem = GetItemFromSlot(sourceType, sourceIndex);
                Item? targetItem = GetItemFromSlot(targetType, targetIndex);
                
                if (sourceItem == null)
                {
                    Console.WriteLine("MoveItemBetweenSlots: Source item is null, nothing to move");
                    return; // Nothing to move
                }
                
                // Create clones of the items to avoid direct reference modifications
                Item? sourceItemClone = sourceItem.Clone();
                Item? targetItemClone = targetItem?.Clone();
                
                // Check if target slot can accept the source item
                bool canAcceptItem = CanMoveItemToSlot(sourceItem, targetType);
                if (!canAcceptItem)
                {
                    Console.WriteLine($"Target slot type {targetType} cannot accept item of type {sourceItem.Type}");
                    return;
                }
                
                // Special handling for stacking items
                if (targetItem != null && sourceItem.Name == targetItem.Name && 
                    sourceItem.IsStackable && targetItem.IsStackable &&
                    sourceItem.Rarity == targetItem.Rarity)
                {
                    // Try to stack items
                    int available = targetItem.MaxStackSize - targetItem.StackSize;
                    if (available > 0)
                    {
                        int amountToMove = Math.Min(available, sourceItem.StackSize);
                        Console.WriteLine($"Stacking {amountToMove} items from {sourceType}[{sourceIndex}] to {targetType}[{targetIndex}]");
                        
                        // Update the target item with increased stack size
                        targetItemClone = targetItem.Clone();
                        targetItemClone.StackSize += amountToMove;
                        
                        // Update source item stack or remove if empty
                        if (amountToMove >= sourceItem.StackSize)
                        {
                            // All items were moved
                            sourceItemClone = null;
                        }
                        else
                        {
                            // Some items remain in source
                            sourceItemClone = sourceItem.Clone();
                            sourceItemClone.StackSize -= amountToMove;
                        }
                        
                        // Apply the changes
                        SetItemToSlot(targetType, targetIndex, targetItemClone);
                        SetItemToSlot(sourceType, sourceIndex, sourceItemClone);
                        
                        // Update slot displays
                        RefreshAllSlots();
                        return;
                    }
                }
                
                // Regular item swap: set items in reverse order to avoid potential overwrites
                Console.WriteLine($"Swapping items between {sourceType}[{sourceIndex}] and {targetType}[{targetIndex}]");
                
                // First, remove both items to avoid reference issues
                RemoveItemFromSlot(sourceType, sourceIndex);
                RemoveItemFromSlot(targetType, targetIndex);
                
                // Then set the items in their new positions
                if (targetItemClone != null)
                    SetItemToSlot(sourceType, sourceIndex, targetItemClone);
                else
                    SetItemToSlot(sourceType, sourceIndex, null);
                    
                SetItemToSlot(targetType, targetIndex, sourceItemClone);
                
                // Update equipment on player if applicable
                if (targetType == "Helmet" || targetType == "Chestplate" || targetType == "Leggings" || 
                    targetType == "Weapon" || targetType == "Shield")
                {
                    UpdatePlayerEquipment(targetType, sourceItemClone);
                }
                
                if (sourceType == "Helmet" || sourceType == "Chestplate" || sourceType == "Leggings" || 
                    sourceType == "Weapon" || sourceType == "Shield")
                {
                    UpdatePlayerEquipment(sourceType, targetItemClone);
                }
                
                // Refresh all slots to ensure UI consistency
                RefreshAllSlots();
                
                Console.WriteLine("MoveItemBetweenSlots: Operation completed successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error moving item between slots: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
        
        // Update player equipment when items are equipped/unequipped
        private void UpdatePlayerEquipment(string slotType, Item? item)
        {
            if (_gameState.Player == null) return;
            
            EquipmentSlot equipmentSlot;
            switch (slotType)
            {
                case "Helmet":
                    equipmentSlot = EquipmentSlot.Helmet;
                    break;
                case "Chestplate":
                    equipmentSlot = EquipmentSlot.Chestplate;
                    break;
                case "Leggings":
                    equipmentSlot = EquipmentSlot.Leggings;
                    break;
                case "Weapon":
                    equipmentSlot = EquipmentSlot.MainHand;
                    break;
                case "Shield":
                    equipmentSlot = EquipmentSlot.Shield;
                    break;
                default:
                    return;
            }
            
            if (item == null)
            {
                _gameState.Player.EquippedItems.Remove(equipmentSlot);
            }
            else
            {
                _gameState.Player.EquippedItems[equipmentSlot] = item;
            }
            
            // Update character stats
            _gameState.Player.CalculateStats();
        }
        
        // Refresh all slots to ensure UI consistency
        private void RefreshAllSlots()
        {
            try
            {
                // Обеспечиваем правильное количество слотов в коллекциях
                if (InventorySlots.Count < 15)
                {
                    Console.WriteLine($"RefreshAllSlots: InventorySlots содержит только {InventorySlots.Count} элементов, добавляем недостающие");
                    while (InventorySlots.Count < 15)
                    {
                        InventorySlots.Add(new ItemSlot(SlotType.Inventory) { Index = InventorySlots.Count });
                    }
                }
                
                if (QuickSlots.Count < 2)
                {
                    Console.WriteLine($"RefreshAllSlots: QuickSlots содержит только {QuickSlots.Count} элементов, добавляем недостающие");
                    while (QuickSlots.Count < 2)
                    {
                        QuickSlots.Add(new ItemSlot(SlotType.Quick) { Index = QuickSlots.Count });
                    }
                }
                
                if (CraftSlots.Count < 9)
                {
                    Console.WriteLine($"RefreshAllSlots: CraftSlots содержит только {CraftSlots.Count} элементов, добавляем недостающие");
                    while (CraftSlots.Count < 9)
                    {
                        CraftSlots.Add(new ItemSlot(SlotType.Craft) { Index = CraftSlots.Count });
                    }
                }
                
                // Проверяем целостность коллекции Items в Inventory
                if (_gameState.Inventory.Items.Count < 15)
                {
                    Console.WriteLine($"RefreshAllSlots: _gameState.Inventory.Items содержит только {_gameState.Inventory.Items.Count} элементов");
                }
                
                // Обновляем модель инвентаря и её внутренние коллекции
                _gameState.Inventory.OnInventoryChanged();
                
                // Обновляем все слоты
                RefreshInventorySlots();
                RefreshQuickSlots();
                RefreshCraftSlots();
                RefreshEquipmentSlots();
                
                // Update craft result
                UpdateCraftResult();
                
                // Обновляем свойства верхнего уровня
                OnPropertyChanged(nameof(PlayerInventory));
                OnPropertyChanged(nameof(InventorySlots));
                OnPropertyChanged(nameof(QuickSlots));
                OnPropertyChanged(nameof(CraftSlots));
                OnPropertyChanged(nameof(CraftResultSlot));
                
                // Обновляем модель состояния
                OnPropertyChanged(nameof(GameState));
                
                // Обновляем все слоты напрямую чтобы гарантировать синхронизацию с моделью
                for (int i = 0; i < Math.Min(_gameState.Inventory.Items.Count, InventorySlots.Count); i++)
                {
                    if (InventorySlots[i].Item != _gameState.Inventory.Items[i])
                    {
                        InventorySlots[i].Item = _gameState.Inventory.Items[i];
                    }
                }
                
                // Обновляем оставшиеся визуальные элементы
                OnPropertyChanged(nameof(HelmetSlot));
                OnPropertyChanged(nameof(ChestplateSlot));
                OnPropertyChanged(nameof(LeggingsSlot));
                OnPropertyChanged(nameof(WeaponSlot));
                OnPropertyChanged(nameof(ShieldSlot));
                OnPropertyChanged(nameof(TrashSlot));
                
                // Обновляем свойства персонажа
                OnPropertyChanged(nameof(PlayerHealth));
                OnPropertyChanged(nameof(PlayerDamage));
                OnPropertyChanged(nameof(PlayerDefense));
                
                // Принудительно обновить UI после всех обновлений
                ForceUIUpdate();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in RefreshAllSlots: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        private void UseQuickSlot(object parameter)
        {
            try
            {
                if (parameter is int slotIndex && slotIndex >= 0 && slotIndex < QuickSlots.Count)
                {
                    var item = QuickSlots[slotIndex].Item;
                    if (item != null && item.Type == ItemType.Consumable)
                    {
                        UseItem(item);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UseQuickSlot: {ex.Message}");
            }
        }

        private void MoveToTrash(object parameter)
        {
            try
            {
                if (parameter is MoveItemData moveData)
                {
                    // Get the item from its source
                    Item? item = GetItemFromSlot(moveData.SourceType, moveData.SourceIndex);
                    
                    if (item != null)
                    {
                        // Set the item to trash slot in both ViewModel and Model
                        TrashSlot.Item = item;
                        _gameState.Inventory.TrashItem = item;
                        
                        // Remove item from original slot
                        RemoveItemFromSlot(moveData.SourceType, moveData.SourceIndex);
                        
                        // Update character stats if needed
                        RefreshEquipmentSlots();
                        OnPropertyChanged(nameof(PlayerHealth));
                        OnPropertyChanged(nameof(PlayerDefense));
                        OnPropertyChanged(nameof(PlayerDamage));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in MoveToTrash: {ex.Message}");
            }
        }

        // Handle property changes on craft slots to update craft result
        private void CraftSlot_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ItemSlot.Item))
            {
                // When craft slot contents change, recalculate the craft result
                UpdateCraftResult();
            }
        }

        // Update the craft result slot based on current craft slot items
        private void UpdateCraftResult()
        {
            try
            {
                Console.WriteLine("UpdateCraftResult: Refreshing available crafting recipes");
                
                // Clear the current crafting recipes
                AvailableCraftingRecipes.Clear();
                
                // Check if crafting system is available
                if (_gameState.CraftingSystem == null)
                {
                    Console.WriteLine("UpdateCraftResult: Crafting system is not available");
                    return;
                }
                
                // Get all available recipes
                var availableRecipes = _gameState.CraftingSystem.GetAvailableRecipes();
                int recipeIndex = 0;
                
                Console.WriteLine($"UpdateCraftResult: Checking {availableRecipes.Count} recipes against inventory");
                
                // Check which recipes can be crafted with the current inventory
                foreach (var recipe in availableRecipes)
                {
                    bool canCraft = _gameState.CraftingSystem.CanCraft(recipe, _gameState.Inventory);
                    
                    if (canCraft)
                    {
                        // Add craftable recipe to the collection
                        var craftableRecipe = new CraftableRecipeViewModel(recipe, recipeIndex++);
                        
                        // Create a list of materials text for display
                        craftableRecipe.MaterialsText = new List<string>();
                        foreach (var material in recipe.Materials)
                        {
                            craftableRecipe.MaterialsText.Add($"{material.Key}: {material.Value}");
                        }
                        
                        AvailableCraftingRecipes.Add(craftableRecipe);
                        Console.WriteLine($"Found recipe for {recipe.Result.Name} - can be crafted with current inventory");
                        
                        // Debug: Print out recipe materials
                        foreach (var material in recipe.Materials)
                        {
                            Console.WriteLine($"  - Requires: {material.Key} x{material.Value}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Recipe for {recipe.Result.Name} - cannot be crafted with current inventory");
                    }
                }
                
                Console.WriteLine($"UpdateCraftResult: Found {AvailableCraftingRecipes.Count} craftable recipes");
                
                // Force UI update for recipe list
                OnPropertyChanged(nameof(AvailableCraftingRecipes));
                
                // Make sure the CraftingViewModel is updated with the latest recipes
                if (_craftingViewModel != null)
                {
                    _craftingViewModel.RefreshAvailableRecipes();
                }
                
                if (AvailableCraftingRecipes.Count == 0)
                {
                    Console.WriteLine("No recipes can be crafted with current inventory");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UpdateCraftResult: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
        
        // Take the crafted item based on recipe index
        public bool TakeCraftResult(int recipeIndex = 0)
        {
            try
            {
                Console.WriteLine($"TakeCraftResult called with index: {recipeIndex}");
                
                // Find the selected recipe
                CraftableRecipeViewModel? craftableRecipe = null;
                foreach (var recipeVM in AvailableCraftingRecipes)
                {
                    if (recipeVM.RecipeIndex == recipeIndex)
                    {
                        craftableRecipe = recipeVM;
                        Console.WriteLine($"Found recipe with index {recipeIndex}: {recipeVM.Result.Name}");
                        break;
                    }
                }
                
                // If no recipe found, check if we have any available recipes
                if (craftableRecipe == null && AvailableCraftingRecipes.Count > 0)
                {
                    craftableRecipe = AvailableCraftingRecipes[0];
                    Console.WriteLine($"Using default recipe: {craftableRecipe.Result.Name}");
                }
                
                // If still no recipe found, return false
                if (craftableRecipe == null)
                {
                    Console.WriteLine("No craftable recipe available");
                    return false;
                }
                
                // Clone the crafted item
                Item craftedItem = craftableRecipe.Result.Clone();
                Console.WriteLine($"Attempting to craft: {craftedItem.Name}");
                
                // Get the recipe
                CraftingRecipe recipeToUse = craftableRecipe.Recipe;
                
                // Check if we still can craft this recipe (inventory might have changed)
                if (!_gameState.CraftingSystem.CanCraft(recipeToUse, _gameState.Inventory))
                {
                    Console.WriteLine("Can no longer craft this recipe - ingredients may have been used");
                    return false;
                }
                
                // Get a list of all required materials for debugging
                Console.WriteLine("Required materials:");
                foreach (var material in recipeToUse.Materials)
                {
                    Console.WriteLine($" - {material.Key}: {material.Value}");
                }
                
                try
                {
                    // Проверка целостности инвентаря перед крафтом
                    if (InventorySlots.Count < 15)
                    {
                        Console.WriteLine($"ВНИМАНИЕ: InventorySlots содержит только {InventorySlots.Count} элементов перед крафтом, добавляем недостающие");
                        while (InventorySlots.Count < 15)
                        {
                            InventorySlots.Add(new ItemSlot(SlotType.Inventory) { Index = InventorySlots.Count });
                        }
                    }
                    
                    // Проверка соответствия размера коллекции Items в Inventory
                    if (_gameState.Inventory.Items.Count < 15)
                    {
                        Console.WriteLine($"ВНИМАНИЕ: Inventory.Items содержит только {_gameState.Inventory.Items.Count} элементов перед крафтом");
                    }
                    
                    // Первое обновление интерфейса перед крафтом
                    ForceUIUpdate();
                    
                    // Consume materials from inventory - using the CraftingSystem's Craft method directly
                    bool craftSuccess = _gameState.CraftingSystem.Craft(recipeToUse, _gameState.Inventory, _gameState);
                    if (!craftSuccess)
                    {
                        Console.WriteLine("Failed to craft item using CraftingSystem.Craft method");
                        return false;
                    }
                    
                    // Play a crafting sound
                    _gameState.PlaySound(Services.SoundType.ItemCrafted);
                    
                    // Проверка целостности инвентаря после крафта
                    if (_gameState.Inventory.Items.Count < 15)
                    {
                        Console.WriteLine($"ВНИМАНИЕ: Inventory.Items содержит только {_gameState.Inventory.Items.Count} элементов после крафта");
                    }
                    
                    if (InventorySlots.Count < 15)
                    {
                        Console.WriteLine($"ВНИМАНИЕ: InventorySlots содержит только {InventorySlots.Count} элементов после крафта");
                    }
                    
                    // Обновляем слоты с учетом возможных проблем с размером коллекций
                    // Обновляем основные слоты инвентаря
                    for (int i = 0; i < Math.Min(_gameState.Inventory.Items.Count, InventorySlots.Count); i++)
                    {
                        InventorySlots[i].Item = _gameState.Inventory.Items[i];
                        if (InventorySlots[i].Item != null)
                        {
                            InventorySlots[i].Item.NotifyPropertyChanged("StackSize");
                        }
                    }
                    
                    // Обновляем основные свойства для привязки UI
                    OnPropertyChanged(nameof(InventorySlots));
                    OnPropertyChanged(nameof(PlayerInventory));
                    
                    // Принудительно обновляем все отображение инвентаря
                    Console.WriteLine("Calling ForceUIUpdate to refresh inventory display after craft");
                    ForceUIUpdate();
                    
                    // Обновляем доступные рецепты
                    UpdateCraftResult();
                    
                    Console.WriteLine($"Successfully crafted {craftedItem.Name}");
                    return true;
                }
                catch (Exception innerEx)
                {
                    Console.WriteLine($"Error during craft operation: {innerEx.Message}");
                    Console.WriteLine($"Inner Stack trace: {innerEx.StackTrace}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in TakeCraftResult: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        private void ShowRecipeBook()
        {
            try
            {
                // Set flag to show recipe book popup
                IsRecipeBookVisible = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ShowRecipeBook: {ex.Message}");
            }
        }

        // Метод для перемещения предметов в слоты крафта
        private void MoveToCraftSlot(MoveItemData moveData)
        {
            // This method is no longer used as we've removed the 3x3 crafting grid
            // We're keeping the stub for backwards compatibility
            Console.WriteLine("MoveToCraftSlot: This functionality has been replaced with a simpler crafting system");
            UpdateCraftResult();
        }
        
        // Метод для перемещения предметов между любыми слотами (включая крафт)
        public void MoveToCraft(MoveItemData moveData)
        {
            // This method is no longer used as we've removed the 3x3 crafting grid
            // We're keeping the stub for backwards compatibility
            Console.WriteLine("MoveToCraft: This functionality has been replaced with a simpler crafting system");
            UpdateCraftResult();
        }
        
        // Метод для обновления предмета в слоте
        private void UpdateSlotItem(string slotType, int index, Item item)
        {
            try
            {
                Console.WriteLine($"UpdateSlotItem: Updating {slotType}[{index}] with item {item.Name}");
                
                switch (slotType)
                {
                    case "Inventory":
                        if (index >= 0 && index < InventorySlots.Count)
                        {
                            InventorySlots[index].Item = item;
                        }
                        break;
                        
                    case "Quick":
                        if (index >= 0 && index < QuickSlots.Count)
                        {
                            QuickSlots[index].Item = item;
                        }
                        break;
                        
                    case "Craft":
                        if (index >= 0 && index < CraftSlots.Count)
                        {
                            CraftSlots[index].Item = item;
                            RefreshCraftSlots();
                        }
                        break;
                        
                    case "Helmet":
                        HelmetSlot.Item = item;
                        break;
                        
                    case "Chestplate":
                        ChestplateSlot.Item = item;
                        break;
                        
                    case "Leggings":
                        LeggingsSlot.Item = item;
                        break;
                        
                    case "Weapon":
                        WeaponSlot.Item = item;
                        break;
                        
                    case "Shield":
                        ShieldSlot.Item = item;
                        break;
                        
                    case "Trash":
                        TrashSlot.Item = item;
                        break;
                        
                    default:
                        Console.WriteLine($"WARNING: Unknown slot type in UpdateSlotItem: {slotType}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UpdateSlotItem: {ex.Message}");
            }
        }

        // Метод для принудительного обновления UI после операций с предметами
        public void ForceUIUpdate()
        {
            try
            {
                Console.WriteLine("ForceUIUpdate: Refreshing inventory UI");
                
                // Обновляем базовую модель инвентаря
                _gameState.Inventory.OnInventoryChanged();
                
                // Проверяем, что количество слотов соответствует ожидаемому (15)
                if (InventorySlots.Count < 15)
                {
                    Console.WriteLine($"ВНИМАНИЕ: InventorySlots содержит только {InventorySlots.Count} элементов, добавляем недостающие");
                    while (InventorySlots.Count < 15)
                    {
                        InventorySlots.Add(new ItemSlot(SlotType.Inventory) { Index = InventorySlots.Count });
                    }
                }
                
                // Обновляем все коллекции слотов
                OnPropertyChanged(nameof(InventorySlots));
                OnPropertyChanged(nameof(QuickSlots));
                OnPropertyChanged(nameof(CraftSlots));
                
                // Синхронизируем состояние каждого слота с базовым инвентарем
                for (int i = 0; i < _gameState.Inventory.Items.Count && i < InventorySlots.Count; i++)
                {
                    InventorySlots[i].Item = _gameState.Inventory.Items[i];
                }
                
                // Обновляем отдельные слоты
                OnPropertyChanged(nameof(HelmetSlot));
                OnPropertyChanged(nameof(ChestplateSlot));
                OnPropertyChanged(nameof(LeggingsSlot));
                OnPropertyChanged(nameof(WeaponSlot));
                OnPropertyChanged(nameof(ShieldSlot));
                OnPropertyChanged(nameof(TrashSlot));
                OnPropertyChanged(nameof(CraftResultSlot));
                
                // Обновляем элементы в коллекциях
                foreach (var slot in InventorySlots)
                {
                    if (slot != null && slot.Item != null)
                    {
                        // Принудительно уведомляем об изменении свойств предмета
                        slot.Item.NotifyPropertyChanged("StackSize");
                        slot.Item.NotifyPropertyChanged("Icon");
                        
                        // Уведомляем также о свойстве Item, чтобы UI обновил отображение
                        slot.OnPropertyChanged(nameof(ItemSlot.Item));
                        slot.OnPropertyChanged(nameof(ItemSlot.IsEmpty));
                        slot.OnPropertyChanged(nameof(ItemSlot.HasItem));
                    }
                    else if (slot != null)
                    {
                        // Уведомляем даже для пустых слотов
                        slot.OnPropertyChanged(nameof(ItemSlot.Item));
                        slot.OnPropertyChanged(nameof(ItemSlot.IsEmpty));
                        slot.OnPropertyChanged(nameof(ItemSlot.HasItem));
                    }
                }
                
                foreach (var slot in QuickSlots)
                {
                    if (slot != null && slot.Item != null)
                    {
                        slot.Item.NotifyPropertyChanged("StackSize");
                        slot.Item.NotifyPropertyChanged("Icon");
                        
                        // Уведомляем также о свойстве Item
                        slot.OnPropertyChanged(nameof(ItemSlot.Item));
                        slot.OnPropertyChanged(nameof(ItemSlot.IsEmpty));
                        slot.OnPropertyChanged(nameof(ItemSlot.HasItem));
                    }
                    else if (slot != null)
                    {
                        // Уведомляем даже для пустых слотов
                        slot.OnPropertyChanged(nameof(ItemSlot.Item));
                        slot.OnPropertyChanged(nameof(ItemSlot.IsEmpty));
                        slot.OnPropertyChanged(nameof(ItemSlot.HasItem));
                    }
                }
                
                foreach (var slot in CraftSlots)
                {
                    if (slot != null && slot.Item != null)
                    {
                        slot.Item.NotifyPropertyChanged("StackSize");
                        slot.Item.NotifyPropertyChanged("Icon");
                        
                        // Уведомляем также о свойстве Item
                        slot.OnPropertyChanged(nameof(ItemSlot.Item));
                        slot.OnPropertyChanged(nameof(ItemSlot.IsEmpty));
                        slot.OnPropertyChanged(nameof(ItemSlot.HasItem));
                    }
                    else if (slot != null)
                    {
                        // Уведомляем даже для пустых слотов
                        slot.OnPropertyChanged(nameof(ItemSlot.Item));
                        slot.OnPropertyChanged(nameof(ItemSlot.IsEmpty));
                        slot.OnPropertyChanged(nameof(ItemSlot.HasItem));
                    }
                }
                
                // Для CraftResultSlot тоже обновляем
                if (CraftResultSlot != null)
                {
                    CraftResultSlot.OnPropertyChanged(nameof(ItemSlot.Item));
                    CraftResultSlot.OnPropertyChanged(nameof(ItemSlot.IsEmpty));
                    CraftResultSlot.OnPropertyChanged(nameof(ItemSlot.HasItem));
                }
                
                // Обновляем свойства персонажа
                OnPropertyChanged(nameof(PlayerHealth));
                OnPropertyChanged(nameof(PlayerDamage));
                OnPropertyChanged(nameof(PlayerDefense));
                
                // Принудительно обновляем коллекцию доступных рецептов
                OnPropertyChanged(nameof(AvailableCraftingRecipes));
                
                // Обновляем доступность крафта
                UpdateCraftResult();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ForceUIUpdate: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        // Method to force refresh of craft result
        private void RefreshCraftResult(object parameter = null)
        {
            UpdateCraftResult();
            Console.WriteLine("Manually refreshed craft result");
        }

        // Get required materials for the current craftable item (for tooltip)
        public string GetCraftingRequirements()
        {
            if (CraftResultSlot.Item == null)
                return "Нет доступных рецептов";
                
            // Find the recipe for the current craft result
            foreach (var recipe in _gameState.CraftingSystem.Recipes)
            {
                if (recipe.Result.Name == CraftResultSlot.Item.Name)
                {
                    // Build a string listing required materials
                    var materials = new List<string>();
                    foreach (var mat in recipe.Materials)
                    {
                        materials.Add($"{mat.Key} x{mat.Value}");
                    }
                    
                    return string.Join("\n", materials);
                }
            }
            
            return "Неизвестный рецепт";
        }

        // Handle inventory changed event for automatic crafting updates
        private void Inventory_Changed(object sender, EventArgs e)
        {
            Console.WriteLine("Inventory changed, updating available crafting recipes");
            UpdateCraftResult();
            
            // Make sure CraftingViewModel is updated as well
            if (_craftingViewModel != null)
            {
                _craftingViewModel.RefreshAvailableRecipes();
            }
        }
    }
    
    // Data for equipping items
    public class EquipItemData
    {
        public int InventoryIndex { get; set; }
        public string EquipmentSlot { get; set; } = string.Empty;
    }
    
    // Data for moving items
    public class MoveItemData
    {
        public int SourceIndex { get; set; }
        public int TargetIndex { get; set; }
        public string SourceType { get; set; } = "";
        public string TargetType { get; set; } = "";
    }
    
    // View model for craftable recipes
    public class CraftableRecipeViewModel
    {
        public CraftingRecipe Recipe { get; set; }
        public Item Result { get; set; }
        public int RecipeIndex { get; set; }
        public List<string> MaterialsText { get; set; } = new List<string>();

        public CraftableRecipeViewModel(CraftingRecipe recipe, int index)
        {
            Recipe = recipe;
            Result = recipe.Result;
            RecipeIndex = index;
            
            // Initialize material text
            MaterialsText = new List<string>();
            foreach (var material in recipe.Materials)
            {
                MaterialsText.Add($"{material.Key}: {material.Value}");
            }
        }
    }
} 