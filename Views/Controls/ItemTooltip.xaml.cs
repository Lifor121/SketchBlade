using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SketchBlade.Models;
using SketchBlade.Services;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using System.IO;
using SketchBlade.Utilities;

namespace SketchBlade.Views.Controls
{
    public partial class ItemTooltip : UserControl
    {
        private Item _item;
        private SimplifiedCraftingRecipe _recipe;
        
        public ItemTooltip()
        {
            InitializeComponent();
            this.Visibility = Visibility.Collapsed;
            
            // Подписываемся на изменения языка для автоматического обновления переводов
            LocalizationService.Instance.LanguageChanged += OnLanguageChanged;
            
            // Подписываемся на событие выгрузки для очистки ресурсов
            this.Unloaded += ItemTooltip_Unloaded;
        }
        
        private void ItemTooltip_Unloaded(object sender, RoutedEventArgs e)
        {
            // Отписываемся от события при выгрузке контрола
            LocalizationService.Instance.LanguageChanged -= OnLanguageChanged;
        }
        
        private void OnLanguageChanged(object? sender, EventArgs e)
        {
            // Обновляем отображение при изменении языка
            if (_item != null && this.Visibility == Visibility.Visible)
            {
                UpdateDisplay();
            }
        }
        
        public void SetItem(Item item)
        {
            if (item == null)
            {
                this.Visibility = Visibility.Collapsed;
                return;
            }
            
            _item = item;
            _recipe = null; // Сбрасываем рецепт при обычном показе предмета
            UpdateDisplay();
            this.Visibility = Visibility.Visible;
        }

        public void SetItemWithRecipe(Item item, SimplifiedCraftingRecipe recipe)
        {
            if (item == null)
            {
                this.Visibility = Visibility.Collapsed;
                return;
            }
            
            _item = item;
            _recipe = recipe;
            UpdateDisplay();
            this.Visibility = Visibility.Visible;
        }
        
        public void Hide()
        {
            this.Visibility = Visibility.Collapsed;
            _recipe = null;
        }
        
        private void UpdateDisplay()
        {
            if (_item == null) return;
            
            // Set basic information with localization
            ItemNameText.Text = GetLocalizedItemName(_item);
            ItemTypeText.Text = GetItemTypeDisplayName(_item.Type);
            
            // Set name color based on rarity
            ItemNameText.Foreground = GetRarityBrush(_item.Rarity);
            
            // Show material (only for weapons and armor)
            if (_item.Type == ItemType.Weapon || _item.Type == ItemType.Helmet || 
                _item.Type == ItemType.Chestplate || _item.Type == ItemType.Leggings || 
                _item.Type == ItemType.Shield)
            {
                MaterialPanel.Visibility = Visibility.Visible;
                ItemMaterialText.Text = GetItemMaterialDisplayName(_item.Material);
            }
            else
            {
                MaterialPanel.Visibility = Visibility.Collapsed;
            }
            
            // Show stack size for stackable items
            if (_item.IsStackable)
            {
                StackSizePanel.Visibility = Visibility.Visible;
                ItemMaxStackText.Text = _item.MaxStackSize.ToString();
            }
            else
            {
                StackSizePanel.Visibility = Visibility.Collapsed;
            }
            
            // Show/hide stats based on item type
            if (_item.Type == ItemType.Weapon)
            {
                DamagePanel.Visibility = Visibility.Visible;
                DefensePanel.Visibility = Visibility.Collapsed;
                ItemDamageText.Text = _item.Damage.ToString();
            }
            else if (_item.Type == ItemType.Helmet || 
                     _item.Type == ItemType.Chestplate || 
                     _item.Type == ItemType.Leggings ||
                     _item.Type == ItemType.Shield)
            {
                DamagePanel.Visibility = Visibility.Collapsed;
                DefensePanel.Visibility = Visibility.Visible;
                ItemDefenseText.Text = _item.Defense.ToString();
            }
            else
            {
                DamagePanel.Visibility = Visibility.Collapsed;
                DefensePanel.Visibility = Visibility.Collapsed;
            }

            // Show recipe if available
            UpdateRecipeDisplay();
            
            // Hide description for all items
            ItemDescriptionText.Visibility = Visibility.Collapsed;
        }

