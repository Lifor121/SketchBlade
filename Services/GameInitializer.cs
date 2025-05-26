using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using SketchBlade.Models;
using SketchBlade.Services;
using SketchBlade.Utilities;

namespace SketchBlade.Services
{
    public class GameInitializer
    {
        public void InitializeNewGame(GameData gameData)
        {
            InitializePlayer(gameData);
            InitializeInventory(gameData);
            InitializeLocations(gameData);
            SetupStartingLocation(gameData);
        }

        private void InitializePlayer(GameData gameData)
        {
            gameData.Player = new Character
            {
                Name = "Hero",
                MaxHealth = 100,
                CurrentHealth = 100,
                Attack = 10,
                Defense = 5,
                ImagePath = AssetPaths.Characters.PLAYER
            };

            LoggingService.LogInfo("Player character initialized");
        }

        private void InitializeInventory(GameData gameData)
        {
            LoggingService.LogInfo("=== ИНИЦИАЛИЗАЦИЯ СТАРТОВОГО ИНВЕНТАРЯ ===");
            
            var startingItems = new List<(string name, int quantity)>
            {
                ("Дерево", 20),           
                ("Трава", 10),            
                ("Палка", 5),             
                ("Зелье лечения", 3),     
                ("Ткань", 5),          
                ("Фляга", 3),           
                ("Железная руда", 8),     
                ("Железный слиток", 4),   
                ("Золотой слиток", 2)  
            };
            
            foreach (var (itemName, quantity) in startingItems)
            {
                var item = ItemFactory.CreateItem(itemName, 1);
                if (item != null)
                {
                    bool added = gameData.Inventory.AddItem(item, quantity);
                    LoggingService.LogInfo($"{itemName} added: {added} (Stack: {quantity})");
                }
                else
                {
                    LoggingService.LogError($"Не удалось создать предмет: {itemName} x{quantity}");
                }
            }
            
            var nonNullItems = gameData.Inventory.Items.Count(item => item != null);
            LoggingService.LogInfo("Starting items given to player");
            gameData.Inventory.OnInventoryChanged();
        }

        private void InitializeLocations(GameData gameData)
        {
            var locations = new[]
            {
                CreateLocation("Locations.Village", "Мирная деревня", LocationType.Village, true, false),
                CreateLocation("Locations.Forest", "Тёмный лес", LocationType.Forest, false, false),
                CreateLocation("Locations.Cave", "Глубокая пещера", LocationType.Cave, false, false),
                CreateLocation("Locations.Ancient Ruins", "Древние руины", LocationType.Ruins, false, false),
                CreateLocation("Locations.Dark Castle", "Проклятый замок", LocationType.Castle, false, false)
            };

            gameData.Locations.Clear();
            foreach (var location in locations)
            {
                gameData.Locations.Add(location);
            }

            SetupLootTables(gameData.Locations);
            LoggingService.LogDebug($"Created {locations.Length} locations");
        }

        private Location CreateLocation(string name, string description, LocationType type, bool isUnlocked, bool isCompleted)
        {
            string spritePath = type switch
            {
                LocationType.Village => AssetPaths.Locations.VILLAGE,
                LocationType.Forest => AssetPaths.Locations.FOREST,
                LocationType.Cave => AssetPaths.Locations.CAVE,
                LocationType.Ruins => AssetPaths.Locations.RUINS,
                LocationType.Castle => AssetPaths.Locations.CASTLE,
                _ => AssetPaths.DEFAULT_IMAGE
            };

            var hero = CreateEnemy(type, true);

            return new Location
            {
                Name = name,
                Description = description,
                LocationType = type,
                IsUnlocked = isUnlocked,
                IsCompleted = isCompleted,
                SpritePath = spritePath, 
                Hero = hero,
                HeroDefeated = false
            };
        }

        private void SetupLootTables(ObservableCollection<Location> locations)
        {
            var lootTables = new[]
            {
                ("Village", new[] { "Wood", "Herbs", "Cloth", "Water Flask" }),
                ("Forest", new[] { "Wood", "Herbs", "Iron Ore", "Crystal Dust", "Feathers" }),
                ("Cave", new[] { "Iron Ore", "Iron Ingot", "Gunpowder", "Gold Ore" }),
                ("Ruins", new[] { "Gold Ore", "Gold Ingot", "Poison Extract", "Luminite Fragment" }),
                ("Castle", new[] { "Gold Ingot", "Luminite Fragment", "Luminite" })
            };

            foreach (var (locationName, materials) in lootTables)
            {
                var location = locations.FirstOrDefault(l => l.Name == locationName);
                if (location != null)
                {
                    location.LootTable = materials.ToList();
                }
            }
        }

        private void SetupStartingLocation(GameData gameData)
        {
            var village = gameData.Locations.FirstOrDefault(l => l.Name == "Village");
            if (village != null)
            {
                gameData.CurrentLocation = village;
                gameData.CurrentLocationIndex = 0;
                village.IsUnlocked = true;
                
                LoggingService.LogInfo("Starting location set to Village");
            }
        }

