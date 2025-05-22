using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.ComponentModel;
using SketchBlade.Models;
using SketchBlade.ViewModels;
using SketchBlade.Views.Controls;
using SketchBlade.Views.Controls.Recipes;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Threading;
using System.Collections.ObjectModel;

namespace SketchBlade.Views
{
    public partial class InventoryView : UserControl
    {
        private InventoryViewModel? _viewModel;
        
        public InventoryViewModel? ViewModel 
        { 
            get => _viewModel; 
            set 
            { 
                if (_viewModel != null)
                {
                    _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
                }
                
                _viewModel = value;
                
                if (_viewModel != null)
                {
                    _viewModel.PropertyChanged += ViewModel_PropertyChanged;
                    DataContext = _viewModel;
                    
                    // Initialize CraftingViewModel to ensure it's ready
                    var craftingViewModel = _viewModel.CraftingViewModel;
                    craftingViewModel.RefreshAvailableRecipes();
                }
            }
        }
        
        private RecipeBookPopup? _recipeBookPopup;
        
        public InventoryView()
        {
            try
            {
                InitializeComponent();
                
                this.Loaded += InventoryView_Loaded;
                this.DataContextChanged += InventoryView_DataContextChanged;
                
                _recipeBookPopup = new RecipeBookPopup();
                _recipeBookPopup.CloseRequested += RecipeBookPopup_CloseRequested;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in InventoryView constructor: {ex.Message}");
            }
        }

        private void InventoryView_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DataContext == null)
                {
                    MessageBox.Show("ERROR: DataContext is null in InventoryView_Loaded");
                }
                else
                {
                    if (DataContext is InventoryViewModel viewModel)
                    {
                        Application.Current.Resources["InventoryViewModel"] = viewModel;
                        
                        // Отложим установку RecipeBookPopupHost до полной загрузки UI
                        Dispatcher.BeginInvoke(new Action(() => {
                            try
                            {
                                // Set the RecipeBookPopupHost on the CraftingViewModel
                                if (RecipeBookPopup != null && RecipeBookPopupHost != null)
                                {
                                    viewModel.CraftingViewModel.SetRecipeBookPopupHost(RecipeBookPopupHost);
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Ошибка при установке RecipeBookPopupHost: {ex.Message}");
                            }
                        }), System.Windows.Threading.DispatcherPriority.Loaded);
                        
                        viewModel.ForceUIUpdate();
                    }
                }
                
                ConnectEventHandlers();
                
                this.KeyDown += InventoryView_KeyDown;
                this.PreviewKeyDown += InventoryView_PreviewKeyDown;
                
                Window? window = Window.GetWindow(this);
                if (window != null)
                {
                    window.PreviewKeyDown += Window_PreviewKeyDown;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in InventoryView_Loaded: {ex.Message}");
            }
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            CheckHotkey(e);
        }
        
