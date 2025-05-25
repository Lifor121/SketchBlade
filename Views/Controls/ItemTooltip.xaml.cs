using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SketchBlade.Models;
using SketchBlade.Services;

namespace SketchBlade.Views.Controls
{
    public partial class ItemTooltip : UserControl
    {
        private Item _item;
        
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
            UpdateDisplay();
            this.Visibility = Visibility.Visible;
        }
        
        public void Hide()
        {
            this.Visibility = Visibility.Collapsed;
        }
        
        private void UpdateDisplay()
        {
            if (_item == null) return;
            
            // Set basic information
            ItemNameText.Text = _item.Name;
            ItemTypeText.Text = GetItemTypeDisplayName(_item.Type);
            ItemDescriptionText.Text = _item.Description;
            
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
    }
} 
