using System;
using SketchBlade.Models;

namespace SketchBlade.Services
{
    public interface IFileSaveService
    {
        void SaveGame(GameData GameData);
        object LoadGame();
        void ApplySaveData(GameData GameData, object saveData);
        bool SaveExists();
        void TriggerAutoSave();
        bool AutoSaveEnabled { get; set; }
        TimeSpan AutoSaveInterval { get; set; }
    }
} 