        private void InventoryView_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            CheckHotkey(e);
        }
        
        private void InventoryView_KeyDown(object sender, KeyEventArgs e)
        {
            CheckHotkey(e);
        }
        
        private void CheckHotkey(KeyEventArgs e)
        {
            if (e.Key >= Key.D1 && e.Key <= Key.D9 && (Keyboard.Modifiers & ModifierKeys.Control) != 0)
            {
                int index = e.Key - Key.D1;
                
                if (ViewModel != null && index < 2)
                {
                    ViewModel.UseQuickSlotCommand.Execute(index);
                    e.Handled = true;
                }
            }
        }
        
        private void InventoryView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is InventoryViewModel oldViewModel)
            {
                oldViewModel.PropertyChanged -= ViewModel_PropertyChanged;
            }
            
            if (e.NewValue is InventoryViewModel newViewModel)
            {
                newViewModel.PropertyChanged += ViewModel_PropertyChanged;
            }
        }
        
        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(InventoryViewModel.IsRecipeBookVisible))
            {
                if (ViewModel != null && ViewModel.IsRecipeBookVisible)
                {
                    ShowRecipeBook();
                }
            }
        }
        
        private void ShowRecipeBook()
        {
            if (RecipeBookPopup != null && ViewModel?.GameState?.CraftingSystem != null)
            {
                // Загружаем рецепты из CraftingViewModel
                var craftingSystem = ViewModel.GameState.CraftingSystem;
                var recipes = new ObservableCollection<RecipeBookEntry>();
                
                // Получаем все доступные рецепты
                var allRecipes = craftingSystem.GetAvailableRecipes();
                Console.WriteLine($"ShowRecipeBook: Найдено {allRecipes.Count} рецептов");
                
                foreach (var recipe in allRecipes)
                {
                    if (recipe == null) continue;
                    
                    bool canCraft = craftingSystem.CanCraft(recipe, ViewModel.GameState.Inventory);
                    
                    var recipeEntry = new RecipeBookEntry
                    {
                        Recipe = recipe,
                        CanCraft = canCraft,
                        IconPath = recipe.Result?.SpritePath ?? "Assets/Images/def.png"
                    };
                    
                    recipes.Add(recipeEntry);
                }
                
                // Передаем рецепты и GameState в RecipeBookPopup
                RecipeBookPopup.LoadRecipes(recipes, ViewModel.GameState);
                RecipeBookPopup.Visibility = System.Windows.Visibility.Visible;
            }
        }
        
        private void RecipeBookPopup_CloseRequested(object? sender, EventArgs e)
        {
            if (ViewModel != null)
            {
                ViewModel.IsRecipeBookVisible = false;
            }
        }
        
        private void ConnectEventHandlers()
        {
            try
            {
                if (HelmetSlot != null)
                {
                    HelmetSlot.SlotMouseDown += EquipmentSlot_MouseDown;
                    HelmetSlot.SlotDrop += EquipmentSlot_Drop;
                    HelmetSlot.ValidateItemForSlot += Slot_ValidateItemForSlot;
                    HelmetSlot.ItemMoveRequested += Slot_ItemMoveRequested;
                    HelmetSlot.ItemEquipRequested += Slot_ItemEquipRequested;
                    HelmetSlot.ItemTrashRequested += Slot_ItemTrashRequested;
                }
                
                if (ChestSlot != null)
                {
                    ChestSlot.SlotMouseDown += EquipmentSlot_MouseDown;
                    ChestSlot.SlotDrop += EquipmentSlot_Drop;
                    ChestSlot.ValidateItemForSlot += Slot_ValidateItemForSlot;
                    ChestSlot.ItemMoveRequested += Slot_ItemMoveRequested;
                    ChestSlot.ItemEquipRequested += Slot_ItemEquipRequested;
                    ChestSlot.ItemTrashRequested += Slot_ItemTrashRequested;
                }
                
                if (LegsSlot != null)
                {
                    LegsSlot.SlotMouseDown += EquipmentSlot_MouseDown;
                    LegsSlot.SlotDrop += EquipmentSlot_Drop;
                    LegsSlot.ValidateItemForSlot += Slot_ValidateItemForSlot;
                    LegsSlot.ItemMoveRequested += Slot_ItemMoveRequested;
                    LegsSlot.ItemEquipRequested += Slot_ItemEquipRequested;
                    LegsSlot.ItemTrashRequested += Slot_ItemTrashRequested;
                }
                
                if (WeaponSlot != null)
                {
                    WeaponSlot.SlotMouseDown += EquipmentSlot_MouseDown;
                    WeaponSlot.SlotDrop += EquipmentSlot_Drop;
                    WeaponSlot.ValidateItemForSlot += Slot_ValidateItemForSlot;
                    WeaponSlot.ItemMoveRequested += Slot_ItemMoveRequested;
                    WeaponSlot.ItemEquipRequested += Slot_ItemEquipRequested;
                    WeaponSlot.ItemTrashRequested += Slot_ItemTrashRequested;
                }
                
                if (ShieldSlot != null)
                {
                    ShieldSlot.SlotMouseDown += EquipmentSlot_MouseDown;
                    ShieldSlot.SlotDrop += EquipmentSlot_Drop;
                    ShieldSlot.ValidateItemForSlot += Slot_ValidateItemForSlot;
                    ShieldSlot.ItemMoveRequested += Slot_ItemMoveRequested;
                    ShieldSlot.ItemEquipRequested += Slot_ItemEquipRequested;
                    ShieldSlot.ItemTrashRequested += Slot_ItemTrashRequested;
                }
                
                try 
                {
                    var quickSlots = FindSlotControls("Quick");
                    
                    foreach (var slot in quickSlots)
                    {
                        if (slot != null)
                        {
                            slot.SlotMouseDown += QuickSlot_MouseDown;
                            slot.SlotDrop += QuickSlot_Drop;
                            slot.ValidateItemForSlot += Slot_ValidateItemForSlot;
                            slot.ItemMoveRequested += Slot_ItemMoveRequested;
                            slot.ItemEquipRequested += Slot_ItemEquipRequested;
                            slot.ItemTrashRequested += Slot_ItemTrashRequested;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error connecting Quick slots: {ex.Message}");
                }
                
                if (TrashSlot != null)
                {
                    TrashSlot.SlotDrop += TrashSlot_Drop;
                    TrashSlot.ValidateItemForSlot += Slot_ValidateItemForSlot;
                    TrashSlot.ItemMoveRequested += Slot_ItemMoveRequested;
                    TrashSlot.ItemEquipRequested += Slot_ItemEquipRequested;
                    TrashSlot.ItemTrashRequested += Slot_ItemTrashRequested;
                }
                
                try
                {
                    var inventorySlots = FindSlotControls("Inventory");
                    
                    foreach (var slot in inventorySlots)
                    {
                        if (slot != null)
                        {
                            slot.SlotMouseDown += InventorySlot_MouseDown;
                            slot.SlotDrop += InventorySlot_Drop;
                            slot.ValidateItemForSlot += Slot_ValidateItemForSlot;
                            slot.ItemMoveRequested += Slot_ItemMoveRequested;
                            slot.ItemEquipRequested += Slot_ItemEquipRequested;
                            slot.ItemTrashRequested += Slot_ItemTrashRequested;
                            slot.SplitStackRequested += InventorySlot_SplitStackRequested;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error connecting Inventory slots: {ex.Message}");
                }
                
                try
                {
                    var craftSlots = FindSlotControls("Craft");
                    
                    foreach (var slot in craftSlots)
                    {
                        if (slot != null)
                        {
                            slot.SlotMouseDown += CraftSlot_MouseDown;
                            slot.SlotDrop += CraftSlot_Drop;
                            slot.ValidateItemForSlot += Slot_ValidateItemForSlot;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error connecting Craft slots: {ex.Message}");
                }
                
                try
                {
                    var craftResultSlots = FindSlotControls("CraftResult");
                    
                    CoreInventorySlot? craftResultSlot = craftResultSlots.FirstOrDefault();
                    if (craftResultSlot != null)
                    {
                        craftResultSlot.SlotMouseDown += CraftResultSlot_MouseDown;
                        craftResultSlot.ValidateItemForSlot += Slot_ValidateItemForSlot;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error connecting CraftResult slot: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in ConnectEventHandlers: {ex.Message}");
                MessageBox.Show($"Stack trace: {ex.StackTrace}");
            }
        }
        
        private void Border_MouseLeftButtonDown(object? sender, MouseButtonEventArgs e)
        {
        }
        
        private void EquipmentSlot_MouseDown(object? sender, MouseButtonEventArgs e)
        {
            if (!(sender is CoreInventorySlot slot))
                return;
                
            if (e.ChangedButton == MouseButton.Left && slot.Item != null)
            {
                // Начинаем операцию перетаскивания
                ItemSlotInfo slotInfo = new ItemSlotInfo(slot.SlotType, slot.SlotIndex);
                DataObject dragData = new DataObject();
                dragData.SetData("ItemSlotInfo", slotInfo);
                
                DragDropEffects result = DragDrop.DoDragDrop(slot, dragData, DragDropEffects.Move);
                
                e.Handled = true;
            }
        }
        
        private void QuickSlot_MouseDown(object? sender, MouseButtonEventArgs e)
        {
            if (!(sender is CoreInventorySlot slot))
                return;
                
            if (e.ChangedButton == MouseButton.Left && slot.Item != null)
            {
                // Начинаем операцию перетаскивания
                ItemSlotInfo slotInfo = new ItemSlotInfo(slot.SlotType, slot.SlotIndex);
                DataObject dragData = new DataObject();
                dragData.SetData("ItemSlotInfo", slotInfo);
                
                DragDropEffects result = DragDrop.DoDragDrop(slot, dragData, DragDropEffects.Move);
                
                e.Handled = true;
            }
        }
        
        private void EquipmentSlot_Drop(object? sender, DragEventArgs e)
        {
            if (ViewModel == null || !(sender is CoreInventorySlot targetSlot))
                return;
                
            try
            {
                if (e.Data.GetDataPresent("ItemSlotInfo"))
                {
                    ItemSlotInfo? dragInfo = e.Data.GetData("ItemSlotInfo") as ItemSlotInfo;
                    
                    if (dragInfo != null)
                    {
                        MoveItemData moveData = new MoveItemData
                        {
                            SourceIndex = dragInfo.SlotIndex,
                            TargetIndex = targetSlot.SlotIndex,
                            SourceType = dragInfo.SlotType,
                            TargetType = targetSlot.SlotType
                        };
                        
                        ViewModel.MoveToQuickSlotCommand.Execute(moveData);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in EquipmentSlot_Drop: {ex.Message}");
            }
        }
        
        private void QuickSlot_Drop(object? sender, DragEventArgs e)
        {
            if (!(sender is CoreInventorySlot targetSlot) || ViewModel == null)
                return;
                
            try
            {
                if (sender is FrameworkElement element)
                {
                    element.Opacity = 1.0;
                }
                
                if (e.Data.GetDataPresent("ItemSlotInfo"))
                {
                    ItemSlotInfo? dragInfo = e.Data.GetData("ItemSlotInfo") as ItemSlotInfo;
                    
                    if (dragInfo != null)
                    {
                        MoveItemData moveData = new MoveItemData
                        {
                            SourceType = dragInfo.SlotType,
                            SourceIndex = dragInfo.SlotIndex,
                            TargetType = targetSlot.SlotType,
                            TargetIndex = targetSlot.SlotIndex
                        };
                        
                        if (ViewModel.MoveToQuickSlotCommand.CanExecute(moveData))
                        {
                            ViewModel.MoveToQuickSlotCommand.Execute(moveData);
                            
                            VerifyItemStacks(dragInfo, new ItemSlotInfo(targetSlot.SlotType, targetSlot.SlotIndex));
                            
                            ViewModel.ForceUIUpdate();
                        }
                    }
                }
                else if (e.Data.GetDataPresent(typeof(Item)))
                {
                    Item? item = e.Data.GetData(typeof(Item)) as Item;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in QuickSlot_Drop: {ex.Message}");
                MessageBox.Show($"Stack trace: {ex.StackTrace}");
            }
        }
        
        private void TrashSlot_Drop(object? sender, DragEventArgs e)
        {
            if (!(sender is CoreInventorySlot targetSlot))
                return;
                
            try
            {
                if (e.Data.GetDataPresent("ItemSlotInfo"))
                {
                    ItemSlotInfo? dragInfo = e.Data.GetData("ItemSlotInfo") as ItemSlotInfo;
                    
                    if (dragInfo != null && ViewModel != null)
                    {
                        MoveItemData moveData = new MoveItemData
                        {
                            SourceIndex = dragInfo.SlotIndex,
                            TargetIndex = targetSlot.SlotIndex,
                            SourceType = dragInfo.SlotType,
                            TargetType = targetSlot.SlotType
                        };
                        
                        ViewModel.MoveToTrashCommand.Execute(moveData);
                        e.Handled = true;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in TrashSlot_Drop: {ex.Message}");
            }
        }
        
        private List<CoreInventorySlot> FindSlotControls(string slotType)
        {
            var slots = new List<CoreInventorySlot>();
            
            try
            {
                FindSlots(this, slotType, slots);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"ERROR in FindSlotControls: {ex.Message}");
                MessageBox.Show($"Stack trace: {ex.StackTrace}");
            }
            
            return slots;
        }

        private void FindSlots(DependencyObject parent, string slotType, List<CoreInventorySlot> results)
        {
            if (parent == null)
            {
                return;
            }
            
            try
            {
                int childCount = VisualTreeHelper.GetChildrenCount(parent);
                
                for (int i = 0; i < childCount; i++)
                {
                    try
                    {
                        var child = VisualTreeHelper.GetChild(parent, i);
                        
                        if (child == null)
                        {
                            continue;
                        }
                        
                        if (child is CoreInventorySlot slot)
                        {
                            if (slot.SlotType == slotType)
                            {
                                results.Add(slot);
                            }
                        }
                        
                        FindSlots(child, slotType, results);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"ERROR processing child {i} in FindSlots: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"ERROR in FindSlots: {ex.Message}");
                MessageBox.Show($"Stack trace: {ex.StackTrace}");
            }
        }

        private void InventorySlot_MouseDown(object? sender, MouseButtonEventArgs e)
        {
            if (!(sender is CoreInventorySlot slot))
                return;
                
            if (e.ChangedButton == MouseButton.Left && slot.Item != null)
            {
                // Начинаем операцию перетаскивания
                ItemSlotInfo slotInfo = new ItemSlotInfo(slot.SlotType, slot.SlotIndex);
                DataObject dragData = new DataObject();
                dragData.SetData("ItemSlotInfo", slotInfo);
                
                DragDropEffects result = DragDrop.DoDragDrop(slot, dragData, DragDropEffects.Move);
                
                e.Handled = true;
            }
            else if (e.ChangedButton == MouseButton.Right && slot.Item != null && slot.Item.IsStackable && slot.Item.StackSize > 1)
            {
                e.Handled = true;
            }
            else if (e.ChangedButton == MouseButton.Right && slot.Item != null && slot.Item.Type == ItemType.Consumable)
            {
                if (ViewModel != null)
                {
                    ViewModel.UseItemCommand.Execute(slot.Item);
                    e.Handled = true;
                }
            }
        }

        private void InventorySlot_Drop(object? sender, DragEventArgs e)
        {
            if (!(sender is CoreInventorySlot targetSlot) || ViewModel == null)
                return;
                
            try
            {
                if (sender is FrameworkElement element)
                {
                    element.Opacity = 1.0;
                }
                
                if (e.Data.GetDataPresent("ItemSlotInfo"))
                {
                    ItemSlotInfo? dragInfo = e.Data.GetData("ItemSlotInfo") as ItemSlotInfo;
                    
                    if (dragInfo != null)
                    {
                        MoveItemData moveData = new MoveItemData
                        {
                            SourceType = dragInfo.SlotType,
                            SourceIndex = dragInfo.SlotIndex,
                            TargetType = targetSlot.SlotType,
                            TargetIndex = targetSlot.SlotIndex
                        };
                        
                        if (ViewModel.MoveToInventorySlotCommand.CanExecute(moveData))
                        {
                            ViewModel.MoveToInventorySlotCommand.Execute(moveData);
                            
                            VerifyItemStacks(dragInfo, new ItemSlotInfo(targetSlot.SlotType, targetSlot.SlotIndex));
                            
                            ViewModel.ForceUIUpdate();
                        }
                    }
                }
                else if (e.Data.GetDataPresent(typeof(Item)))
                {
                    Item? item = e.Data.GetData(typeof(Item)) as Item;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in InventorySlot_Drop: {ex.Message}");
                MessageBox.Show($"Stack trace: {ex.StackTrace}");
            }
        }

        private void CraftSlot_MouseDown(object? sender, MouseButtonEventArgs e)
        {
            if (sender is CoreInventorySlot slot)
            {
                if (e.ChangedButton == MouseButton.Left)
                {
                    if (slot.Item != null)
                    {
                        ItemSlotInfo slotInfo = new ItemSlotInfo(slot.SlotType, slot.SlotIndex);
                        DataObject dragData = new DataObject();
                        dragData.SetData("ItemSlotInfo", slotInfo);
                        
                        DragDropEffects result = DragDrop.DoDragDrop(slot, dragData, DragDropEffects.Move);
                        
                        e.Handled = true;
                    }
                }
            }
        }

        private void VerifyItemStacks(ItemSlotInfo? sourceInfo, ItemSlotInfo? targetInfo)
        {
            if (ViewModel == null)
                return;
                
            try
            {
                if (sourceInfo != null)
                {
                    var sourceItem = ViewModel.GetItemFromSlot(sourceInfo.SlotType, sourceInfo.SlotIndex);
                }
                
                if (targetInfo != null)
                {
                    var targetItem = ViewModel.GetItemFromSlot(targetInfo.SlotType, targetInfo.SlotIndex);
                }
                
                Dispatcher.BeginInvoke(new Action(() => {
                }), System.Windows.Threading.DispatcherPriority.Render);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in VerifyItemStacks: {ex.Message}");
            }
        }
        
        private void CraftSlot_Drop(object? sender, DragEventArgs e)
        {
            if (!(sender is CoreInventorySlot targetSlot) || ViewModel == null)
                return;
                
            try
            {
                if (sender is FrameworkElement element)
                {
                    element.Opacity = 1.0;
                }
                
                if (e.Data.GetDataPresent("ItemSlotInfo"))
                {
                    ItemSlotInfo? dragInfo = e.Data.GetData("ItemSlotInfo") as ItemSlotInfo;
                    
                    if (dragInfo != null)
                    {
                        MoveItemData moveData = new MoveItemData
                        {
                            SourceType = dragInfo.SlotType,
                            SourceIndex = dragInfo.SlotIndex,
                            TargetType = targetSlot.SlotType,
                            TargetIndex = targetSlot.SlotIndex
                        };
                        
                        if (ViewModel.MoveToCraftSlotCommand.CanExecute(moveData))
                        {
                            ViewModel.MoveToCraftSlotCommand.Execute(moveData);
                            
                            VerifyItemStacks(dragInfo, new ItemSlotInfo(targetSlot.SlotType, targetSlot.SlotIndex));
                            
                            ViewModel.ForceUIUpdate();
                        }
                    }
                }
                else if (e.Data.GetDataPresent(typeof(Item)))
                {
                    Item? item = e.Data.GetData(typeof(Item)) as Item;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in CraftSlot_Drop: {ex.Message}");
                MessageBox.Show($"Stack trace: {ex.StackTrace}");
            }
        }

        private void CraftResultSlot_MouseDown(object? sender, MouseButtonEventArgs e)
        {
            if (sender is CoreInventorySlot slot && ViewModel != null && slot.Item != null)
            {
                if (e.ChangedButton == MouseButton.Left)
                {
                    ViewModel.TakeCraftResult();
                    e.Handled = true;
                }
            }
        }

        private void TrashSlot_MouseDown(object? sender, MouseButtonEventArgs e)
        {
            if (sender is CoreInventorySlot slot)
            {
                if (e.ChangedButton == MouseButton.Left)
                {
                    if (ViewModel != null)
                    {
                        ViewModel.TrashSlot.Item = null;
                    }
                    e.Handled = true;
                }
            }
        }

        private void Slot_ValidateItemForSlot(object? sender, ValidateItemTypeEventArgs e)
        {
            try
            {
                e.IsValid = false;
                
                if (ViewModel == null)
                {
                    return;
                }
                
                Item? sourceItem = ViewModel.GetItemFromSlot(e.SourceSlotType, e.SourceSlotIndex);
                if (sourceItem == null)
                {
                    return;
                }
                
                if (e.SourceSlotType == e.TargetSlotType)
                {
                    e.IsValid = true;
                    return;
                }
                
                if (e.SourceSlotType == "Craft" && e.TargetSlotType == "Inventory")
                {
                    e.IsValid = true;
                    return;
                }
                
                if (e.TargetSlotType == "Inventory")
                {
                    e.IsValid = true;
                    return;
                }
                
                if (e.TargetSlotType == "Craft")
                {
                    e.IsValid = true;
                    return;
                }
                
                switch (e.TargetSlotType)
                {
                    case "Quick":
                        if (sourceItem.Type == ItemType.Consumable)
                        {
                            e.IsValid = true;
                        }
                        break;
                        
                    case "Weapon":
                        if (sourceItem.Type == ItemType.Weapon)
                        {
                            e.IsValid = true;
                        }
                        break;
                        
                    case "Shield":
                        if (sourceItem.Type == ItemType.Shield)
                        {
                            e.IsValid = true;
                        }
                        break;
                        
                    case "Chestplate":
                        if (sourceItem.Type == ItemType.Chestplate)
                        {
                            e.IsValid = true;
                        }
                        break;
                        
                    case "Leggings":
                        if (sourceItem.Type == ItemType.Leggings)
                        {
                            e.IsValid = true;
                        }
                        break;
                        
                    case "Helmet":
                        if (sourceItem.Type == ItemType.Helmet)
                        {
                            e.IsValid = true;
                        }
                        break;

                    case "Trash":
                        e.IsValid = true;
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in Slot_ValidateItemForSlot: {ex.Message}");
                e.IsValid = false;
            }
        }

        private void Slot_ItemMoveRequested(object? sender, MoveItemData e)
        {
            if (ViewModel == null)
                return;
                
            try
            {
                if (e.TargetType == "Chestplate" || e.TargetType == "Helmet" || 
                    e.TargetType == "Leggings" || e.TargetType == "Weapon" || 
                    e.TargetType == "Shield")
                {
                    ViewModels.EquipItemData equipData = new ViewModels.EquipItemData
                    {
                        InventoryIndex = e.SourceIndex,
                        EquipmentSlot = e.TargetType
                    };
                    
                    if (e.SourceType == "Inventory")
                    {
                        ViewModel.EquipItem(equipData);
                    }
                }
                else
                {
                    ViewModel.MoveToQuickSlot(e);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in Slot_ItemMoveRequested: {ex.Message}");
                MessageBox.Show($"Stack trace: {ex.StackTrace}");
            }
        }
        
        private void Slot_ItemEquipRequested(object? sender, Controls.EquipItemData e)
        {
            if (ViewModel == null)
                return;
                
            try
            {
                ViewModels.EquipItemData equipData = new ViewModels.EquipItemData
                {
                    InventoryIndex = e.SourceIndex
                };
                
                switch (e.EquipmentType)
                {
                    case "Helmet":
                        equipData.EquipmentSlot = "Helmet";
                        break;
                    case "Chestplate":
                        equipData.EquipmentSlot = "Armor";
                        break;
                    case "Leggings":
                        equipData.EquipmentSlot = "Leggings";
                        break;
                    case "Weapon":
                        equipData.EquipmentSlot = "Weapon";
                        break;
                    case "Shield":
                        equipData.EquipmentSlot = "Shield";
                        break;
                    default:
                        return;
                }
                
                ViewModel.EquipItem(equipData);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in Slot_ItemEquipRequested: {ex.Message}");
                MessageBox.Show($"Stack trace: {ex.StackTrace}");
            }
        }
        
        private void Slot_ItemTrashRequested(object? sender, ItemTrashEventArgs e)
        {
            if (ViewModel == null)
                return;
                
            try
            {
                MoveItemData moveData = new MoveItemData
                {
                    SourceType = e.SourceType,
                    SourceIndex = e.SourceIndex,
                    TargetType = "Trash",
                    TargetIndex = 0
                };
                
                ViewModel.MoveToQuickSlot(moveData);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in Slot_ItemTrashRequested: {ex.Message}");
                MessageBox.Show($"Stack trace: {ex.StackTrace}");
            }
        }

        private void RightPanel_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is ScrollViewer scrollViewer)
            {
                if (e.Delta < 0)
                {
                    scrollViewer.LineDown();
                }
                else
                {
                    scrollViewer.LineUp();
                }
                
                e.Handled = true;
            }
        }

        private void CraftSlot_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("ItemSlotInfo") || e.Data.GetDataPresent(typeof(Item)))
            {
                e.Effects = DragDropEffects.Move;
                e.Handled = true;
                
                if (sender is FrameworkElement element)
                {
                    element.Opacity = 0.7;
                }
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }
        
        private void CraftSlot_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("ItemSlotInfo") || e.Data.GetDataPresent(typeof(Item)))
            {
                e.Effects = DragDropEffects.Move;
                e.Handled = true;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }
        
        private void CraftSlot_DragLeave(object sender, DragEventArgs e)
        {
            if (sender is FrameworkElement element)
            {
                element.Opacity = 1.0;
            }
        }

        private void InventorySlot_SplitStackRequested(object? sender, SplitStackEventArgs e)
        {
            if (ViewModel != null)
            {
                ViewModel.SplitStackCommand.Execute(e);
            }
        }
    }
} 