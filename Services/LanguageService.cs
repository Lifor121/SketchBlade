using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using SketchBlade.Models;

namespace SketchBlade.Services
{
    public class LanguageService
    {
        private static Dictionary<string, Dictionary<Language, string>> _translations = new Dictionary<string, Dictionary<Language, string>>();
        private static Language _currentLanguage = Language.Russian;
        
        // Пути к файлам локализации
        private static readonly string RussianLocalizationFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Localization", "russian.json");
        private static readonly string EnglishLocalizationFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Localization", "english.json");
        
        public static Language CurrentLanguage
        {
            get => _currentLanguage;
            set
            {
                if (_currentLanguage != value)
                {
                    _currentLanguage = value;
                    UpdateCulture();
                }
            }
        }
        
        static LanguageService()
        {
            LoadTranslationsFromFiles();
        }
        
        private static void LoadTranslationsFromFiles()
        {
            try
            {
                _translations.Clear();
                
                Console.WriteLine("Loading translations from files...");
                
                // Загружаем русские тексты
                if (File.Exists(RussianLocalizationFile))
                {
                    Console.WriteLine($"Found Russian localization file: {RussianLocalizationFile}");
                    LoadTranslationsFromFile(RussianLocalizationFile, Language.Russian);
                }
                else
                {
                    Console.WriteLine($"Russian localization file not found: {RussianLocalizationFile}");
                    Console.WriteLine($"Current directory: {Directory.GetCurrentDirectory()}");
                    Console.WriteLine($"Full path: {Path.GetFullPath(RussianLocalizationFile)}");
                }
                
                // Загружаем английские тексты
                if (File.Exists(EnglishLocalizationFile))
                {
                    Console.WriteLine($"Found English localization file: {EnglishLocalizationFile}");
                    LoadTranslationsFromFile(EnglishLocalizationFile, Language.English);
                }
                else
                {
                    Console.WriteLine($"English localization file not found: {EnglishLocalizationFile}");
                    Console.WriteLine($"Current directory: {Directory.GetCurrentDirectory()}");
                    Console.WriteLine($"Full path: {Path.GetFullPath(EnglishLocalizationFile)}");
                }
                
                // If files not found, use built-in translations
                if (_translations.Count == 0)
                {
                    Console.WriteLine("No translation files found, using built-in translations");
                    InitializeTranslations();
                }
                else
                {
                    Console.WriteLine($"Successfully loaded {_translations.Count} translation keys");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading translations: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                InitializeTranslations(); // Fallback to built-in translations
            }
        }
        
        private static void LoadTranslationsFromFile(string filePath, Language language)
        {
            try
            {
                string jsonString = File.ReadAllText(filePath);
                using JsonDocument document = JsonDocument.Parse(jsonString);
                
                // Рекурсивно обрабатываем JSON для создания плоской структуры ключей
                ProcessJsonElement(document.RootElement, "", language);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading translations from {filePath}: {ex.Message}");
            }
        }
        
        private static void ProcessJsonElement(JsonElement element, string keyPrefix, Language language)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                foreach (JsonProperty property in element.EnumerateObject())
                {
                    string newKey = string.IsNullOrEmpty(keyPrefix) ? 
                        property.Name : 
                        $"{keyPrefix}.{property.Name}";
                    
                    if (property.Value.ValueKind == JsonValueKind.Object)
                    {
                        ProcessJsonElement(property.Value, newKey, language);
                    }
                    else if (property.Value.ValueKind == JsonValueKind.String)
                    {
                        AddTranslation(newKey, language, property.Value.GetString() ?? "");
                    }
                }
            }
        }
        
