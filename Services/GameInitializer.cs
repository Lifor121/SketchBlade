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
                Name = LocalizationService.Instance.GetTranslation("Characters.PlayerName"),
                MaxHealth = 100,
                CurrentHealth = 100,
                Attack = 10,
                Defense = 5,
                IsPlayer = true,  // ИСПРАВЛЕНИЕ: добавляем IsPlayer = true
                ImagePath = AssetPaths.Characters.PLAYER
            };

            LoggingService.LogDebug($"InitializePlayer: Created player with IsPlayer = {gameData.Player.IsPlayer}, Name = {gameData.Player.Name}");
            LoggingService.LogInfo("Player character initialized");
        }

        private void InitializeInventory(GameData gameData)
        {
            LoggingService.LogInfo("=== ИНИЦИАЛИЗАЦИЯ СТАРТОВОГО ИНВЕНТАРЯ ===");
            
            var startingItems = new List<(string name, int quantity)>
            {
                ("Дерево", 10),
                ("Зелье лечения", 6),
                ("Зелье ярости", 1),
                ("Зелье неуязвимости", 1),
                ("Бомба", 1),
                ("Подушка", 1),
                ("Отравленный сюрикен", 1),
                ("Деревянный меч", 1),
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
            
            // Удаляем автоматическое заполнение панели быстрого доступа
            // Теперь игрок сам может выбирать, какие предметы помещать в быстрые слоты
            
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
            // LoggingService.LogDebug($"Created {locations.Length} locations");
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
            // Используем LocationType для более надежного поиска локаций
            var lootTables = new[]
            {
                (LocationType.Village, new[] { "Wood", "Herbs", "Cloth", "Water Flask" }),
                (LocationType.Forest, new[] { "Wood", "Herbs", "Iron Ore", "Crystal Dust", "Feathers" }),
                (LocationType.Cave, new[] { "Iron Ore", "Iron Ingot", "Gunpowder", "Gold Ore" }),
                (LocationType.Ruins, new[] { "Gold Ore", "Gold Ingot", "Poison Extract", "Luminite Fragment" }),
                (LocationType.Castle, new[] { "Gold Ingot", "Luminite Fragment", "Luminite" })
            };

            foreach (var (locationType, materials) in lootTables)
            {
                var location = locations.FirstOrDefault(l => l.LocationType == locationType);
                if (location != null)
                {
                    location.LootTable = materials.ToList();
                    LoggingService.LogInfo($"SetupLootTables: Настроена таблица лута для {locationType}: [{string.Join(", ", materials)}]");
                }
                else
                {
                    LoggingService.LogWarning($"SetupLootTables: Не найдена локация типа {locationType}");
                }
            }
        }

        private void SetupStartingLocation(GameData gameData)
        {
            var village = gameData.Locations.FirstOrDefault(l => l.LocationType == LocationType.Village);
            if (village != null)
            {
                gameData.CurrentLocation = village;
                gameData.CurrentLocationIndex = 0;
                village.IsUnlocked = true;
                
                LoggingService.LogInfo($"Starting location set to {village.Name} (Village)");
            }
            else
            {
                LoggingService.LogError("Village location not found!");
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
            // Базовые статы для обычных врагов
            var baseStats = locationType switch
            {
                LocationType.Village => (GetLocalizedEnemyName(locationType, false), 20, 5, 2),
                LocationType.Forest => (GetLocalizedEnemyName(locationType, false), 40, 12, 5),
                LocationType.Cave => (GetLocalizedEnemyName(locationType, false), 60, 18, 8),
                LocationType.Ruins => (GetLocalizedEnemyName(locationType, false), 80, 25, 12),
                LocationType.Castle => (GetLocalizedEnemyName(locationType, false), 120, 35, 18),
                _ => ("Неизвестный враг", 30, 8, 3)
            };

            if (isHero)
            {
                // Для героев используем специальные названия из локализации
                string heroName = GetLocalizedEnemyName(locationType, true);
                return (
                    heroName,
                    (int)(baseStats.Item2 * 1.5),
                    (int)(baseStats.Item3 * 1.3),
                    (int)(baseStats.Item4 * 1.2)
                );
            }

            return baseStats;
        }

        private string GetLocalizedEnemyName(LocationType locationType, bool isHero)
        {
            if (isHero)
            {
                string heroKey = locationType switch
                {
                    LocationType.Village => "Characters.Heroes.VillageElder",
                    LocationType.Forest => "Characters.Heroes.ForestGuardian",
                    LocationType.Cave => "Characters.Heroes.CaveTroll",
                    LocationType.Ruins => "Characters.Heroes.GuardianGolem",
                    LocationType.Castle => "Characters.Heroes.DarkKing",
                    _ => "Characters.Heroes.VillageElder"
                };
                return LocalizationService.Instance.GetTranslation(heroKey);
            }
            else
            {
                string key = $"Characters.Enemies.{locationType}.Regular";
                return LocalizationService.Instance.GetTranslation(key);
            }
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
            // Логирование отключено
            /*
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
            */
        }
    }
} 
