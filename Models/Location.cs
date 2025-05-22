using System;
using System.Windows.Media.Imaging;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Text.Json.Serialization;
using SketchBlade.Services;

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
        
        // Translated name and description properties
        [JsonIgnore]
        public string TranslatedName => LanguageService.GetTranslation($"Locations.{Type}.Name");
        
        [JsonIgnore]
        public string TranslatedDescription => LanguageService.GetTranslation($"Locations.{Type}.Description");
        
        // Храним только путь к изображению
        public string SpritePath { get; set; } = string.Empty;
        
        public Character? Hero { get; set; }
        public bool IsCompleted { get; set; }
        public bool IsUnlocked { get; set; }
        public Character[]? Enemies { get; set; }
        public Item[] PossibleLoot { get; set; } = Array.Empty<Item>();
        
        // Основные свойства для системы прогрессии согласно README
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
        public bool CheckAvailability(GameState gameState)
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
                        if (gameState.Locations != null)
                        {
                            foreach (var location in gameState.Locations)
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
        public void CompleteLocation(GameState gameState)
        {
            IsCompleted = true;
            CompletionCount++;
            
            // Разблокируем следующие доступные локации
            if (gameState.Locations != null)
            {
                foreach (var location in gameState.Locations)
                {
                    if (!location.IsUnlocked)
                    {
                        location.IsUnlocked = location.CheckAvailability(gameState);
                    }
                }
            }
        }
        
        public List<Item> GenerateLoot(int count)
        {
            if (PossibleLoot == null || PossibleLoot.Length == 0)
            {
                Console.WriteLine("WARNING: Location.GenerateLoot - PossibleLoot is empty or null");
                return new List<Item>();
            }
                
            List<Item> loot = new List<Item>();
            Random random = new Random();
            
            try
            {
                Console.WriteLine($"Generating {count} loot items for location '{Name}'");
                
                for (int i = 0; i < count; i++)
                {
                    int index = random.Next(PossibleLoot.Length);
                    
                    if (PossibleLoot[index] == null)
                    {
                        Console.WriteLine($"WARNING: Null item at index {index} in PossibleLoot");
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
                        Console.WriteLine($"Generated stackable item: {item.Name} x{item.StackSize}");
                    }
                    else
                    {
                        Console.WriteLine($"Generated non-stackable item: {item.Name}");
                    }
                    
                    loot.Add(item);
                }
                
                Console.WriteLine($"Successfully generated {loot.Count} loot items");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in GenerateLoot: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
            
            return loot;
        }
        
        // Generate enemies for this location
        public List<Character> GenerateEnemies(int count = 1)
        {
            List<Character> enemies = new List<Character>();
            var balanceService = new Services.GameBalanceService();
            
            Console.WriteLine($"Generating {count} enemies for location {Name}");
            
            for (int i = 0; i < count; i++)
            {
                // Generate common enemies based on location type
                Character enemy = balanceService.GenerateEnemy(Type);
                
                if (enemy != null)
                {
                    enemies.Add(enemy);
                    Console.WriteLine($"Generated enemy: {enemy.Name}, HP: {enemy.CurrentHealth}/{enemy.MaxHealth}");
                }
                else
                {
                    Console.WriteLine("Failed to generate enemy!");
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
                Console.WriteLine("Added default wolf enemy as fallback");
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
                GameState gameState = Application.Current.Resources["GameState"] as GameState;
                if (gameState != null)
                {
                    foreach (string locationName in RequiredCompletedLocations)
                    {
                        // Find the required location
                        var requiredLocation = gameState.Locations.FirstOrDefault(l => l.Name == locationName);
                        
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
                enemy.SpritePath = $"Assets/Images/Enemies/{enemy.Name.ToLower().Replace(" ", "_")}.png";
                
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
    public class LocationIndicator
    {
        public int Index { get; set; }
        public bool IsSelected { get; set; }
        public bool IsCompleted { get; set; }
        public bool IsUnlocked { get; set; }
        public bool IsAvailable { get; set; }
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