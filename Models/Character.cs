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
        private string _imagePath = AssetPaths.DEFAULT_IMAGE;
        private bool _isDefeated = false;
        private double _healthPercent = 100;
        private int _gold = 0;
        
        [NonSerialized]
        private int _temporaryAttackBonus = 0;
        [NonSerialized]
        private int _temporaryDefenseBonus = 0;
        [NonSerialized]
        private int _attackBonusTurnsRemaining = 0;
        [NonSerialized]
        private int _defenseBonusTurnsRemaining = 0;
        
        private Dictionary<string, string> _equippedItemsData = new Dictionary<string, string>();
        
        [NonSerialized]
        private Dictionary<EquipmentSlot, Item> _equipment = new Dictionary<EquipmentSlot, Item>();
        
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
                SaveEquippedItemsData();
            }
        }
        
        public Dictionary<string, string> EquippedItemsData
        {
            get => _equippedItemsData;
            set
            {
                _equippedItemsData = value;
                LoadEquippedItemsFromData();
            }
        }
        
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
                OnPropertyChanged(nameof(HealthDisplay));
                OnPropertyChanged(nameof(TotalMaxHealth));
                
                // Пересчитываем процент здоровья
                HealthPercent = (double)_currentHealth / _maxHealth * 100;
            } 
        }
        
        public int Health
        {
            get => _currentHealth;
            set 
            {
                CurrentHealth = value;
            }
        }
        
        public int CurrentHealth 
        { 
            get => _currentHealth; 
            set 
            { 
                _currentHealth = Math.Max(0, Math.Min(value, MaxHealth)); 
                OnPropertyChanged();
                OnPropertyChanged(nameof(Health));
                OnPropertyChanged(nameof(HealthDisplay));
                
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
                OnPropertyChanged(nameof(TotalAttack));
            } 
        }
        
        public int Defense 
        { 
            get => _defense; 
            set 
            { 
                _defense = value; 
                OnPropertyChanged(); 
                OnPropertyChanged(nameof(TotalDefense));
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
        
        public Item? EquippedWeapon => EquippedItems.ContainsKey(EquipmentSlot.MainHand) ? EquippedItems[EquipmentSlot.MainHand] : null;
        public Item? EquippedArmor => EquippedItems.ContainsKey(EquipmentSlot.Chestplate) ? EquippedItems[EquipmentSlot.Chestplate] : null;
        public Item? EquippedShield => EquippedItems.ContainsKey(EquipmentSlot.Shield) ? EquippedItems[EquipmentSlot.Shield] : null;
        public Item? EquippedHelmet => EquippedItems.ContainsKey(EquipmentSlot.Helmet) ? EquippedItems[EquipmentSlot.Helmet] : null;
        public Item? EquippedLeggings => EquippedItems.ContainsKey(EquipmentSlot.Leggings) ? EquippedItems[EquipmentSlot.Leggings] : null;
        
        public int TotalAttack => GetTotalAttack();
        public int TotalDefense => GetTotalDefense();
        public int TotalMaxHealth => GetTotalMaxHealth();
        
        public string ImagePath 
        { 
            get => _imagePath; 
            set 
            { 
                _imagePath = value; 
                OnPropertyChanged();
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
        
        public int Level { get; set; } = 1;
        public int XP { get; set; } = 0;
        public int XPToNextLevel { get; set; } = 100;
        public int Money { get; set; } = 0;
        public string SpritePath { get; set; } = string.Empty;
        public bool IsBoss { get; set; } = false;
        
        public LocationType LocationType { get; set; } = LocationType.Village;
        
        [JsonIgnore]
        public string TranslatedName 
        {
            get 
            {
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
                    
                    if (!string.IsNullOrEmpty(heroTranslation) && heroTranslation != heroKey)
                    {
                        return heroTranslation;
                    }
                }
                else if (Type == "Enemy")
                {
                    string[] locations = { "Village", "Forest", "Cave", "Ruins", "Castle" };
                    
                    foreach (var location in locations)
                    {
                        string enemyKey = $"Characters.Enemies.{location}.Regular";
                        string enemyTranslation = LocalizationService.Instance.GetTranslation(enemyKey);
                        
                        if (!string.IsNullOrEmpty(enemyTranslation) && enemyTranslation != enemyKey)
                        {
                            return enemyTranslation;
                        }
                    }
                }
                
                return Name;
            }
        }
        
        [JsonIgnore]
        public List<string> SpecialAbilities { get; set; } = new List<string>();
        
        [field: NonSerialized]
        public event PropertyChangedEventHandler? PropertyChanged;
        
        public Character()
        {
            CurrentHealth = MaxHealth;
            _equipment = new Dictionary<EquipmentSlot, Item>();
            LoadSprite();
        }
        
        private void LoadSprite()
        {
            try
            {
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
        
        /// <summary>
        /// Публичный метод для обновления спрайта персонажа.
        /// Автоматически определяет правильный путь к изображению на основе типа персонажа.
        /// </summary>
        public void UpdateSprite()
        {
            LoadSprite();
        }
        
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
        
        public bool EquipItem(Item item)
        {
            try
            {
                if (item == null)
                {
                    return false;
                }
                
                if (item.EquipSlot == EquipmentSlot.MainHand || 
                    item.EquipSlot == EquipmentSlot.Chestplate || 
                    item.EquipSlot == EquipmentSlot.Helmet || 
                    item.EquipSlot == EquipmentSlot.Leggings || 
                    item.EquipSlot == EquipmentSlot.Shield)
                {
                    EquipmentSlot slot = item.EquipSlot;
                    
                    if (EquippedItems.ContainsKey(slot))
                    {
                        EquippedItems[slot] = item;
                    }
                    else
                    {
                        EquippedItems[slot] = item;
                    }
                    
                    SaveEquippedItemsData();
                    
                    CalculateStats();
                    
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
        
        public Item? UnequipItem(EquipmentSlot slot)
        {
            try
            {
                if (EquippedItems.ContainsKey(slot))
                {
                    var item = EquippedItems[slot];
                    EquippedItems.Remove(slot);
                    CalculateStats();
                    
                    OnPropertyChanged(nameof(EquippedItems));
                    OnPropertyChanged(GetEquipmentPropertyName(slot));
                    OnPropertyChanged(nameof(TotalAttack));
                    OnPropertyChanged(nameof(TotalDefense));
                    OnPropertyChanged(nameof(TotalMaxHealth));
                    
                    return item;
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Ошибка снятия экипировки: {ex.Message}", ex);
            }
            
            return null;
        }
        
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
        
        public void CalculateStats()
        {
            int originalAttack = Attack;
            int originalDefense = Defense;
            int originalMaxHealth = MaxHealth;
            
            Attack = IsPlayer ? 10 : 5;
            Defense = IsPlayer ? 5 : 3;
            MaxHealth = IsPlayer ? 100 : 50;
            
            foreach (var kvp in EquippedItems)
            {
                Item item = kvp.Value;
                
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
            
            if (originalAttack != Attack)
                OnPropertyChanged(nameof(Attack));
                
            if (originalDefense != Defense)
                OnPropertyChanged(nameof(Defense));
                
            if (originalMaxHealth != MaxHealth)
            {
                double healthRatio = (double)CurrentHealth / originalMaxHealth;
                CurrentHealth = (int)(MaxHealth * healthRatio);
                
                OnPropertyChanged(nameof(MaxHealth));
                OnPropertyChanged(nameof(CurrentHealth));
                OnPropertyChanged(nameof(HealthDisplay));
            }
            
            OnPropertyChanged(nameof(TotalAttack));
            OnPropertyChanged(nameof(TotalDefense));
            OnPropertyChanged(nameof(TotalMaxHealth));
        }
        
        public void TakeDamage(int damage)
        {
            int actualDamage = Math.Max(1, damage);
            CurrentHealth = Math.Max(0, CurrentHealth - actualDamage);
        }
        
        public int CalculateAttackDamage(out bool isCritical)
        {
            int baseDamage = GetTotalAttack();
            
            Random rand = new Random();
            double randomFactor = 0.8 + (rand.NextDouble() * 0.4);
            int damage = (int)(baseDamage * randomFactor);
            
            isCritical = IsAttackCritical();
            if (isCritical)
            {
                damage = (int)(damage * 1.5);
            }
            
            return Math.Max(1, damage);
        }
        
        public int CalculateAttackDamage()
        {
            return CalculateAttackDamage(out _);
        }
        
        public void Heal(int amount)
        {
            CurrentHealth = Math.Min(MaxHealth, CurrentHealth + amount);
        }
        
        public int CalculateDamage(Character target)
        {
            if (target == null) return 0;
            
            int baseDamage = GetTotalAttack();
            int targetDefense = target.GetTotalDefense();
            
            int damage = baseDamage - (targetDefense / 2);
            
            Random rand = new Random();
            double randomFactor = 0.8 + (rand.NextDouble() * 0.4);
            damage = (int)(damage * randomFactor);
            
            return Math.Max(1, damage);
        }
        
        public bool IsAttackCritical()
        {
            Random rand = new Random();
            return rand.NextDouble() < 0.1;
        }
        
        public void SetTemporaryAttackBonus(int bonusAmount, int turnsDuration)
        {
            _temporaryAttackBonus = bonusAmount;
            _attackBonusTurnsRemaining = turnsDuration;
            OnPropertyChanged(nameof(TotalAttack));
        }

        public void SetTemporaryDefenseBonus(int bonusAmount, int turnsDuration)
        {
            _temporaryDefenseBonus = bonusAmount;
            _defenseBonusTurnsRemaining = turnsDuration;
            OnPropertyChanged(nameof(TotalDefense));
        }

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
                    LoggingService.LogError($"Applied poison to {Name} for {duration} turns with power {power}", null);
                    break;
                case BuffType.Stun:
                    LoggingService.LogError($"Stunned {Name} for {duration} turns", null);
                    break;
                default:
                    LoggingService.LogError($"Buff type {buffType} not implemented", null);
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
        
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void SetDefeated(bool defeated)
        {
            IsDefeated = defeated;
        }

        /// <summary>
        /// Принудительно обновляет UI для этого персонажа
        /// </summary>
        public void RefreshUI()
        {
            OnPropertyChanged(nameof(CurrentHealth));
            OnPropertyChanged(nameof(MaxHealth));
            OnPropertyChanged(nameof(HealthDisplay));
            OnPropertyChanged(nameof(TotalAttack));
            OnPropertyChanged(nameof(TotalDefense));
            OnPropertyChanged(nameof(IsDefeated));
            OnPropertyChanged(nameof(HealthPercent));
        }
    }
} 
