using System;
using System.IO;
using System.Text.Json;
using SketchBlade.Models;

namespace SketchBlade.Services
{
    public class GameSaveManager
    {
        private const string SAVE_FILE_NAME = "savegame.json";
        private readonly string _saveFilePath;

        public GameSaveManager()
        {
            string saveDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Saves");
            Directory.CreateDirectory(saveDirectory);
            _saveFilePath = Path.Combine(saveDirectory, SAVE_FILE_NAME);
        }

        public bool HasSaveFile()
        {
            return File.Exists(_saveFilePath);
        }

        public bool SaveGame(GameData gameData)
        {
            try
            {
                if (gameData == null || !gameData.IsValid())
                {
                    LoggingService.LogError("Cannot save invalid game data");
                    return false;
                }

                var saveData = gameData.CreateSaveCopy();
                
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    IgnoreReadOnlyProperties = false,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                string jsonString = JsonSerializer.Serialize(saveData, options);
                File.WriteAllText(_saveFilePath, jsonString);

                LoggingService.LogInfo($"Game saved successfully to {_saveFilePath}");
                return true;
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Failed to save game", ex);
                return false;
            }
        }

        public GameData? LoadGame()
        {
            try
            {
                if (!HasSaveFile())
                {
                    LoggingService.LogInfo("No save file found");
                    return null;
                }

                string jsonString = File.ReadAllText(_saveFilePath);
                
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    PropertyNameCaseInsensitive = true
                };

                var gameData = JsonSerializer.Deserialize<GameData>(jsonString, options);

                if (gameData == null || !gameData.IsValid())
                {
                    LoggingService.LogError("Loaded game data is invalid");
                    return null;
                }

                LoggingService.LogInfo("Game loaded successfully");
                return gameData;
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Failed to load game", ex);
                return null;
            }
        }

        public bool DeleteSaveFile()
        {
            try
            {
                if (HasSaveFile())
                {
                    File.Delete(_saveFilePath);
                    LoggingService.LogInfo("Save file deleted");
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Failed to delete save file", ex);
                return false;
            }
        }

        public bool CreateBackup()
        {
            try
            {
                if (!HasSaveFile()) return false;

                string backupPath = _saveFilePath + $".backup_{DateTime.Now:yyyyMMdd_HHmmss}";
                File.Copy(_saveFilePath, backupPath);
                
                LoggingService.LogInfo($"Backup created: {backupPath}");
                return true;
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Failed to create backup", ex);
                return false;
            }
        }

        public bool AutoSave(GameData gameData, DateTime lastSaveTime, TimeSpan interval)
        {
            if (DateTime.Now - lastSaveTime < interval)
            {
                return false;
            }

            return SaveGame(gameData);
        }
    }
} 