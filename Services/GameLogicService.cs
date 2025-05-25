using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using SketchBlade.Models;
using SketchBlade.Services;
using SketchBlade.Utilities;
using System.Threading.Tasks;

namespace SketchBlade.Services
{
    /// <summary>
    /// Консолидированный сервис игровой логики
    /// Объединяет инициализацию игры и балансировку
    /// </summary>
    public interface IGameLogicService
    {
        // Game initialization
        GameData CreateNewGame();
        Character CreatePlayer();
        Inventory CreateInventory();
        ObservableCollection<Location> CreateLocations();
        
        // Balance and scaling
        int CalculateScaledValue(int baseValue, int playerLevel, double scalingFactor = 1.0);
        int CalculateEnemyHealth(int baseHealth, int locationDifficulty, int playerLevel);
        int CalculateEnemyAttack(int baseAttack, int locationDifficulty, int playerLevel);
        int CalculateXPReward(int enemyLevel, bool isBoss = false);
        int CalculateGoldReward(int enemyLevel, bool isBoss = false);
        
        // Difficulty management
        LocationDifficultyLevel GetLocationDifficulty(int locationIndex);
        bool IsLocationUnlocked(int locationIndex, GameData gameData);
        int GetRecommendedLevel(int locationIndex);
        
        // Item generation
        List<Item> GenerateRandomLoot(int locationIndex, int quantity = 1);
        Item? CreateRandomItem(ItemType type, int locationTier);
        
        // Enemy generation
        List<Character> GenerateEnemies(Location location, int playerLevel);
        Character CreateEnemy(string enemyType, int locationTier, int playerLevel);
    }

    public class GameLogicService : IGameLogicService
    {
        private static readonly Lazy<GameLogicService> _instance = new(() => new GameLogicService());
        public static GameLogicService Instance => _instance.Value;

        private readonly Random _random = new();

        // Balance constants
        private const double HEALTH_SCALING_FACTOR = 1.2;
        private const double ATTACK_SCALING_FACTOR = 1.1;
        private const double XP_BASE_MULTIPLIER = 10;
        private const double GOLD_BASE_MULTIPLIER = 5;
        private const int BASE_PLAYER_HEALTH = 100;
        private const int BASE_PLAYER_ATTACK = 10;
        private const int BASE_PLAYER_DEFENSE = 5;

        // Location data
        private readonly Dictionary<int, (string Name, LocationType Type, LocationDifficultyLevel Difficulty)> _locationDefinitions = new()
        {
            [0] = ("Village", LocationType.Village, LocationDifficultyLevel.Easy),
            [1] = ("Forest", LocationType.Forest, LocationDifficultyLevel.Medium),
            [2] = ("Cave", LocationType.Cave, LocationDifficultyLevel.Hard),
            [3] = ("Ancient Ruins", LocationType.Ruins, LocationDifficultyLevel.VeryHard),
            [4] = ("Dark Castle", LocationType.Castle, LocationDifficultyLevel.Extreme)
        };

        private GameLogicService()
        {
            LoggingService.LogDebug("GameLogicService initialized");
        }

        #region Game Initialization

        public GameData CreateNewGame()
        {
            try
            {
                LoggingService.LogDebug("Creating new game");

                var gameData = new GameData
                {
                    Gold = 100,
                    CurrentLocationIndex = 0,
                    CurrentScreen = "MainMenu",
                    Player = CreatePlayer(),
                    Inventory = CreateInventory(),
                    Locations = CreateLocations(),
                    Settings = new GameSettings
                    {
                        Language = Language.Russian,
                        Difficulty = Difficulty.Normal,
                        UIScale = 1.0
                    }
                };

                // Give player starting equipment
                GiveStartingItems(gameData);

                LoggingService.LogDebug("New game created successfully");
                return gameData;
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error creating new game: {ex.Message}", ex);
                throw;
            }
        }

        public Character CreatePlayer()
        {
            try
            {
                var player = new Character
                {
                    Name = "Игрок",
                    MaxHealth = BASE_PLAYER_HEALTH,
                    CurrentHealth = BASE_PLAYER_HEALTH,
                    Attack = BASE_PLAYER_ATTACK,
                    Defense = BASE_PLAYER_DEFENSE,
                    IsPlayer = true,
                    Level = 1,
                    XP = 0,
                    XPToNextLevel = CalculateXPToNextLevel(1),
                    Gold = 100,
                    Money = 100,
                    ImagePath = AssetPaths.Characters.PLAYER,
                    EquippedItems = new Dictionary<EquipmentSlot, Item>()
                };

                LoggingService.LogDebug("Player character created");
                return player;
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error creating player: {ex.Message}", ex);
                throw;
            }
        }

