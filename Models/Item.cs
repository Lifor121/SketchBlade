using System;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using System.ComponentModel;
using System.Runtime.CompilerServices;

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
        [NonSerialized]
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
            } 
        }
        
        public BitmapImage? Icon 
        { 
            get 
            {
                if (_icon == null && !string.IsNullOrEmpty(SpritePath))
                {
                    // Lazy loading при первом обращении
                    try
                    {
                        _icon = Helpers.ImageHelper.LoadImage(SpritePath);
                    }
                    catch (Exception)
                    {
                        // Если не удалось загрузить, используем иконку по умолчанию
                        _icon = Helpers.ImageHelper.CreateEmptyImage();
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
                SpritePath = SpritePath
            };
            
            // Clone stat bonuses
            foreach (var pair in StatBonuses)
            {
                clone.StatBonuses[pair.Key] = pair.Value;
            }
            
            return clone;
        }
        
        // Generate sprite path based on item type and material
        public void UpdateSpritePath()
        {
            if (!string.IsNullOrEmpty(SpritePath) && !SpritePath.Contains("def.png"))
            {
                return; // Skip if already set and not default
            }
            
            // Generate path based on type and material
            switch (Type)
            {
                case ItemType.Weapon:
                    string materialName = Material.ToString().ToLower();
                    SpritePath = $"Assets/Images/items/weapons/{materialName}_sword.png";
                    break;
                case ItemType.Helmet:
                    materialName = Material.ToString().ToLower();
                    SpritePath = $"Assets/Images/items/armor/{materialName}_helmet.png";
                    break;
                case ItemType.Chestplate:
                    materialName = Material.ToString().ToLower();
                    SpritePath = $"Assets/Images/items/armor/{materialName}_chest.png";
                    break;
                case ItemType.Leggings:
                    materialName = Material.ToString().ToLower();
                    SpritePath = $"Assets/Images/items/armor/{materialName}_legs.png";
                    break;
                case ItemType.Shield:
                    materialName = Material.ToString().ToLower();
                    SpritePath = $"Assets/Images/items/armor/{materialName}_shield.png";
                    break;
                case ItemType.Consumable:
                    // Consumables are named directly by their name
                    string consumableName = Name.ToLower().Replace(" ", "_");
                    SpritePath = $"Assets/Images/items/consumables/{consumableName}.png";
                    break;
                case ItemType.Material:
                    // Materials are in their own directory
                    string materialTypeName = Name.ToLower().Replace(" ", "_");
                    SpritePath = $"Assets/Images/items/materials/{materialTypeName}.png";
                    break;
                default:
                    SpritePath = "Assets/Images/def.png";
                    break;
            }
            
            // Log the path assignment for debugging
            Console.WriteLine($"UpdateSpritePath: Item '{Name}' assigned sprite path: {SpritePath}");
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
                Console.WriteLine($"Error in NotifyPropertyChanged: {ex.Message}");
            }
        }
    }
} 