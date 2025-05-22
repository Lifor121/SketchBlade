using System;
using System.IO;
using System.Text.Json;
using System.Linq;
using SketchBlade.Models;
using System.Collections.ObjectModel;
using System.Collections.Generic;

namespace SketchBlade.Services
{
    public class GameSaveService : IFileSaveService
    {
        private static readonly string SaveFileName = "savegame.dat";
        private static readonly string BackupSaveFileName = "savegame.backup.dat";
        private readonly GameState _gameState;
        private System.Threading.Timer _autoSaveTimer;
        private bool _savePending = false;
        
        public bool AutoSaveEnabled { get; set; } = true;
        public TimeSpan AutoSaveInterval { get; set; } = TimeSpan.FromMinutes(5);
        
        // Определяет, нужно ли автоматически сохранять после значимых действий
        public bool SaveAfterSignificantActions { get; set; } = true;
        
        public GameSaveService()
        {
            // Initialize auto-save timer
            _autoSaveTimer = new System.Threading.Timer(
                _ => TriggerAutoSave(),
                null,
                AutoSaveInterval,
                AutoSaveInterval);
        }
        
        public GameSaveService(GameState gameState) : this()
        {
            _gameState = gameState;
        }
        
        public void SaveGame(GameState gameState)
        {
            try
            {
                // Create backup of previous save if it exists
                if (SaveExists())
                {
                    try
                    {
                        File.Copy(SaveFileName, BackupSaveFileName, true);
                    }
                    catch (Exception ex)
                    {
                        // Log backup errors
                        Console.WriteLine($"Failed to create backup: {ex.Message}");
                    }
                }
                
                // Create a minimal serializable version of the game state with only necessary data
                var saveData = new Dictionary<string, object>();
                
                // Save basic scalar properties
                saveData["Gold"] = gameState.Gold;
                saveData["CurrentLocationIndex"] = gameState.CurrentLocationIndex;
                saveData["CurrentScreen"] = gameState.CurrentScreen;
                
                // Save player character (only necessary data)
                if (gameState.Player != null)
                {
                    var playerData = new Dictionary<string, object>
                    {
                        ["Name"] = gameState.Player.Name,
                        ["MaxHealth"] = gameState.Player.MaxHealth,
                        ["CurrentHealth"] = gameState.Player.CurrentHealth,
                        ["Attack"] = gameState.Player.Attack,
                        ["Defense"] = gameState.Player.Defense,
                        ["Level"] = gameState.Player.Level, 
                        ["XP"] = gameState.Player.XP,
                        ["XPToNextLevel"] = gameState.Player.XPToNextLevel,
                        ["Money"] = gameState.Player.Money,
                        ["ImagePath"] = gameState.Player.ImagePath ?? "Assets/Images/player.png"
                    };
                    
                    // Save equipped items if they exist
                    if (gameState.Player.EquippedItems != null)
                    {
                        var equippedItems = new Dictionary<string, Dictionary<string, object>>();
                        foreach (var slot in gameState.Player.EquippedItems.Keys)
                        {
                            var item = gameState.Player.EquippedItems[slot];
                            if (item != null)
                            {
                                equippedItems[slot.ToString()] = SerializeItem(item);
                            }
                        }
                        playerData["EquippedItems"] = equippedItems;
                    }
                    
                    saveData["Player"] = playerData;
                }
                
                // Save inventory items
                if (gameState.Inventory?.Items != null)
                {
                    // Save main inventory slots
                    var inventoryItems = new List<object?>();
                    foreach (var item in gameState.Inventory.Items)
                    {
                        if (item != null)
                        {
                            inventoryItems.Add(SerializeItem(item));
                        }
                        else
                        {
                            inventoryItems.Add(null);
                        }
                    }
                    saveData["InventoryItems"] = inventoryItems;
                    
                    // Save quick slots
                    if (gameState.Inventory.QuickItems != null)
                    {
                        var quickItems = new List<object?>();
                        foreach (var item in gameState.Inventory.QuickItems)
                        {
                            if (item != null)
                            {
                                quickItems.Add(SerializeItem(item));
                            }
                            else
                            {
                                quickItems.Add(null);
                            }
                        }
                        saveData["QuickItems"] = quickItems;
                    }
                    
                    // Save crafting slots
                    if (gameState.Inventory.CraftItems != null)
                    {
                        var craftItems = new List<object?>();
                        foreach (var item in gameState.Inventory.CraftItems)
                        {
                            if (item != null)
                            {
                                craftItems.Add(SerializeItem(item));
                            }
                            else
                            {
                                craftItems.Add(null);
                            }
                        }
                        saveData["CraftItems"] = craftItems;
                    }
                    
                    // Save trash slot if it exists
                    if (gameState.Inventory.TrashItem != null)
                    {
                        saveData["TrashItem"] = SerializeItem(gameState.Inventory.TrashItem);
                    }
                }
                
                // Save locations
                var locationsData = new List<Dictionary<string, object>>();
                if (gameState.Locations != null)
                {
                    foreach (var location in gameState.Locations)
                    {
                        if (location != null)
                        {
                            var locationData = new Dictionary<string, object>
                            {
                                ["Name"] = location.Name,
                                ["Description"] = location.Description,
                                ["Type"] = (int)location.Type,
                                ["IsCompleted"] = location.IsCompleted,
                                ["IsUnlocked"] = location.IsUnlocked,
                                ["IsAvailable"] = location.IsAvailable,
                                ["Difficulty"] = (int)location.Difficulty,
                                ["MinPlayerLevel"] = location.MinPlayerLevel,
                                ["MaxCompletions"] = location.MaxCompletions,
                                ["HeroDefeated"] = location.HeroDefeated,
                                ["CompletionCount"] = location.CompletionCount,
                                ["SpritePath"] = location.SpritePath
                            };
                            
                            // Save hero data if it exists
                            if (location.Hero != null)
                            {
                                locationData["Hero"] = new Dictionary<string, object>
                                {
                                    ["Name"] = location.Hero.Name,
                                    ["MaxHealth"] = location.Hero.MaxHealth,
                                    ["CurrentHealth"] = location.Hero.CurrentHealth,
                                    ["Attack"] = location.Hero.Attack,
                                    ["Defense"] = location.Hero.Defense,
                                    ["IsHero"] = location.Hero.IsHero,
                                    ["Type"] = location.Hero.Type ?? "Unknown",
                                    ["ImagePath"] = location.Hero.ImagePath ?? "Assets/Images/enemy.png"
                                };
                            }
                            
                            // Add lists of requirements
                            if (location.RequiredCompletedLocations != null && location.RequiredCompletedLocations.Count > 0)
                            {
                                locationData["RequiredCompletedLocations"] = location.RequiredCompletedLocations;
                            }
                            
                            if (location.RequiredItems != null && location.RequiredItems.Count > 0)
                            {
                                locationData["RequiredItems"] = location.RequiredItems;
                            }
                            
                            locationsData.Add(locationData);
                        }
                    }
                    saveData["Locations"] = locationsData;
                }
                
                // Save settings
                if (gameState.Settings != null)
                {
                    var settingsData = new Dictionary<string, object>
                    {
                        ["IsMusicEnabled"] = gameState.Settings.IsMusicEnabled,
                        ["AreSoundEffectsEnabled"] = gameState.Settings.AreSoundEffectsEnabled,
                        ["MusicVolume"] = gameState.Settings.MusicVolume,
                        ["SoundEffectsVolume"] = gameState.Settings.SoundEffectsVolume,
                        ["Difficulty"] = (int)gameState.Settings.Difficulty,
                        ["Language"] = gameState.Settings.Language,
                        ["UIScale"] = gameState.Settings.UIScale,
                        ["ShowDamageNumbers"] = gameState.Settings.ShowCombatDamageNumbers
                    };
                    saveData["Settings"] = settingsData;
                }
                
                // Serialize minimal version of data
                var options = new JsonSerializerOptions { WriteIndented = true };
                var jsonString = JsonSerializer.Serialize(saveData, options);
                
                // Write to file
                File.WriteAllText(SaveFileName, jsonString);
                
                // Update auto-save timer
                ResetAutoSaveTimer();
                
                Console.WriteLine("Game saved successfully.");
            }
            catch (Exception ex)
            {
                // Log error
                Console.WriteLine($"Error saving game: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
        
        // Helper method to serialize an item
        private Dictionary<string, object> SerializeItem(Item item)
        {
            if (item == null) return null;
            
            var itemData = new Dictionary<string, object>
            {
                ["Name"] = item.Name,
                ["Description"] = item.Description,
                ["Type"] = (int)item.Type,
                ["Rarity"] = (int)item.Rarity,
                ["Value"] = item.Value,
                ["Damage"] = item.Damage,
                ["Defense"] = item.Defense,
                ["SpritePath"] = item.SpritePath ?? "",
                ["IsStackable"] = item.IsStackable,
                ["StackSize"] = item.StackSize,
                ["MaxStackSize"] = item.MaxStackSize,
                ["EquipSlot"] = (int)item.EquipSlot,
                ["EffectPower"] = item.EffectPower,
                ["Material"] = (int)item.Material,
                ["Weight"] = item.Weight
            };
            
            // Add stat bonuses if they exist
            if (item.StatBonuses != null && item.StatBonuses.Count > 0)
            {
                var statBonuses = new Dictionary<string, int>();
                foreach (var stat in item.StatBonuses.Keys)
                {
                    statBonuses[stat] = item.StatBonuses[stat];
                }
                itemData["StatBonuses"] = statBonuses;
            }
            
            return itemData;
        }
        
        public object LoadGame()
        {
            if (!SaveExists())
                return null;
            
            try
            {
                // Read .dat file
                string jsonString = File.ReadAllText(SaveFileName);
                
                // Десериализуем как Dictionary
                var saveData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonString);
                if (saveData == null)
                {
                    Console.WriteLine("Failed to deserialize save data: empty or invalid JSON");
                    return null;
                }
                
                Console.WriteLine("Game data loaded successfully from: " + SaveFileName);
                return saveData;
            }
            catch (JsonException jsonEx)
            {
                // Log specific JSON error
                Console.WriteLine($"JSON error loading save: {jsonEx.Message}");
                Console.WriteLine($"Path: {jsonEx.Path}, LineNumber: {jsonEx.LineNumber}");
                Console.WriteLine($"Stack trace: {jsonEx.StackTrace}");
                
                // Try loading backup
                return TryLoadBackup();
            }
            catch (NotSupportedException nsEx)
            {
                // Specific handling for non-supported types
                Console.WriteLine($"NotSupportedException during load: {nsEx.Message}");
                Console.WriteLine($"This is likely due to a non-serializable type");
                Console.WriteLine($"Stack trace: {nsEx.StackTrace}");
                
                // Try loading backup
                return TryLoadBackup();
            }
            catch (Exception ex)
            {
                // Log general error
                Console.WriteLine($"Error loading save: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                
                // Try loading backup
                return TryLoadBackup();
            }
        }
        
        private object TryLoadBackup()
        {
            try
            {
                if (File.Exists(BackupSaveFileName))
                {
                    Console.WriteLine("Attempting to load from backup file: " + BackupSaveFileName);
                    string backupJson = File.ReadAllText(BackupSaveFileName);
                    
                    var saveData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(backupJson);
                    Console.WriteLine("Game loaded successfully from backup");
                    return saveData;
                }
            }
            catch (Exception backupEx)
            {
                // Log backup load error
                Console.WriteLine($"Error loading backup save: {backupEx.Message}");
                Console.WriteLine($"Stack trace: {backupEx.StackTrace}");
            }
            
            Console.WriteLine("Failed to load game from both main and backup save files");
            return null;
        }
        
        public void ApplySaveData(GameState gameState, object saveData)
        {
            if (saveData == null || !(saveData is Dictionary<string, JsonElement> data))
            {
                Console.WriteLine("Invalid save data format, can't load game");
                return;
            }
            
            try
            {
                Console.WriteLine("Applying save data to game state...");
                
                // Restore basic properties
                if (data.TryGetValue("Gold", out JsonElement goldElement))
                {
                    gameState.Gold = goldElement.GetInt32();
                }
                
                if (data.TryGetValue("CurrentLocationIndex", out JsonElement locationIndexElement))
                {
                    gameState.CurrentLocationIndex = locationIndexElement.GetInt32();
                }
                
                if (data.TryGetValue("CurrentScreen", out JsonElement currentScreenElement) && 
                    currentScreenElement.ValueKind == JsonValueKind.String)
                {
                    // Don't restore battle screen on load
                    string screenName = currentScreenElement.GetString();
                    if (screenName != "BattleView")
                    {
                        gameState.CurrentScreen = screenName;
                    }
                    else
                    {
                        gameState.CurrentScreen = "WorldMapView";
                    }
                }
                
                // Restore player character
                if (data.TryGetValue("Player", out JsonElement playerElement) && 
                    playerElement.ValueKind == JsonValueKind.Object)
                {
                    // Create player if null
                    if (gameState.Player == null)
                    {
                        gameState.Player = new Character();
                    }
                    
                    var playerData = playerElement.EnumerateObject();
                    foreach (var property in playerData)
                    {
                        switch (property.Name)
                        {
                            case "Name":
                                gameState.Player.Name = property.Value.GetString() ?? "Hero";
                                break;
                            case "MaxHealth":
                                gameState.Player.MaxHealth = property.Value.GetInt32();
                                break;
                            case "CurrentHealth":
                                gameState.Player.CurrentHealth = property.Value.GetInt32();
                                break;
                            case "Attack":
                                gameState.Player.Attack = property.Value.GetInt32();
                                break;
                            case "Defense":
                                gameState.Player.Defense = property.Value.GetInt32();
                                break;
                            case "Level":
                                gameState.Player.Level = property.Value.GetInt32();
                                break;
                            case "XP":
                                gameState.Player.XP = property.Value.GetInt32();
                                break;
                            case "XPToNextLevel":
                                gameState.Player.XPToNextLevel = property.Value.GetInt32();
                                break;
                            case "Money":
                                gameState.Player.Money = property.Value.GetInt32();
                                break;
                            case "ImagePath":
                                gameState.Player.ImagePath = property.Value.GetString() ?? "Assets/Images/player.png";
                                break;
                            case "EquippedItems":
                                if (property.Value.ValueKind == JsonValueKind.Object)
                                {
                                    // Restore equipment
                                    var equippedItems = property.Value.EnumerateObject();
                                    
                                    // Ensure the equipment dictionary is initialized
                                    if (gameState.Player.EquippedItems == null)
                                    {
                                        gameState.Player.EquippedItems = new Dictionary<EquipmentSlot, Item>();
                                    }
                                    else
                                    {
                                        // Clear existing equipped items to avoid duplicates
                                        gameState.Player.EquippedItems.Clear();
                                    }
                                    
                                    // Log equipment restoration
                                    Console.WriteLine("Restoring equipped items:");
                                    
                                    foreach (var equipmentSlotProperty in equippedItems)
                                    {
                                        try 
                                        {
                                            // Convert string slot name back to EquipmentSlot enum
                                            if (Enum.TryParse<EquipmentSlot>(equipmentSlotProperty.Name, out var slot) && 
                                                equipmentSlotProperty.Value.ValueKind == JsonValueKind.Object)
                                            {
                                                // Create new item from data
                                                var itemData = equipmentSlotProperty.Value;
                                                var item = CreateItemFromJson(itemData);
                                                
                                                if (item != null)
                                                {
                                                    // Add item to player's equipment
                                                    gameState.Player.EquippedItems[slot] = item;
                                                    Console.WriteLine($"  - Equipped {item.Name} to slot {slot}");
                                                }
                                                else
                                                {
                                                    Console.WriteLine($"  - Failed to create item for slot {slot}");
                                                }
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            Console.WriteLine($"  - Error equipping item: {ex.Message}");
                                        }
                                    }
                                    
                                    // Force recalculate player stats after equipment restoration
                                    gameState.Player.CalculateStats();
                                    Console.WriteLine($"Restored equipment complete. Current attack: {gameState.Player.TotalAttack}, defense: {gameState.Player.TotalDefense}");
                                }
                                break;
                        }
                    }
                }
                
                // Restore inventory
                // First make sure inventory is initialized
                if (gameState.Inventory == null)
                {
                    gameState.Inventory = new Inventory(15);
                }
                else
                {
                    // Clear existing inventory to avoid duplicates
                    gameState.Inventory.Clear();
                }
                
                // Restore main inventory items
                if (data.TryGetValue("InventoryItems", out JsonElement inventoryItemsElement) && 
                    inventoryItemsElement.ValueKind == JsonValueKind.Array)
                {
                    var items = inventoryItemsElement.EnumerateArray().ToList();
                    for (int i = 0; i < items.Count && i < gameState.Inventory.Items.Count; i++)
                    {
                        if (items[i].ValueKind != JsonValueKind.Null && items[i].ValueKind != JsonValueKind.Undefined)
                        {
                            var item = CreateItemFromJson(items[i]);
                            if (item != null)
                            {
                                gameState.Inventory.Items[i] = item;
                                Console.WriteLine($"Restored item {item.Name} to inventory slot {i}");
                            }
                        }
                    }
                }
                else if (data.TryGetValue("Inventory", out JsonElement oldInventoryElement) && 
                         oldInventoryElement.ValueKind == JsonValueKind.Array)
                {
                    // Handle old save format for backward compatibility
                    int slotIndex = 0;
                    foreach (var itemElement in oldInventoryElement.EnumerateArray())
                    {
                        if (itemElement.ValueKind == JsonValueKind.Object && slotIndex < gameState.Inventory.Items.Count)
                        {
                            var item = CreateItemFromJson(itemElement);
                            if (item != null)
                            {
                                gameState.Inventory.Items[slotIndex++] = item;
                                Console.WriteLine($"Restored item {item.Name} from old format");
                            }
                        }
                    }
                }
                
                // Restore quick slots
                if (data.TryGetValue("QuickItems", out JsonElement quickItemsElement) && 
                    quickItemsElement.ValueKind == JsonValueKind.Array)
                {
                    var items = quickItemsElement.EnumerateArray().ToList();
                    for (int i = 0; i < items.Count && i < gameState.Inventory.QuickItems.Count; i++)
                    {
                        if (items[i].ValueKind != JsonValueKind.Null && items[i].ValueKind != JsonValueKind.Undefined)
                        {
                            var item = CreateItemFromJson(items[i]);
                            if (item != null)
                            {
                                gameState.Inventory.QuickItems[i] = item;
                                Console.WriteLine($"Restored item {item.Name} to quick slot {i}");
                            }
                        }
                    }
                }
                
                // Restore craft slots
                if (data.TryGetValue("CraftItems", out JsonElement craftItemsElement) && 
                    craftItemsElement.ValueKind == JsonValueKind.Array)
                {
                    var items = craftItemsElement.EnumerateArray().ToList();
                    for (int i = 0; i < items.Count && i < gameState.Inventory.CraftItems.Count; i++)
                    {
                        if (items[i].ValueKind != JsonValueKind.Null && items[i].ValueKind != JsonValueKind.Undefined)
                        {
                            var item = CreateItemFromJson(items[i]);
                            if (item != null)
                            {
                                gameState.Inventory.CraftItems[i] = item;
                                Console.WriteLine($"Restored item {item.Name} to craft slot {i}");
                            }
                        }
                    }
                }
                
                // Restore trash item
                if (data.TryGetValue("TrashItem", out JsonElement trashItemElement) && 
                    trashItemElement.ValueKind == JsonValueKind.Object)
                {
                    var trashItem = CreateItemFromJson(trashItemElement);
                    if (trashItem != null)
                    {
                        gameState.Inventory.TrashItem = trashItem;
                        Console.WriteLine($"Restored {trashItem.Name} to trash slot");
                    }
                }
                
                // Restore locations
                if (data.TryGetValue("Locations", out JsonElement locationsElement) && 
                    locationsElement.ValueKind == JsonValueKind.Array && 
                    gameState.Locations != null)
                {
                    foreach (var locationElement in locationsElement.EnumerateArray())
                    {
                        if (locationElement.TryGetProperty("Name", out JsonElement nameElement))
                        {
                            string locationName = nameElement.GetString();
                            var existingLocation = gameState.Locations.FirstOrDefault(l => l.Name == locationName);
                            
                            if (existingLocation != null)
                            {
                                // Update existing location
                                if (locationElement.TryGetProperty("IsCompleted", out JsonElement completedElement))
                                    existingLocation.IsCompleted = completedElement.GetBoolean();
                                
                                if (locationElement.TryGetProperty("IsUnlocked", out JsonElement unlockedElement))
                                    existingLocation.IsUnlocked = unlockedElement.GetBoolean();
                                
                                if (locationElement.TryGetProperty("IsAvailable", out JsonElement availableElement))
                                    existingLocation.IsAvailable = availableElement.GetBoolean();
                                
                                if (locationElement.TryGetProperty("CompletionCount", out JsonElement countElement))
                                    existingLocation.CompletionCount = countElement.GetInt32();
                                
                                if (locationElement.TryGetProperty("HeroDefeated", out JsonElement heroDefeatedElement))
                                    existingLocation.HeroDefeated = heroDefeatedElement.GetBoolean();
                                
                                if (locationElement.TryGetProperty("SpritePath", out JsonElement spritePathElement))
                                    existingLocation.SpritePath = spritePathElement.GetString() ?? string.Empty;
                                
                                // Restore hero state if needed
                                if (locationElement.TryGetProperty("Hero", out JsonElement heroElement) &&
                                    heroElement.ValueKind == JsonValueKind.Object &&
                                    existingLocation.Hero != null)
                                {
                                    if (heroElement.TryGetProperty("CurrentHealth", out JsonElement healthElement))
                                        existingLocation.Hero.CurrentHealth = healthElement.GetInt32();
                                        
                                    if (heroElement.TryGetProperty("MaxHealth", out JsonElement maxHealthElement))
                                        existingLocation.Hero.MaxHealth = maxHealthElement.GetInt32();
                                        
                                    if (heroElement.TryGetProperty("ImagePath", out JsonElement imagePathElement))
                                        existingLocation.Hero.ImagePath = imagePathElement.GetString() ?? "Assets/Images/enemy.png";
                                }
                                
                                Console.WriteLine($"Updated location {locationName} with saved state");
                            }
                        }
                    }
                }
                
                // Restore settings if they exist
                if (data.TryGetValue("Settings", out JsonElement settingsElement) && 
                    settingsElement.ValueKind == JsonValueKind.Object)
                {
                    if (settingsElement.TryGetProperty("IsMusicEnabled", out JsonElement musicEnabledElement))
                        gameState.Settings.IsMusicEnabled = musicEnabledElement.GetBoolean();
                        
                    if (settingsElement.TryGetProperty("AreSoundEffectsEnabled", out JsonElement soundEnabledElement))
                        gameState.Settings.AreSoundEffectsEnabled = soundEnabledElement.GetBoolean();
                        
                    if (settingsElement.TryGetProperty("MusicVolume", out JsonElement musicVolumeElement))
                        gameState.Settings.MusicVolume = musicVolumeElement.GetDouble();
                        
                    if (settingsElement.TryGetProperty("SoundEffectsVolume", out JsonElement soundVolumeElement))
                        gameState.Settings.SoundEffectsVolume = soundVolumeElement.GetDouble();
                        
                    if (settingsElement.TryGetProperty("Difficulty", out JsonElement difficultyElement))
                        gameState.Settings.Difficulty = (Difficulty)difficultyElement.GetInt32();
                        
                    if (settingsElement.TryGetProperty("Language", out JsonElement languageElement))
                    {
                        if (languageElement.ValueKind == JsonValueKind.String)
                        {
                            string langStr = languageElement.GetString() ?? "Russian";
                            if (Enum.TryParse<Language>(langStr, out var lang))
                            {
                                gameState.Settings.Language = lang;
                            }
                        }
                        else if (languageElement.ValueKind == JsonValueKind.Number)
                        {
                            gameState.Settings.Language = (Language)languageElement.GetInt32();
                        }
                    }
                        
                    if (settingsElement.TryGetProperty("UIScale", out JsonElement uiScaleElement))
                        gameState.Settings.UIScale = uiScaleElement.GetDouble();
                        
                    if (settingsElement.TryGetProperty("ShowDamageNumbers", out JsonElement showDamageElement))
                        gameState.Settings.ShowCombatDamageNumbers = showDamageElement.GetBoolean();
                }
                
                // Restore current location by index
                if (gameState.Locations != null && 
                    gameState.CurrentLocationIndex >= 0 && 
                    gameState.CurrentLocationIndex < gameState.Locations.Count)
                {
                    gameState.CurrentLocation = gameState.Locations[gameState.CurrentLocationIndex];
                    Console.WriteLine($"Set current location to {gameState.CurrentLocation.Name}");
                }
                else if (gameState.Locations != null && gameState.Locations.Count > 0)
                {
                    // Default to first location if index is invalid
                    gameState.CurrentLocation = gameState.Locations[0];
                    gameState.CurrentLocationIndex = 0;
                    Console.WriteLine("Invalid location index, reset to first location");
                }
                
                gameState.HasSaveGame = true;
                Console.WriteLine("Game state restored successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error applying save data: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                
                // Initialize with defaults if loading failed
                gameState.Initialize();
                Console.WriteLine("Initialized with defaults due to loading error");
            }
        }
        
        public bool SaveExists()
        {
            return File.Exists(SaveFileName);
        }
        
        public void TriggerAutoSave()
        {
            if (AutoSaveEnabled && _gameState != null && !_savePending)
            {
                _savePending = true;
                try
                {
                    SaveGame(_gameState);
                }
                finally
                {
                    _savePending = false;
                }
            }
        }
        
        // Метод для сохранения после значительных действий
        public void SaveAfterAction(GameActionType actionType)
        {
            if (!SaveAfterSignificantActions || _gameState == null)
                return;
                
            // Сохраняем игру после следующих значимых действий
            switch (actionType)
            {
                case GameActionType.BattleCompleted:
                case GameActionType.LocationChanged:
                case GameActionType.BossDefeated:
                case GameActionType.ItemCrafted:
                case GameActionType.EquipmentChanged:
                    Console.WriteLine($"Auto-saving after: {actionType}");
                    TriggerAutoSave();
                    break;
            }
        }
        
        private void ResetAutoSaveTimer()
        {
            _autoSaveTimer.Change(AutoSaveInterval, AutoSaveInterval);
        }
        
        // Helper method to create an item from JsonElement
        private Item? CreateItemFromJson(JsonElement itemData)
        {
            try
            {
                var item = new Item();
                
                foreach (var property in itemData.EnumerateObject())
                {
                    switch (property.Name)
                    {
                        case "Name":
                            item.Name = property.Value.GetString() ?? string.Empty;
                            break;
                        case "Description":
                            item.Description = property.Value.GetString() ?? string.Empty;
                            break;
                        case "Type":
                            if (property.Value.TryGetInt32(out int typeValue))
                            {
                                item.Type = (ItemType)typeValue;
                            }
                            break;
                        case "Rarity":
                            if (property.Value.TryGetInt32(out int rarityValue))
                            {
                                item.Rarity = (ItemRarity)rarityValue;
                            }
                            break;
                        case "Value":
                            if (property.Value.TryGetInt32(out int value))
                            {
                                item.Value = value;
                            }
                            break;
                        case "Damage":
                            if (property.Value.TryGetInt32(out int damage))
                            {
                                item.Damage = damage;
                            }
                            break;
                        case "Defense":
                            if (property.Value.TryGetInt32(out int defense))
                            {
                                item.Defense = defense;
                            }
                            break;
                        case "SpritePath":
                            item.SpritePath = property.Value.GetString() ?? string.Empty;
                            break;
                        case "StackSize":
                            if (property.Value.TryGetInt32(out int stackSize))
                            {
                                item.StackSize = stackSize;
                            }
                            break;
                        case "MaxStackSize":
                            if (property.Value.TryGetInt32(out int maxStackSize))
                            {
                                item.MaxStackSize = maxStackSize;
                            }
                            break;
                        case "EquipSlot":
                            // Skip setting EquipSlot since it's a read-only property
                            break;
                        case "EffectPower":
                            if (property.Value.TryGetInt32(out int effectPower))
                            {
                                item.EffectPower = effectPower;
                            }
                            break;
                        case "Material":
                            if (property.Value.TryGetInt32(out int materialValue))
                            {
                                item.Material = (ItemMaterial)materialValue;
                            }
                            break;
                        case "Weight":
                            if (property.Value.TryGetDouble(out double weight))
                            {
                                item.Weight = (float)weight;
                            }
                            break;
                        case "StatBonuses":
                            if (property.Value.ValueKind == JsonValueKind.Object)
                            {
                                item.StatBonuses = new Dictionary<string, int>();
                                foreach (var bonus in property.Value.EnumerateObject())
                                {
                                    if (bonus.Value.TryGetInt32(out int bonusValue))
                                    {
                                        item.StatBonuses[bonus.Name] = bonusValue;
                                    }
                                }
                            }
                            break;
                    }
                }
                
                // Validate item fields and ensure defaults for critical properties
                if (string.IsNullOrEmpty(item.Name))
                {
                    item.Name = "Unknown Item";
                }
                
                if (string.IsNullOrEmpty(item.Description))
                {
                    item.Description = "No description available";
                }
                
                if (string.IsNullOrEmpty(item.SpritePath))
                {
                    // Set appropriate default sprite path based on item type
                    switch (item.Type)
                    {
                        case ItemType.Weapon:
                            item.SpritePath = "Assets/Images/items/weapons/wooden_sword.png";
                            break;
                        case ItemType.Helmet:
                        case ItemType.Chestplate:
                        case ItemType.Leggings:
                            item.SpritePath = "Assets/Images/items/armor/wooden_armor.png";
                            break;
                        case ItemType.Shield:
                            item.SpritePath = "Assets/Images/items/armor/iron_shield.png";
                            break;
                        case ItemType.Consumable:
                            item.SpritePath = "Assets/Images/items/consumables/healing_potion.png";
                            break;
                        case ItemType.Material:
                            item.SpritePath = "Assets/Images/items/materials/wood.png";
                            break;
                        default:
                            item.SpritePath = "Assets/Images/items/def.png";
                            break;
                    }
                }
                
                // Ensure stack size is at least 1
                if (item.StackSize <= 0)
                {
                    item.StackSize = 1;
                }
                
                // Ensure max stack size is at least 1
                if (item.MaxStackSize <= 0)
                {
                    // Set default max stack size based on item type
                    switch (item.Type)
                    {
                        case ItemType.Material:
                            item.MaxStackSize = 99;
                            break;
                        case ItemType.Consumable:
                            item.MaxStackSize = 10;
                            break;
                        default:
                            item.MaxStackSize = 1;
                            break;
                    }
                }
                
                // Ensure stack size doesn't exceed max stack size
                if (item.StackSize > item.MaxStackSize)
                {
                    item.StackSize = item.MaxStackSize;
                }
                
                return item;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating item from JSON: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return null;
            }
        }
    }
    
    // Типы значимых действий для автосохранения
    public enum GameActionType
    {
        BattleCompleted,
        LocationChanged,
        BossDefeated,
        ItemCrafted,
        EquipmentChanged
    }
} 