        public Inventory CreateInventory()
        {
            try
            {
                var inventory = new Inventory(15)
                {
                    Gold = 100
                };

                LoggingService.LogDebug("Inventory created");
                return inventory;
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error creating inventory: {ex.Message}", ex);
                throw;
            }
        }

        public ObservableCollection<Location> CreateLocations()
        {
            try
            {
                var locations = new ObservableCollection<Location>();

                foreach (var kvp in _locationDefinitions)
                {
                    var locationData = kvp.Value;
                    var location = new Location
                    {
                        Name = LocalizationService.Instance.GetTranslation($"Locations.{locationData.Name}"),
                        Description = LocalizationService.Instance.GetTranslation($"Locations.{locationData.Name}.Description"),
                        Type = locationData.Type,
                        Difficulty = locationData.Difficulty,
                        IsUnlocked = kvp.Key == 0, // Only village is unlocked initially
                        IsAvailable = kvp.Key == 0,
                        IsCompleted = false,
                        HeroDefeated = false,
                        MinPlayerLevel = GetRecommendedLevel(kvp.Key),
                        MaxCompletions = -1, // Unlimited
                        CompletionCount = 0,
                        SpritePath = AssetPaths.Locations.GetLocationPathByName(locationData.Name.ToLower()),
                        Hero = CreateLocationHero(locationData.Name, kvp.Key),
                        LocationType = locationData.Type, // Compatibility property
                        ImagePath = AssetPaths.Locations.GetLocationPathByName(locationData.Name.ToLower()) // Compatibility property
                    };

                    locations.Add(location);
                }

                LoggingService.LogDebug($"Created {locations.Count} locations");
                return locations;
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error creating locations: {ex.Message}", ex);
                throw;
            }
        }

        #endregion

        #region Balance and Scaling

        public int CalculateScaledValue(int baseValue, int playerLevel, double scalingFactor = 1.0)
        {
            try
            {
                if (baseValue <= 0 || playerLevel <= 0)
                    return baseValue;

                var scaled = baseValue * Math.Pow(scalingFactor, playerLevel - 1);
                return Math.Max(1, (int)Math.Round(scaled));
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error calculating scaled value: {ex.Message}", ex);
                return baseValue;
            }
        }

        public int CalculateEnemyHealth(int baseHealth, int locationDifficulty, int playerLevel)
        {
            try
            {
                var difficultyMultiplier = GetDifficultyMultiplier(locationDifficulty);
                var scaledHealth = CalculateScaledValue(baseHealth, playerLevel, HEALTH_SCALING_FACTOR);
                return (int)(scaledHealth * difficultyMultiplier);
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error calculating enemy health: {ex.Message}", ex);
                return baseHealth;
            }
        }

        public int CalculateEnemyAttack(int baseAttack, int locationDifficulty, int playerLevel)
        {
            try
            {
                var difficultyMultiplier = GetDifficultyMultiplier(locationDifficulty);
                var scaledAttack = CalculateScaledValue(baseAttack, playerLevel, ATTACK_SCALING_FACTOR);
                return (int)(scaledAttack * difficultyMultiplier);
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error calculating enemy attack: {ex.Message}", ex);
                return baseAttack;
            }
        }

        public int CalculateXPReward(int enemyLevel, bool isBoss = false)
        {
            try
            {
                var baseXP = (int)(enemyLevel * XP_BASE_MULTIPLIER);
                if (isBoss)
                {
                    baseXP *= 3; // Bosses give 3x XP
                }
                
                // Add random variation ±20%
                var variation = _random.NextDouble() * 0.4 - 0.2; // -0.2 to +0.2
                return Math.Max(1, (int)(baseXP * (1 + variation)));
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error calculating XP reward: {ex.Message}", ex);
                return 10;
            }
        }

        public int CalculateGoldReward(int enemyLevel, bool isBoss = false)
        {
            try
            {
                var baseGold = (int)(enemyLevel * GOLD_BASE_MULTIPLIER);
                if (isBoss)
                {
                    baseGold *= 2; // Bosses give 2x gold
                }
                
                // Add random variation ±30%
                var variation = _random.NextDouble() * 0.6 - 0.3; // -0.3 to +0.3
                return Math.Max(1, (int)(baseGold * (1 + variation)));
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error calculating gold reward: {ex.Message}", ex);
                return 5;
            }
        }

        #endregion

        #region Difficulty Management

        public LocationDifficultyLevel GetLocationDifficulty(int locationIndex)
        {
            if (_locationDefinitions.TryGetValue(locationIndex, out var definition))
            {
                return definition.Difficulty;
            }
            return LocationDifficultyLevel.Medium;
        }

