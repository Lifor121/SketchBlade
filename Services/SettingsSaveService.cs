using System;
using System.IO;
using System.Text.Json;
using SketchBlade.Models;

namespace SketchBlade.Services
{
    public class SettingsSaveService
    {
        private static readonly string SettingsFileName = "gamesettings.json";
        
        public static void SaveSettings(GameSettings settings)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true // JSON файл для настроек удобнее хранить в читаемом формате
                };
                
                string jsonString = JsonSerializer.Serialize(settings, options);
                
                // Сохраняем в JSON файл
                File.WriteAllText(SettingsFileName, jsonString);
            }
            catch (Exception ex)
            {
                // Логирование ошибки
                Console.WriteLine($"Error saving settings: {ex.Message}");
            }
        }
        
        public static GameSettings LoadSettings()
        {
            try
            {
                if (File.Exists(SettingsFileName))
                {
                    string jsonString = File.ReadAllText(SettingsFileName);
                    var settings = JsonSerializer.Deserialize<GameSettings>(jsonString);
                    
                    if (settings != null)
                    {
                        return settings;
                    }
                }
            }
            catch (Exception ex)
            {
                // Логирование ошибки
                Console.WriteLine($"Error loading settings: {ex.Message}");
            }
            
            // Если не удалось загрузить, возвращаем настройки по умолчанию
            return new GameSettings();
        }
    }
} 