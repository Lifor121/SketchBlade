using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using SketchBlade.Models;
using SketchBlade.Services;
using SketchBlade.Utilities;

namespace SketchBlade.Services
{
    /// <summary>
    /// Инициализатор игры - отвечает за настройку начальных данных
    /// </summary>
    public class GameInitializer
    {
        /// <summary>
        /// Инициализирует новую игру
        /// </summary>
        public void InitializeNewGame(GameData gameData)
        {
            InitializePlayer(gameData);
            InitializeInventory(gameData);
            InitializeLocations(gameData);
            SetupStartingLocation(gameData);
        }

        /// <summary>
        /// Создает начального игрока
        /// </summary>
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

        /// <summary>
        /// Настраивает начальный инвентарь
        /// </summary>
        private void InitializeInventory(GameData gameData)
        {
            LoggingService.LogInfo("=== ИНИЦИАЛИЗАЦИЯ СТАРТОВОГО ИНВЕНТАРЯ ===");
            
            // Увеличенные количества для доступности большего числа рецептов
            var startingItems = new List<(string name, int quantity)>
            {
                ("Дерево", 20),           // Увеличено с 1 до 20 - для деревянных предметов
                ("Трава", 10),            // Увеличено с 1 до 10 - для зелий
                ("Палка", 5),             // Увеличено с 1 до 5 - для оружия
                ("Зелье лечения", 3),     // Оставляем 3
                ("Ткань", 5),             // Увеличено с 1 до 5
                ("Фляга", 3),             // Оставляем 3
                ("Железная руда", 8),     // Увеличено с 1 до 8 - для выплавки слитков
                ("Железный слиток", 4),   // Увеличено с 1 до 4 - для железного оружия/брони
                ("Золотой слиток", 2)     // Увеличено с 1 до 2 - для золотого оружия
            };
            
            foreach (var (itemName, quantity) in startingItems)
            {
                var item = ItemFactory.CreateItem(itemName, 1);
                if (item != null)
                {
                    LoggingService.LogDebug($"Создан предмет-шаблон: {item.Name} для добавления количества: {quantity}");
                    bool added = gameData.Inventory.AddItem(item, quantity);
                    LoggingService.LogInfo($"{itemName} added: {added} (Stack: {quantity})");
                }
                else
                {
                    LoggingService.LogError($"Не удалось создать предмет: {itemName} x{quantity}");
                }
            }
            
            // Диагностика результата
            var nonNullItems = gameData.Inventory.Items.Count(item => item != null);
            LoggingService.LogDebug($"После добавления стартовых предметов: {nonNullItems} не-null предметов в инвентаре");
            
            foreach (var item in gameData.Inventory.Items.Where(item => item != null))
            {
                LoggingService.LogDebug($"Стартовый предмет в инвентаре: {item.Name} x{item.StackSize}");
            }
            
            LoggingService.LogInfo("Starting items given to player");
            
            // ОПТИМИЗАЦИЯ: Вызываем OnInventoryChanged только один раз в конце
            gameData.Inventory.OnInventoryChanged();
        }

        /// <summary>
        /// Создает локации мира
        /// </summary>
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

        /// <summary>
        /// Создает одну локацию
        /// </summary>
        private Location CreateLocation(string name, string description, LocationType type, bool isUnlocked, bool isCompleted)
        {
            // Determine correct sprite path based on location type
            string spritePath = type switch
            {
                LocationType.Village => AssetPaths.Locations.VILLAGE,
                LocationType.Forest => AssetPaths.Locations.FOREST,
                LocationType.Cave => AssetPaths.Locations.CAVE,
                LocationType.Ruins => AssetPaths.Locations.RUINS,
                LocationType.Castle => AssetPaths.Locations.CASTLE,
                _ => AssetPaths.DEFAULT_IMAGE
            };

            // Создаем героя для каждой локации
            var hero = CreateEnemy(type, true);

            return new Location
            {
                Name = name,
                Description = description,
                LocationType = type,
                IsUnlocked = isUnlocked,
                IsCompleted = isCompleted,
                SpritePath = spritePath,  // Use SpritePath instead of ImagePath
                Hero = hero,  // ДОБАВЛЕНО: устанавливаем героя для локации
                HeroDefeated = false  // ДОБАВЛЕНО: инициализируем статус героя
            };
        }