        public bool IsLocationUnlocked(int locationIndex, GameData gameData)
        {
            try
            {
                if (locationIndex == 0) return true; // Village always unlocked

                // Check if previous location is completed
                if (locationIndex > 0 && locationIndex < gameData.Locations.Count)
                {
                    var previousLocation = gameData.Locations[locationIndex - 1];
                    return previousLocation.HeroDefeated;
                }

                return false;
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error checking location unlock: {ex.Message}", ex);
                return false;
            }
        }

        public int GetRecommendedLevel(int locationIndex)
        {
            return locationIndex switch
            {
                0 => 1,  // Village
                1 => 3,  // Forest
                2 => 6,  // Cave
                3 => 10, // Ruins
                4 => 15, // Castle
                _ => 1
            };
        }

        #endregion

        #region Item Generation

        public List<Item> GenerateRandomLoot(int locationIndex, int quantity = 1)
        {
            try
            {
                var loot = new List<Item>();
                var locationTier = Math.Max(1, locationIndex + 1);

                for (int i = 0; i < quantity; i++)
                {
                    var itemType = GetRandomLootType(locationIndex);
                    var item = CreateRandomItem(itemType, locationTier);
                    if (item != null)
                    {
                        loot.Add(item);
                    }
                }

                LoggingService.LogDebug($"Generated {loot.Count} loot items for location {locationIndex}");
                return loot;
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error generating random loot: {ex.Message}", ex);
                return new List<Item>();
            }
        }

        public Item? CreateRandomItem(ItemType type, int locationTier)
        {
            try
            {
                var material = GetRandomMaterial(locationTier);
                var rarity = GetRandomRarity(locationTier);

                // Create basic item based on type and tier
                return type switch
                {
                    ItemType.Material => CreateMaterialByTier(material, rarity, _random.Next(1, 5)),
                    ItemType.Consumable => ItemFactory.CreateHealingPotion(1),
                    ItemType.Weapon => CreateWeaponByMaterial(material),
                    ItemType.Helmet => ItemFactory.CreateArmorForSlot(material, ItemSlotType.Head),
                    ItemType.Chestplate => ItemFactory.CreateArmorForSlot(material, ItemSlotType.Chest),
                    ItemType.Leggings => ItemFactory.CreateArmorForSlot(material, ItemSlotType.Legs),
                    ItemType.Shield => ItemFactory.CreateShieldForMaterial(material),
                    _ => null
                };
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error creating random item: {ex.Message}", ex);
                return null;
            }
        }

        #endregion

        #region Enemy Generation

        public List<Character> GenerateEnemies(Location location, int playerLevel)
        {
            try
            {
                var enemies = new List<Character>();
                var locationTier = GetLocationTier(location.Type);
                var enemyCount = _random.Next(1, 4); // 1-3 enemies

                for (int i = 0; i < enemyCount; i++)
                {
                    var enemyType = GetRandomEnemyType(location.Type);
                    var enemy = CreateEnemy(enemyType, locationTier, playerLevel);
                    enemies.Add(enemy);
                }

                LoggingService.LogDebug($"Generated {enemies.Count} enemies for {location.Name}");
                return enemies;
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error generating enemies: {ex.Message}", ex);
                return new List<Character>();
            }
        }

        public Character CreateEnemy(string enemyType, int locationTier, int playerLevel)
        {
            try
            {
                var baseStats = GetEnemyBaseStats(enemyType);
                var difficulty = (int)GetLocationDifficulty(locationTier - 1);

                var enemy = new Character
                {
                    Name = LocalizationService.Instance.GetTranslation($"Enemies.{enemyType}"),
                    Type = enemyType,
                    IsPlayer = false,
                    IsHero = false,
                    Level = Math.Max(1, playerLevel + _random.Next(-1, 2)),
                    ImagePath = AssetPaths.Enemies.GetEnemyPath(enemyType)
                };

                // Calculate scaled stats
                enemy.MaxHealth = CalculateEnemyHealth(baseStats.Health, difficulty, enemy.Level);
                enemy.CurrentHealth = enemy.MaxHealth;
                enemy.Attack = CalculateEnemyAttack(baseStats.Attack, difficulty, enemy.Level);
                enemy.Defense = CalculateScaledValue(baseStats.Defense, enemy.Level, 1.05);

                LoggingService.LogDebug($"Created enemy: {enemy.Name} (Level {enemy.Level})");
                return enemy;
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error creating enemy: {ex.Message}", ex);
                throw;
            }
        }

        #endregion

