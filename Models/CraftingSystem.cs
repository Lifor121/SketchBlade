using System;
using System.Collections.Generic;
using System.Linq;

namespace SketchBlade.Models
{
    [Serializable]
    public class CraftingPattern
    {
        public List<string?> Grid { get; set; } = new List<string?>();
        
        public CraftingPattern()
        {
            // Initialize a 3x3 grid with null values
            for (int i = 0; i < 9; i++)
            {
                Grid.Add(null);
            }
        }
        
        // Constructor to create pattern from a 3x3 array
        public CraftingPattern(string?[,] pattern)
        {
            Grid = new List<string?>();
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    Grid.Add(pattern[i, j]);
                }
            }
        }
        
        // Constructor to create pattern from a list
        public CraftingPattern(List<string?> pattern)
        {
            if (pattern.Count != 9)
            {
                throw new ArgumentException("Pattern must have exactly 9 elements");
            }
            Grid = new List<string?>(pattern);
        }
        
        // Helper method to get the value at a specific row and column
        public string? GetAt(int row, int col)
        {
            if (row < 0 || row >= 3 || col < 0 || col >= 3)
            {
                throw new ArgumentOutOfRangeException("Position is outside the 3x3 grid");
            }
            
            return Grid[row * 3 + col];
        }
        
        // Helper method to set the value at a specific row and column
        public void SetAt(int row, int col, string? value)
        {
            if (row < 0 || row >= 3 || col < 0 || col >= 3)
            {
                throw new ArgumentOutOfRangeException("Position is outside the 3x3 grid");
            }
            
            Grid[row * 3 + col] = value;
        }
        
        // Check if this pattern matches another pattern
        public bool Matches(CraftingPattern other)
        {
            if (other == null || other.Grid == null || Grid == null)
                return false;
                
            // For each position in the recipe pattern
            for (int i = 0; i < 9; i++)
            {
                // If recipe has a material requirement at this position
                if (Grid[i] != null)
                {
                    // Check if the user pattern has the matching material at this position
                    if (other.Grid[i] != Grid[i])
                    {
                        return false;
                    }
                }
            }
            
            return true;
        }
    }
    
    [Serializable]
    public class CraftingRecipe
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Dictionary<string, int> Materials { get; set; } = new Dictionary<string, int>();
        public CraftingPattern? Pattern { get; set; }
        public Item Result { get; set; } = null!;
        public ItemType ResultType { get; set; }
        public ItemRarity ResultRarity { get; set; }
        public int ResultQuantity { get; set; } = 1;
        
        public CraftingRecipe() { }
        
        // Constructor for materials-based recipes
        public CraftingRecipe(string name, Item result, Dictionary<string, int> materials, int resultQuantity = 1)
        {
            Name = name;
            Result = result;
            Materials = materials;
            ResultQuantity = resultQuantity;
            ResultType = result.Type;
            ResultRarity = result.Rarity;
            Description = $"Creates {resultQuantity} {result.Name}(s)";
        }
        
        // Constructor for pattern-based recipes
        public CraftingRecipe(string name, Item result, CraftingPattern pattern, int resultQuantity = 1)
        {
            Name = name;
            Result = result;
            Pattern = pattern;
            ResultQuantity = resultQuantity;
            ResultType = result.Type;
            ResultRarity = result.Rarity;
            Description = $"Creates {resultQuantity} {result.Name}(s)";
            
            // Also calculate the total materials needed for this recipe
            CalculateMaterialsFromPattern(pattern);
        }
        
        // Helper method to calculate the total materials needed for this pattern
        private void CalculateMaterialsFromPattern(CraftingPattern pattern)
        {
            // Count the materials required by the pattern
            foreach (var material in pattern.Grid)
            {
                if (material != null)
                {
                    if (Materials.ContainsKey(material))
                    {
                        Materials[material]++;
                    }
                    else
                    {
                        Materials[material] = 1;
                    }
                }
            }
        }
    }
    
    [Serializable]
    public class CraftingSystem
    {
        private List<CraftingRecipe> _recipes = new List<CraftingRecipe>();
        private GameState _gameState;
        
        public List<CraftingRecipe> Recipes => _recipes;
        public GameState GameState => _gameState;
        
        public CraftingSystem(GameState gameState)
        {
            _gameState = gameState;
            InitializeRecipes();
        }
        
        private void InitializeRecipes()
        {
            // Create material-based recipes instead of pattern-based recipes
            
            // Wooden Sword recipe
            var woodenSwordRecipe = new CraftingRecipe(
                "Wooden Sword",
                ItemFactory.CreateWoodenWeapon(),
                new Dictionary<string, int> { { "Wood", 2 }, { "Stick", 1 } }
            );
            _recipes.Add(woodenSwordRecipe);
            
            // Iron Sword recipe
            var ironSwordRecipe = new CraftingRecipe(
                "Iron Sword",
                ItemFactory.CreateIronWeapon(),
                new Dictionary<string, int> { { "Iron Ingot", 2 }, { "Stick", 1 } }
            );
            _recipes.Add(ironSwordRecipe);
            
            // Gold Sword recipe
            var goldSwordRecipe = new CraftingRecipe(
                "Gold Sword",
                ItemFactory.CreateGoldWeapon(),
                new Dictionary<string, int> { { "Gold Ingot", 2 }, { "Stick", 1 } }
            );
            _recipes.Add(goldSwordRecipe);
            
            // Luminite Weapon recipe
            var luminiteWeaponRecipe = new CraftingRecipe(
                "Luminite Blade",
                ItemFactory.CreateLuminiteWeapon(),
                new Dictionary<string, int> { 
                    { "Luminite", 2 }, 
                    { "Gold Ingot", 1 }, 
                    { "Luminite Fragment", 2 } 
                }
            );
            _recipes.Add(luminiteWeaponRecipe);
            
            // Stick recipe
            var stickRecipe = new CraftingRecipe(
                "Stick",
                new Item 
                {
                    Name = "Stick",
                    Description = "Basic crafting material for tools and weapons",
                    Type = ItemType.Material,
                    Rarity = ItemRarity.Common,
                    MaxStackSize = 99,
                    Value = 1,
                    SpritePath = "Assets/Items/Materials/stick.png"
                },
                new Dictionary<string, int> { { "Wood", 2 } },
                4 // Get 4 sticks from the recipe
            );
            _recipes.Add(stickRecipe);
            
            // Wooden Helmet recipe
            var woodenHelmetRecipe = new CraftingRecipe(
                "Wooden Helmet",
                ItemFactory.CreateWoodenArmor(ItemSlotType.Head),
                new Dictionary<string, int> { { "Wood", 5 } }
            );
            _recipes.Add(woodenHelmetRecipe);
            
            // Wooden Chestplate recipe
            var woodenChestplateRecipe = new CraftingRecipe(
                "Wooden Chestplate",
                ItemFactory.CreateWoodenArmor(ItemSlotType.Chest),
                new Dictionary<string, int> { { "Wood", 8 } }
            );
            _recipes.Add(woodenChestplateRecipe);
            
            // Wooden Leggings recipe
            var woodenLeggingsRecipe = new CraftingRecipe(
                "Wooden Leggings",
                ItemFactory.CreateWoodenArmor(ItemSlotType.Legs),
                new Dictionary<string, int> { { "Wood", 7 } }
            );
            _recipes.Add(woodenLeggingsRecipe);
            
            // Iron Helmet recipe
            var ironHelmetRecipe = new CraftingRecipe(
                "Iron Helmet",
                ItemFactory.CreateIronArmor(ItemSlotType.Head),
                new Dictionary<string, int> { { "Iron Ingot", 5 } }
            );
            _recipes.Add(ironHelmetRecipe);
            
            // Iron Chestplate recipe
            var ironChestplateRecipe = new CraftingRecipe(
                "Iron Chestplate",
                ItemFactory.CreateIronArmor(ItemSlotType.Chest),
                new Dictionary<string, int> { { "Iron Ingot", 8 } }
            );
            _recipes.Add(ironChestplateRecipe);
            
            // Iron Leggings recipe
            var ironLeggingsRecipe = new CraftingRecipe(
                "Iron Leggings",
                ItemFactory.CreateIronArmor(ItemSlotType.Legs),
                new Dictionary<string, int> { { "Iron Ingot", 7 } }
            );
            _recipes.Add(ironLeggingsRecipe);
            
            // Gold Helmet recipe
            var goldHelmetRecipe = new CraftingRecipe(
                "Gold Helmet",
                ItemFactory.CreateGoldArmor(ItemSlotType.Head),
                new Dictionary<string, int> { { "Gold Ingot", 5 } }
            );
            _recipes.Add(goldHelmetRecipe);
            
            // Gold Chestplate recipe
            var goldChestplateRecipe = new CraftingRecipe(
                "Gold Chestplate",
                ItemFactory.CreateGoldArmor(ItemSlotType.Chest),
                new Dictionary<string, int> { { "Gold Ingot", 8 } }
            );
            _recipes.Add(goldChestplateRecipe);
            
            // Gold Leggings recipe
            var goldLeggingsRecipe = new CraftingRecipe(
                "Gold Leggings",
                ItemFactory.CreateGoldArmor(ItemSlotType.Legs),
                new Dictionary<string, int> { { "Gold Ingot", 7 } }
            );
            _recipes.Add(goldLeggingsRecipe);
            
            // Luminite Helmet recipe
            var luminiteHelmetRecipe = new CraftingRecipe(
                "Luminite Helmet",
                ItemFactory.CreateLuminiteArmor(ItemSlotType.Head),
                new Dictionary<string, int> { 
                    { "Luminite", 3 }, 
                    { "Luminite Fragment", 2 },
                    { "Gold Ingot", 2 }
                }
            );
            _recipes.Add(luminiteHelmetRecipe);
            
            // Luminite Chestplate recipe
            var luminiteChestplateRecipe = new CraftingRecipe(
                "Luminite Chestplate",
                ItemFactory.CreateLuminiteArmor(ItemSlotType.Chest),
                new Dictionary<string, int> { 
                    { "Luminite", 5 }, 
                    { "Luminite Fragment", 3 },
                    { "Gold Ingot", 3 }
                }
            );
            _recipes.Add(luminiteChestplateRecipe);
            
            // Luminite Leggings recipe
            var luminiteLeggingsRecipe = new CraftingRecipe(
                "Luminite Leggings",
                ItemFactory.CreateLuminiteArmor(ItemSlotType.Legs),
                new Dictionary<string, int> { 
                    { "Luminite", 4 }, 
                    { "Luminite Fragment", 3 },
                    { "Gold Ingot", 2 }
                }
            );
            _recipes.Add(luminiteLeggingsRecipe);
            
            // Iron Shield recipe
            var ironShieldRecipe = new CraftingRecipe(
                "Iron Shield",
                ItemFactory.CreateIronShield(),
                new Dictionary<string, int> { { "Iron Ingot", 6 }, { "Wood", 2 } }
            );
            _recipes.Add(ironShieldRecipe);
            
            // Gold Shield recipe
            var goldShieldRecipe = new CraftingRecipe(
                "Gold Shield",
                ItemFactory.CreateGoldShield(),
                new Dictionary<string, int> { { "Gold Ingot", 6 }, { "Iron Ingot", 2 } }
            );
            _recipes.Add(goldShieldRecipe);
            
            // Luminite Shield recipe
            var luminiteShieldRecipe = new CraftingRecipe(
                "Luminite Shield",
                ItemFactory.CreateLuminiteShield(),
                new Dictionary<string, int> { 
                    { "Luminite", 3 }, 
                    { "Luminite Fragment", 3 },
                    { "Gold Ingot", 3 }
                }
            );
            _recipes.Add(luminiteShieldRecipe);
            
            // Iron Ingot from Iron Ore (smelting recipe)
            var ironIngotRecipe = new CraftingRecipe(
                "Iron Ingot",
                ItemFactory.CreateIronIngot(),
                new Dictionary<string, int> { { "Iron Ore", 1 } }
            );
            _recipes.Add(ironIngotRecipe);
            
            // Gold Ingot from Gold Ore (smelting recipe)
            var goldIngotRecipe = new CraftingRecipe(
                "Gold Ingot",
                ItemFactory.CreateGoldIngot(),
                new Dictionary<string, int> { { "Gold Ore", 1 } }
            );
            _recipes.Add(goldIngotRecipe);
            
            // Luminite from Fragments
            var luminiteRecipe = new CraftingRecipe(
                "Luminite",
                ItemFactory.CreateLuminite(),
                new Dictionary<string, int> { { "Luminite Fragment", 4 } }
            );
            _recipes.Add(luminiteRecipe);
            
            // Healing Potion recipe
            var healingPotionRecipe = new CraftingRecipe(
                "Healing Potion",
                ItemFactory.CreateHealingPotion(),
                new Dictionary<string, int> { { "Herb", 2 }, { "Flask", 1 } }
            );
            _recipes.Add(healingPotionRecipe);
            
            // Rage Potion recipe
            var ragePotionRecipe = new CraftingRecipe(
                "Rage Potion",
                ItemFactory.CreateRagePotion(),
                new Dictionary<string, int> { { "Crystal Dust", 2 }, { "Flask", 1 } }
            );
            _recipes.Add(ragePotionRecipe);
            
            // Invulnerability Potion recipe
            var invulnerabilityPotionRecipe = new CraftingRecipe(
                "Invulnerability Potion",
                ItemFactory.CreateInvulnerabilityPotion(),
                new Dictionary<string, int> { { "Luminite Fragment", 1 }, { "Crystal Dust", 2 }, { "Flask", 1 } }
            );
            _recipes.Add(invulnerabilityPotionRecipe);
            
            // Bomb recipe
            var bombRecipe = new CraftingRecipe(
                "Bomb",
                ItemFactory.CreateBomb(),
                new Dictionary<string, int> { { "Gunpowder", 2 }, { "Iron Ingot", 1 } }
            );
            _recipes.Add(bombRecipe);
            
            // Pillow recipe
            var pillowRecipe = new CraftingRecipe(
                "Pillow",
                ItemFactory.CreatePillow(),
                new Dictionary<string, int> { { "Cloth", 3 }, { "Feather", 2 } }
            );
            _recipes.Add(pillowRecipe);
            
            // Poisoned Shuriken recipe
            var poisonedShurikenRecipe = new CraftingRecipe(
                "Poisoned Shuriken",
                ItemFactory.CreatePoisonedShuriken(),
                new Dictionary<string, int> { { "Iron Ingot", 1 }, { "Poison Extract", 1 } }
            );
            _recipes.Add(poisonedShurikenRecipe);
        }
        
        // Check if a craft is possible with the given inventory
        public bool CanCraft(CraftingRecipe recipe, Inventory inventory)
        {
            if (recipe == null || inventory == null) 
            {
                return false;
            }
            
            try
            {
                // Check if we have all required materials
                foreach (var materialPair in recipe.Materials)
                {
                    string materialName = materialPair.Key;
                    int requiredAmount = materialPair.Value;
                    
                    // Count how many of this material we have
                    int availableAmount = 0;
                    
                    // Сначала проверяем слоты крафта
                    foreach (var item in inventory.CraftItems.Where(i => i?.Name == materialName))
                    {
                        if (item != null)
                        {
                            availableAmount += item.StackSize;
                            if (availableAmount >= requiredAmount)
                                break;
                        }
                    }
                    
                    // Если еще нужны материалы, проверяем основной инвентарь
                    if (availableAmount < requiredAmount)
                    {
                        foreach (var item in inventory.Items.Where(i => i?.Name == materialName))
                        {
                            if (item != null)
                            {
                                availableAmount += item.StackSize;
                                if (availableAmount >= requiredAmount)
                                    break;
                            }
                        }
                    }
                    
                    if (availableAmount < requiredAmount)
                    {
                        return false;
                    }
                }
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in CanCraft: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }
        
        // Check if a pattern-based craft is possible
        public bool CanCraftPattern(CraftingRecipe recipe, CraftingPattern userPattern, Inventory inventory)
        {
            // With the simplified crafting system, we only need to check if we have the required materials
            // We no longer require the exact pattern to match
            
            // Count the materials in the userPattern
            Dictionary<string, int> providedMaterials = new Dictionary<string, int>();
            foreach (var material in userPattern.Grid)
            {
                if (material != null)
                {
                    if (providedMaterials.ContainsKey(material))
                    {
                        providedMaterials[material]++;
                    }
                    else
                    {
                        providedMaterials[material] = 1;
                    }
                }
            }
            
            // Check if we have enough of each required material in the pattern
            foreach (var material in recipe.Materials)
            {
                if (!providedMaterials.ContainsKey(material.Key) || providedMaterials[material.Key] < material.Value)
                {
                    return false;
                }
            }
            
            return true;
        }
        
        // Attempt to craft an item and update inventory
        public bool Craft(CraftingRecipe recipe, Inventory inventory, GameState gameState)
        {
            if (recipe == null || inventory == null || gameState == null)
            {
                Console.WriteLine("CraftingSystem.Craft: Recipe, inventory, or gameState is null");
                return false;
            }
                
            if (!CanCraft(recipe, inventory))
            {
                Console.WriteLine("CraftingSystem.Craft: Cannot craft this recipe with current inventory");
                return false;
            }
            
            Console.WriteLine($"CraftingSystem.Craft: Starting to craft {recipe.Name}");
            Console.WriteLine($"Required materials:");
            foreach (var material in recipe.Materials)
            {
                Console.WriteLine($" - {material.Key}: {material.Value}");
            }
            
            try
            {
                // Сначала создаем копию скрафченного предмета - чтобы убедиться, что у нас есть что добавить
                var craftedItem = recipe.Result.Clone();
                if (craftedItem == null)
                {
                    Console.WriteLine("Error: Failed to create crafted item");
                    return false;
                }
                
                // Set the correct stack size based on recipe
                craftedItem.StackSize = recipe.ResultQuantity;
                
                Console.WriteLine($"Created crafted item: {craftedItem.Name} x{craftedItem.StackSize}");
                
                // Обязательно проверяем место в инвентаре до удаления предметов
                bool hasSpace = false;
                
                // Сначала проверяем пустые слоты
                for (int i = 0; i < inventory.Items.Count; i++)
                {
                    if (inventory.Items[i] == null)
                    {
                        hasSpace = true;
                        break;
                    }
                }
                
                // Затем проверяем стакающиеся предметы
                if (!hasSpace && craftedItem.IsStackable)
                {
                    for (int i = 0; i < inventory.Items.Count; i++)
                    {
                        var existingItem = inventory.Items[i];
                        if (existingItem != null && 
                            existingItem.Name == craftedItem.Name && 
                            existingItem.Type == craftedItem.Type && 
                            existingItem.StackSize < existingItem.MaxStackSize)
                        {
                            hasSpace = true;
                            break;
                        }
                    }
                }
                
                if (!hasSpace)
                {
                    Console.WriteLine("Error: No space in inventory for crafted item");
                    return false;
                }
                
                // Также очищаем слоты крафта, если они использовались
                bool shouldClearCraftSlots = false;
                
                // Создаем временный словарь для отслеживания, сколько материалов еще нужно удалить
                Dictionary<string, int> materialsToRemove = new Dictionary<string, int>();
                foreach (var material in recipe.Materials)
                {
                    materialsToRemove[material.Key] = material.Value;
                }
                
                // Сначала проверяем, есть ли материалы в сетке крафта
                for (int i = 0; i < inventory.CraftItems.Count; i++)
                {
                    var craftItem = inventory.CraftItems[i];
                    if (craftItem != null && materialsToRemove.ContainsKey(craftItem.Name) && materialsToRemove[craftItem.Name] > 0)
                    {
                        shouldClearCraftSlots = true;
                        
                        if (craftItem.StackSize <= materialsToRemove[craftItem.Name])
                        {
                            // Remove entire stack
                            materialsToRemove[craftItem.Name] -= craftItem.StackSize;
                            inventory.CraftItems[i] = null;
                        }
                        else
                        {
                            // Remove partial stack
                            craftItem.StackSize -= materialsToRemove[craftItem.Name];
                            materialsToRemove[craftItem.Name] = 0;
                        }
                    }
                }
                
                // Затем удаляем оставшиеся материалы из основного инвентаря
                foreach (var materialPair in materialsToRemove.ToList())
                {
                    string materialName = materialPair.Key;
                    int remainingToRemove = materialPair.Value;
                    
                    if (remainingToRemove <= 0)
                        continue;
                        
                    // Получаем все предметы этого типа из инвентаря
                    for (int i = 0; i < inventory.Items.Count; i++)
                    {
                        var item = inventory.Items[i];
                        if (item != null && item.Name == materialName && remainingToRemove > 0)
                        {
                            if (item.StackSize <= remainingToRemove)
                            {
                                // Remove entire stack
                                remainingToRemove -= item.StackSize;
                                inventory.Items[i] = null;
                            }
                            else
                            {
                                // Remove partial stack
                                item.StackSize -= remainingToRemove;
                                remainingToRemove = 0;
                            }
                        }
                        
                        if (remainingToRemove <= 0)
                            break;
                    }
                    
                    if (remainingToRemove > 0)
                    {
                        Console.WriteLine($"Error: Failed to remove all required {materialName}, missing {remainingToRemove}");
                        return false;
                    }
                }
                
                // Если слоты крафта использовались, очищаем их
                if (shouldClearCraftSlots)
                {
                    Console.WriteLine("Clearing craft slots");
                    for (int i = 0; i < inventory.CraftItems.Count; i++)
                    {
                        inventory.CraftItems[i] = null;
                    }
                }
                
                // Добавляем скрафченный предмет в инвентарь
                Console.WriteLine($"Adding crafted item to inventory: {craftedItem.Name} x{craftedItem.StackSize}");
                
                // Находим пустой слот или существующий стак
                bool itemAdded = false;
                
                // Сначала пробуем добавить к существующему стаку
                if (craftedItem.IsStackable)
                {
                    for (int i = 0; i < inventory.Items.Count; i++)
                    {
                        var existingItem = inventory.Items[i];
                        if (existingItem != null && 
                            existingItem.Name == craftedItem.Name && 
                            existingItem.Type == craftedItem.Type && 
                            existingItem.StackSize < existingItem.MaxStackSize)
                        {
                            // Сколько можно добавить в этот стак
                            int canAdd = existingItem.MaxStackSize - existingItem.StackSize;
                            int toAdd = Math.Min(canAdd, craftedItem.StackSize);
                            
                            existingItem.StackSize += toAdd;
                            craftedItem.StackSize -= toAdd;
                            
                            Console.WriteLine($"Added {toAdd} to existing stack, remaining: {craftedItem.StackSize}");
                            
                            if (craftedItem.StackSize <= 0)
                            {
                                itemAdded = true;
                                break;
                            }
                        }
                    }
                }
                
                // Если еще осталось что добавить, ищем пустой слот
                if (!itemAdded || craftedItem.StackSize > 0)
                {
                    for (int i = 0; i < inventory.Items.Count; i++)
                    {
                        if (inventory.Items[i] == null)
                        {
                            inventory.Items[i] = craftedItem;
                            Console.WriteLine($"Added item to empty slot {i}");
                            itemAdded = true;
                            break;
                        }
                    }
                }
                
                if (!itemAdded)
                {
                    Console.WriteLine("Failed to add crafted item to inventory!");
                    return false;
                }
                
                // Используем публичный метод OnInventoryChanged, который сам вызовет OnPropertyChanged
                inventory.OnInventoryChanged();
                
                // Принудительно обновляем UI вызывая ForceUIUpdate в InventoryViewModel
                if (gameState.CurrentScreenViewModel is ViewModels.InventoryViewModel inventoryViewModel)
                {
                    Console.WriteLine("Calling ForceUIUpdate from InventoryViewModel to immediately refresh UI");
                    inventoryViewModel.ForceUIUpdate();
                    
                    // Обновляем отображение рецептов
                    inventoryViewModel.RefreshCraftResultCommand?.Execute(null);
                }
                
                // Обновляем все предметы, чтобы гарантировать обновление UI
                for (int i = 0; i < inventory.Items.Count; i++)
                {
                    if (inventory.Items[i] != null)
                    {
                        inventory.Items[i].NotifyPropertyChanged("StackSize");
                        inventory.Items[i].NotifyPropertyChanged("Name");
                    }
                }
                
                Console.WriteLine("Crafting completed successfully");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in CraftingSystem.Craft: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }
        
        // Get all available recipes
        public List<CraftingRecipe> GetAvailableRecipes()
        {
            try
            {
                Console.WriteLine("GetAvailableRecipes: Получение доступных рецептов");
                // Локализуем названия рецептов
                foreach (var recipe in _recipes)
                {
                    if (recipe != null)
                    {
                        // Получаем локализованное название рецепта
                        string localizedName = Services.ItemLocalizationService.GetLocalizedRecipeName(recipe.Name);
                        
                        // Если локализация найдена и отличается от оригинального названия
                        if (!string.IsNullOrEmpty(localizedName) && localizedName != recipe.Name)
                        {
                            recipe.Name = localizedName;
                        }
                        
                        // Также локализуем описание, если оно основано на имени предмета
                        if (recipe.Result != null && recipe.Description.Contains(recipe.Result.Name))
                        {
                            string localizedItemName = Services.ItemLocalizationService.GetLocalizedItemName(recipe.Result.Name);
                            recipe.Description = $"Creates {recipe.ResultQuantity} {localizedItemName}(s)";
                        }
                    }
                }
                
                return _recipes;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetAvailableRecipes: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return _recipes;
            }
        }
        
        // Get recipes that use a specific material
        public List<CraftingRecipe> GetRecipesUsingMaterial(string materialName)
        {
            return _recipes.Where(r => r.Materials.ContainsKey(materialName)).ToList();
        }
        
        // Find a recipe that matches materials in a pattern
        public CraftingRecipe? FindRecipeByPattern(CraftingPattern pattern)
        {
            if (pattern == null || pattern.Grid == null)
            {
                Console.WriteLine("FindRecipeByPattern: pattern или pattern.Grid равны null");
                return null;
            }
                
            try
            {
                // Count the materials in the provided pattern
                Dictionary<string, int> providedMaterials = new Dictionary<string, int>();
                bool hasAnyMaterials = false;
                
                Console.WriteLine("FindRecipeByPattern: Анализ паттерна:");
                for (int i = 0; i < pattern.Grid.Count && i < 9; i++)
                {
                    string? material = pattern.Grid[i];
                    if (!string.IsNullOrEmpty(material))
                    {
                        hasAnyMaterials = true;
                        if (providedMaterials.ContainsKey(material))
                        {
                            providedMaterials[material]++;
                        }
                        else
                        {
                            providedMaterials[material] = 1;
                        }
                        Console.WriteLine($"  Материал: {material}, количество: {providedMaterials[material]}");
                    }
                }
                
                if (!hasAnyMaterials)
                {
                    Console.WriteLine("FindRecipeByPattern: Паттерн не содержит материалов");
                    return null;
                }
                
                // Find a recipe that matches the provided materials
                Console.WriteLine($"FindRecipeByPattern: Поиск рецепта среди {_recipes.Count} рецептов");
                foreach (var recipe in _recipes)
                {
                    bool allMaterialsMatch = true;
                    Dictionary<string, int> requiredMaterials = new Dictionary<string, int>(recipe.Materials);
                    
                    Console.WriteLine($"FindRecipeByPattern: Проверка рецепта '{recipe.Name}'");
                    
                    // Check if all provided materials match the recipe
                    foreach (var material in providedMaterials)
                    {
                        string materialName = material.Key;
                        int providedCount = material.Value;
                        
                        // Check if the recipe requires this material
                        if (!requiredMaterials.ContainsKey(materialName) || requiredMaterials[materialName] != providedCount)
                        {
                            allMaterialsMatch = false;
                            Console.WriteLine($"  Материал '{materialName}' не соответствует рецепту");
                            break;
                        }
                        else
                        {
                            Console.WriteLine($"  Материал '{materialName}' соответствует рецепту");
                        }
                    }
                    
                    // Check if all required materials are provided
                    if (allMaterialsMatch)
                    {
                        foreach (var material in requiredMaterials)
                        {
                            string materialName = material.Key;
                            int requiredCount = material.Value;
                            
                            if (!providedMaterials.ContainsKey(materialName) || providedMaterials[materialName] != requiredCount)
                            {
                                allMaterialsMatch = false;
                                Console.WriteLine($"  Не хватает материала '{materialName}' для рецепта");
                                break;
                            }
                        }
                    }
                    
                    if (allMaterialsMatch)
                    {
                        Console.WriteLine($"FindRecipeByPattern: Найден соответствующий рецепт '{recipe.Name}'");
                        return recipe;
                    }
                }
                
                Console.WriteLine("FindRecipeByPattern: Соответствующий рецепт не найден");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в FindRecipeByPattern: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return null;
            }
        }
    }
} 