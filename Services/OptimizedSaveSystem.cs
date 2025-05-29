using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using SketchBlade.Models;
using SketchBlade.Services;
using SketchBlade.Utilities;

namespace SketchBlade.Services
{
    /// <summary>
    /// Оптимизированная система сохранений с минимальным размером файла
    /// </summary>
    public class OptimizedSaveSystem
    {
        // Конфигурация файлов
        private static readonly string SAVE_FILE = Path.Combine(ResourcePathManager.SavesPath, "savegame.dat");
        private static readonly string BACKUP_FILE = Path.Combine(ResourcePathManager.SavesPath, "savegame.backup.dat");
        
        /// <summary>
        /// Структура оптимизированного сохранения
        /// </summary>
        public class OptimizedSaveData
        {
            public int Gold { get; set; }
            public int CurrentLocationIndex { get; set; }
            public PlayerData Player { get; set; } = new();
            public List<ItemData> InventoryItems { get; set; } = new();
            public List<ItemData> QuickItems { get; set; } = new();
            public Dictionary<string, ItemData> EquippedItems { get; set; } = new();
            public List<LocationData> Locations { get; set; } = new();
        }
        
        /// <summary>
        /// Минимальные данные игрока
        /// </summary>
        public class PlayerData
        {
            public int MaxHealth { get; set; }
            public int CurrentHealth { get; set; }
            public int Attack { get; set; }
            public int Defense { get; set; }
            public int Level { get; set; }
            public int XP { get; set; }
        }
        
        /// <summary>
        /// Минимальные данные предмета
        /// </summary>
        public class ItemData
        {
            public string Id { get; set; } = "";
            public int Quantity { get; set; } = 1;
        }
        
        /// <summary>
        /// Минимальные данные локации
        /// </summary>
        public class LocationData
        {
            public int Type { get; set; }
            public bool IsCompleted { get; set; }
            public bool IsUnlocked { get; set; }
            public bool IsAvailable { get; set; }
            public bool HeroDefeated { get; set; }
            public PlayerData? Hero { get; set; }
        }
        