        #region Private Helper Methods

        private void GiveStartingItems(GameData gameData)
        {
            try
            {
                LoggingService.LogDebug("Starting to give starting items to player");
                
                // Give wooden sword
                var woodenSword = ItemFactory.CreateWoodenWeapon();
                LoggingService.LogInfo($"Adding item: {woodenSword.Name} x1");
                bool swordAdded = gameData.Inventory.AddItem(woodenSword);
                LoggingService.LogInfo($"Wooden sword added: {swordAdded}");

                // Give some health potions
                var healthPotion = ItemFactory.CreateHealingPotion(1);
                LoggingService.LogInfo($"Adding item: {healthPotion.Name} x1");
                bool potionAdded = gameData.Inventory.AddItem(healthPotion);
                LoggingService.LogInfo($"Health potion added: {potionAdded}");

                // Give some basic materials
                var wood = ItemFactory.CreateWood(1);
                LoggingService.LogInfo($"Adding item: {wood.Name} x1");
                bool woodAdded = gameData.Inventory.AddItem(wood);
                LoggingService.LogInfo($"Wood added: {woodAdded}");

                // КРИТИЧЕСКОЕ ИСПРАВЛЕНИЕ: Принудительно обновляем инвентарь
                gameData.Inventory.OnInventoryChanged();
                LoggingService.LogDebug($"После добавления стартовых предметов: {gameData.Inventory.Items.Count(x => x != null)} не-null предметов в инвентаре");
                
                // Логируем содержимое инвентаря для отладки
                foreach (var item in gameData.Inventory.Items.Where(x => x != null))
                {
                    LoggingService.LogDebug($"Стартовый предмет в инвентаре: {item.Name} x{item.StackSize}");
                }
                
                LoggingService.LogDebug("Starting items given to player");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error giving starting items: {ex.Message}", ex);
                throw;
            }
        }

        private Character CreateLocationHero(string locationName, int locationIndex)
        {
            try
            {
                var heroName = $"{locationName} Guardian";
                var recommendedLevel = GetRecommendedLevel(locationIndex);
                var difficulty = (int)GetLocationDifficulty(locationIndex);

                var hero = new Character
                {
                    Name = LocalizationService.Instance.GetTranslation($"Heroes.{locationName}"),
                    Type = $"{locationName}Hero",
                    IsPlayer = false,
                    IsHero = true,
                    Level = recommendedLevel + 2, // Heroes are 2 levels above recommended
                    ImagePath = AssetPaths.Enemies.GetHeroPath(locationName)
                };

                // Calculate hero stats (stronger than regular enemies)
                var baseHealth = 150 + (locationIndex * 50);
                var baseAttack = 15 + (locationIndex * 5);
                var baseDefense = 8 + (locationIndex * 3);

                hero.MaxHealth = CalculateEnemyHealth(baseHealth, difficulty, hero.Level);
                hero.CurrentHealth = hero.MaxHealth;
                hero.Attack = CalculateEnemyAttack(baseAttack, difficulty, hero.Level);
                hero.Defense = CalculateScaledValue(baseDefense, hero.Level, 1.1);

                LoggingService.LogDebug($"Created location hero: {hero.Name}");
                return hero;
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error creating location hero: {ex.Message}", ex);
                throw;
            }
        }

        private int CalculateXPToNextLevel(int currentLevel)
        {
            return (int)(100 * Math.Pow(1.2, currentLevel - 1));
        }

        private double GetDifficultyMultiplier(int difficulty)
        {
            return difficulty switch
            {
                (int)LocationDifficultyLevel.Easy => 0.8,
                (int)LocationDifficultyLevel.Medium => 1.0,
                (int)LocationDifficultyLevel.Hard => 1.3,
                (int)LocationDifficultyLevel.VeryHard => 1.6,
                (int)LocationDifficultyLevel.Extreme => 2.0,
                _ => 1.0
            };
        }

        private ItemType GetRandomLootType(int locationIndex)
        {
            var roll = _random.NextDouble();
            return locationIndex switch
            {
                0 => roll < 0.7 ? ItemType.Material : ItemType.Consumable, // Village: mostly materials
                1 => roll < 0.5 ? ItemType.Material : (roll < 0.8 ? ItemType.Consumable : ItemType.Weapon), // Forest: mixed
                _ => roll < 0.3 ? ItemType.Material : (roll < 0.6 ? ItemType.Consumable : (roll < 0.8 ? ItemType.Weapon : GetRandomArmorType()))
            };
        }

