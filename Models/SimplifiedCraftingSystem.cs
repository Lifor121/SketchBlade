using System;
using System.Collections.Generic;
using System.Linq;
using SketchBlade.Services;
using SketchBlade.Utilities;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Controls;

namespace SketchBlade.Models
{
    [Serializable]
    public class SimplifiedCraftingRecipe
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Dictionary<string, int> RequiredMaterials { get; set; } = new Dictionary<string, int>();
        public Item Result { get; set; } = null!;
        public int ResultQuantity { get; set; } = 1;
        
        public SimplifiedCraftingRecipe() { }
        
        public SimplifiedCraftingRecipe(string name, Item result, Dictionary<string, int> materials, int resultQuantity = 1)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Result = result ?? throw new ArgumentNullException(nameof(result));
            RequiredMaterials = materials ?? throw new ArgumentNullException(nameof(materials));
            ResultQuantity = Math.Max(1, resultQuantity);
            Description = $"Создает {resultQuantity} {result.Name}";
        }

        public bool IsValid()
        {
            return !string.IsNullOrEmpty(Name) && 
                   Result != null && 
                   RequiredMaterials.Count > 0 && 
                   ResultQuantity > 0;
        }
    }

    public interface ICraftingService
    {
        bool CanCraft(SimplifiedCraftingRecipe recipe, Inventory inventory);
        bool Craft(SimplifiedCraftingRecipe recipe, Inventory inventory);
        List<SimplifiedCraftingRecipe> GetAvailableRecipes();
        List<SimplifiedCraftingRecipe> GetCraftableRecipes(Inventory inventory);
        Dictionary<string, int> GetMissingMaterials(SimplifiedCraftingRecipe recipe, Inventory inventory);
    }

    [Serializable]
    public class SimplifiedCraftingSystem : ICraftingService
    {
        private readonly List<SimplifiedCraftingRecipe> _recipes = new List<SimplifiedCraftingRecipe>();
        
        public SimplifiedCraftingSystem()
        {
            InitializeRecipes();
        }

        private void InitializeRecipes()
        {
            try
            {                
                AddValidatedRecipe("Wooden Sword", ItemFactory.CreateWoodenWeapon(), 
                    new Dictionary<string, int> { 
                        { "Дерево", 2 },
                        { "Палка", 1 } 
                    });

                AddValidatedRecipe("Iron Sword", ItemFactory.CreateIronWeapon(),
                    new Dictionary<string, int> { 
                        { "Железный слиток", 2 }, 
                        { "Палка", 1 } 
                    });

                AddValidatedRecipe("Gold Sword", ItemFactory.CreateGoldWeapon(),
                    new Dictionary<string, int> { 
                        { "Золотой слиток", 2 }, 
                        { "Палка", 1 } 
                    });

                AddValidatedRecipe("Luminite Blade", ItemFactory.CreateLuminiteWeapon(),
                    new Dictionary<string, int> { 
                        { "Люминит", 2 }, 
                        { "Золотой слиток", 1 }, 
                        { "Фрагмент люминита", 2 } 
                    });

                AddValidatedRecipe("Stick", CreateStick(), 
                    new Dictionary<string, int> { 
                        { "Дерево", 2 } 
                    }, 4);

                AddValidatedRecipe("Wooden Helmet", ItemFactory.CreateWoodenArmor(ItemSlotType.Head),
                    new Dictionary<string, int> { 
                        { "Дерево", 5 } 
                    });

                AddValidatedRecipe("Iron Helmet", ItemFactory.CreateIronArmor(ItemSlotType.Head),
                    new Dictionary<string, int> { 
                        { "Железный слиток", 5 } 
                    });

                AddValidatedRecipe("Iron Shield", ItemFactory.CreateIronShield(),
                    new Dictionary<string, int> { 
                        { "Железный слиток", 6 }, 
                        { "Дерево", 2 } 
                    });

                AddValidatedRecipe("Healing Potion", ItemFactory.CreateHealingPotion(),
                    new Dictionary<string, int> { 
                        { "Трава", 2 }, 
                        { "Фляга", 1 } 
                    });

                AddValidatedRecipe("Iron Ingot", ItemFactory.CreateIronIngot(),
                    new Dictionary<string, int> { 
                        { "Железная руда", 1 } 
                    });

                LoggingService.LogInfo($"Инициализировано {_recipes.Count} рецептов крафта");
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Ошибка при инициализации рецептов", ex);
            }
        }

        private void AddValidatedRecipe(string name, Item? result, Dictionary<string, int> materials, int quantity = 1)
        {
            if (result == null)
            {
                LoggingService.LogError($"Пропущен рецепт {name} - результат null");
                return;
            }

            var recipe = new SimplifiedCraftingRecipe(name, result, materials, quantity);
            if (recipe.IsValid())
            {
                _recipes.Add(recipe);
            }
            else
            {
                LoggingService.LogError($"Невалидный рецепт: {name}");
            }
        }

        private Item CreateStick()
        {
            return new Item
            {
                Name = "Палка",
                Description = LocalizationService.Instance.GetTranslation("Items.Stick.Description"),
                Type = ItemType.Material,
                Rarity = ItemRarity.Common,
                MaxStackSize = 99,
                Value = 1,
                SpritePath = AssetPaths.Materials.STICK
            };
        }

        public bool CanCraft(SimplifiedCraftingRecipe recipe, Inventory inventory)
        {
            if (recipe?.RequiredMaterials == null || inventory?.Items == null)
                return false;

            try
            {
                foreach (var material in recipe.RequiredMaterials)
                {
                    int available = CountMaterialInInventory(material.Key, inventory);
                    if (available < material.Value)
                    {
                        return false;
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Ошибка при проверке возможности крафта {recipe.Name}", ex);
                return false;
            }
        }

        public bool Craft(SimplifiedCraftingRecipe recipe, Inventory inventory)
        {
            if (!CanCraft(recipe, inventory))
            {
                LoggingService.LogWarning($"Cannot craft {recipe.Name} - insufficient materials");
                return false;
            }

            try
            {
                LoggingService.LogInfo($"Начинаем крафт {recipe.Name}");
                LoggingService.LogInfo($"Требуемые материалы: {string.Join(", ", recipe.RequiredMaterials.Select(m => $"{m.Key}x{m.Value}"))}");
                
                var inventoryItemsBefore = inventory.Items.Where(item => item != null).Select(item => $"{item.Name}x{item.StackSize}").ToList();
                LoggingService.LogInfo($"Предметы в инвентаре до крафта: {string.Join(", ", inventoryItemsBefore)}");
                LoggingService.LogInfo($"Количество не-null предметов до крафта: {inventory.Items.Count(item => item != null)}");
                
                foreach (var material in recipe.RequiredMaterials)
                {
                    LoggingService.LogInfo($"Пытаемся удалить {material.Value} {material.Key}");
                    
                    if (!RemoveMaterialFromInventory(material.Key, material.Value, inventory))
                    {
                        LoggingService.LogError($"Ошибка при удалении материала {material.Key}");
                        return false;
                    }
                    
                    LoggingService.LogInfo($"Успешно удалили {material.Value} {material.Key}");
                }
                
                var resultItem = CreateCraftResult(recipe);
                if (resultItem == null)
                {
                    LoggingService.LogError($"Не удалось создать результат крафта для {recipe.Name}");
                    return false;
                }

                bool addResult = inventory.AddItem(resultItem, recipe.ResultQuantity);
                if (addResult)
                {
                    var inventoryItemsAfterAdd = inventory.Items.Where(item => item != null).Select(item => $"{item.Name}x{item.StackSize}").ToList();
                    LoggingService.LogInfo($"Предметы в инвентаре после добавления результата: {string.Join(", ", inventoryItemsAfterAdd)}");
                    LoggingService.LogInfo($"Количество не-null предметов после добавления: {inventory.Items.Count(item => item != null)}");
                    LoggingService.LogInfo($"Успешно создан предмет: {recipe.Name} x{recipe.ResultQuantity}");
                    
                    inventory.OnInventoryChanged();
                    return true;
                }

                LoggingService.LogError($"Не удалось добавить созданный предмет {recipe.Name} в инвентарь");
                return false;
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Ошибка при крафте {recipe.Name}", ex);
                return false;
            }
        }

        public List<SimplifiedCraftingRecipe> GetAvailableRecipes()
        {
            return new List<SimplifiedCraftingRecipe>(_recipes);
        }

        public List<SimplifiedCraftingRecipe> GetCraftableRecipes(Inventory inventory)
        {
            return _recipes.Where(recipe => CanCraft(recipe, inventory)).ToList();
        }

        public Dictionary<string, int> GetMissingMaterials(SimplifiedCraftingRecipe recipe, Inventory inventory)
        {
            var missing = new Dictionary<string, int>();

            foreach (var material in recipe.RequiredMaterials)
            {
                int available = CountMaterialInInventory(material.Key, inventory);
                if (available < material.Value)
                {
                    missing[material.Key] = material.Value - available;
                }
            }

            return missing;
        }

        private int CountMaterialInInventory(string materialName, Inventory inventory)
        {
            if (string.IsNullOrEmpty(materialName))
                return 0;

            var matchingItems = inventory.Items
                .Where(item => item?.Name == materialName)
                .ToList();
                
            int totalCount = matchingItems.Sum(item => item?.StackSize ?? 0);
            
            return totalCount;
        }

        private bool RemoveMaterialFromInventory(string materialName, int requiredAmount, Inventory inventory)
        {
            if (string.IsNullOrEmpty(materialName) || requiredAmount <= 0)
                return false;

            LoggingService.LogInfo($"Удаляем {requiredAmount} {materialName} из инвентаря");
            
            int remaining = requiredAmount;
            bool anyChanges = false;
            
            for (int i = 0; i < inventory.Items.Count && remaining > 0; i++)
            {
                var item = inventory.Items[i];
                if (item?.Name == materialName)
                {
                    int toRemove = Math.Min(remaining, item.StackSize);
                    LoggingService.LogInfo($"Удаляем {toRemove} из слота {i} (было {item.StackSize})");
                    
                    if (toRemove >= item.StackSize)
                    {
                        inventory.Items[i] = null;
                        remaining -= item.StackSize;
                        anyChanges = true;
                    }
                    else
                    {
                        item.StackSize -= toRemove;
                        item.NotifyPropertyChanged("StackSize");
                        item.NotifyPropertyChanged("Name");
                        
                        remaining -= toRemove;
                        anyChanges = true;
                    }
                    
                    // ИСПРАВЛЕНИЕ: Принудительно обновляем UI сразу после изменения StackSize
                    ForceUpdateInventoryUI();
                }
            }
            
            bool success = remaining == 0;
            LoggingService.LogInfo($"Результат удаления материала: {success}, не найдено: {remaining}");
            return success;
        }

        private Item CreateCraftResult(SimplifiedCraftingRecipe recipe)
        {
            try
            {
                var craftedItem = recipe.Result.Clone();
                craftedItem.StackSize = recipe.ResultQuantity;

                LoggingService.LogInfo($"Создаем результат крафта: {recipe.ResultQuantity} {craftedItem.Name}");
                LoggingService.LogInfo($"Созданный предмет - Name: '{craftedItem.Name}', Type: {craftedItem.Type}, StackSize: {craftedItem.StackSize}, SpritePath: '{craftedItem.SpritePath}'");

                if (string.IsNullOrEmpty(craftedItem.SpritePath))
                {
                    LoggingService.LogWarning($"SpritePath пустой для {craftedItem.Name}, устанавливаем дефолтный");
                    craftedItem.SpritePath = Utilities.AssetPaths.DEFAULT_IMAGE;
                }

                return craftedItem;
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Ошибка при создании результата крафта для {recipe.Name}", ex);
                return null;
            }
        }

        /// <summary>
        /// Принудительное обновление UI инвентаря (аналогично ForceUpdateUIControls в InventoryViewModel)
        /// </summary>
        private void ForceUpdateInventoryUI()
        {
            try
            {
                // Получаем InventoryViewModel из ресурсов приложения
                if (System.Windows.Application.Current?.Resources.Contains("InventoryViewModel") == true)
                {
                    var inventoryViewModel = System.Windows.Application.Current.Resources["InventoryViewModel"] as ViewModels.InventoryViewModel;
                    if (inventoryViewModel != null)
                    {
                        // Используем Dispatcher для обновления на UI потоке
                        System.Windows.Application.Current.Dispatcher.BeginInvoke(new System.Action(() =>
                        {
                            // Вызываем метод принудительного обновления UI
                            var forceUpdateMethod = inventoryViewModel.GetType().GetMethod("ForceUpdateUIControls", 
                                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                            forceUpdateMethod?.Invoke(inventoryViewModel, null);
                        }), System.Windows.Threading.DispatcherPriority.Render);
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"ForceUpdateInventoryUI error: {ex.Message}", ex);
            }
        }

        private IEnumerable<T> FindVisualChildren<T>(System.Windows.DependencyObject depObj) where T : System.Windows.DependencyObject
        {
            if (depObj == null) yield break;
            
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(depObj, i);
                
                if (child is T typedChild)
                    yield return typedChild;
                
                foreach (var childOfChild in FindVisualChildren<T>(child))
                    yield return childOfChild;
            }
        }
    }
} 
