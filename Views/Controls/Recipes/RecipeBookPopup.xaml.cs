using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;
using SketchBlade.Models;

namespace SketchBlade.Views.Controls.Recipes
{
    /// <summary>
    /// Логика взаимодействия для RecipeBookPopup.xaml
    /// </summary>
    public partial class RecipeBookPopup : UserControl, INotifyPropertyChanged
    {
        // Событие закрытия попапа
        public event EventHandler CloseRequested;
        public event PropertyChangedEventHandler? PropertyChanged;
        
        private ObservableCollection<RecipeBookEntry> _allRecipes = new ObservableCollection<RecipeBookEntry>();
        private List<RecipeBookEntry> _filteredRecipes = new List<RecipeBookEntry>();
        private RecipeBookEntry _selectedRecipe;
        private string _searchText = string.Empty;
        private GameState _gameState;
        
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged();
                    FilterRecipes(_searchText);
                }
            }
        }
        
        // Конструктор
        public RecipeBookPopup()
        {
            InitializeComponent();
            DataContext = this;
        }
        
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
        // Метод для загрузки рецептов из коллекции RecipeBookEntries
        public void LoadRecipes(ObservableCollection<RecipeBookEntry> recipeEntries, GameState gameState)
        {
            if (recipeEntries == null || gameState == null)
            {
                Console.WriteLine("RecipeBookPopup: recipeEntries или gameState равны null");
                return;
            }
            
            _gameState = gameState;
            _allRecipes = recipeEntries;
            _filteredRecipes = _allRecipes.ToList();
            
            Console.WriteLine($"RecipeBookPopup: Загружено {_allRecipes.Count} рецептов");
            
            // Сортируем рецепты: сначала те, которые можно создать, затем по имени
            _filteredRecipes = _filteredRecipes
                .OrderByDescending(r => r.CanCraft)
                .ThenBy(r => r.Recipe.Name)
                .ToList();
            
            // Загружаем список рецептов
            RecipeList.ItemsSource = _filteredRecipes;
            
            // Если есть рецепты, выбираем первый
            if (_filteredRecipes.Count > 0)
            {
                RecipeList.SelectedIndex = 0;
            }
            else
            {
                // Очищаем детали рецепта, если нет рецептов
                ClearRecipeDetails();
            }
        }
        
        // Метод для загрузки рецептов из CraftingSystem
        public void LoadRecipes(CraftingSystem craftingSystem)
        {
            if (craftingSystem == null || craftingSystem.GameState == null)
            {
                Console.WriteLine("RecipeBookPopup: craftingSystem равен null");
                return;
            }
            
            var recipes = new ObservableCollection<RecipeBookEntry>();
            var allRecipes = craftingSystem.GetAvailableRecipes();
            
            Console.WriteLine($"LoadRecipes: Найдено {allRecipes.Count} рецептов в CraftingSystem");
            
            foreach (var recipe in allRecipes)
            {
                if (recipe == null) continue;
                
                bool canCraft = craftingSystem.CanCraft(recipe, craftingSystem.GameState.Inventory);
                
                var recipeEntry = new RecipeBookEntry
                {
                    Recipe = recipe,
                    CanCraft = canCraft,
                    IconPath = recipe.Result?.SpritePath ?? "Assets/Images/def.png"
                };
                
                recipes.Add(recipeEntry);
            }
            
            // Используем существующий метод для загрузки рецептов
            LoadRecipes(recipes, craftingSystem.GameState);
        }
        
        // Метод для отображения деталей рецепта
        private void DisplayRecipeDetails(RecipeBookEntry recipeEntry)
        {
            if (recipeEntry == null || recipeEntry.Recipe == null)
            {
                ClearRecipeDetails();
                return;
            }
            
            _selectedRecipe = recipeEntry;
            var recipe = recipeEntry.Recipe;
            
            // Устанавливаем основную информацию
            RecipeNameText.Text = recipe.Name;
            ResultNameText.Text = recipe.Result?.Name ?? "Неизвестный предмет";
            ResultDescriptionText.Text = recipe.Result?.Description ?? "";
            ResultQuantityText.Text = $"Количество: {recipe.ResultQuantity}";
            
            // Локализуем тип и редкость
            string itemType = Services.LanguageService.GetTranslation($"ItemTypes.{recipe.ResultType}");
            if (string.IsNullOrEmpty(itemType) || itemType.Contains("ItemTypes."))
            {
                itemType = recipe.ResultType.ToString();
            }
            
            string itemRarity = Services.LanguageService.GetTranslation($"ItemRarity.{recipe.ResultRarity}");
            if (string.IsNullOrEmpty(itemRarity) || itemRarity.Contains("ItemRarity."))
            {
                itemRarity = recipe.ResultRarity.ToString();
            }
            
            ResultTypeText.Text = itemType;
            ResultRarityText.Text = itemRarity;
            
            // Устанавливаем изображение результата
            ResultImage.Source = recipeEntry.Icon;
            
            // Отображаем материалы с изображениями
            var materialsWithImages = recipe.Materials.Select(m => new MaterialWithImage
            {
                Key = m.Key,
                Value = m.Value,
                ImageSource = GetItemSpriteByName(m.Key)
            }).ToList();
            
            MaterialsList.ItemsSource = materialsWithImages;
        }
        
        // Метод для поиска спрайта предмета по имени
        private BitmapImage GetItemSpriteByName(string itemName)
        {
            // Попытка найти предмет в инвентаре по имени
            if (_gameState != null)
            {
                var item = _gameState.Inventory.Items.FirstOrDefault(i => i != null && i.Name == itemName);
                if (item != null)
                {
                    if (item.Icon != null)
                    {
                        return item.Icon;
                    }
                    else if (!string.IsNullOrEmpty(item.SpritePath))
                    {
                        return Helpers.ImageHelper.GetImageWithFallback(item.SpritePath);
                    }
                }
            }
            
            // Попытка определить стандартный путь к спрайту на основе имени предмета
            string itemTypePath = "materials";
            string itemFileName = itemName.ToLower().Replace(" ", "_");
            
            if (itemName.Contains("Sword") || itemName.Contains("Axe") || itemName.Contains("Blade"))
            {
                itemTypePath = "weapons";
            }
            else if (itemName.Contains("Helmet") || itemName.Contains("Chestplate") || itemName.Contains("Leggings") || itemName.Contains("Shield"))
            {
                itemTypePath = "armor";
            }
            else if (itemName.Contains("Potion") || itemName.Contains("Bomb") || itemName.Contains("Shuriken"))
            {
                itemTypePath = "consumables";
            }
            
            string spritePath = $"Assets/Images/items/{itemTypePath}/{itemFileName}.png";
            
            // Используем ImageHelper для загрузки изображения с поддержкой запасного варианта
            return Helpers.ImageHelper.GetImageWithFallback(spritePath);
        }
        
        // Метод для очистки деталей рецепта
        private void ClearRecipeDetails()
        {
            RecipeNameText.Text = Services.LanguageService.GetTranslation("Crafting.SelectRecipe");
            ResultNameText.Text = Services.LanguageService.GetTranslation("Crafting.ItemName");
            ResultDescriptionText.Text = Services.LanguageService.GetTranslation("Crafting.ItemDescription");
            ResultQuantityText.Text = Services.LanguageService.GetTranslation("Crafting.Quantity");
            ResultTypeText.Text = Services.LanguageService.GetTranslation("Crafting.TypeValue");
            ResultRarityText.Text = Services.LanguageService.GetTranslation("Crafting.RarityValue");
            ResultImage.Source = null;
            MaterialsList.ItemsSource = null;
        }
        
        // Метод для поиска рецептов
        private void FilterRecipes(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                // If search is empty, show all recipes
                RecipeList.ItemsSource = _allRecipes;
            }
            else
            {
                // Filter recipes by name containing search text (case insensitive)
                _filteredRecipes = _allRecipes
                    .Where(r => r.Recipe.Name.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();
                RecipeList.ItemsSource = _filteredRecipes;
            }
        }
        
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // The filtering is now handled by the property setter
            // This method remains for the event binding in XAML
        }
        
        // Обработчики событий
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
        
        private void RecipeList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RecipeList.SelectedItem is RecipeBookEntry recipe)
            {
                DisplayRecipeDetails(recipe);
            }
        }
        
        // Модель данных для отображения материалов с изображениями
        private class MaterialWithImage
        {
            public string Key { get; set; }
            public int Value { get; set; }
            public BitmapImage ImageSource { get; set; }
        }
    }
} 