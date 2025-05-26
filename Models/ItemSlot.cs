using System;
using System.Windows.Media.Imaging;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using SketchBlade.Services;

namespace SketchBlade.Models
{
    public enum SlotType
    {
        Inventory,
        Helmet,
        Chestplate,
        Leggings,
        Weapon,
        Shield,
        Consumable,
        Trash,
        Quick,
        Craft,
        CraftResult
    }

    [Serializable]
    public class ItemSlot : INotifyPropertyChanged
    {
        private Item _item;
        private int _index;
        
        public Item Item 
        { 
            get => _item; 
            set 
            {
                if (_item != value)
                {
                    if (value != null && !CanAcceptItem(value))
                    {
                        return;
                    }
                    
                    _item = value; 
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsEmpty));
                    OnPropertyChanged(nameof(HasItem));
                }
            } 
        }
        
        public int Index
        {
            get => _index;
            set
            {
                if (_index != value)
                {
                    _index = value;
                    OnPropertyChanged();
                }
            }
        }
        
        public SlotType Type { get; set; }
        public bool IsEmpty => Item == null;
        public bool HasItem => Item != null;
        
        [field: NonSerialized]
        public event PropertyChangedEventHandler? PropertyChanged;

        public ItemSlot(SlotType type = SlotType.Inventory)
        {
            Type = type;
        }

        public bool CanAcceptItem(Item item)
        {
            if (item == null) return true;
            
            switch (Type)
            {
                case SlotType.Inventory:
                case SlotType.Trash:
                case SlotType.Quick:
                    return true;
                case SlotType.Helmet:
                    return item.Type == ItemType.Helmet;
                case SlotType.Chestplate:
                    return item.Type == ItemType.Chestplate;
                case SlotType.Leggings:
                    return item.Type == ItemType.Leggings;
                case SlotType.Weapon:
                    return item.Type == ItemType.Weapon;
                case SlotType.Shield:
                    return item.Type == ItemType.Shield;
                case SlotType.Consumable:
                    return item.Type == ItemType.Consumable;
                case SlotType.Craft:
                    return true;
                case SlotType.CraftResult:
                    return false;
                default:
                    return false;
            }
        }
        
        public ItemType GetRequiredItemType()
        {
            switch (Type)
            {
                case SlotType.Helmet: return ItemType.Helmet;
                case SlotType.Chestplate: return ItemType.Chestplate;
                case SlotType.Leggings: return ItemType.Leggings;
                case SlotType.Weapon: return ItemType.Weapon;
                case SlotType.Shield: return ItemType.Shield;
                case SlotType.Consumable: return ItemType.Consumable;
                default: return ItemType.Unknown;
            }
        }
        
        public void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 