        // Встроенные переводы как запасной вариант
        private static void InitializeTranslations()
        {
            // Main Menu
            AddTranslation("MainMenu.NewGame", Language.Russian, "Новая игра");
            AddTranslation("MainMenu.NewGame", Language.English, "New Game");
            
            AddTranslation("MainMenu.ContinueGame", Language.Russian, "Продолжить игру");
            AddTranslation("MainMenu.ContinueGame", Language.English, "Continue Game");
            
            AddTranslation("MainMenu.Options", Language.Russian, "Настройки");
            AddTranslation("MainMenu.Options", Language.English, "Options");
            
            AddTranslation("MainMenu.Exit", Language.Russian, "Выход");
            AddTranslation("MainMenu.Exit", Language.English, "Exit");
            
            // Settings
            AddTranslation("Settings.Title", Language.Russian, "Настройки");
            AddTranslation("Settings.Title", Language.English, "Settings");
            
            AddTranslation("Settings.Language", Language.Russian, "Языковые настройки");
            AddTranslation("Settings.Language", Language.English, "Language Settings");
            
            AddTranslation("Settings.GameLanguage", Language.Russian, "Язык игры");
            AddTranslation("Settings.GameLanguage", Language.English, "Game Language");
            
            AddTranslation("Settings.Audio", Language.Russian, "Настройки звука");
            AddTranslation("Settings.Audio", Language.English, "Audio Settings");
            
            AddTranslation("Settings.Music", Language.Russian, "Громкость музыки");
            AddTranslation("Settings.Music", Language.English, "Music Volume");
            
            AddTranslation("Settings.SoundEffects", Language.Russian, "Громкость эффектов");
            AddTranslation("Settings.SoundEffects", Language.English, "Sound Effects");
            
            AddTranslation("Settings.MuteSounds", Language.Russian, "Отключить звук");
            AddTranslation("Settings.MuteSounds", Language.English, "Mute Sounds");
            
            AddTranslation("Settings.Interface", Language.Russian, "Интерфейс");
            AddTranslation("Settings.Interface", Language.English, "Interface");
            
            AddTranslation("Settings.UIScale", Language.Russian, "Масштаб интерфейса");
            AddTranslation("Settings.UIScale", Language.English, "UI Scale");
            
            AddTranslation("Settings.Tooltips", Language.Russian, "Подсказки");
            AddTranslation("Settings.Tooltips", Language.English, "Tooltips");
            
            AddTranslation("Settings.ShowItemTooltips", Language.Russian, "Показывать описания предметов при наведении");
            AddTranslation("Settings.ShowItemTooltips", Language.English, "Show item descriptions on hover");
            
            AddTranslation("Settings.Combat", Language.Russian, "Бой");
            AddTranslation("Settings.Combat", Language.English, "Combat");
            
            AddTranslation("Settings.ShowDamageNumbers", Language.Russian, "Показывать числа урона");
            AddTranslation("Settings.ShowDamageNumbers", Language.English, "Show damage numbers");
            
            AddTranslation("Settings.Save", Language.Russian, "Сохранить");
            AddTranslation("Settings.Save", Language.English, "Save");
            
            AddTranslation("Settings.SavedStatus", Language.Russian, "Сохранено!");
            AddTranslation("Settings.SavedStatus", Language.English, "Saved!");
            
            AddTranslation("Settings.ResetDefaults", Language.Russian, "Сбросить настройки");
            AddTranslation("Settings.ResetDefaults", Language.English, "Reset to Defaults");
            
            AddTranslation("Settings.UnsavedChanges", Language.Russian, "У вас есть несохраненные изменения. Сохранить их перед выходом?");
            AddTranslation("Settings.UnsavedChanges", Language.English, "You have unsaved changes. Save them before leaving?");
            
            AddTranslation("Settings.UnsavedChangesTitle", Language.Russian, "Несохраненные изменения");
            AddTranslation("Settings.UnsavedChangesTitle", Language.English, "Unsaved Changes");
            
            // Common
            AddTranslation("Common.Back", Language.Russian, "Назад");
            AddTranslation("Common.Back", Language.English, "Back");
            
            AddTranslation("Common.Enable", Language.Russian, "Включить");
            AddTranslation("Common.Enable", Language.English, "Enable");
            
            // Languages
            AddTranslation("Language.Russian", Language.Russian, "Русский");
            AddTranslation("Language.Russian", Language.English, "Russian");
            
            AddTranslation("Language.English", Language.Russian, "Английский");
            AddTranslation("Language.English", Language.English, "English");
            
            // Difficulty
            AddTranslation("Difficulty.Easy", Language.Russian, "Легкий");
            AddTranslation("Difficulty.Easy", Language.English, "Easy");
            
            AddTranslation("Difficulty.Normal", Language.Russian, "Нормальный");
            AddTranslation("Difficulty.Normal", Language.English, "Normal");
            
            AddTranslation("Difficulty.Hard", Language.Russian, "Сложный");
            AddTranslation("Difficulty.Hard", Language.English, "Hard");
            
            // Checkboxes
            AddTranslation("Settings.ShowTutorials", Language.Russian, "Показывать подсказки");
            AddTranslation("Settings.ShowTutorials", Language.English, "Show tutorials");
            
            AddTranslation("Settings.ShowDamageNumbers", Language.Russian, "Показывать числа урона");
            AddTranslation("Settings.ShowDamageNumbers", Language.English, "Show damage numbers");
            
            // Audio text
            AddTranslation("Audio.MuteAllSounds", Language.Russian, "Выключить все звуки");
            AddTranslation("Audio.MuteAllSounds", Language.English, "Mute all sounds");
        }
        
        private static void AddTranslation(string key, Language language, string text)
        {
            if (!_translations.ContainsKey(key))
            {
                _translations[key] = new Dictionary<Language, string>();
            }
            
            _translations[key][language] = text;
        }
        
        public static string GetTranslation(string key)
        {
            if (_translations.TryGetValue(key, out var languageDict))
            {
                if (languageDict.TryGetValue(_currentLanguage, out var translation))
                {
                    return translation;
                }
                
                // Fallback to English
                if (languageDict.TryGetValue(Language.English, out var englishTranslation))
                {
                    return englishTranslation;
                }
            }
            
            // Return key if no translation found
            Console.WriteLine($"Translation not found for key: {key}, current language: {_currentLanguage}");
            return key;
        }
        
        private static void UpdateCulture()
        {
            // Set the current culture based on selected language
            CultureInfo culture = _currentLanguage switch
            {
                Language.English => new CultureInfo("en-US"),
                Language.Russian => new CultureInfo("ru-RU"),
                _ => CultureInfo.CurrentCulture
            };
            
            CultureInfo.CurrentUICulture = culture;
        }
        
        // Debug method to check translations
        public static void DebugTranslations()
        {
            Console.WriteLine($"Current language: {_currentLanguage}");
            Console.WriteLine($"Total translation keys: {_translations.Count}");
            
            // Print first 10 keys as sample
            int count = 0;
            foreach (var key in _translations.Keys)
            {
                if (count++ < 10)
                {
                    string value = GetTranslation(key);
                    Console.WriteLine($"Sample translation - Key: {key}, Value: {value}");
                }
            }
        }
    }
} 