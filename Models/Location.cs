using System;
using System.Windows.Media.Imaging;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Text.Json.Serialization;
using SketchBlade.Services;
using System.IO;
using SketchBlade.Utilities;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SketchBlade.Models
{
    public enum LocationType
    {
        Village,
        Forest,
        Cave,
        Ruins,
        Castle
    }

    public enum LocationDifficultyLevel
    {
        Easy,
        Medium,
        Hard,
        VeryHard,
        Extreme
    }

    [Serializable]
    public class Location
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public LocationType Type { get; set; }
        
        public LocationType LocationType 
        { 
            get => Type; 
            set => Type = value; 
        }
        
        public string ImagePath 
        { 
            get => SpritePath; 
            set => SpritePath = value; 
        }
        
        [JsonIgnore]
        public string TranslatedName => LocalizationService.Instance.GetTranslation($"Locations.{Type}.Name");
        
        [JsonIgnore]
        public string TranslatedDescription => LocalizationService.Instance.GetTranslation($"Locations.{Type}.Description");
        
        public string SpritePath { get; set; } = string.Empty;
        
        public Character? Hero { get; set; }
        public bool IsCompleted { get; set; }
        public bool IsUnlocked { get; set; }
        public Character[]? Enemies { get; set; }
        public Item[] PossibleLoot { get; set; } = Array.Empty<Item>();
        
        public List<string> LootTable { get; set; } = new List<string>();
        
        public LocationDifficultyLevel Difficulty { get; set; }
        public List<string> RequiredCompletedLocations { get; set; } = new List<string>();
        
        public List<string> RequiredItems { get; set; } = new List<string>();
        public int MinPlayerLevel { get; set; } = 1;
        public int MaxCompletions { get; set; } = 5;
        public bool HeroDefeated { get; set; }
        public int CompletionCount { get; set; }
        
        public bool IsSelected { get; set; }
        public bool IsAvailable { get; set; }
        
        public Location()
        {
            PossibleLoot = new Item[0];
            IsUnlocked = false;
        }
        
        public bool CheckAvailability(GameData GameData)
        {
            if (IsUnlocked && IsCompleted)
            {
                IsAvailable = true;
                return true;
            }
            
            if (IsUnlocked)
            {
                if (RequiredCompletedLocations.Count > 0)
                {
                    foreach (var requiredLocation in RequiredCompletedLocations)
                    {
                        bool found = false;
                        if (GameData.Locations != null)
                        {
                            foreach (var location in GameData.Locations)
                            {
                                if (location.Name == requiredLocation && location.IsCompleted)
                                {
                                    found = true;
                                    break;
                                }
                            }
                        }
                        
                        if (!found)
                        {
                            IsAvailable = false;
                            return false;
                        }
                    }
                }
                
                if (RequiredItems.Count > 0)
                {
                    // Add implementation for required items check if needed
                    // This is a placeholder for future implementation
                }
                
                IsAvailable = true;
                return true;
            }
            
            IsAvailable = false;
            return false;
        }
        
        public void CompleteLocation(GameData GameData)
        {                
            IsCompleted = true;
            CompletionCount++;
            
            if (!HeroDefeated)
            {
                HeroDefeated = true;
            }
            
            if (GameData.Locations != null)
            {
                int currentIndex = -1;
                for (int i = 0; i < GameData.Locations.Count; i++)
                {
                    if (GameData.Locations[i].Name == Name)
                    {
                        currentIndex = i;
                        break;
                    }
                }
                                
                if (currentIndex >= 0 && currentIndex < GameData.Locations.Count - 1)
                {
                    var nextLocation = GameData.Locations[currentIndex + 1];
                    nextLocation.IsUnlocked = true;
                    nextLocation.IsAvailable = true;
                }
                
                foreach (var location in GameData.Locations)
                {
                    if (location.IsUnlocked) 
                    {
                        continue;
                    }
                    
                    if (location.RequiredCompletedLocations.Contains(Name))
                    {
                        bool allRequirementsMet = true;
                        
                        foreach (var requiredLocName in location.RequiredCompletedLocations)
                        {
                            bool foundCompleted = false;
                            
                            foreach (var loc in GameData.Locations)
                            {
                                if (loc.Name == requiredLocName && loc.IsCompleted)
                                {
                                    foundCompleted = true;
                                    break;
                                }
                            }
                            
                            if (!foundCompleted)
                            {
                                allRequirementsMet = false;
                                break;
                            }
                        }
                        
                        if (allRequirementsMet)
                        {
                            location.IsUnlocked = true;
                            location.IsAvailable = true;
                        }
                    }
                }
            }     
        }
        
        public List<Item> GenerateLoot(int count)
        {
            if (PossibleLoot == null || PossibleLoot.Length == 0)
            {
                LoggingService.LogWarning($"Location.GenerateLoot - PossibleLoot is empty or null for location {Name}");
                return new List<Item>();
            }
                
            List<Item> loot = new List<Item>();
            Random random = new Random();
            
            try
            {                
                for (int i = 0; i < count; i++)
                {
                    int index = random.Next(PossibleLoot.Length);
                    
                    if (PossibleLoot[index] == null)
                    {
                        LoggingService.LogWarning($"Null item at index {index} in PossibleLoot");
                        continue;
                    }
                    
                    Item template = PossibleLoot[index];
                    Item item = new Item
                    {
                        Name = template.Name,
                        Description = template.Description,
                        Type = template.Type,
                        Rarity = template.Rarity,
                        Material = template.Material,
                        MaxStackSize = template.MaxStackSize,
                        Value = template.Value,
                        Weight = template.Weight,
                        Damage = template.Damage,
                        Defense = template.Defense,
                        EffectPower = template.EffectPower,
                        SpritePath = template.SpritePath,
                        StackSize = 1
                    };
                    
                    foreach (var bonus in template.StatBonuses)
                    {
                        item.StatBonuses.Add(bonus.Key, bonus.Value);
                    }
                    
                    if (item.IsStackable)
                    {
                        item.StackSize = random.Next(1, Math.Min(item.MaxStackSize, 5) + 1);
                    }
                    
                    loot.Add(item);
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"ERROR in GenerateLoot: {ex.Message}", ex);
            }
            
            return loot;
        }
        
        public List<Character> GenerateEnemies(int count = 1, Difficulty? difficulty = null)
        {
            List<Character> enemies = new List<Character>();
                        
            for (int i = 0; i < count; i++)
            {
                Character enemy = Services.GameBalanceService.GenerateEnemy(Type, false, difficulty);
                
                if (enemy != null)
                {
                    enemies.Add(enemy);
                }
            }
            
            if (enemies.Count == 0)
            {
                Character defaultEnemy = new Character
                {
                    Name = "Wolf",
                    MaxHealth = 30,
                    CurrentHealth = 30,
                    Attack = 5,
                    Defense = 2,
                    Type = "Animal"
                };
                enemies.Add(defaultEnemy);
            }
            
            return enemies;
        }

        public bool CanCharacterEnter(Character character)
        {
            if (!IsUnlocked)
                return false;
            
            if (IsCompleted)
                return true;
            
            if (RequiredCompletedLocations.Count > 0)
            {
                GameData GameData = Application.Current.Resources["GameData"] as GameData;
                if (GameData != null)
                {
                    foreach (string locationName in RequiredCompletedLocations)
                    {
                        var requiredLocation = GameData.Locations.FirstOrDefault(l => l.Name == locationName);
                        
                        if (requiredLocation != null && !requiredLocation.IsCompleted)
                            return false;
                    }
                }
            }
            
            return true;
        }

        public void SetSprite(string path)
        {
            SpritePath = path;
        }

        public List<EnemyData> GetRandomEnemies()
        {
            Random random = new Random();
            int enemyCount = random.Next(1, 4);
            List<EnemyData> enemies = new List<EnemyData>();
            
            var balanceService = new Services.GameBalanceService();
            
            for (int i = 0; i < enemyCount; i++)
            {
                EnemyData enemy = new EnemyData();
                
                switch (Type)
                {
                    case LocationType.Village:
                        enemy.Name = balanceService.GetRandomEnemyName(Type);
                        enemy.Health = 15 + random.Next(10);
                        enemy.Attack = 3 + random.Next(3);
                        enemy.Defense = 1 + random.Next(2);
                        enemy.Level = 1;
                        break;
                        
                    case LocationType.Forest:
                        enemy.Name = balanceService.GetRandomEnemyName(Type);
                        enemy.Health = 25 + random.Next(15);
                        enemy.Attack = 5 + random.Next(5);
                        enemy.Defense = 3 + random.Next(3);
                        enemy.Level = 2 + random.Next(2);
                        break;
                        
                    case LocationType.Cave:
                        enemy.Name = balanceService.GetRandomEnemyName(Type);
                        enemy.Health = 40 + random.Next(20);
                        enemy.Attack = 8 + random.Next(7);
                        enemy.Defense = 5 + random.Next(5);
                        enemy.Level = 4 + random.Next(3);
                        break;
                        
                    case LocationType.Ruins:
                        enemy.Name = balanceService.GetRandomEnemyName(Type);
                        enemy.Health = 60 + random.Next(30);
                        enemy.Attack = 12 + random.Next(8);
                        enemy.Defense = 8 + random.Next(6);
                        enemy.Level = 7 + random.Next(4);
                        break;
                        
                    case LocationType.Castle:
                        enemy.Name = balanceService.GetRandomEnemyName(Type);
                        enemy.Health = 80 + random.Next(40);
                        enemy.Attack = 15 + random.Next(10);
                        enemy.Defense = 10 + random.Next(8);
                        enemy.Level = 10 + random.Next(5);
                        break;
                        
                    default:
                        enemy.Name = "Unknown Enemy";
                        enemy.Health = 20;
                        enemy.Attack = 5;
                        enemy.Defense = 2;
                        enemy.Level = 1;
                        break;
                }
                
                enemy.SpritePath = AssetPaths.Enemies.GetEnemyPathByName(enemy.Name);
                enemies.Add(enemy);
            }
            
            return enemies;
        }

        public EnemyData? GetHeroData()
        {
            if (Hero == null) return null;
            
            return new EnemyData
            {
                Name = Hero.Name,
                Health = Hero.MaxHealth,
                Attack = Hero.Attack,
                Defense = Hero.Defense,
                Level = Hero.Level,
                SpritePath = Hero.SpritePath
            };
        }
    }
    
    public class LocationIndicator : INotifyPropertyChanged
    {
        private int _index;
        private bool _isSelected;
        private bool _isCompleted;
        private bool _isUnlocked;
        private bool _isAvailable;

        public int Index 
        { 
            get => _index; 
            set 
            { 
                _index = value; 
                OnPropertyChanged(); 
            } 
        }
        
        public bool IsSelected 
        { 
            get => _isSelected; 
            set 
            { 
                _isSelected = value; 
                OnPropertyChanged(); 
            } 
        }
        
        public bool IsCompleted 
        { 
            get => _isCompleted; 
            set 
            { 
                _isCompleted = value; 
                OnPropertyChanged(); 
            } 
        }
        
        public bool IsUnlocked 
        { 
            get => _isUnlocked; 
            set 
            { 
                _isUnlocked = value; 
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

    public class EnemyData
    {
        public string Name { get; set; } = string.Empty;
        public int Health { get; set; }
        public int Attack { get; set; }
        public int Defense { get; set; }
        public int Level { get; set; }
        public string SpritePath { get; set; } = string.Empty;
    }
} 
