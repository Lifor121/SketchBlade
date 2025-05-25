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
        
        // ��������� �������� LocationType ��� ������������� (alias ��� Type)
        public LocationType LocationType 
        { 
            get => Type; 
            set => Type = value; 
        }
        
        // ��������� �������� ImagePath ��� ������������� (alias ��� SpritePath)
        public string ImagePath 
        { 
            get => SpritePath; 
            set => SpritePath = value; 
        }
        
        // Translated name and description properties
        [JsonIgnore]
        public string TranslatedName => LocalizationService.Instance.GetTranslation($"Locations.{Type}.Name");
        
        [JsonIgnore]
        public string TranslatedDescription => LocalizationService.Instance.GetTranslation($"Locations.{Type}.Description");
        
        // Храним только путь к изображению
        public string SpritePath { get; set; } = string.Empty;
        
        public Character? Hero { get; set; }
        public bool IsCompleted { get; set; }
        public bool IsUnlocked { get; set; }
        public Character[]? Enemies { get; set; }
        public Item[] PossibleLoot { get; set; } = Array.Empty<Item>();
        
        // ��������� ��� ������������� � BattleLogic
        public List<string> LootTable { get; set; } = new List<string>();
        
        // Оѽовные свойства для системы прогрессии согласно README
        public LocationDifficultyLevel Difficulty { get; set; }
        public List<string> RequiredCompletedLocations { get; set; } = new List<string>();
        
        // Добавленные свойства для совместимости
        public List<string> RequiredItems { get; set; } = new List<string>();
        public int MinPlayerLevel { get; set; } = 1;
        public int MaxCompletions { get; set; } = 5;
        public bool HeroDefeated { get; set; }
        public int CompletionCount { get; set; }
        
        // UI helper properties for location selection
        public bool IsSelected { get; set; }
        public bool IsAvailable { get; set; }
        
        public Location()
        {
            PossibleLoot = new Item[0];
            IsUnlocked = false;
        }
        
        // Проверяет, доступна ли локация для посещения
        public bool CheckAvailability(GameData GameData)
        {
            // If already unlocked and completed, it's always available
            if (IsUnlocked && IsCompleted)
            {
                IsAvailable = true;
                return true;
            }
            
            // For unlocked locations, we need to check prerequisites
            if (IsUnlocked)
            {
                // Check required completed locations
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
                
                // Check required items if needed
                if (RequiredItems.Count > 0)
                {
                    // Add implementation for required items check if needed
                    // This is a placeholder for future implementation
                }
                
                IsAvailable = true;
                return true;
            }
            
            // If not unlocked, it's not available
            IsAvailable = false;
            return false;
        }
        
        // Отмечает локацию как пройденную и обновляет состояние
        public void CompleteLocation(GameData GameData)
        {
            LoggingService.LogDebug($"Completing location: {Name}");
                
            IsCompleted = true;
            CompletionCount++;
            
            // Important: Make sure hero is marked as defeated for proper progression
            if (!HeroDefeated)
            {
                HeroDefeated = true;
                LoggingService.LogDebug($"Explicitly marked hero of {Name} as defeated");
            }
            
            // Разблокируем следующие доступные локации
            if (GameData.Locations != null)
            {
                // First, find the current location index
                int currentIndex = -1;
                for (int i = 0; i < GameData.Locations.Count; i++)
                {
                    if (GameData.Locations[i].Name == Name)
                    {
                        currentIndex = i;
                        break;
                    }
                }
                
                LoggingService.LogDebug($"Current location index: {currentIndex}, Total locations: {GameData.Locations.Count}");
                
                // If we found the current location and there's a next location, unlock it directly
                if (currentIndex >= 0 && currentIndex < GameData.Locations.Count - 1)
                {
                    var nextLocation = GameData.Locations[currentIndex + 1];
                    nextLocation.IsUnlocked = true;
                    nextLocation.IsAvailable = true;
                    
                    LoggingService.LogDebug($"DIRECTLY unlocked next location: {nextLocation.Name}");
                }
                
                // Also unlock any locations that require this location
                foreach (var location in GameData.Locations)
                {
                    // Skip already unlocked locations
                    if (location.IsUnlocked) 
                    {
                        LoggingService.LogDebug($"Location {location.Name} is already unlocked");
                        continue;
                    }
                    
                    // Check if this location requires the completed location
                    if (location.RequiredCompletedLocations.Contains(Name))
                    {
                        bool allRequirementsMet = true;
                        
                        // Check all requirements for the location
                        foreach (var requiredLocName in location.RequiredCompletedLocations)
                        {
                            bool foundCompleted = false;
                            
                            // Find the required location and check if it's completed
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
                                LoggingService.LogDebug($"Required location {requiredLocName} is not completed for unlocking {location.Name}");
                                break;
                            }
                        }
                        
                        // If all requirements are met, unlock the location
                        if (allRequirementsMet)
                        {
                            location.IsUnlocked = true;
                            location.IsAvailable = true;
                            LoggingService.LogDebug($"Unlocked location based on requirements: {location.Name}");
                                
                            // Show notification if UI thread is available
                            try
                            {
                                System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                                    System.Windows.MessageBox.Show($"You've unlocked a new location: {location.Name}!", 
                                                "New Location Available", 
                                                System.Windows.MessageBoxButton.OK, 
                                                System.Windows.MessageBoxImage.Information);
                                }));
                            }
                            catch (Exception ex)
                            {
                                LoggingService.LogError($"Error showing unlock notification: {ex.Message}", ex);
                            }
                        }
                    }
                }
            }
            
            LoggingService.LogDebug($"Location {Name} completed successfully");
        }
        
        public List<Item> GenerateLoot(int count)
        {
            if (PossibleLoot == null || PossibleLoot.Length == 0)
            {
                LoggingService.LogError($"WARNING: Location.GenerateLoot - PossibleLoot is empty or null for location {Name}");
                return new List<Item>();
            }
                
            List<Item> loot = new List<Item>();
            Random random = new Random();
            
            try
            {
                LoggingService.LogDebug($"Generating {count} loot items for location '{Name}'");
                
                for (int i = 0; i < count; i++)
                {
                    int index = random.Next(PossibleLoot.Length);
                    
                    if (PossibleLoot[index] == null)
                    {
                        LoggingService.LogError($"WARNING: Null item at index {index} in PossibleLoot");
                        continue;
                    }
                    
                    // Создаем новую копию предмета для уверенности
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
                        StackSize = 1 // По умолчанию 1, изменится ниже если предмет стакается
                    };
                    
                    // Копируем статистические бонусы
                    foreach (var bonus in template.StatBonuses)
                    {
                        item.StatBonuses.Add(bonus.Key, bonus.Value);
                    }
                    
                    // Для стакающихся предметов генерируем случайный размер стопки
                    if (item.IsStackable)
                    {
                        item.StackSize = random.Next(1, Math.Min(item.MaxStackSize, 5) + 1);
                        LoggingService.LogDebug($"Generated stackable item: {item.Name} x{item.StackSize}");
                    }
                    else
                    {
                        LoggingService.LogDebug($"Generated non-stackable item: {item.Name}");
                    }
                    
                    loot.Add(item);
                }
                
                LoggingService.LogDebug($"Successfully generated {loot.Count} loot items");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"ERROR in GenerateLoot: {ex.Message}", ex);
            }
            
            return loot;
        }
        
        // Generate enemies for this location
        public List<Character> GenerateEnemies(int count = 1, Difficulty? difficulty = null)
        {
            List<Character> enemies = new List<Character>();
            
            LoggingService.LogDebug($"Generating {count} enemies for location {Name}");
            
            for (int i = 0; i < count; i++)
            {
                // Generate common enemies based on location type
                Character enemy = Services.GameBalanceService.GenerateEnemy(Type, false, difficulty);
                
                if (enemy != null)
                {
                    enemies.Add(enemy);
                    LoggingService.LogDebug($"Generated enemy: {enemy.Name}, HP: {enemy.CurrentHealth}/{enemy.MaxHealth}");
                }
                else
                {
                    LoggingService.LogDebug($"Failed to generate enemy!");
                }
            }
            
            // If no enemies were generated, create a default enemy
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
                LoggingService.LogDebug($"Added default wolf enemy as fallback");
            }
            
            return enemies;
        }

        // Check if a character meets the requirements to enter this location
        public bool CanCharacterEnter(Character character)
        {
            // Check if the location is unlocked
            if (!IsUnlocked)
                return false;
            
            // Always allow access to already completed locations
            if (IsCompleted)
                return true;
            
            // Check required completed locations
            if (RequiredCompletedLocations.Count > 0)
            {
                GameData GameData = Application.Current.Resources["GameData"] as GameData;
                if (GameData != null)
                {
                    foreach (string locationName in RequiredCompletedLocations)
                    {
                        // Find the required location
                        var requiredLocation = GameData.Locations.FirstOrDefault(l => l.Name == locationName);
                        
                        // If location exists and is not completed, the character can't enter
                        if (requiredLocation != null && !requiredLocation.IsCompleted)
                            return false;
                    }
                }
            }
            
            return true;
        }

        // Метод для установки пути к спрайту (для обратной совместимости)
        public void SetSprite(string path)
        {
            SpritePath = path;
        }

        public List<EnemyData> GetRandomEnemies()
        {
            // Return 1-3 random enemies based on location type and difficulty
            Random random = new Random();
            int enemyCount = random.Next(1, 4); // 1-3 enemies
            List<EnemyData> enemies = new List<EnemyData>();
            
            // Используем GameBalanceService для получения имен врагов
            var balanceService = new Services.GameBalanceService();
            
            for (int i = 0; i < enemyCount; i++)
            {
                EnemyData enemy = new EnemyData();
                
                // Set base stats according to location type
                switch (Type)
                {
                    case LocationType.Village:
                        // Используем имя врага из локализации
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
                
                // Set a default sprite path based on enemy name
                enemy.SpritePath = AssetPaths.Enemies.GetEnemyPathByName(enemy.Name);
                
                enemies.Add(enemy);
            }
            
            return enemies;
        }

        public EnemyData? GetHeroData()
        {
            // If the location doesn't have a hero boss, return null
            if (Hero == null) return null;
            
            // Convert the Hero Character to EnemyData format
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
    
    // Класс для отображения индикаторов локаций на карте мира
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