        private void UpdateRecipeDisplay()
        {
            if (_recipe != null)
            {
                RecipePanel.Visibility = Visibility.Visible;
                
                // Create material view models
                var materialViewModels = new List<RecipeMaterialViewModel>();
                
                foreach (var material in _recipe.RequiredMaterials)
                {
                    var materialVM = new RecipeMaterialViewModel
                    {
                        Required = material.Value,
                        IconPath = GetMaterialIconPath(material.Key)
                    };
                    materialViewModels.Add(materialVM);
                }

                RecipeMaterialsControl.ItemsSource = materialViewModels;
            }
            else
            {
                RecipePanel.Visibility = Visibility.Collapsed;
                RecipeMaterialsControl.ItemsSource = null;
            }
        }

        private string GetMaterialIconPath(string materialName)
        {
            try
            {
                // Создаем временный предмет для получения пути к иконке
                var tempItem = ItemFactory.CreateMaterialByName(materialName);
                if (tempItem != null && !string.IsNullOrEmpty(tempItem.SpritePath))
                {
                    return tempItem.SpritePath;
                }

                // Fallback: попробуем найти по названию
                return GetIconPathByMaterialName(materialName);
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Ошибка при получении иконки для материала {materialName}: {ex.Message}", ex);
                return AssetPaths.DEFAULT_IMAGE;
            }
        }

        private string GetIconPathByMaterialName(string materialName)
        {
            // Маппинг названий материалов на пути к иконкам
            var materialIconPaths = new Dictionary<string, string>
            {
                { "Дерево", AssetPaths.Materials.WOOD },
                { "Палка", AssetPaths.Materials.STICK },
                { "Железный слиток", AssetPaths.Materials.IRON_INGOT },
                { "Золотой слиток", AssetPaths.Materials.GOLD_INGOT },
                { "Люминит", AssetPaths.Materials.LUMINITE },
                { "Фрагмент люминита", AssetPaths.Materials.LUMINITE_FRAGMENT },
                { "Трава", AssetPaths.Materials.HERB },
                { "Фляга", AssetPaths.Materials.FLASK },
                { "Кристаллическая пыль", AssetPaths.Materials.CRYSTAL_DUST },
                { "Ткань", AssetPaths.Materials.CLOTH },
                { "Перо", AssetPaths.Materials.FEATHER },
                { "Экстракт яда", AssetPaths.Materials.POISON_EXTRACT },
                { "Порох", AssetPaths.Materials.GUNPOWDER },
                { "Железная руда", AssetPaths.Materials.IRON_ORE },
                { "Золотая руда", AssetPaths.Materials.GOLD_ORE }
            };

            return materialIconPaths.TryGetValue(materialName, out string iconPath) 
                ? iconPath 
                : AssetPaths.DEFAULT_IMAGE;
        }
        
        private SolidColorBrush GetRarityBrush(ItemRarity rarity)
        {
            switch (rarity)
            {
                case ItemRarity.Common:
                    return new SolidColorBrush(Colors.White);
                case ItemRarity.Uncommon:
                    return new SolidColorBrush(Color.FromRgb(76, 175, 80)); // Green
                case ItemRarity.Rare:
                    return new SolidColorBrush(Color.FromRgb(33, 150, 243)); // Blue
                case ItemRarity.Epic:
                    return new SolidColorBrush(Color.FromRgb(156, 39, 176)); // Purple
                case ItemRarity.Legendary:
                    return new SolidColorBrush(Color.FromRgb(255, 193, 7)); // Gold
                default:
                    return new SolidColorBrush(Colors.White);
            }
        }
        
        private string GetItemTypeDisplayName(ItemType type)
        {
            return LocalizationService.Instance.GetTranslation($"ItemTypes.{type}");
        }
        
        private string GetItemMaterialDisplayName(ItemMaterial material)
        {
            if (material == ItemMaterial.None)
                return "";
                
            return LocalizationService.Instance.GetTranslation($"ItemMaterials.{material}");
        }

