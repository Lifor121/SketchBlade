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
    /// <summary>
    /// Консолидированный сервис управления игрой
    /// Объединяет сохранение, автосохранение и настройки
    /// </summary>
    public interface ICoreGameService
    {
        // Game Save Operations
        bool SaveGame(GameData gameData);
        object? LoadGame();
        bool SaveExists();
        bool HasSaveFile();
        void DeleteSave();
        
        // Auto-save functionality
        void StartAutoSave(Action saveAction);
        void StopAutoSave();
        void SaveNow();
        bool IsAutoSaveEnabled { get; set; }
        TimeSpan AutoSaveInterval { get; set; }
        
        // Settings management
        void SaveSettings();
        void LoadSettings();
        
        // Events
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

        // File paths
        private static readonly string SaveFileName = "savegame.dat";
        private static readonly string BackupSaveFileName = "savegame.backup.dat";
        private static readonly string SettingsFileName = "settings.json";

        // Auto-save
        private Timer? _autoSaveTimer;
        private Action? _saveAction;
        private readonly object _lockObject = new object();
        private bool _isDisposed = false;

        // Properties
        public bool IsAutoSaveEnabled { get; set; } = true;
        public TimeSpan AutoSaveInterval { get; set; } = TimeSpan.FromMinutes(5);

        // Events
        public event EventHandler<GameSaveEventArgs>? GameSaved;
        public event EventHandler<GameLoadEventArgs>? GameLoaded;

        private CoreGameService()
        {
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
                // Create backup
                CreateBackup();

                // Serialize game data
                var saveData = SerializeGameData(gameData);
                var json = JsonSerializer.Serialize(saveData, new JsonSerializerOptions { WriteIndented = true });

                // Save to file
                File.WriteAllText(SaveFileName, json);

                LoggingService.LogDebug("Game saved successfully");
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
                    LoggingService.LogDebug("Save file not found");
                    return null;
                }

                var json = File.ReadAllText(SaveFileName);
                var saveData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

                LoggingService.LogDebug("Game loaded successfully");
                GameLoaded?.Invoke(this, new GameLoadEventArgs { Success = true, LoadedData = saveData });
                
                return saveData;
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error loading game: {ex.Message}", ex);
                
                // Try backup
                var backup = TryLoadBackup();
                if (backup != null)
                {
                    LoggingService.LogDebug("Loaded from backup");
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

                LoggingService.LogDebug("Save files deleted");
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
                    LoggingService.LogDebug("Auto-save started");
                }
            }
        }

        public void StopAutoSave()
        {
            lock (_lockObject)
            {
                _autoSaveTimer?.Dispose();
                _autoSaveTimer = null;
                LoggingService.LogDebug("Auto-save stopped");
            }
        }

        public void SaveNow()
        {
            if (_saveAction != null)
            {
                try
                {
                    _saveAction.Invoke();
                    LoggingService.LogDebug("Manual save triggered");
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
                LoggingService.LogDebug("Auto-save completed");
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

                LoggingService.LogDebug("Settings saved");
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
                    LoggingService.LogDebug("Settings file not found, using defaults");
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

                LoggingService.LogDebug("Settings loaded");
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
                return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
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

            // Serialize player
            if (gameData.Player != null)
            {
                saveData["Player"] = SerializePlayer(gameData.Player);
            }

            // Serialize inventory
            if (gameData.Inventory != null)
            {
                saveData["Inventory"] = SerializeInventory(gameData.Inventory);
            }

            // Serialize locations
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
                ["ImagePath"] = player.ImagePath ?? AssetPaths.Characters.PLAYER
            };

            // Serialize equipped items
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

            // Serialize items collections
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
                ["SpritePath"] = item.SpritePath ?? "",
                ["IsStackable"] = item.IsStackable,
                ["StackSize"] = item.StackSize,
                ["MaxStackSize"] = item.MaxStackSize,
                ["EquipSlot"] = (int)item.EquipSlot,
                ["EffectPower"] = item.EffectPower,
                ["Material"] = (int)item.Material,
                ["Weight"] = item.Weight
            };
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_isDisposed) return;

            StopAutoSave();
            SaveSettings();
            _isDisposed = true;
            LoggingService.LogDebug("CoreGameService disposed");
        }

        #endregion
    }
} 