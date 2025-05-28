using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SketchBlade.Models;
using SketchBlade.Services;
using SketchBlade.Utilities;

namespace SketchBlade.Services
{
    public interface ICoreGameService
    {
        bool SaveGame(GameData gameData);
        object? LoadGame();
        bool SaveExists();
        bool HasSaveFile();
        void DeleteSave();
        
        void StartAutoSave(Action saveAction);
        void StopAutoSave();
        void SaveNow();
        bool IsAutoSaveEnabled { get; set; }
        TimeSpan AutoSaveInterval { get; set; }
        
        void SaveSettings();
        void LoadSettings();
        
        event EventHandler<GameSaveEventArgs>? GameSaved;
        event EventHandler<GameLoadEventArgs>? GameLoaded;
    }

    public class GameSaveEventArgs : EventArgs
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class GameLoadEventArgs : EventArgs
    {
        public bool Success { get; set; }
        public object? LoadedData { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class CoreGameService : ICoreGameService, IDisposable
    {
        private static readonly Lazy<CoreGameService> _instance = new(() => new CoreGameService());
        public static CoreGameService Instance => _instance.Value;

        private static readonly string SaveFileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Saves", "savegame.dat");
        private static readonly string BackupSaveFileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Saves", "savegame.backup.dat");
        private static readonly string SettingsFileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Saves", "settings.json");

        private Timer? _autoSaveTimer;
        private Action? _saveAction;
        private readonly object _lockObject = new object();
        private bool _isDisposed = false;

        public bool IsAutoSaveEnabled { get; set; } = true;
        public TimeSpan AutoSaveInterval { get; set; } = TimeSpan.FromMinutes(5);

        public event EventHandler<GameSaveEventArgs>? GameSaved;
        public event EventHandler<GameLoadEventArgs>? GameLoaded;

        private CoreGameService()
        {
            // Ensure the saves directory exists
            var saveDirectory = Path.GetDirectoryName(SaveFileName);
            if (!string.IsNullOrEmpty(saveDirectory) && !Directory.Exists(saveDirectory))
            {
                Directory.CreateDirectory(saveDirectory);
            }
            
            LoadSettings();
        }

        #region Game Save Operations

        public bool SaveGame(GameData gameData)
        {
            if (gameData == null)
            {
                LoggingService.LogError("GameData cannot be null");
                GameSaved?.Invoke(this, new GameSaveEventArgs { Success = false, ErrorMessage = "GameData is null" });
                return false;
            }

            try
            {
                CreateBackup();

                var saveData = SerializeGameData(gameData);
                var json = JsonSerializer.Serialize(saveData, new JsonSerializerOptions { WriteIndented = true });

                File.WriteAllText(SaveFileName, json);

                GameSaved?.Invoke(this, new GameSaveEventArgs { Success = true });
                return true;
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error saving game: {ex.Message}", ex);
                GameSaved?.Invoke(this, new GameSaveEventArgs { Success = false, ErrorMessage = ex.Message });
                return false;
            }
        }

        public object? LoadGame()
        {
            try
            {
                if (!File.Exists(SaveFileName))
                {
                    return null;
                }

                var json = File.ReadAllText(SaveFileName);
                var saveData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

                if (saveData == null)
                {
                    LoggingService.LogError("Failed to deserialize save data");
                    return null;
                }

                // Десериализуем данные в объект GameData
                var gameData = DeserializeGameData(saveData);

                GameLoaded?.Invoke(this, new GameLoadEventArgs { Success = true, LoadedData = gameData });
                
                return gameData;
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error loading game: {ex.Message}", ex);
                
                var backup = TryLoadBackup();
                if (backup != null)
                {
                    LoggingService.LogInfo("Loaded from backup");
                    GameLoaded?.Invoke(this, new GameLoadEventArgs { Success = true, LoadedData = backup });
                    return backup;
                }

                GameLoaded?.Invoke(this, new GameLoadEventArgs { Success = false, ErrorMessage = ex.Message });
                return null;
            }
        }

        public bool SaveExists()
        {
            return File.Exists(SaveFileName);
        }

        public bool HasSaveFile()
        {
            return SaveExists();
        }

        public void DeleteSave()
        {
            try
            {
                if (File.Exists(SaveFileName))
                    File.Delete(SaveFileName);
                
                if (File.Exists(BackupSaveFileName))
                    File.Delete(BackupSaveFileName);
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error deleting save files: {ex.Message}", ex);
            }
        }

        #endregion

        #region Auto-Save

        public void StartAutoSave(Action saveAction)
        {
            if (_isDisposed) return;

            _saveAction = saveAction ?? throw new ArgumentNullException(nameof(saveAction));

            lock (_lockObject)
            {
                StopAutoSave();

                if (IsAutoSaveEnabled)
                {
                    _autoSaveTimer = new Timer(PerformAutoSave, null, AutoSaveInterval, AutoSaveInterval);
                }
            }
        }

        public void StopAutoSave()
        {
            lock (_lockObject)
            {
                _autoSaveTimer?.Dispose();
                _autoSaveTimer = null;
            }
        }

        public void SaveNow()
        {
            if (_saveAction != null)
            {
                try
                {
                    _saveAction.Invoke();
                }
                catch (Exception ex)
                {
                    LoggingService.LogError($"Error in manual save: {ex.Message}", ex);
                }
            }
        }

        private void PerformAutoSave(object? state)
        {
            if (_isDisposed || _saveAction == null) return;

            try
            {
                _saveAction.Invoke();
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Auto-save failed: {ex.Message}", ex);
            }
        }

        #endregion

        #region Settings Management

        public void SaveSettings()
        {
            try
            {
                var settings = new Dictionary<string, object>
                {
                    ["IsAutoSaveEnabled"] = IsAutoSaveEnabled,
                    ["AutoSaveIntervalMinutes"] = AutoSaveInterval.TotalMinutes
                };

                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsFileName, json);
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error saving settings: {ex.Message}", ex);
            }
        }

        public void LoadSettings()
        {
            try
            {
                if (!File.Exists(SettingsFileName))
                {
                    return;
                }

                var json = File.ReadAllText(SettingsFileName);
                var settings = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

                if (settings != null)
                {
                    if (settings.TryGetValue("IsAutoSaveEnabled", out var autoSaveElement))
                        IsAutoSaveEnabled = autoSaveElement.GetBoolean();

                    if (settings.TryGetValue("AutoSaveIntervalMinutes", out var intervalElement))
                        AutoSaveInterval = TimeSpan.FromMinutes(intervalElement.GetDouble());
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error loading settings: {ex.Message}", ex);
            }
        }

        #endregion

        #region Helper Methods

        private void CreateBackup()
        {
            try
            {
                if (File.Exists(SaveFileName))
                {
                    File.Copy(SaveFileName, BackupSaveFileName, true);
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Failed to create backup: {ex.Message}", ex);
            }
        }

        private object? TryLoadBackup()
        {
            try
            {
                if (!File.Exists(BackupSaveFileName))
                    return null;

                var json = File.ReadAllText(BackupSaveFileName);
                var saveData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
                
                if (saveData == null)
                {
                    LoggingService.LogError("Failed to deserialize backup save data");
                    return null;
                }

                // Десериализуем данные в объект GameData
                return DeserializeGameData(saveData);
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error loading backup: {ex.Message}", ex);
                return null;
            }
        }

        private Dictionary<string, object> SerializeGameData(GameData gameData)
        {
            var saveData = new Dictionary<string, object>
            {
                ["Gold"] = gameData.Gold,
                ["CurrentLocationIndex"] = gameData.CurrentLocationIndex,
                ["CurrentScreen"] = gameData.CurrentScreen
            };

            if (gameData.Player != null)
            {
                saveData["Player"] = SerializePlayer(gameData.Player);
            }

            if (gameData.Inventory != null)
            {
                saveData["Inventory"] = SerializeInventory(gameData.Inventory);
            }

            if (gameData.Locations != null)
            {
                saveData["Locations"] = SerializeLocations(gameData.Locations);
            }

            return saveData;
        }

        private Dictionary<string, object> SerializePlayer(Character player)
        {
            var playerData = new Dictionary<string, object>
            {
                ["Name"] = player.Name,
                ["MaxHealth"] = player.MaxHealth,
                ["CurrentHealth"] = player.CurrentHealth,
                ["Attack"] = player.Attack,
                ["Defense"] = player.Defense,
                ["Level"] = player.Level,
                ["XP"] = player.XP,
                ["Money"] = player.Money,
                ["IsPlayer"] = player.IsPlayer,
                ["IsHero"] = player.IsHero,
                ["Type"] = player.Type,
                ["LocationType"] = (int)player.LocationType
            };

            if (player.EquippedItems != null)
            {
                var equipped = new Dictionary<string, object>();
                foreach (var kvp in player.EquippedItems)
                {
                    if (kvp.Value != null)
                    {
                        equipped[kvp.Key.ToString()] = SerializeItem(kvp.Value);
                    }
                }
                playerData["EquippedItems"] = equipped;
            }

            return playerData;
        }

        private Dictionary<string, object> SerializeInventory(Inventory inventory)
        {
            var inventoryData = new Dictionary<string, object>
            {
                ["Gold"] = inventory.Gold
            };

            inventoryData["Items"] = SerializeItemCollection(inventory.Items);
            inventoryData["QuickItems"] = SerializeItemCollection(inventory.QuickItems);
            inventoryData["CraftItems"] = SerializeItemCollection(inventory.CraftItems);

            if (inventory.TrashItem != null)
            {
                inventoryData["TrashItem"] = SerializeItem(inventory.TrashItem);
            }

            return inventoryData;
        }

        private List<object?> SerializeItemCollection(IEnumerable<Item?> items)
        {
            var result = new List<object?>();
            foreach (var item in items)
            {
                result.Add(item != null ? SerializeItem(item) : null);
            }
            return result;
        }

        private List<Dictionary<string, object>> SerializeLocations(IEnumerable<Location> locations)
        {
            var result = new List<Dictionary<string, object>>();
            foreach (var location in locations)
            {
                var locationData = new Dictionary<string, object>
                {
                    ["Name"] = location.Name,
                    ["Description"] = location.Description,
                    ["Type"] = (int)location.Type,
                    ["IsCompleted"] = location.IsCompleted,
                    ["IsUnlocked"] = location.IsUnlocked,
                    ["IsAvailable"] = location.IsAvailable,
                    ["HeroDefeated"] = location.HeroDefeated
                };

                if (location.Hero != null)
                {
                    locationData["Hero"] = SerializePlayer(location.Hero);
                }

                result.Add(locationData);
            }
            return result;
        }

        private Dictionary<string, object> SerializeItem(Item item)
        {
            return new Dictionary<string, object>
            {
                ["Name"] = item.Name,
                ["Description"] = item.Description,
                ["Type"] = (int)item.Type,
                ["Rarity"] = (int)item.Rarity,
                ["Value"] = item.Value,
                ["Damage"] = item.Damage,
                ["Defense"] = item.Defense,
                ["StackSize"] = item.StackSize,
                ["MaxStackSize"] = item.MaxStackSize,
                ["EffectPower"] = item.EffectPower,
                ["Material"] = (int)item.Material,
                ["Weight"] = item.Weight
            };
        }

        private GameData DeserializeGameData(Dictionary<string, JsonElement> saveData)
        {
            var gameData = new GameData
            {
                Gold = saveData["Gold"].GetInt32(),
                CurrentLocationIndex = saveData["CurrentLocationIndex"].GetInt32(),
                CurrentScreen = saveData["CurrentScreen"].GetString() ?? "MainMenuView"
            };

            if (saveData.TryGetValue("Player", out var playerElement))
            {
                var playerDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(playerElement.GetRawText());
                if (playerDict != null)
                {
                    gameData.Player = DeserializePlayer(playerDict);
                }
            }

            if (saveData.TryGetValue("Inventory", out var inventoryElement))
            {
                var inventoryDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(inventoryElement.GetRawText());
                if (inventoryDict != null)
                {
                    gameData.Inventory = DeserializeInventory(inventoryDict);
                }
            }

            if (saveData.TryGetValue("Locations", out var locationsElement))
            {
                var locationsList = JsonSerializer.Deserialize<List<JsonElement>>(locationsElement.GetRawText());
                if (locationsList != null)
                {
                    gameData.Locations = new ObservableCollection<Location>(DeserializeLocations(locationsList));
                }
            }

            return gameData;
        }

        private Character DeserializePlayer(Dictionary<string, JsonElement> playerData)
        {
            var player = new Character
            {
                Name = playerData["Name"].GetString() ?? "Player",
                MaxHealth = playerData["MaxHealth"].GetInt32(),
                CurrentHealth = playerData["CurrentHealth"].GetInt32(),
                Attack = playerData["Attack"].GetInt32(),
                Defense = playerData["Defense"].GetInt32(),
                Level = playerData["Level"].GetInt32(),
                XP = playerData["XP"].GetInt32(),
                Money = playerData["Money"].GetInt32(),
                IsPlayer = playerData["IsPlayer"].GetBoolean(),
                IsHero = playerData["IsHero"].GetBoolean(),
                Type = playerData["Type"].GetString() ?? "Humanoid",
                LocationType = (LocationType)playerData["LocationType"].GetInt32()
            };

            if (playerData.TryGetValue("EquippedItems", out var equippedItemsElement))
            {
                var equippedDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(equippedItemsElement.GetRawText());
                if (equippedDict != null)
                {
                    player.EquippedItems = DeserializeEquippedItems(equippedDict);
                }
            }

            // Автоматически определяем путь к изображению на основе типа персонажа
            player.UpdateSprite();

            return player;
        }

        private Dictionary<EquipmentSlot, Item> DeserializeEquippedItems(Dictionary<string, JsonElement> equippedData)
        {
            var equipped = new Dictionary<EquipmentSlot, Item>();
            foreach (var kvp in equippedData)
            {
                if (Enum.TryParse<EquipmentSlot>(kvp.Key, out var slot))
                {
                    var itemDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(kvp.Value.GetRawText());
                    if (itemDict != null)
                    {
                        var item = DeserializeItem(itemDict);
                        if (item != null)
                        {
                            equipped[slot] = item;
                        }
                    }
                }
            }
            return equipped;
        }

        private Inventory DeserializeInventory(Dictionary<string, JsonElement> inventoryData)
        {
            var inventory = new Inventory
            {
                Gold = inventoryData["Gold"].GetInt32()
            };

            if (inventoryData.TryGetValue("Items", out var itemsElement))
            {
                var itemsList = JsonSerializer.Deserialize<List<JsonElement>>(itemsElement.GetRawText());
                if (itemsList != null)
                {
                    var items = DeserializeItemList(itemsList);
                    // Очищаем и заполняем коллекцию
                    inventory.Items.Clear();
                    foreach (var item in items)
                    {
                        inventory.Items.Add(item);
                    }
                }
            }

            if (inventoryData.TryGetValue("QuickItems", out var quickItemsElement))
            {
                var quickItemsList = JsonSerializer.Deserialize<List<JsonElement>>(quickItemsElement.GetRawText());
                if (quickItemsList != null)
                {
                    var quickItems = DeserializeItemList(quickItemsList);
                    // Очищаем и заполняем коллекцию
                    inventory.QuickItems.Clear();
                    foreach (var item in quickItems)
                    {
                        inventory.QuickItems.Add(item);
                    }
                }
            }

            if (inventoryData.TryGetValue("CraftItems", out var craftItemsElement))
            {
                var craftItemsList = JsonSerializer.Deserialize<List<JsonElement>>(craftItemsElement.GetRawText());
                if (craftItemsList != null)
                {
                    var craftItems = DeserializeItemList(craftItemsList);
                    // Очищаем и заполняем коллекцию
                    inventory.CraftItems.Clear();
                    foreach (var item in craftItems)
                    {
                        inventory.CraftItems.Add(item);
                    }
                }
            }

            if (inventoryData.TryGetValue("TrashItem", out var trashItemElement))
            {
                var trashItemDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(trashItemElement.GetRawText());
                if (trashItemDict != null)
                {
                    inventory.TrashItem = DeserializeItem(trashItemDict);
                }
            }

            return inventory;
        }

        private List<Item?> DeserializeItemList(List<JsonElement> itemElements)
        {
            var items = new List<Item?>();
            foreach (var itemElement in itemElements)
            {
                if (itemElement.ValueKind == JsonValueKind.Null)
                {
                    items.Add(null);
                }
                else
                {
                    var itemDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(itemElement.GetRawText());
                    if (itemDict != null)
                    {
                        items.Add(DeserializeItem(itemDict));
                    }
                    else
                    {
                        items.Add(null);
                    }
                }
            }
            return items;
        }

        private List<Location> DeserializeLocations(IEnumerable<JsonElement> locationElements)
        {
            var locations = new List<Location>();
            foreach (var locationElement in locationElements)
            {
                var locationDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(locationElement.GetRawText());
                if (locationDict != null)
                {
                    locations.Add(DeserializeLocation(locationDict));
                }
            }
            return locations;
        }

        private Item? DeserializeItem(Dictionary<string, JsonElement> itemData)
        {
            if (itemData == null) return null;

            var item = new Item
            {
                Name = itemData["Name"].GetString() ?? "",
                Description = itemData["Description"].GetString() ?? "",
                Type = (ItemType)itemData["Type"].GetInt32(),
                Rarity = (ItemRarity)itemData["Rarity"].GetInt32(),
                Value = itemData["Value"].GetInt32(),
                Damage = itemData["Damage"].GetInt32(),
                Defense = itemData["Defense"].GetInt32(),
                StackSize = itemData["StackSize"].GetInt32(),
                MaxStackSize = itemData["MaxStackSize"].GetInt32(),
                EffectPower = itemData["EffectPower"].GetInt32(),
                Material = (ItemMaterial)itemData["Material"].GetInt32(),
                Weight = itemData["Weight"].GetSingle()
            };

            // Автоматически определяем путь к спрайту на основе типа и материала
            item.UpdateSpritePath();

            return item;
        }

        private Location DeserializeLocation(Dictionary<string, JsonElement> locationData)
        {
            var location = new Location
            {
                Name = locationData["Name"].GetString() ?? "",
                Description = locationData["Description"].GetString() ?? "",
                Type = (LocationType)locationData["Type"].GetInt32(),
                IsCompleted = locationData["IsCompleted"].GetBoolean(),
                IsUnlocked = locationData["IsUnlocked"].GetBoolean(),
                IsAvailable = locationData["IsAvailable"].GetBoolean(),
                HeroDefeated = locationData["HeroDefeated"].GetBoolean()
            };

            if (locationData.TryGetValue("Hero", out var heroElement))
            {
                var heroDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(heroElement.GetRawText());
                if (heroDict != null)
                {
                    location.Hero = DeserializePlayer(heroDict);
                }
            }

            // Автоматически определяем путь к спрайту на основе типа локации
            location.UpdateSpritePath();

            return location;
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_isDisposed) return;

            StopAutoSave();
            SaveSettings();
            _isDisposed = true;
        }

        #endregion
    }
} 