        private string GetLocalizedItemName(Item item)
        {
            // Маппинг русских названий на английские ключи
            string englishKey = GetEnglishKeyForItem(item.Name);
            
            if (!string.IsNullOrEmpty(englishKey))
            {
                var localized = LocalizationService.Instance.GetTranslation($"Items.{englishKey}.Name");
                if (!string.IsNullOrEmpty(localized) && !localized.StartsWith("Items."))
                    return localized;
            }
            
            // Fallback to original name
            return item.Name;
        }

        private string GetLocalizedItemDescription(Item item)
        {
            // Маппинг русских названий на английские ключи
            string englishKey = GetEnglishKeyForItem(item.Name);
            
            if (!string.IsNullOrEmpty(englishKey))
            {
                var localized = LocalizationService.Instance.GetTranslation($"Items.{englishKey}.Description");
                if (!string.IsNullOrEmpty(localized) && !localized.StartsWith("Items."))
                    return localized;
            }
            
            // Fallback to original description
            return item.Description;
        }

        private string GetEnglishKeyForItem(string itemName)
        {
            // Маппинг русских названий на английские ключи локализации
            var nameMapping = new Dictionary<string, string>
            {
                // Материалы
                { "Дерево", "Wood" },
                { "Палка", "Stick" },
                { "Ткань", "Cloth" },
                { "Трава", "Herb" },
                { "Перо", "Feather" },
                { "Фляга", "Flask" },
                { "Фляга с водой", "WaterFlask" },
                { "Железная руда", "IronOre" },
                { "Железный слиток", "IronIngot" },
                { "Золотая руда", "GoldOre" },
                { "Золотой слиток", "GoldIngot" },
                { "Кристаллическая пыль", "CrystalDust" },
                { "Порох", "Gunpowder" },
                { "Экстракт яда", "PoisonExtract" },
                { "Фрагмент люминита", "LuminiteFragment" },
                { "Люминит", "Luminite" },
                
                // Оружие
                { "Деревянный меч", "WoodenSword" },
                { "Железный меч", "IronSword" },
                { "Золотой меч", "GoldenSword" },
                { "Люминитовый меч", "LuminiteSword" },
                
                // Броня
                { "Деревянный шлем", "WoodenHelmet" },
                { "Деревянный нагрудник", "WoodenChestplate" },
                { "Деревянные поножи", "WoodenLeggings" },
                { "Железный шлем", "IronHelmet" },
                { "Железный нагрудник", "IronChestplate" },
                { "Железные поножи", "IronLeggings" },
                { "Золотой шлем", "GoldenHelmet" },
                { "Золотой нагрудник", "GoldenChestplate" },
                { "Золотые поножи", "GoldenLeggings" },
                { "Люминитовый шлем", "LuminiteHelmet" },
                { "Люминитовый нагрудник", "LuminiteChestplate" },
                { "Люминитовые поножи", "LuminiteLeggings" },
                
                // Щиты
                { "Железный щит", "IronShield" },
                { "Золотой щит", "GoldenShield" },
                { "Люминитовый щит", "LuminiteShield" },
                
                // Расходуемые предметы
                { "Зелье лечения", "HealingPotion" },
                { "Зелье ярости", "RagePotion" },
                { "Зелье неуязвимости", "InvulnerabilityPotion" },
                { "Подушка", "Pillow" },
                { "Отравленный сюрикен", "PoisonedShuriken" },
                { "Бомба", "Bomb" }
            };

            return nameMapping.TryGetValue(itemName, out string englishKey) ? englishKey : string.Empty;
        }
    }

    // ViewModel для отображения материала в рецепте
    public class RecipeMaterialViewModel
    {
        public int Required { get; set; }
        public string IconPath { get; set; } = string.Empty;
        
        public BitmapImage? Icon
        {
            get
            {
                try
                {
                    if (string.IsNullOrEmpty(IconPath))
                        return null;

                    string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, IconPath);
                    if (File.Exists(fullPath))
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri(fullPath, UriKind.Absolute);
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                        bitmap.Freeze();
                        return bitmap;
                    }
                }
                catch (Exception ex)
                {
                    LoggingService.LogError($"Ошибка при загрузке иконки материала: {ex.Message}", ex);
                }
                
                return null;
            }
        }
    }
} 
