using System;
using System.Windows.Media.Imaging;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.IO;
using System.Windows.Media;
using SketchBlade.Helpers;
using SketchBlade.Services;
using System.Text.Json.Serialization;
using SketchBlade.Utilities;

namespace SketchBlade.Models
{
    public enum EquipmentSlot
    {
        Helmet,
        Chestplate,
        Leggings,
        MainHand,
        Shield
    }

    // Добавляем enum BuffType для управления эффектами предметов
    public enum BuffType
    {
        Attack,
        Defense,
        Health,
        Stun,
        Poison,
        Regeneration
    }

    [Serializable]
    public class Character : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private int _maxHealth = 100;
        private int _currentHealth = 100;
        private int _attack = 5;
        private int _defense = 3;
        [NonSerialized]
        private BitmapImage? _sprite;
        private string _imagePath = AssetPaths.DEFAULT_IMAGE; // Default to def.png initially
        private bool _isDefeated = false;
        private double _healthPercent = 100;
        private int _gold = 0;
        
        // Временные бонусы для атаки и защиты
        [NonSerialized]
        private int _temporaryAttackBonus = 0;
        [NonSerialized]
        private int _temporaryDefenseBonus = 0;
        [NonSerialized]
        private int _attackBonusTurnsRemaining = 0;
        [NonSerialized]
        private int _defenseBonusTurnsRemaining = 0;
        
        // Сериализуемая версия экипировки для сохранения без циклических ссылок
        private Dictionary<string, string> _equippedItemsData = new Dictionary<string, string>();
        
        // Current equipment (key: slot, value: item) - non-serializable for operational use
        [NonSerialized]
        private Dictionary<EquipmentSlot, Item> _equipment = new Dictionary<EquipmentSlot, Item>();
        