        /// <summary>
        /// Сохранить игру в оптимизированном формате
        /// </summary>
        public static bool SaveGame(GameData gameData)
        {
            try
            {
                // Ensure the saves directory exists
                var saveDirectory = Path.GetDirectoryName(SAVE_FILE);
                if (!string.IsNullOrEmpty(saveDirectory) && !Directory.Exists(saveDirectory))
                {
                    Directory.CreateDirectory(saveDirectory);
                }
                
                // Создаем резервную копию
                if (File.Exists(SAVE_FILE))
                {
                    File.Copy(SAVE_FILE, BACKUP_FILE, true);
                }
                
                var saveData = new OptimizedSaveData
                {
                    Gold = gameData.Gold,
                    CurrentLocationIndex = gameData.CurrentLocationIndex,
                    Player = ConvertPlayer(gameData.Player),
                    InventoryItems = ConvertInventoryItems(gameData.Inventory.Items),
                    QuickItems = ConvertQuickItems(gameData.Inventory.QuickItems),
                    EquippedItems = ConvertEquippedItems(gameData.Player?.EquippedItems),
                    Locations = ConvertLocations(gameData.Locations)
                };
                
                var json = JsonSerializer.Serialize(saveData, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                
                File.WriteAllText(SAVE_FILE, json);
                
                LoggingService.LogInfo($"Игра сохранена в оптимизированном формате. Размер файла: {new FileInfo(SAVE_FILE).Length} байт");
                return true;
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Ошибка сохранения игры: {ex.Message}", ex);
                return false;
            }
        }
        
        /// <summary>
        /// Загрузить игру из оптимизированного формата
        /// </summary>
        public static GameData? LoadGame()
        {
            try
            {
                if (!File.Exists(SAVE_FILE))
                {
                    LoggingService.LogInfo("Файл сохранения не найден");
                    return null;
                }
                
                var json = File.ReadAllText(SAVE_FILE);
                var saveData = JsonSerializer.Deserialize<OptimizedSaveData>(json);
                
                if (saveData == null)
                {
                    LoggingService.LogError("Не удалось десериализовать данные сохранения");
                    return TryLoadBackup();
                }
                
                var gameData = new GameData();
                
                // Восстанавливаем основные данные
                gameData.Gold = saveData.Gold;
                gameData.CurrentLocationIndex = saveData.CurrentLocationIndex;
                
                // Восстанавливаем игрока
                gameData.Player = RestorePlayer(saveData.Player);
                
                // Восстанавливаем инвентарь
                gameData.Inventory = new Inventory();
                RestoreInventoryItems(gameData.Inventory, saveData.InventoryItems);
                RestoreQuickItems(gameData.Inventory, saveData.QuickItems);
                
                // Восстанавливаем экипированные предметы
                RestoreEquippedItems(gameData.Player, saveData.EquippedItems);
                
                // Восстанавливаем локации
                RestoreLocations(gameData, saveData.Locations);
                
                LoggingService.LogInfo("Игра успешно загружена из оптимизированного формата");
                return gameData;
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Ошибка загрузки игры: {ex.Message}", ex);
                return TryLoadBackup();
            }
        }
        
        private static GameData? TryLoadBackup()
        {
            try
            {
                if (!File.Exists(BACKUP_FILE))
                    return null;
                    
                var json = File.ReadAllText(BACKUP_FILE);
                var saveData = JsonSerializer.Deserialize<OptimizedSaveData>(json);
                
                if (saveData == null)
                    return null;
                    
                LoggingService.LogInfo("Загружена резервная копия сохранения");
                
                // Используем ту же логику восстановления
                var gameData = new GameData();
                gameData.Gold = saveData.Gold;
                gameData.CurrentLocationIndex = saveData.CurrentLocationIndex;
                gameData.Player = RestorePlayer(saveData.Player);
                gameData.Inventory = new Inventory();
                RestoreInventoryItems(gameData.Inventory, saveData.InventoryItems);
                RestoreQuickItems(gameData.Inventory, saveData.QuickItems);
                RestoreEquippedItems(gameData.Player, saveData.EquippedItems);
                RestoreLocations(gameData, saveData.Locations);
                
                return gameData;
            }
            catch
            {
                return null;
            }
        }
        
        private static PlayerData ConvertPlayer(Character? player)
        {
            if (player == null)
                return new PlayerData();
                
            return new PlayerData
            {
                MaxHealth = player.MaxHealth,
                CurrentHealth = player.CurrentHealth,
                Attack = player.Attack,
                Defense = player.Defense,
                Level = player.Level,
                XP = player.XP
            };
        }
        
        private static Character RestorePlayer(PlayerData playerData)
        {
            var player = new Character
            {
                Name = LocalizationService.Instance.GetTranslation("Characters.PlayerName"), // Локализованное имя
                MaxHealth = playerData.MaxHealth,
                CurrentHealth = playerData.CurrentHealth,
                Attack = playerData.Attack,
                Defense = playerData.Defense,
                Level = playerData.Level,
                XP = playerData.XP,
                IsPlayer = true,
                IsHero = false,
                Type = "Humanoid", // Всегда фиксированный тип
                LocationType = LocationType.Village
            };
            
            player.UpdateSprite();
            return player;
        }
        
        private static List<ItemData> ConvertInventoryItems(IList<Item?> items)
        {
            var result = new List<ItemData>();
            
            foreach (var item in items)
            {
                if (item != null)
                {
                    var itemId = ItemRegistry.GetItemId(item);
                    if (!string.IsNullOrEmpty(itemId))
                    {
                        result.Add(new ItemData 
                        { 
                            Id = itemId, 
                            Quantity = item.StackSize 
                        });
                    }
                }
                else
                {
                    result.Add(null!); // Сохраняем null для пустых слотов
                }
            }
            
            return result;
        }
        
        private static void RestoreInventoryItems(Inventory inventory, List<ItemData> itemsData)
        {
            inventory.Items.Clear();
            
            foreach (var itemData in itemsData)
            {
                if (itemData != null && !string.IsNullOrEmpty(itemData.Id))
                {
                    var item = ItemRegistry.CreateItem(itemData.Id, itemData.Quantity);
                    inventory.Items.Add(item);
                }
                else
                {
                    inventory.Items.Add(null);
                }
            }
        }
        
        private static List<ItemData> ConvertQuickItems(IList<Item?> quickItems)
        {
            var result = new List<ItemData>();
            
            foreach (var item in quickItems)
            {
                if (item != null)
                {
                    var itemId = ItemRegistry.GetItemId(item);
                    if (!string.IsNullOrEmpty(itemId))
                    {
                        result.Add(new ItemData 
                        { 
                            Id = itemId, 
                            Quantity = item.StackSize 
                        });
                    }
                    else
                    {
                        result.Add(null!);
                    }
                }
                else
                {
                    result.Add(null!);
                }
            }
            
            return result;
        }
        
        private static void RestoreQuickItems(Inventory inventory, List<ItemData> quickItemsData)
        {
            inventory.QuickItems.Clear();
            
            foreach (var itemData in quickItemsData)
            {
                if (itemData != null && !string.IsNullOrEmpty(itemData.Id))
                {
                    var item = ItemRegistry.CreateItem(itemData.Id, itemData.Quantity);
                    inventory.QuickItems.Add(item);
                }
                else
                {
                    inventory.QuickItems.Add(null);
                }
            }
        }
        
        private static Dictionary<string, ItemData> ConvertEquippedItems(Dictionary<EquipmentSlot, Item>? equippedItems)
        {
            var result = new Dictionary<string, ItemData>();
            
            if (equippedItems != null)
            {
                foreach (var kvp in equippedItems)
                {
                    if (kvp.Value != null)
                    {
                        var itemId = ItemRegistry.GetItemId(kvp.Value);
                        if (!string.IsNullOrEmpty(itemId))
                        {
                            result[kvp.Key.ToString()] = new ItemData 
                            { 
                                Id = itemId, 
                                Quantity = kvp.Value.StackSize 
                            };
                        }
                    }
                }
            }
            
            return result;
        }
        
        private static void RestoreEquippedItems(Character? player, Dictionary<string, ItemData> equippedItemsData)
        {
            if (player == null) return;
            
            player.EquippedItems.Clear();
            
            foreach (var kvp in equippedItemsData)
            {
                if (Enum.TryParse<EquipmentSlot>(kvp.Key, out var slot) && 
                    !string.IsNullOrEmpty(kvp.Value.Id))
                {
                    var item = ItemRegistry.CreateItem(kvp.Value.Id, kvp.Value.Quantity);
                    if (item != null)
                    {
                        player.EquippedItems[slot] = item;
                    }
                }
            }
        }
        
        private static List<LocationData> ConvertLocations(IList<Location> locations)
        {
            var result = new List<LocationData>();
            
            foreach (var location in locations)
            {
                result.Add(new LocationData
                {
                    Type = (int)location.Type,
                    IsCompleted = location.IsCompleted,
                    IsUnlocked = location.IsUnlocked,
                    IsAvailable = location.IsAvailable,
                    HeroDefeated = location.HeroDefeated,
                    Hero = location.Hero != null ? ConvertPlayer(location.Hero) : null
                });
            }
            
            return result;
        }
        
        private static void RestoreLocations(GameData gameData, List<LocationData> locationsData)
        {
            // Инициализируем локации через GameInitializer
            var gameInitializer = new GameInitializer();
            gameData.Locations.Clear();
            
            // Создаем локации через GameLogicService
            var locations = GameLogicService.Instance.CreateLocations();
            foreach (var location in locations)
            {
                gameData.Locations.Add(location);
            }
            
            // Восстанавливаем только изменяемые данные
            for (int i = 0; i < Math.Min(gameData.Locations.Count, locationsData.Count); i++)
            {
                var location = gameData.Locations[i];
                var locationData = locationsData[i];
                
                location.IsCompleted = locationData.IsCompleted;
                location.IsUnlocked = locationData.IsUnlocked;
                location.IsAvailable = locationData.IsAvailable;
                location.HeroDefeated = locationData.HeroDefeated;
                
                // Восстанавливаем героя локации
                if (locationData.Hero != null && location.Hero != null)
                {
                    location.Hero.MaxHealth = locationData.Hero.MaxHealth;
                    location.Hero.CurrentHealth = locationData.Hero.CurrentHealth;
                    location.Hero.Attack = locationData.Hero.Attack;
                    location.Hero.Defense = locationData.Hero.Defense;
                    location.Hero.Level = locationData.Hero.Level;
                    location.Hero.XP = locationData.Hero.XP;
                }
            }
        }
        
        /// <summary>
        /// Проверить, существует ли файл сохранения
        /// </summary>
        public static bool HasSaveFile()
        {
            return File.Exists(SAVE_FILE);
        }
        
        /// <summary>
        /// Удалить файл сохранения
        /// </summary>
        public static void DeleteSaveFile()
        {
            try
            {
                if (File.Exists(SAVE_FILE))
                    File.Delete(SAVE_FILE);
                if (File.Exists(BACKUP_FILE))
                    File.Delete(BACKUP_FILE);
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Ошибка удаления файла сохранения: {ex.Message}", ex);
            }
        }
    }
} 