        private ItemMaterial GetRandomMaterial(int locationTier)
        {
            return locationTier switch
            {
                1 => ItemMaterial.Wood,
                2 => _random.NextDouble() < 0.7 ? ItemMaterial.Wood : ItemMaterial.Iron,
                3 => _random.NextDouble() < 0.3 ? ItemMaterial.Wood : (_random.NextDouble() < 0.8 ? ItemMaterial.Iron : ItemMaterial.Gold),
                4 => _random.NextDouble() < 0.1 ? ItemMaterial.Iron : (_random.NextDouble() < 0.7 ? ItemMaterial.Gold : ItemMaterial.Luminite),
                5 => _random.NextDouble() < 0.5 ? ItemMaterial.Gold : ItemMaterial.Luminite,
                _ => ItemMaterial.Wood
            };
        }

        private ItemRarity GetRandomRarity(int locationTier)
        {
            var roll = _random.NextDouble();
            return locationTier switch
            {
                1 => ItemRarity.Common,
                2 => roll < 0.8 ? ItemRarity.Common : ItemRarity.Uncommon,
                3 => roll < 0.6 ? ItemRarity.Common : (roll < 0.9 ? ItemRarity.Uncommon : ItemRarity.Rare),
                4 => roll < 0.4 ? ItemRarity.Common : (roll < 0.7 ? ItemRarity.Uncommon : (roll < 0.95 ? ItemRarity.Rare : ItemRarity.Epic)),
                5 => roll < 0.2 ? ItemRarity.Common : (roll < 0.5 ? ItemRarity.Uncommon : (roll < 0.8 ? ItemRarity.Rare : (roll < 0.98 ? ItemRarity.Epic : ItemRarity.Legendary))),
                _ => ItemRarity.Common
            };
        }

        private int GetLocationTier(LocationType locationType)
        {
            return locationType switch
            {
                LocationType.Village => 1,
                LocationType.Forest => 2,
                LocationType.Cave => 3,
                LocationType.Ruins => 4,
                LocationType.Castle => 5,
                _ => 1
            };
        }

        private string GetRandomEnemyType(LocationType locationType)
        {
            var enemies = locationType switch
            {
                LocationType.Village => new[] { "Bandit", "Thief" },
                LocationType.Forest => new[] { "Wolf", "Bear", "Goblin" },
                LocationType.Cave => new[] { "Orc", "Troll", "Spider" },
                LocationType.Ruins => new[] { "Skeleton", "Wraith", "Golem" },
                LocationType.Castle => new[] { "Knight", "Demon", "Dragon" },
                _ => new[] { "Bandit" }
            };

            return enemies[_random.Next(enemies.Length)];
        }

        private (int Health, int Attack, int Defense) GetEnemyBaseStats(string enemyType)
        {
            return enemyType switch
            {
                "Bandit" => (30, 8, 2),
                "Thief" => (25, 10, 1),
                "Wolf" => (35, 12, 3),
                "Bear" => (50, 15, 5),
                "Goblin" => (28, 9, 2),
                "Orc" => (60, 18, 6),
                "Troll" => (80, 20, 8),
                "Spider" => (40, 14, 4),
                "Skeleton" => (45, 16, 5),
                "Wraith" => (55, 22, 3),
                "Golem" => (100, 15, 12),
                "Knight" => (90, 25, 10),
                "Demon" => (120, 30, 8),
                "Dragon" => (200, 40, 15),
                _ => (30, 10, 3)
            };
        }

        private Item CreateMaterialByTier(ItemMaterial material, ItemRarity rarity, int amount)
        {
            return material switch
            {
                ItemMaterial.Wood => ItemFactory.CreateWood(amount),
                ItemMaterial.Iron => ItemFactory.CreateIronOre(amount),
                ItemMaterial.Gold => ItemFactory.CreateGoldOre(amount),
                ItemMaterial.Luminite => ItemFactory.CreateLuminite(amount),
                _ => ItemFactory.CreateWood(amount)
            };
        }

        private Item CreateWeaponByMaterial(ItemMaterial material)
        {
            return material switch
            {
                ItemMaterial.Wood => ItemFactory.CreateWoodenWeapon(),
                ItemMaterial.Iron => ItemFactory.CreateIronWeapon(),
                ItemMaterial.Gold => ItemFactory.CreateGoldWeapon(),
                ItemMaterial.Luminite => ItemFactory.CreateLuminiteWeapon(),
                _ => ItemFactory.CreateWoodenWeapon()
            };
        }

        private ItemType GetRandomArmorType()
        {
            var armorTypes = new[] { ItemType.Helmet, ItemType.Chestplate, ItemType.Leggings };
            return armorTypes[_random.Next(armorTypes.Length)];
        }

        #endregion
    }
} 