        // Public property for equipment
        public Dictionary<EquipmentSlot, Item> EquippedItems
        {
            get => _equipment;
            set
            {
                _equipment = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TotalAttack));
                OnPropertyChanged(nameof(TotalDefense));
                OnPropertyChanged(nameof(TotalMaxHealth));
                // Update serializable version
                SaveEquippedItemsData();
            }
        }
        
        // Serialization-safe property for equipment data
        public Dictionary<string, string> EquippedItemsData
        {
            get => _equippedItemsData;
            set
            {
                _equippedItemsData = value;
                LoadEquippedItemsFromData();
            }
        }
        
        // Save equipped items to the serializable dictionary
        private void SaveEquippedItemsData()
        {
            _equippedItemsData.Clear();
            foreach (var pair in _equipment)
            {
                if (pair.Value != null)
                {
                    _equippedItemsData[pair.Key.ToString()] = pair.Value.Name;
                }
            }
        }
        
        // Load equipped items from the serializable dictionary
        // This depends on having access to the full item repository to reconstruct the items
        private void LoadEquippedItemsFromData()
        {
            // This method should be called after deserialization when the item repository is available
            // The actual implementation would depend on your game's item system
        }
        
        // Basic properties
        public string Name 
        { 
            get => _name; 
            set 
            { 
                _name = value; 
                OnPropertyChanged(); 
            } 
        }
        
        public int MaxHealth 
        { 
            get => _maxHealth; 
            set 
            { 
                _maxHealth = value; 
                OnPropertyChanged(); 
            } 
        }
        
        // Health property for binding in XAML (aliases CurrentHealth)
        public int Health
        {
            get => _currentHealth;
            set 
            {
                CurrentHealth = value; // This will apply all the constraints and raise notifications
            }
        }
        
        public int CurrentHealth 
        { 
            get => _currentHealth; 
            set 
            { 
                _currentHealth = Math.Max(0, Math.Min(value, MaxHealth)); 
                OnPropertyChanged();
                OnPropertyChanged(nameof(Health)); // Also notify Health property changed
                
                // Update health percent and defeated status when health changes
                HealthPercent = (double)_currentHealth / MaxHealth * 100;
                IsDefeated = _currentHealth <= 0;
            } 
        }
        
        public int Attack 
        { 
            get => _attack; 
            set 
            { 
                _attack = value; 
                OnPropertyChanged(); 
            } 
        }
        
        public int Defense 
        { 
            get => _defense; 
            set 
            { 
                _defense = value; 
                OnPropertyChanged(); 
            } 
        }
        
        [JsonIgnore]
        public BitmapImage? Sprite 
        { 
            get => _sprite; 
            set 
            { 
                _sprite = value; 
                OnPropertyChanged(); 
            } 
        }
        
        public string Type { get; set; } = "Humanoid";
        public bool IsPlayer { get; set; } = false;
        public bool IsHero { get; set; } = false;
        
        // Helper properties for easy access to equipment
        public Item? EquippedWeapon => EquippedItems.ContainsKey(EquipmentSlot.MainHand) ? EquippedItems[EquipmentSlot.MainHand] : null;
        public Item? EquippedArmor => EquippedItems.ContainsKey(EquipmentSlot.Chestplate) ? EquippedItems[EquipmentSlot.Chestplate] : null;
        public Item? EquippedShield => EquippedItems.ContainsKey(EquipmentSlot.Shield) ? EquippedItems[EquipmentSlot.Shield] : null;
        public Item? EquippedHelmet => EquippedItems.ContainsKey(EquipmentSlot.Helmet) ? EquippedItems[EquipmentSlot.Helmet] : null;
        public Item? EquippedLeggings => EquippedItems.ContainsKey(EquipmentSlot.Leggings) ? EquippedItems[EquipmentSlot.Leggings] : null;
        
        // Computed properties for total stats including equipment bonuses
        public int TotalAttack => GetTotalAttack();
        public int TotalDefense => GetTotalDefense();
        public int TotalMaxHealth => GetTotalMaxHealth();
        
        // Additional properties to support battle system
        public string ImagePath 
        { 
            get => _imagePath; 
            set 
            { 
                _imagePath = value; 
                OnPropertyChanged();
                // Try to load the sprite from the image path
                LoadSprite();
            } 
        }
        
        public double HealthPercent 
        { 
            get => _healthPercent; 
            private set 
            { 
                _healthPercent = value; 
                OnPropertyChanged(); 
            } 
        }
        
        public bool IsDefeated 
        { 
            get => _isDefeated; 
            private set 
            { 
                _isDefeated = value; 
                OnPropertyChanged(); 
            } 
        }
        
        // Additional computed property for health display
        public string HealthDisplay => $"{CurrentHealth}/{MaxHealth}";
        
        public int Gold 
        { 
            get => _gold; 
            set 
            { 
                _gold = value; 
                OnPropertyChanged(); 
            } 
        }
        
        // Add the missing properties
        public int Level { get; set; } = 1;
        public int XP { get; set; } = 0;
        public int XPToNextLevel { get; set; } = 100;
        public int Money { get; set; } = 0;
        public string SpritePath { get; set; } = string.Empty;
        public bool IsBoss { get; set; } = false;
        
        // Add location type field
        public LocationType LocationType { get; set; } = LocationType.Village;
        
        // Translated name property that uses LanguageService
        [JsonIgnore]
        public string TranslatedName 
        {
            get 
            {
                // For heroes
                if (IsHero)
                {
                    string heroKey = LocationType switch
                    {
                        LocationType.Village => "Characters.Heroes.VillageElder",
                        LocationType.Forest => "Characters.Heroes.ForestGuardian",
                        LocationType.Cave => "Characters.Heroes.CaveTroll",
                        LocationType.Ruins => "Characters.Heroes.GuardianGolem",
                        LocationType.Castle => "Characters.Heroes.DarkKing",
                        _ => "Characters.Heroes.VillageElder"
                    };
                    string heroTranslation = LocalizationService.Instance.GetTranslation(heroKey);
                    
                    // If translation exists, return it
                    if (!string.IsNullOrEmpty(heroTranslation) && heroTranslation != heroKey)
                    {
                        return heroTranslation;
                    }
                }
                // For enemies
                else if (Type == "Enemy")
                {
                    // Try to find translation in all location sections
                    string[] locations = { "Village", "Forest", "Cave", "Ruins", "Castle" };
                    
                    foreach (var location in locations)
                    {
                        string enemyKey = $"Characters.Enemies.{location}.Regular";
                        string enemyTranslation = LocalizationService.Instance.GetTranslation(enemyKey);
                        
                        // If translation exists, return it
                        if (!string.IsNullOrEmpty(enemyTranslation) && enemyTranslation != enemyKey)
                        {
                            return enemyTranslation;
                        }
                    }
                }
                
                // Otherwise return the original name
                return Name;
            }
        }
        
        // Список вражеских способностей для босса или особых врагов
        [JsonIgnore]
        public List<string> SpecialAbilities { get; set; } = new List<string>();
        
        [field: NonSerialized]
        public event PropertyChangedEventHandler? PropertyChanged;
        
        public Character()
        {
            // Default initialization
            CurrentHealth = MaxHealth;
            _equipment = new Dictionary<EquipmentSlot, Item>();
            LoadSprite();
        }
        
        // Method to load the sprite image
        private void LoadSprite()
        {
            try
            {
                // Set default sprite path based on character type
                if (string.IsNullOrEmpty(ImagePath) || ImagePath == AssetPaths.DEFAULT_IMAGE)
                {
                    if (IsPlayer)
                    {
                        ImagePath = AssetPaths.Characters.PLAYER;
                    }
                    else if (IsHero)
                    {
                        ImagePath = AssetPaths.Characters.HERO;
                    }
                    else
                    {
                        ImagePath = AssetPaths.Characters.NPC;
                    }
                }
                
                Sprite = ResourceService.Instance.GetImage(ImagePath);
                
                if (Sprite == null)
                {
                    Sprite = ResourceService.Instance.GetImage(AssetPaths.DEFAULT_IMAGE);
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Ошибка загрузки спрайта персонажа из {ImagePath}: {ex.Message}", ex);
                Sprite = ResourceService.Instance.GetImage(AssetPaths.DEFAULT_IMAGE);
            }
        }
        
        // Method to get total attack including equipment bonuses
        public int GetTotalAttack()
        {
            int totalAttack = Attack + _temporaryAttackBonus;
            
            foreach (var equipment in EquippedItems.Values)
            {
                if (equipment != null)
                {
                    totalAttack += equipment.AttackBonus;
                }
            }
            
            return totalAttack;
        }
        
        // Method to get total defense including equipment bonuses
        public int GetTotalDefense()
        {
            int totalDefense = Defense + _temporaryDefenseBonus;
            
            foreach (var equipment in EquippedItems.Values)
            {
                if (equipment != null)
                {
                    totalDefense += equipment.DefenseBonus;
                }
            }
            
            return totalDefense;
        }
        
        // Method to get total health including equipment bonuses
        public int GetTotalMaxHealth()
        {
            int totalMaxHealth = MaxHealth;
            
            foreach (var equipment in EquippedItems.Values)
            {
                if (equipment != null)
                {
                    totalMaxHealth += equipment.HealthBonus;
                }
            }
            
            return totalMaxHealth;
        }
        
        // Method to equip an item
        public bool EquipItem(Item item)
        {
            try
            {
                if (item == null)
                {
                    return false;
                }
                
                // Не делать ничего, если для предмета не назначен слот
                if (item.EquipSlot == EquipmentSlot.MainHand || 
                    item.EquipSlot == EquipmentSlot.Chestplate || 
                    item.EquipSlot == EquipmentSlot.Helmet || 
                    item.EquipSlot == EquipmentSlot.Leggings || 
                    item.EquipSlot == EquipmentSlot.Shield)
                {
                    EquipmentSlot slot = item.EquipSlot;
                    
                    // Unequip existing item from that slot
                    if (EquippedItems.ContainsKey(slot))
                    {
                        // Если что-то уже надето в этот слот, просто заменяем
                        EquippedItems[slot] = item;
                    }
                    else
                    {
                        // В слоте ничего нет, просто экипируем
                        EquippedItems[slot] = item;
                    }
                    
                    // Обновляем сериализуемую версию экипировки
                    SaveEquippedItemsData();
                    
                    // Recalculate stats
                    CalculateStats();
                    
                    // Notify that equipment has changed
                    OnPropertyChanged(nameof(EquippedItems));
                    OnPropertyChanged(GetEquipmentPropertyName(slot));
                    OnPropertyChanged(nameof(TotalAttack));
                    OnPropertyChanged(nameof(TotalDefense));
                    OnPropertyChanged(nameof(TotalMaxHealth));
                    
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Ошибка экипировки предмета: {ex.Message}", ex);
                return false;
            }
        }
        
        // Method to unequip an item
        public Item? UnequipItem(EquipmentSlot slot)
        {
            try
            {
                if (EquippedItems.ContainsKey(slot))
                {
                    var item = EquippedItems[slot];
                    EquippedItems.Remove(slot);
                    CalculateStats();
                    
                    // Update UI properties after successful unequip
                    OnPropertyChanged(GetEquipmentPropertyName(slot));
                    
                    return item;
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Ошибка снятия экипировки: {ex.Message}", ex);
            }
            
            return null;
        }
        
        // Get the property name for a specific equipment slot
        private string GetEquipmentPropertyName(EquipmentSlot slot)
        {
            return slot switch
            {
                EquipmentSlot.MainHand => nameof(EquippedWeapon),
                EquipmentSlot.Helmet => nameof(EquippedHelmet),
                EquipmentSlot.Chestplate => nameof(EquippedArmor),
                EquipmentSlot.Leggings => nameof(EquippedLeggings),
                EquipmentSlot.Shield => nameof(EquippedShield),
                _ => "Unknown"
            };
        }
        
        // Calculate all stats based on base stats + equipment
        public void CalculateStats()
        {
            // Store original values
            int originalAttack = Attack;
            int originalDefense = Defense;
            int originalMaxHealth = MaxHealth;
            
            // Reset to base values
            // (This should be your character's base stats without any equipment)
            Attack = IsPlayer ? 10 : 5; // Players start with higher base stats
            Defense = IsPlayer ? 5 : 3;
            MaxHealth = IsPlayer ? 100 : 50;
            
            // Add bonuses from equipment
            foreach (var kvp in EquippedItems)
            {
                Item item = kvp.Value;
                
                // Add direct stat bonuses
                if (item.Type == ItemType.Weapon)
                {
                    Attack += item.Damage;
                }
                else if (item.Type == ItemType.Shield)
                {
                    Defense += item.Defense;
                }
                else if (item.Type == ItemType.Helmet || 
                         item.Type == ItemType.Chestplate || 
                         item.Type == ItemType.Leggings)
                {
                    Defense += item.Defense;
                }
                
                // Add any additional stat bonuses from the item
                foreach (var statBonus in item.StatBonuses)
                {
                    switch (statBonus.Key.ToLower())
                    {
                        case "attack":
                            Attack += statBonus.Value;
                            break;
                        case "defense":
                            Defense += statBonus.Value;
                            break;
                        case "health":
                            MaxHealth += statBonus.Value;
                            break;
                    }
                }
            }
            
            // If stats changed, notify property changed
            if (originalAttack != Attack)
                OnPropertyChanged(nameof(Attack));
                
            if (originalDefense != Defense)
                OnPropertyChanged(nameof(Defense));
                
            if (originalMaxHealth != MaxHealth)
            {
                // Adjust current health proportionally if max health changed
                double healthRatio = (double)CurrentHealth / originalMaxHealth;
                CurrentHealth = (int)(MaxHealth * healthRatio);
                
                OnPropertyChanged(nameof(MaxHealth));
                OnPropertyChanged(nameof(CurrentHealth));
                OnPropertyChanged(nameof(HealthDisplay));
            }
            
            // Also notify computed properties
            OnPropertyChanged(nameof(TotalAttack));
            OnPropertyChanged(nameof(TotalDefense));
            OnPropertyChanged(nameof(TotalMaxHealth));
        }
        
        // Apply damage to the character
        public void TakeDamage(int damage)
        {
            int actualDamage = Math.Max(1, damage); // Минимум 1 урона
            CurrentHealth = Math.Max(0, CurrentHealth - actualDamage);
        }
        
        // Enhanced damage calculation with critical hit detection
        public int CalculateAttackDamage(out bool isCritical)
        {
            int baseDamage = GetTotalAttack();
            
            // Случайный фактор ±20%
            Random rand = new Random();
            double randomFactor = 0.8 + (rand.NextDouble() * 0.4); // от 0.8 до 1.2
            int damage = (int)(baseDamage * randomFactor);
            
            // Проверка критического удара (10% шанс)
            isCritical = IsAttackCritical();
            if (isCritical)
            {
                damage = (int)(damage * 1.5); // Критический урон +50%
            }
            
            return Math.Max(1, damage); // Минимум 1 урона
        }
        
        // Overload of the original method for backward compatibility
        public int CalculateAttackDamage()
        {
            return CalculateAttackDamage(out _);
        }
        
        // Heal the character
        public void Heal(int amount)
        {
            CurrentHealth = Math.Min(MaxHealth, CurrentHealth + amount);
        }
        
        // Calculate damage to a target
        public int CalculateDamage(Character target)
        {
            if (target == null) return 0;
            
            int baseDamage = GetTotalAttack();
            int targetDefense = target.GetTotalDefense();
            
            // Формула урона: базовый урон - (защита цели / 2)
            int damage = baseDamage - (targetDefense / 2);
            
            // Случайный фактор ±20%
            Random rand = new Random();
            double randomFactor = 0.8 + (rand.NextDouble() * 0.4);
            damage = (int)(damage * randomFactor);
            
            return Math.Max(1, damage); // Минимум 1 урона
        }
        
        // Check if attack is critical (10% chance)
        public bool IsAttackCritical()
        {
            Random rand = new Random();
            return rand.NextDouble() < 0.1; // 10% шанс критического удара
        }
        
        // Установка временного бонуса к атаке
        public void SetTemporaryAttackBonus(int bonusAmount, int turnsDuration)
        {
            _temporaryAttackBonus = bonusAmount;
            _attackBonusTurnsRemaining = turnsDuration;
        }

        // Установка временного бонуса к защите
        public void SetTemporaryDefenseBonus(int bonusAmount, int turnsDuration)
        {
            _temporaryDefenseBonus = bonusAmount;
            _defenseBonusTurnsRemaining = turnsDuration;
            OnPropertyChanged(nameof(TotalDefense));
        }

        // Добавляем метод ApplyBuff для применения различных эффектов
        public void ApplyBuff(BuffType buffType, int power, int duration)
        {
            switch (buffType)
            {
                case BuffType.Attack:
                    SetTemporaryAttackBonus(power, duration);
                    break;
                case BuffType.Defense:
                    SetTemporaryDefenseBonus(power, duration);
                    break;
                case BuffType.Health:
                    Heal(power);
                    break;
                case BuffType.Poison:
                    LoggingService.LogError($"[{DateTime.Now}] Applied poison to {Name} for {duration} turns with power {power}", null);
                    break;
                case BuffType.Stun:
                    LoggingService.LogError($"[{DateTime.Now}] Stunned {Name} for {duration} turns", null);
                    break;
                default:
                    LoggingService.LogError($"[{DateTime.Now}] Buff type {buffType} not implemented", null);
                    break;
            }
        }
        
        public void UpdateTemporaryBonuses()
        {
            if (_attackBonusTurnsRemaining > 0)
            {
                _attackBonusTurnsRemaining--;
                if (_attackBonusTurnsRemaining <= 0)
                {
                    _temporaryAttackBonus = 0;
                    OnPropertyChanged(nameof(TotalAttack));
                }
            }

            if (_defenseBonusTurnsRemaining > 0)
            {
                _defenseBonusTurnsRemaining--;
                if (_defenseBonusTurnsRemaining <= 0)
                {
                    _temporaryDefenseBonus = 0;
                    OnPropertyChanged(nameof(TotalDefense));
                }
            }
        }
        
        // Property changed notification
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Публичный метод для установки статуса поражения
        public void SetDefeated(bool defeated)
        {
            IsDefeated = defeated;
        }
    }
} 