        /// <summary>
        /// Настраивает таблицы лута для локаций
        /// </summary>
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

        /// <summary>
        /// Устанавливает начальную локацию
        /// </summary>
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

        /// <summary>
        /// Создает врагов для локации
        /// </summary>
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

        /// <summary>
        /// Получает характеристики врага по локации
        /// </summary>
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
                // Герои сильнее обычных врагов
                return (
                    $"Герой {baseStats.Item1}",
                    (int)(baseStats.Item2 * 1.5),
                    (int)(baseStats.Item3 * 1.3),
                    (int)(baseStats.Item4 * 1.2)
                );
            }

            return baseStats;
        }

        /// <summary>
        /// Проверяет целостность инициализированных данных
        /// </summary>
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

        /// <summary>
        /// Генерирует врагов для локации (обратная совместимость)
        /// </summary>
        public List<Character> GenerateEnemiesForLocation(Location location, bool isHeroBattle)
        {
            var enemies = new List<Character>();

            if (isHeroBattle)
            {
                // Только герой
                var hero = CreateEnemy(location.LocationType, true);
                enemies.Add(hero);
            }
            else
            {
                // Несколько обычных врагов
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

        /// <summary>
        /// Создает новую игру (обратная совместимость)
        /// </summary>
        public GameData CreateNewGame()
        {
            var gameData = new GameData();
            InitializeNewGame(gameData);
            return gameData;
        }

        /// <summary>
        /// Создает тестовый инвентарь с материалами для крафта
        /// </summary>
        public void CreateTestCraftingInventory(GameData gameData)
        {
            try
            {
                LoggingService.LogInfo("=== СОЗДАНИЕ ТЕСТОВОГО ИНВЕНТАРЯ ДЛЯ КРАФТА ===");
                
                // Очищаем инвентарь
                for (int i = 0; i < gameData.Inventory.Items.Count; i++)
                {
                    gameData.Inventory.Items[i] = null;
                }
                
                // Добавляем материалы, необходимые для всех рецептов
                var craftingMaterials = new List<(Func<int, Item> factory, int count, string name)>
                {
                    (ItemFactory.CreateWood, 50, "Дерево"),           // Для деревянных мечей и щитов
                    (ItemFactory.CreateStick, 20, "Палка"),           // Для мечей
                    (ItemFactory.CreateIronIngot, 30, "Железный слиток"), // Для железных изделий
                    (ItemFactory.CreateGoldIngot, 15, "Золотой слиток"),  // Для золотых изделий
                    (ItemFactory.CreateLuminite, 10, "Люминит"),      // Для люминитовых изделий
                    (ItemFactory.CreateLuminiteFragment, 10, "Фрагмент люминита"), // Для люминитовых изделий
                    (ItemFactory.CreateHerb, 20, "Трава"),            // Для зелий
                    (ItemFactory.CreateFlask, 10, "Фляга"),           // Для зелий
                    (ItemFactory.CreateIronOre, 20, "Железная руда") // Для выплавки слитков
                };
                
                foreach (var (factory, count, name) in craftingMaterials)
                {
                    var item = factory(count);
                    bool added = gameData.Inventory.AddItem(item);
                    LoggingService.LogInfo($"Тестовый материал добавлен - {name}: {added} (количество: {item.StackSize})");
                }
                
                // Диагностика финального состояния
                var finalItems = gameData.Inventory.Items.Where(item => item != null).ToList();
                LoggingService.LogInfo($"Тестовый инвентарь создан: {finalItems.Count} различных материалов");
                
                foreach (var item in finalItems)
                {
                    LoggingService.LogInfo($"  - {item.Name} x{item.StackSize}");
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Ошибка при создании тестового инвентаря", ex);
            }
        }

        /// <summary>
        /// Очищает лог файл ошибок
        /// </summary>
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