        public Character CreateEnemy(LocationType locationType, bool isHero = false)
        {
            var enemyData = GetEnemyData(locationType, isHero);
            
            return new Character
            {
                Name = enemyData.Name,
                MaxHealth = enemyData.Health,
                CurrentHealth = enemyData.Health,
                Attack = enemyData.Attack,
                Defense = enemyData.Defense,
                IsHero = isHero,
                LocationType = locationType,
                ImagePath = AssetPaths.Enemies.GetEnemyByLocationType(locationType.ToString(), isHero)
            };
        }

        private (string Name, int Health, int Attack, int Defense) GetEnemyData(LocationType locationType, bool isHero)
        {
            var baseStats = locationType switch
            {
                LocationType.Village => ("Крыса", 20, 5, 2),
                LocationType.Forest => ("Волк", 40, 12, 5),
                LocationType.Cave => ("Орк", 60, 18, 8),
                LocationType.Ruins => ("Скелет", 80, 25, 12),
                LocationType.Castle => ("Дракон", 120, 35, 18),
                _ => ("Неизвестный враг", 30, 8, 3)
            };

            if (isHero)
            {
                return (
                    $"Герой {baseStats.Item1}",
                    (int)(baseStats.Item2 * 1.5),
                    (int)(baseStats.Item3 * 1.3),
                    (int)(baseStats.Item4 * 1.2)
                );
            }

            return baseStats;
        }

        public bool ValidateInitialization(GameData gameData)
        {
            if (gameData.Player == null)
            {
                LoggingService.LogError("Player not initialized");
                return false;
            }

            if (gameData.Locations.Count == 0)
            {
                LoggingService.LogError("No locations initialized");
                return false;
            }

            if (gameData.CurrentLocation == null)
            {
                LoggingService.LogError("Current location not set");
                return false;
            }

            if (gameData.Inventory.Items.Count == 0)
            {
                LoggingService.LogError("Starting inventory is empty");
                return false;
            }

            LoggingService.LogInfo("Game initialization validation passed");
            return true;
        }

        public List<Character> GenerateEnemiesForLocation(Location location, bool isHeroBattle)
        {
            var enemies = new List<Character>();

            if (isHeroBattle)
            {
                var hero = CreateEnemy(location.LocationType, true);
                enemies.Add(hero);
            }
            else
            {
                int enemyCount = location.LocationType switch
                {
                    LocationType.Village => 1,
                    LocationType.Forest => 2,
                    LocationType.Cave => 2,
                    LocationType.Ruins => 3,
                    LocationType.Castle => 3,
                    _ => 1
                };

                for (int i = 0; i < enemyCount; i++)
                {
                    var enemy = CreateEnemy(location.LocationType, false);
                    enemies.Add(enemy);
                }
            }

            return enemies;
        }

        public GameData CreateNewGame()
        {
            var gameData = new GameData();
            InitializeNewGame(gameData);
            return gameData;
        }

        public void CreateTestCraftingInventory(GameData gameData)
        {
            try
            {
                LoggingService.LogInfo("=== СОЗДАНИЕ ТЕСТОВОГО ИНВЕНТАРЯ ДЛЯ КРАФТА ===");
                
                for (int i = 0; i < gameData.Inventory.Items.Count; i++)
                {
                    gameData.Inventory.Items[i] = null;
                }
                
                var craftingMaterials = new List<(Func<int, Item> factory, int count, string name)>
                {
                    (ItemFactory.CreateWood, 50, "Дерево"),     
                    (ItemFactory.CreateStick, 20, "Палка"), 
                    (ItemFactory.CreateIronIngot, 30, "Железный слиток"),
                    (ItemFactory.CreateGoldIngot, 15, "Золотой слиток"),  
                    (ItemFactory.CreateLuminite, 10, "Люминит"),      
                    (ItemFactory.CreateLuminiteFragment, 10, "Фрагмент люминита"), 
                    (ItemFactory.CreateHerb, 20, "Трава"),           
                    (ItemFactory.CreateFlask, 10, "Фляга"),           
                    (ItemFactory.CreateIronOre, 20, "Железная руда") 
                };
                
                foreach (var (factory, count, name) in craftingMaterials)
                {
                    var item = factory(count);
                    bool added = gameData.Inventory.AddItem(item);
                }
                
                var finalItems = gameData.Inventory.Items.Where(item => item != null).ToList();
                LoggingService.LogInfo($"Тестовый инвентарь создан: {finalItems.Count} различных материалов");
                
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Ошибка при создании тестового инвентаря", ex);
            }
        }


        public void ClearErrorLog()
        {
            try
            {
                var logPath = "bin/Debug/net9.0-windows/error_log.txt";
                if (System.IO.File.Exists(logPath))
                {
                    System.IO.File.WriteAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] === ЛОГ ОЧИЩЕН ===\r\n");
                    LoggingService.LogInfo("Лог файл очищен");
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Ошибка при очистке лог файла", ex);
            }
        }
    }
} 