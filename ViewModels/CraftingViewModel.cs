using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using SketchBlade.Models;
using System.Windows.Media.Imaging;

namespace SketchBlade.ViewModels
{
    public class MaterialViewModel : INotifyPropertyChanged
    {
        private string _name;
        private int _amount;
        private int _available;
        private bool _isAvailable;

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged();
            }
        }

        public int Amount
        {
            get => _amount;
            set
            {
                _amount = value;
                OnPropertyChanged();
            }
        }

        public int Available
        {
            get => _available;
            set
            {
                _available = value;
                IsAvailable = _available >= _amount;
                OnPropertyChanged();
            }
        }

        public bool IsAvailable
        {
            get => _isAvailable;
            set
            {
                _isAvailable = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class RecipeBookEntryViewModel : INotifyPropertyChanged
    {
        private CraftingRecipe _recipe;
        private bool _canCraft;
        private System.Windows.Media.Imaging.BitmapImage _resultIcon;
        private string _materialsText;

        public RecipeBookEntryViewModel(CraftingRecipe recipe, bool canCraft, System.Windows.Media.Imaging.BitmapImage resultIcon)
        {
            _recipe = recipe;
            _canCraft = canCraft;
            _resultIcon = resultIcon;
            
            // Generate materials text
            _materialsText = string.Join(", ", recipe.Materials.Select(m => $"{m.Value} {m.Key}"));
        }

        public string Name => _recipe.Name;
        public string Description => _recipe.Description;
        public System.Windows.Media.Imaging.BitmapImage ResultIcon => _resultIcon;
        public string MaterialsText => _materialsText;
        public bool CanCraft
        {
            get => _canCraft;
            set
            {
                _canCraft = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class CraftingRecipeViewModel : INotifyPropertyChanged
    {
        private CraftingRecipe _recipe;
        private bool _isSelected;
        private bool _canCraft;
        
        public CraftingRecipeViewModel(CraftingRecipe recipe)
        {
            _recipe = recipe;
        }
        
        public string Name => _recipe.Name;
        public string Description => _recipe.Description;
        public CraftingRecipe Recipe => _recipe;
        
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }
        
        public bool CanCraft
        {
            get => _canCraft;
            set
            {
                _canCraft = value;
                OnPropertyChanged();
            }
        }
        
        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class CraftingViewModel : INotifyPropertyChanged
    {
        private readonly GameState _gameState;
        private readonly Action<string> _navigateAction;
        private ObservableCollection<CraftingRecipeViewModel> _availableRecipes = new ObservableCollection<CraftingRecipeViewModel>();
        private ObservableCollection<MaterialViewModel> _requiredMaterials = new ObservableCollection<MaterialViewModel>();
        private ObservableCollection<RecipeBookEntry> _recipeBookEntries = new ObservableCollection<RecipeBookEntry>();
        private ObservableCollection<Item?> _craftingGrid = new ObservableCollection<Item?>();
        private CraftingRecipeViewModel? _selectedRecipe;
        private Item? _craftResult;
        private bool _canCraft;
        private bool _showItemStats;
        private bool _showDamage;
        private bool _showDefense;
        private bool _isRecipeBookOpen;
        private System.Windows.Controls.Border? _recipeBookPopupHost;

        public ObservableCollection<CraftingRecipeViewModel> AvailableRecipes
        {
            get => _availableRecipes;
            set
            {
                _availableRecipes = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<MaterialViewModel> RequiredMaterials
        {
            get => _requiredMaterials;
            set
            {
                _requiredMaterials = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<RecipeBookEntry> RecipeBookEntries
        {
            get => _recipeBookEntries;
            set
            {
                _recipeBookEntries = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<Item?> CraftingGrid
        {
            get => _craftingGrid;
            set
            {
                _craftingGrid = value;
                OnPropertyChanged();
            }
        }

        public Item? CraftResult
        {
            get => _craftResult;
            set
            {
                _craftResult = value;
                OnPropertyChanged();
            }
        }

        public CraftingRecipeViewModel? SelectedRecipe
        {
            get => _selectedRecipe;
            set
            {
                _selectedRecipe = value;
                HasSelectedRecipe = value != null;
                UpdateRequiredMaterials();
                UpdateItemStats();
                OnPropertyChanged();
            }
        }

        public bool HasSelectedRecipe { get; private set; }

        public bool CanCraft
        {
            get => _canCraft;
            set
            {
                _canCraft = value;
                OnPropertyChanged();
            }
        }

        public bool ShowItemStats
        {
            get => _showItemStats;
            set
            {
                _showItemStats = value;
                OnPropertyChanged();
            }
        }

        public bool ShowDamage
        {
            get => _showDamage;
            set
            {
                _showDamage = value;
                OnPropertyChanged();
            }
        }

        public bool ShowDefense
        {
            get => _showDefense;
            set
            {
                _showDefense = value;
                OnPropertyChanged();
            }
        }

        public bool IsRecipeBookOpen
        {
            get => _isRecipeBookOpen;
            set
            {
                _isRecipeBookOpen = value;
                OnPropertyChanged();
            }
        }

        public ICommand NavigateCommand { get; }
        public ICommand CraftItemCommand { get; }
        public ICommand OpenRecipeBookCommand { get; }
        public ICommand CloseRecipeBookCommand { get; }
        public ICommand CraftFromPatternCommand { get; }
        public ICommand ClearCraftingGridCommand { get; }

        public event PropertyChangedEventHandler? PropertyChanged;

        public CraftingViewModel(GameState gameState, Action<string> navigateAction, System.Windows.Controls.Border? recipeBookPopupHost = null)
        {
            _gameState = gameState;
            _navigateAction = navigateAction;
            
            // Безопасно устанавливаем _recipeBookPopupHost
            if (recipeBookPopupHost != null)
            {
                try
                {
                    // Проверяем, что элемент находится в визуальном дереве или может быть использован
                    bool isInVisualTree = System.Windows.PresentationSource.FromVisual(recipeBookPopupHost) != null;
                    
                    if (isInVisualTree)
                    {
                        _recipeBookPopupHost = recipeBookPopupHost;
                        Console.WriteLine("RecipeBookPopupHost успешно установлен в конструкторе");
                    }
                    else
                    {
                        Console.WriteLine("RecipeBookPopupHost не находится в визуальном дереве, не устанавливаем его в конструкторе");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при установке RecipeBookPopupHost в конструкторе: {ex.Message}");
                }
            }

            NavigateCommand = new RelayCommand<string>(NavigateToScreen);
            CraftItemCommand = new RelayCommand(_ => CraftSelectedItem(), _ => CanCraft);
            OpenRecipeBookCommand = new RelayCommand(_ => OpenRecipeBook());
            CloseRecipeBookCommand = new RelayCommand(_ => CloseRecipeBook());
            CraftFromPatternCommand = new RelayCommand(_ => CraftFromPattern());
            ClearCraftingGridCommand = new RelayCommand(_ => ClearCraftingGrid());

            // Initialize the crafting grid with 9 empty slots
            for (int i = 0; i < 9; i++)
            {
                _craftingGrid.Add(null);
            }

            // Initial load of recipes
            LoadAvailableRecipes();

            // Subscribe to inventory changes to update recipe availability
            _gameState.Inventory.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(Inventory.Items))
                {
                    LoadAvailableRecipes();
                    UpdateRequiredMaterials();
                    UpdateCraftingPatternResult();
                }
            };
            
            // Подписка на событие InventoryChanged для более надежного обновления UI
            _gameState.Inventory.InventoryChanged += (s, e) =>
            {
                LoadAvailableRecipes();
                UpdateRequiredMaterials();
                UpdateCraftingPatternResult();
            };
        }

        private void NavigateToScreen(string screenName)
        {
            switch (screenName)
            {
                case "InventoryView":
                    _navigateAction("InventoryView");
                    break;
                default:
                    _navigateAction("InventoryView");
                    break;
            }
        }

        private void LoadAvailableRecipes()
        {
            try
            {
                Console.WriteLine("LoadAvailableRecipes: Обновление списка доступных рецептов");
                
                // Получаем текущий выбранный рецепт, если есть
                string selectedRecipeName = SelectedRecipe?.Name;
                
                // Очищаем список доступных рецептов
                AvailableRecipes.Clear();
                
                // Получаем доступные рецепты из системы крафта
                var availableRecipes = _gameState.CraftingSystem.GetAvailableRecipes();
                
                // Добавляем рецепты в коллекцию ViewModel
                foreach (var recipe in availableRecipes)
                {
                    var recipeViewModel = new CraftingRecipeViewModel(recipe);
                    recipeViewModel.CanCraft = _gameState.CraftingSystem.CanCraft(recipe, _gameState.Inventory);
                    AvailableRecipes.Add(recipeViewModel);
                    
                    // Если этот рецепт был выбран ранее, выбираем его снова
                    if (recipe.Name == selectedRecipeName)
                    {
                        SelectedRecipe = recipeViewModel;
                        recipeViewModel.IsSelected = true;
                    }
                }
                
                // Обновляем UI
                OnPropertyChanged(nameof(AvailableRecipes));
                
                // Если был выбран рецепт, обновляем информацию о нем
                if (SelectedRecipe != null)
                {
                    UpdateRequiredMaterials();
                    UpdateItemStats();
                }
                
                Console.WriteLine($"LoadAvailableRecipes: Загружено {AvailableRecipes.Count} рецептов");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in LoadAvailableRecipes: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        private void LoadRecipeBook()
        {
            try
            {
                Console.WriteLine("LoadRecipeBook: Начало загрузки книги рецептов");
                RecipeBookEntries.Clear();
                
                // Get all recipes and check if they can be crafted
                var allRecipes = _gameState.CraftingSystem.GetAvailableRecipes();
                
                if (allRecipes == null || allRecipes.Count == 0)
                {
                    Console.WriteLine("LoadRecipeBook: Нет доступных рецептов");
                    return;
                }
                
                Console.WriteLine($"LoadRecipeBook: Найдено {allRecipes.Count} рецептов");
                
                foreach (var recipe in allRecipes)
                {
                    if (recipe == null)
                    {
                        Console.WriteLine("LoadRecipeBook: Пропуск null рецепта");
                        continue;
                    }
                    
                    bool canCraft = _gameState.CraftingSystem.CanCraft(recipe, _gameState.Inventory);
                    
                    // Создаем запись для книги рецептов с локализованным названием
                    var recipeEntry = new RecipeBookEntry
                    {
                        Recipe = recipe,
                        CanCraft = canCraft,
                        IconPath = recipe.Result?.SpritePath ?? "Assets/Images/def.png"
                    };
                    
                    // Проверяем, что иконка существует
                    if (string.IsNullOrEmpty(recipeEntry.IconPath) || !System.IO.File.Exists(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, recipeEntry.IconPath)))
                    {
                        Console.WriteLine($"LoadRecipeBook: Иконка для рецепта '{recipe.Name}' не найдена по пути '{recipeEntry.IconPath}', используется иконка по умолчанию");
                        recipeEntry.IconPath = "Assets/Images/def.png";
                    }
                    
                    RecipeBookEntries.Add(recipeEntry);
                    Console.WriteLine($"LoadRecipeBook: Добавлен рецепт '{recipe.Name}', можно создать: {canCraft}");
                }
                
                // Сортируем рецепты: сначала те, которые можно создать
                var sortedEntries = RecipeBookEntries.OrderByDescending(r => r.CanCraft).ToList();
                RecipeBookEntries.Clear();
                foreach (var entry in sortedEntries)
                {
                    RecipeBookEntries.Add(entry);
                }
                
                Console.WriteLine($"LoadRecipeBook: Загружено {RecipeBookEntries.Count} рецептов в книгу");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в LoadRecipeBook: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        public void SelectRecipe(CraftingRecipeViewModel recipe)
        {
            if (SelectedRecipe != null)
            {
                SelectedRecipe.IsSelected = false;
            }
            
            recipe.IsSelected = true;
            SelectedRecipe = recipe;
            
            // Update can craft status
            CanCraft = _gameState.CraftingSystem.CanCraft(recipe.Recipe, _gameState.Inventory);
        }

        private void UpdateRequiredMaterials()
        {
            RequiredMaterials.Clear();
            
            if (SelectedRecipe != null)
            {
                // Add each required material
                foreach (var material in SelectedRecipe.Recipe.Materials)
                {
                    // Count available materials in inventory
                    int available = 0;
                    foreach (var item in _gameState.Inventory.Items)
                    {
                        if (item?.Name == material.Key)
                        {
                            available += item.StackSize;
                        }
                    }
                    
                    var materialViewModel = new MaterialViewModel
                    {
                        Name = material.Key,
                        Amount = material.Value,
                        Available = available
                    };
                    
                    RequiredMaterials.Add(materialViewModel);
                }
                
                // Update overall craft possibility
                CanCraft = RequiredMaterials.All(m => m.IsAvailable);
            }
        }

        private void UpdateItemStats()
        {
            if (SelectedRecipe != null)
            {
                var result = SelectedRecipe.Recipe.Result;
                ShowItemStats = result.Type == ItemType.Weapon || 
                               result.Type == ItemType.Helmet || 
                               result.Type == ItemType.Chestplate || 
                               result.Type == ItemType.Leggings ||
                               result.Type == ItemType.Shield;
                
                ShowDamage = result.Type == ItemType.Weapon;
                ShowDefense = result.Type == ItemType.Helmet || 
                             result.Type == ItemType.Chestplate || 
                             result.Type == ItemType.Leggings ||
                             result.Type == ItemType.Shield;
            }
            else
            {
                ShowItemStats = false;
                ShowDamage = false;
                ShowDefense = false;
            }
        }

        private void UpdateCraftingPatternResult()
        {
            try
            {
                Console.WriteLine("UpdateCraftingPatternResult: Начало обновления результата крафта");
                
                // Generate a pattern from the current crafting grid
                var pattern = new CraftingPattern();
                bool hasAnyMaterials = false;
                
                // Проверяем, что крафтинг-грид инициализирован
                if (CraftingGrid == null)
                {
                    Console.WriteLine("UpdateCraftingPatternResult: CraftingGrid равен null");
                    CraftResult = null;
                    CanCraft = false;
                    OnPropertyChanged(nameof(CraftResult));
                    OnPropertyChanged(nameof(CanCraft));
                    return;
                }
                
                for (int i = 0; i < 9 && i < CraftingGrid.Count; i++)
                {
                    if (CraftingGrid[i] != null)
                    {
                        pattern.Grid[i] = CraftingGrid[i]!.Name;
                        hasAnyMaterials = true;
                        Console.WriteLine($"UpdateCraftingPatternResult: Слот {i}: {CraftingGrid[i]!.Name}");
                    }
                    else
                    {
                        pattern.Grid[i] = null;
                        Console.WriteLine($"UpdateCraftingPatternResult: Слот {i}: пусто");
                    }
                }
                
                // Сбрасываем результат, если нет материалов в сетке
                if (!hasAnyMaterials)
                {
                    Console.WriteLine("UpdateCraftingPatternResult: Нет материалов в сетке крафта");
                    CraftResult = null;
                    CanCraft = false;
                    OnPropertyChanged(nameof(CraftResult));
                    OnPropertyChanged(nameof(CanCraft));
                    return;
                }
                
                // Проверяем, что инвентарь инициализирован
                if (_gameState?.Inventory?.CraftItems == null)
                {
                    Console.WriteLine("UpdateCraftingPatternResult: Инвентарь или слоты крафта не инициализированы");
                    CraftResult = null;
                    CanCraft = false;
                    OnPropertyChanged(nameof(CraftResult));
                    OnPropertyChanged(nameof(CanCraft));
                    return;
                }
                
                // Синхронизируем крафт-сетку с моделью
                for (int i = 0; i < 9 && i < CraftingGrid.Count; i++)
                {
                    if (i < _gameState.Inventory.CraftItems.Count)
                    {
                        _gameState.Inventory.CraftItems[i] = CraftingGrid[i];
                    }
                }
                
                // Проверяем, что система крафта инициализирована
                if (_gameState?.CraftingSystem == null)
                {
                    Console.WriteLine("UpdateCraftingPatternResult: CraftingSystem не инициализирована");
                    CraftResult = null;
                    CanCraft = false;
                    OnPropertyChanged(nameof(CraftResult));
                    OnPropertyChanged(nameof(CanCraft));
                    return;
                }
                
                // Find a recipe that matches this pattern
                var matchingRecipe = _gameState.CraftingSystem.FindRecipeByPattern(pattern);
                
                if (matchingRecipe != null && _gameState.CraftingSystem.CanCraft(matchingRecipe, _gameState.Inventory))
                {
                    Console.WriteLine($"UpdateCraftingPatternResult: Найден подходящий рецепт: {matchingRecipe.Name}");
                    
                    try
                    {
                        CraftResult = matchingRecipe.Result.Clone();
                        if (CraftResult != null)
                        {
                            // Set the stack size according to the recipe
                            CraftResult.StackSize = matchingRecipe.ResultQuantity;
                            CanCraft = true;
                            
                            Console.WriteLine($"UpdateCraftingPatternResult: Установлен результат крафта: {CraftResult.Name} x{CraftResult.StackSize}");
                        }
                        else
                        {
                            Console.WriteLine("UpdateCraftingPatternResult: Не удалось клонировать результат рецепта");
                            CanCraft = false;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"UpdateCraftingPatternResult: Ошибка при установке результата крафта: {ex.Message}");
                        CraftResult = null;
                        CanCraft = false;
                    }
                }
                else
                {
                    if (matchingRecipe == null)
                    {
                        Console.WriteLine("UpdateCraftingPatternResult: Не найден подходящий рецепт");
                    }
                    else
                    {
                        Console.WriteLine($"UpdateCraftingPatternResult: Найден рецепт {matchingRecipe.Name}, но недостаточно материалов");
                    }
                    
                    CraftResult = null;
                    CanCraft = false;
                }
                
                // Уведомляем UI об изменениях
                OnPropertyChanged(nameof(CraftResult));
                OnPropertyChanged(nameof(CanCraft));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"UpdateCraftingPatternResult: Ошибка при обновлении результата крафта: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                
                // В случае ошибки сбрасываем результат
                CraftResult = null;
                CanCraft = false;
                OnPropertyChanged(nameof(CraftResult));
                OnPropertyChanged(nameof(CanCraft));
            }
        }

        public bool SetItemInCraftingGrid(int index, Item? item)
        {
            if (index < 0 || index >= 9)
                return false;
            
            try
            {
                CraftingGrid[index] = item;
                
                // Синхронизируем с инвентарем крафта
                if (index < _gameState.Inventory.CraftItems.Count)
                {
                    _gameState.Inventory.CraftItems[index] = item;
                }
                
                // Обновляем результат крафта
                UpdateCraftingPatternResult();
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SetItemInCraftingGrid: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        private void CraftSelectedItem()
        {
            try
            {
                if (_gameState?.CraftingSystem == null)
                {
                    Console.WriteLine("CraftSelectedItem: CraftingSystem is null");
                    return;
                }
                
                if (SelectedRecipe == null)
                {
                    Console.WriteLine("CraftSelectedItem: No recipe selected");
                    return;
                }
                
                if (!CanCraft)
                {
                    Console.WriteLine("CraftSelectedItem: Cannot craft the selected recipe");
                    return;
                }
                
                Console.WriteLine($"CraftSelectedItem: Attempting to craft {SelectedRecipe.Name}");
                
                // Attempt to craft the selected recipe
                bool craftSuccessful = _gameState.CraftingSystem.Craft(SelectedRecipe.Recipe, _gameState.Inventory, _gameState);
                
                if (craftSuccessful)
                {
                    Console.WriteLine($"CraftSelectedItem: Successfully crafted {SelectedRecipe.Name}");
                    
                    // Update recipe availability after crafting
                    LoadAvailableRecipes();
                    UpdateRequiredMaterials();
                    ClearCraftingGrid();
                    
                    // Форсируем обновление всего инвентаря для UI
                    _gameState.Inventory.OnInventoryChanged();
                    
                    // Обновляем все свойства, чтобы обеспечить их отображение
                    OnPropertyChanged(nameof(CraftingGrid));
                    OnPropertyChanged(nameof(CraftResult));
                    OnPropertyChanged(nameof(CanCraft));
                }
                else
                {
                    Console.WriteLine($"CraftSelectedItem: Failed to craft {SelectedRecipe.Name}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in CraftSelectedItem: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        private void CraftFromPattern()
        {
            try
            {
                if (_gameState?.CraftingSystem == null)
                {
                    Console.WriteLine("CraftFromPattern: CraftingSystem is null");
                    return;
                }
                
                Console.WriteLine("CraftFromPattern: Начало крафта из сетки");
                
                // Create a pattern from the crafting grid
                var pattern = new CraftingPattern();
                bool hasAnyMaterials = false;
                
                for (int i = 0; i < 9 && i < CraftingGrid.Count; i++)
                {
                    if (CraftingGrid[i] != null)
                    {
                        pattern.Grid[i] = CraftingGrid[i]!.Name;
                        hasAnyMaterials = true;
                        Console.WriteLine($"CraftFromPattern: Материал в слоте {i}: {CraftingGrid[i]!.Name}");
                    }
                    else
                    {
                        pattern.Grid[i] = null;
                    }
                }
                
                if (!hasAnyMaterials)
                {
                    Console.WriteLine("CraftFromPattern: Нет материалов в сетке крафта");
                    return;
                }
                
                // Синхронизируем с инвентарем
                for (int i = 0; i < 9 && i < CraftingGrid.Count; i++)
                {
                    if (i < _gameState.Inventory.CraftItems.Count)
                    {
                        _gameState.Inventory.CraftItems[i] = CraftingGrid[i];
                    }
                }
                
                // Find a matching recipe
                var matchingRecipe = _gameState.CraftingSystem.FindRecipeByPattern(pattern);
                
                if (matchingRecipe == null)
                {
                    Console.WriteLine("CraftFromPattern: Не найден подходящий рецепт");
                    return;
                }
                
                // Check if we can craft it
                if (!_gameState.CraftingSystem.CanCraft(matchingRecipe, _gameState.Inventory))
                {
                    Console.WriteLine("CraftFromPattern: Недостаточно материалов для крафта");
                    return;
                }
                
                Console.WriteLine($"CraftFromPattern: Найден рецепт {matchingRecipe.Name}, начинаем крафт");
                
                // Attempt to craft
                bool craftSuccessful = _gameState.CraftingSystem.Craft(matchingRecipe, _gameState.Inventory, _gameState);
                
                if (craftSuccessful)
                {
                    Console.WriteLine($"CraftFromPattern: Успешно создан предмет {matchingRecipe.Name}");
                    
                    // Update everything
                    LoadAvailableRecipes();
                    UpdateRequiredMaterials();
                    ClearCraftingGrid();
                    
                    // Форсируем обновление всего инвентаря для UI
                    _gameState.Inventory.OnInventoryChanged();
                    
                    // Обновляем все свойства, чтобы обеспечить отображение в UI
                    OnPropertyChanged(nameof(CraftingGrid));
                    OnPropertyChanged(nameof(CraftResult));
                    OnPropertyChanged(nameof(CanCraft));
                    OnPropertyChanged(nameof(AvailableRecipes));
                    OnPropertyChanged(nameof(RequiredMaterials));
                }
                else
                {
                    Console.WriteLine("CraftFromPattern: Не удалось создать предмет");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in CraftFromPattern: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        private void ClearCraftingGrid()
        {
            try
            {
                // Очищаем сетку крафта в ViewModel
                for (int i = 0; i < 9 && i < CraftingGrid.Count; i++)
                {
                    CraftingGrid[i] = null;
                }
                
                // Очищаем слоты крафта в инвентаре
                for (int i = 0; i < _gameState.Inventory.CraftItems.Count; i++)
                {
                    _gameState.Inventory.CraftItems[i] = null;
                }
                
                // Сбрасываем результат крафта
                CraftResult = null;
                CanCraft = false;
                
                // Обновляем UI
                OnPropertyChanged(nameof(CraftingGrid));
                OnPropertyChanged(nameof(CraftResult));
                OnPropertyChanged(nameof(CanCraft));
                
                // Уведомляем об изменении инвентаря
                _gameState.Inventory.OnInventoryChanged();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ClearCraftingGrid: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        private void OpenRecipeBook()
        {
            LoadRecipeBook();
            IsRecipeBookOpen = true;
        }

        private void CloseRecipeBook()
        {
            IsRecipeBookOpen = false;
        }

        private void ShowRecipeBook()
        {
            try
            {
                Console.WriteLine("ShowRecipeBook: Открытие книги рецептов");
                
                // Загружаем все рецепты перед открытием книги
                LoadRecipeBook();
                
                // Создаем и настраиваем окно книги рецептов
                var recipeBookPopup = new Views.Controls.Recipes.RecipeBookPopup();
                
                // Загружаем рецепты в книгу
                recipeBookPopup.LoadRecipes(RecipeBookEntries, _gameState);
                
                // Подписываемся на событие закрытия
                recipeBookPopup.CloseRequested += (s, e) => 
                {
                    if (_recipeBookPopupHost != null)
                    {
                        try
                        {
                            _recipeBookPopupHost.Visibility = System.Windows.Visibility.Collapsed;
                            if (_recipeBookPopupHost.Child == recipeBookPopup)
                            {
                                _recipeBookPopupHost.Child = null;
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Ошибка при скрытии _recipeBookPopupHost: {ex.Message}");
                        }
                    }
                };
                
                // Отображаем книгу рецептов
                if (_recipeBookPopupHost != null)
                {
                    try
                    {
                        // Проверяем, что элемент находится в визуальном дереве
                        if (System.Windows.PresentationSource.FromVisual(_recipeBookPopupHost) != null)
                        {
                            _recipeBookPopupHost.Child = recipeBookPopup;
                            _recipeBookPopupHost.Visibility = System.Windows.Visibility.Visible;
                            Console.WriteLine("ShowRecipeBook: Книга рецептов отображена");
                        }
                        else
                        {
                            Console.WriteLine("ShowRecipeBook: _recipeBookPopupHost не находится в визуальном дереве");
                            // Вместо этого просто установим флаг открытия книги рецептов
                            IsRecipeBookOpen = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ошибка при установке Child для _recipeBookPopupHost: {ex.Message}");
                        // Вместо этого просто установим флаг открытия книги рецептов
                        IsRecipeBookOpen = true;
                    }
                }
                else
                {
                    Console.WriteLine("ShowRecipeBook: _recipeBookPopupHost равен null");
                    // Вместо этого просто установим флаг открытия книги рецептов
                    IsRecipeBookOpen = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в ShowRecipeBook: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
        public void SetRecipeBookPopupHost(System.Windows.Controls.Border popupHost)
        {
            try
            {
                if (popupHost != null)
                {
                    // Проверяем, что элемент находится в визуальном дереве или может быть использован
                    bool isInVisualTree = System.Windows.PresentationSource.FromVisual(popupHost) != null;
                    
                    if (isInVisualTree)
                    {
                        _recipeBookPopupHost = popupHost;
                        Console.WriteLine("RecipeBookPopupHost успешно установлен");
                    }
                    else
                    {
                        Console.WriteLine("RecipeBookPopupHost не находится в визуальном дереве, не устанавливаем его");
                    }
                }
                else
                {
                    Console.WriteLine("SetRecipeBookPopupHost: передан null");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в SetRecipeBookPopupHost: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        // Public method to refresh available recipes from InventoryViewModel
        public void RefreshAvailableRecipes()
        {
            try
            {
                Console.WriteLine("RefreshAvailableRecipes: Updating available recipes from InventoryViewModel");
                
                // Get the inventory view model's available recipes
                var inventoryViewModel = _gameState.CurrentScreenViewModel as InventoryViewModel;
                if (inventoryViewModel != null)
                {
                    // Clear our current recipes
                    AvailableRecipes.Clear();
                    
                    // Convert from CraftableRecipeViewModel to CraftingRecipeViewModel
                    foreach (var craftableRecipe in inventoryViewModel.AvailableCraftingRecipes)
                    {
                        var recipeViewModel = new CraftingRecipeViewModel(craftableRecipe.Recipe);
                        recipeViewModel.CanCraft = true; // If it's in AvailableCraftingRecipes, it can be crafted
                        AvailableRecipes.Add(recipeViewModel);
                    }
                    
                    // Notify UI of changes
                    OnPropertyChanged(nameof(AvailableRecipes));
                    Console.WriteLine($"RefreshAvailableRecipes: Updated with {AvailableRecipes.Count} recipes");
                }
                else
                {
                    Console.WriteLine("RefreshAvailableRecipes: InventoryViewModel not found");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in RefreshAvailableRecipes: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        // Method to craft an item from the craftable items list
        public void CraftItem(CraftingRecipeViewModel recipe)
        {
            try
            {
                Console.WriteLine($"CraftItem: Attempting to craft {recipe.Name}");
                
                if (!recipe.CanCraft)
                {
                    Console.WriteLine("CraftItem: Cannot craft this recipe - insufficient materials");
                    return;
                }
                
                // Attempt to craft the item
                bool craftSuccessful = _gameState.CraftingSystem.Craft(recipe.Recipe, _gameState.Inventory, _gameState);
                
                if (craftSuccessful)
                {
                    Console.WriteLine($"CraftItem: Successfully crafted {recipe.Name}");
                    
                    // Play a sound
                    _gameState.PlaySound(Services.SoundType.ItemCrafted);
                    
                    // Update recipe availability after crafting
                    LoadAvailableRecipes();
                    UpdateRequiredMaterials();
                    
                    // Notify inventory of changes
                    _gameState.Inventory.OnInventoryChanged();
                    
                    // Update UI
                    OnPropertyChanged(nameof(AvailableRecipes));
                    OnPropertyChanged(nameof(RequiredMaterials));
                    OnPropertyChanged(nameof(CanCraft));
                }
                else
                {
                    Console.WriteLine("CraftItem: Failed to craft item");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in CraftItem: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }
} 