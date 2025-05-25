using System;
using SketchBlade.Models;

namespace SketchBlade.Services
{
    /// <summary>
    /// Interface for file save/load operations
    /// </summary>
    public interface IFileSaveService
    {
        /// <summary>
        /// Save the game state to a file
        /// </summary>
        /// <param name="GameData">Current game state to save</param>
        void SaveGame(GameData GameData);
        
        /// <summary>
        /// Load game state from a save file
        /// </summary>
        /// <returns>Saved game data or null if no save exists</returns>
        object LoadGame();
        
        /// <summary>
        /// Apply saved data to the current game state
        /// </summary>
        /// <param name="GameData">Current game state to update</param>
        /// <param name="saveData">Save data to apply</param>
        void ApplySaveData(GameData GameData, object saveData);
        
        /// <summary>
        /// Check if a save file exists
        /// </summary>
        /// <returns>True if save file exists, false otherwise</returns>
        bool SaveExists();
        
        /// <summary>
        /// Trigger auto-save functionality
        /// </summary>
        void TriggerAutoSave();
        
        /// <summary>
        /// Auto-save enabled flag
        /// </summary>
        bool AutoSaveEnabled { get; set; }
        
        /// <summary>
        /// Auto-save interval
        /// </summary>
        TimeSpan AutoSaveInterval { get; set; }
    }
} 
