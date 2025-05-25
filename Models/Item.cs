using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Windows.Media.Imaging;
using System.Text.Json.Serialization;
using SketchBlade.Services;
using SketchBlade.Utilities;

namespace SketchBlade.Models
{
    public enum ItemType
    {
        Helmet,
        Chestplate,
        Leggings,
        Weapon,
        Shield,
        Consumable,
        Material,
        Unknown
    }

    public enum ItemRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }

    public enum ItemMaterial
    {
        None,
        Wood,
        Iron,
        Gold,
        Luminite
    }

    public enum ItemSlotType
    {
        Head,
        Chest,
        Legs,
        Weapon,
        Shield
    }

    [Serializable]
    public class Item : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private string _description = string.Empty;
        private ItemType _type;
        private ItemRarity _rarity;
        private ItemMaterial _material;
        
        // Mark BitmapImage as non-serializable to prevent JSON serialization issues
        [NonSerialized]
        [JsonIgnore]
        private BitmapImage? _icon;
        
        private int _stackSize = 1;
        private int _maxStackSize = 1;
        private int _value;
        private float _weight = 1.0f;
        private int _damage;
        private int _defense;
        private int _effectPower;
        private string _spritePath = string.Empty;
        
        // Basic properties
        public string Name 
        { 
            get => _name; 
            set 
            { 
                _name = value; 
                NotifyPropertyChanged(); 
            } 
        }
        
        public string Description 
        { 
            get => _description; 
            set 
            { 
                _description = value; 
                NotifyPropertyChanged(); 
            } 
        }
        
        public ItemType Type 
        { 
            get => _type; 
            set 
            { 
                _type = value; 
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(IsStackable));
                NotifyPropertyChanged(nameof(IsUsable));
                NotifyPropertyChanged(nameof(IsEquippable));
                UpdateSpritePath();
            } 
        }
        
        public ItemRarity Rarity 
        { 
            get => _rarity; 
            set 
            { 
                _rarity = value; 
                NotifyPropertyChanged(); 
            } 
        }
        
        public ItemMaterial Material 
        { 
            get => _material; 
            set 
            { 
                _material = value; 
                NotifyPropertyChanged(); 
                UpdateSpritePath();
            } 
        }
        
        // Mark Icon as JsonIgnore to prevent serialization issues
        [JsonIgnore]
        public BitmapImage? Icon 
        { 
            get 
            {
                if (_icon == null && !string.IsNullOrEmpty(SpritePath))
                {
                    // Lazy loading при первом обращении
                    try
                    {
                        _icon = ResourceService.Instance.GetImage(SpritePath);
                    }
                    catch (Exception ex)
                    {
                        LoggingService.LogError($"Ошибка загрузки иконки предмета '{Name}': {ex.Message}", ex);
                        _icon = ResourceService.Instance.GetImage(AssetPaths.DEFAULT_IMAGE);
                    }
                }
                return _icon;
            } 
            set 
            { 
                _icon = value; 
                NotifyPropertyChanged(); 
            } 
        }
        
        public int StackSize
        {
            get => _stackSize;
            set
            {
                if (_stackSize != value)
                {
                    _stackSize = value;
                    NotifyPropertyChanged();
                }
            }
        }
        
        public int MaxStackSize 
        { 
            get => _maxStackSize; 
            set 
            { 
                _maxStackSize = Math.Max(1, value); 
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(IsStackable));
            } 
        }
        
        public int Value 
        { 
            get => _value; 
            set 
            { 
                _value = value; 
                NotifyPropertyChanged(); 
            } 
        }
        
        public float Weight 
        { 
            get => _weight; 
            set 
            { 
                _weight = value; 
                NotifyPropertyChanged(); 
            } 
        }
        
        public int Damage 
        { 
            get => _damage; 
            set 
            { 
                _damage = value; 
                NotifyPropertyChanged(); 
            } 
        }
        
        public int Defense 
        { 
            get => _defense; 
            set 
            { 
                _defense = value; 
                NotifyPropertyChanged(); 
            } 
        }
        
        public int EffectPower 
        { 
            get => _effectPower; 
            set 
            { 
                _effectPower = value; 
                NotifyPropertyChanged(); 
            } 
        }
        
        public string SpritePath 
        { 
            get => _spritePath; 
            set 
            { 
                _spritePath = value; 
                NotifyPropertyChanged(); 
            } 
        }
        
        // Stat bonuses (key - stat name, value - bonus amount)
        public Dictionary<string, int> StatBonuses { get; set; } = new Dictionary<string, int>();
        
        // Other properties
        public bool IsStackable => MaxStackSize > 1;
        public bool IsUsable => Type == ItemType.Consumable;
        public bool IsEquippable => Type == ItemType.Weapon || Type == ItemType.Helmet || 
                                  Type == ItemType.Chestplate || Type == ItemType.Leggings || 
                                  Type == ItemType.Shield;
        
        // Свойство для определения слота экипировки на основе типа предмета
        public EquipmentSlot EquipSlot 
        {
            get
            {
                return Type switch
                {
                    ItemType.Helmet => EquipmentSlot.Helmet,
                    ItemType.Chestplate => EquipmentSlot.Chestplate,
                    ItemType.Leggings => EquipmentSlot.Leggings,
                    ItemType.Weapon => EquipmentSlot.MainHand,
                    ItemType.Shield => EquipmentSlot.Shield,
                    _ => EquipmentSlot.MainHand // По умолчанию используем MainHand
                };
            }
        }
        
        // Бонусы от экипировки для боевых характеристик
        public int AttackBonus => StatBonuses.ContainsKey("Attack") ? StatBonuses["Attack"] : 0;
        public int DefenseBonus => StatBonuses.ContainsKey("Defense") ? StatBonuses["Defense"] : 0;
        public int HealthBonus => StatBonuses.ContainsKey("Health") ? StatBonuses["Health"] : 0;
        
        [field: NonSerialized]
        public event PropertyChangedEventHandler? PropertyChanged;
        
        public Item()
        {
            // Default constructor
        }
        
        // Create an item with basic properties
        public Item(string name, ItemType type, ItemRarity rarity = ItemRarity.Common)
        {
            Name = name;
            Type = type;
            Rarity = rarity;
            
            // Set default max stack size based on type
            switch (type)
            {
                case ItemType.Consumable:
                case ItemType.Material:
                    MaxStackSize = 20;
                    break;
                default:
                    MaxStackSize = 1;
                    break;
            }
            
            // Set default material based on type if not specified
            if (Material == ItemMaterial.None)
            {
                switch (type)
                {
                    case ItemType.Weapon:
                    case ItemType.Helmet:
                    case ItemType.Chestplate:
                    case ItemType.Leggings:
                    case ItemType.Shield:
                        Material = ItemMaterial.Iron; // Default to iron for equipment
                        break;
                }
            }
            
            // Generate sprite path based on type and material
            UpdateSpritePath();
        }
        
        public Item Clone()
        {
            var clone = new Item
            {
                Name = Name,
                Description = Description,
                Type = Type,
                Rarity = Rarity,
                Material = Material,
                StackSize = StackSize,
                MaxStackSize = MaxStackSize,
                Value = Value,
                Weight = Weight,
                Damage = Damage,
                Defense = Defense,
                EffectPower = EffectPower,
                SpritePath = SpritePath,
                StatBonuses = new Dictionary<string, int>(StatBonuses)
            };
            
            return clone;
        }
        
        // Update sprite path based on item type and material
        public void UpdateSpritePath()
        {
            string materialPrefix;
            
            switch (Type)
            {
                case ItemType.Weapon:
                    materialPrefix = GetMaterialPrefix(Material);
                    SpritePath = AssetPaths.Weapons.GetWeaponPath(materialPrefix, "sword");
                    break;
                case ItemType.Helmet:
                    materialPrefix = GetMaterialPrefix(Material);
                    SpritePath = AssetPaths.Armor.GetArmorPath(materialPrefix, "helmet");
                    break;
                case ItemType.Chestplate:
                    materialPrefix = GetMaterialPrefix(Material);
                    SpritePath = AssetPaths.Armor.GetArmorPath(materialPrefix, "chest");
                    break;
                case ItemType.Leggings:
                    materialPrefix = GetMaterialPrefix(Material);
                    SpritePath = AssetPaths.Armor.GetArmorPath(materialPrefix, "legs");
                    break;
                case ItemType.Shield:
                    materialPrefix = GetMaterialPrefix(Material);
                    SpritePath = AssetPaths.Armor.GetArmorPath(materialPrefix, "shield");
                    break;
                case ItemType.Consumable:
                    SpritePath = AssetPaths.Consumables.GetConsumablePath(Name);
                    break;
                case ItemType.Material:
                    SpritePath = AssetPaths.Materials.GetMaterialPath(Name);
                    break;
                default:
                    SpritePath = AssetPaths.DEFAULT_IMAGE;
                    break;
            }
            
            LoggingService.LogDebug($"UpdateSpritePath: Предмет '{Name}' получил путь к спрайту: {SpritePath}");
        }
        
        // Helper method to get the correct material prefix for file names
        private string GetMaterialPrefix(ItemMaterial material)
        {
            return material switch
            {
                ItemMaterial.Wood => "wooden",
                ItemMaterial.Iron => "iron",
                ItemMaterial.Gold => "golden",
                ItemMaterial.Luminite => "luminite",
                _ => "wooden"
            };
        }
        
        // Add items to stack
        public int AddToStack(int amount)
        {
            if (!IsStackable)
                return amount;
                
            int canAdd = MaxStackSize - StackSize;
            int actualAdd = Math.Min(canAdd, amount);
            
            StackSize += actualAdd;
            return amount - actualAdd; // Return remaining amount
        }
        
        // Remove items from stack
        public int RemoveFromStack(int amount)
        {
            int actualRemove = Math.Min(StackSize, amount);
            StackSize -= actualRemove;
            return actualRemove; // Return actual amount removed
        }
        
        // Split stack into a new item stack
        public Item SplitStack(int amount)
        {
            if (amount <= 0 || amount >= StackSize)
                return this;
                
            Item newStack = this.Clone();
            newStack.StackSize = RemoveFromStack(amount);
            return newStack;
        }
        
        // Use the item
        public virtual bool Use(Character target)
        {
            if (Type != ItemType.Consumable)
                return false;
                
            // Apply effect based on the item name or properties
            switch (Name.ToLower())
            {
                case "healing potion":
                    int healAmount = EffectPower > 0 ? EffectPower : 20;
                    target.Health += healAmount;
                    // Ensure health doesn't exceed max health
                    if (target.Health > target.MaxHealth)
                        target.Health = target.MaxHealth;
                    break;
                    
                case "rage potion":
                    // Apply attack buff
                    target.ApplyBuff(BuffType.Attack, EffectPower, 3);
                    break;
                    
                case "зелье неуязвимости":
                    // Apply defense buff
                    target.ApplyBuff(BuffType.Defense, 50, 3);
                    break;
                    
                default:
                    // If no specific effect, return false
                    return false;
            }
            
            // Decrement stack size
            StackSize--;
            
            return true;
        }
        
        // Use the item in combat
        public virtual bool UseInCombat(Character user, List<Character> targets)
        {
            if (Type != ItemType.Consumable)
                return false;
                
            // Apply effect based on the item
            switch (Name.ToLower())
            {
                case "healing potion":
                    // Heal the user
                    int healAmount = EffectPower > 0 ? EffectPower : 20;
                    user.Health += healAmount;
                    // Ensure health doesn't exceed max health
                    if (user.Health > user.MaxHealth)
                        user.Health = user.MaxHealth;
                    break;
                    
                case "rage potion":
                    // Increase user's attack for 3 turns
                    user.ApplyBuff(BuffType.Attack, EffectPower, 3);
                    break;
                    
                case "зелье неуязвимости":
                    // Increase user's defense for 3 turns
                    user.ApplyBuff(BuffType.Defense, 50, 3);
                    break;
                    
                case "bomb":
                    // Deal damage to all enemies
                    int damageAmount = Damage > 0 ? Damage : 45;
                    foreach (var target in targets)
                    {
                        target.TakeDamage(damageAmount);
                    }
                    break;
                    
                case "pillow":
                    // Put enemies to sleep for 2 turns
                    int stunTurns = EffectPower > 0 ? EffectPower : 2;
                    
                    // If multiple targets, affect one random enemy
                    if (targets.Count > 0)
                    {
                        var target = targets[new Random().Next(targets.Count)];
                        target.ApplyBuff(BuffType.Stun, 100, stunTurns);
                    }
                    break;
                    
                case "poisoned shuriken":
                    // Deal damage to one enemy and apply poison
                    int initialDamage = Damage > 0 ? Damage : 15;
                    int poisonDamage = EffectPower > 0 ? EffectPower : 5;
                    
                    // If multiple targets, affect one random enemy
                    if (targets.Count > 0)
                    {
                        var target = targets[new Random().Next(targets.Count)];
                        target.TakeDamage(initialDamage);
                        target.ApplyBuff(BuffType.Poison, poisonDamage, 3);
                    }
                    break;
                    
                default:
                    // If no specific effect, return false
                    return false;
            }
            
            // Decrement stack size
            StackSize--;
            
            return true;
        }
        
        public void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            try
            {
                // Уведомляем о изменении конкретного свойства
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                
                // Если изменяется StackSize, то может понадобиться обновить UI для других свойств
                if (propertyName == "StackSize")
                {
                    // Также обновляем Icon и другие визуальные свойства
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Icon"));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsStackable"));
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error in NotifyPropertyChanged: {ex.Message}", ex);
            }
        }
    }
} 