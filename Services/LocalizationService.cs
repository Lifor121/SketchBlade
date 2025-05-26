using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using SketchBlade.Models;

namespace SketchBlade.Services
{
    public interface ILocalizationService
    {
        Language CurrentLanguage { get; set; }
        event EventHandler? LanguageChanged;
        
        string GetTranslation(string key);
        string GetTranslation(string key, params object[] args);
        
        string GetItemName(string itemKey);
        string GetItemDescription(string itemKey);
        string GetLocalizedItemName(Item item);
        string GetLocalizedItemDescription(Item item);
        
        void ReloadTranslations();
        bool HasTranslation(string key);
        IEnumerable<string> GetAvailableLanguages();
    }

    public class LocalizationService : ILocalizationService
    {
        private static readonly Lazy<LocalizationService> _instance = new(() => new LocalizationService());
        public static LocalizationService Instance => _instance.Value;

        private Dictionary<string, Dictionary<Language, string>> _translations = new();
        private Dictionary<Language, Dictionary<string, string>> _itemTranslations = new();
        private Language _currentLanguage = Language.Russian;

        private static readonly string RussianLocalizationFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Localization", "russian.json");
        private static readonly string EnglishLocalizationFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Localization", "english.json");

        public event EventHandler? LanguageChanged;

        public Language CurrentLanguage
        {
            get => _currentLanguage;
            set
            {
                if (_currentLanguage != value)
                {
                    _currentLanguage = value;
                    UpdateCulture();
                    LanguageChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private LocalizationService()
        {
            LoadTranslationsFromFiles();
            LoadItemTranslations();
        }

        #region Translation Methods

        public string GetTranslation(string key)
        {
            try
            {
                if (string.IsNullOrEmpty(key))
                    return key;

                if (_translations.TryGetValue(key, out var languageDict))
                {
                    if (languageDict.TryGetValue(_currentLanguage, out var translation))
                        return translation;

                    if (_currentLanguage != Language.Russian && languageDict.TryGetValue(Language.Russian, out var russianTranslation))
                        return russianTranslation;

                    if (languageDict.TryGetValue(Language.English, out var englishTranslation))
                        return englishTranslation;
                }

                return key;
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error getting translation for key '{key}': {ex.Message}", ex);
                return key;
            }
        }

        public string GetTranslation(string key, params object[] args)
        {
            try
            {
                var template = GetTranslation(key);
                if (args.Length == 0)
                    return template;

                return string.Format(template, args);
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error formatting translation for key '{key}': {ex.Message}", ex);
                return GetTranslation(key);
            }
        }

        public bool HasTranslation(string key)
        {
            return _translations.ContainsKey(key);
        }

        #endregion

        #region Item Localization

        public string GetItemName(string itemKey)
        {
            try
            {
                if (_itemTranslations.TryGetValue(_currentLanguage, out var items))
                {
                    var nameKey = $"{itemKey}.Name";
                    if (items.TryGetValue(nameKey, out var name))
                        return name;
                }

                return GetTranslation($"Items.{itemKey}.Name");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error getting item name for '{itemKey}': {ex.Message}", ex);
                return itemKey;
            }
        }

        public string GetItemDescription(string itemKey)
        {
            try
            {
                if (_itemTranslations.TryGetValue(_currentLanguage, out var items))
                {
                    var descKey = $"{itemKey}.Description";
                    if (items.TryGetValue(descKey, out var description))
                        return description;
                }

                return GetTranslation($"Items.{itemKey}.Description");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error getting item description for '{itemKey}': {ex.Message}", ex);
                return "No description available";
            }
        }

        public string GetLocalizedItemName(Item item)
        {
            if (item == null) return string.Empty;

            try
            {
                var itemKey = GenerateItemKey(item);
                var localizedName = GetItemName(itemKey);
                
                if (localizedName == itemKey && !string.IsNullOrEmpty(item.Name))
                {
                    localizedName = GetItemName(item.Name.Replace(" ", "_").ToLower());
                }

                return localizedName != itemKey ? localizedName : item.Name;
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error getting localized name for item '{item.Name}': {ex.Message}", ex);
                return item.Name;
            }
        }

        public string GetLocalizedItemDescription(Item item)
        {
            if (item == null) return string.Empty;

            try
            {
                var itemKey = GenerateItemKey(item);
                var localizedDesc = GetItemDescription(itemKey);
                
                if (localizedDesc == "No description available" && !string.IsNullOrEmpty(item.Name))
                {
                    localizedDesc = GetItemDescription(item.Name.Replace(" ", "_").ToLower());
                }

                return localizedDesc != "No description available" ? localizedDesc : item.Description;
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error getting localized description for item '{item.Name}': {ex.Message}", ex);
                return item.Description;
            }
        }

        #endregion

        #region Utility Methods

        public void ReloadTranslations()
        {
            try
            {
                _translations.Clear();
                _itemTranslations.Clear();
                
                LoadTranslationsFromFiles();
                LoadItemTranslations();
                
                LanguageChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error reloading translations: {ex.Message}", ex);
            }
        }

        public IEnumerable<string> GetAvailableLanguages()
        {
            return new[] { "Russian", "English" };
        }

        #endregion

        #region Private Methods

        private void LoadTranslationsFromFiles()
        {
            try
            {
                if (File.Exists(RussianLocalizationFile))
                {
                    LoadLanguageFile(RussianLocalizationFile, Language.Russian);
                }
                else
                {
                    LoggingService.LogError($"Russian localization file not found: {RussianLocalizationFile}");
                    LoadFallbackRussianTranslations();
                }

                if (File.Exists(EnglishLocalizationFile))
                {
                    LoadLanguageFile(EnglishLocalizationFile, Language.English);
                }
                else
                {
                    LoggingService.LogError($"English localization file not found: {EnglishLocalizationFile}");
                    LoadFallbackEnglishTranslations();
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error loading translation files: {ex.Message}", ex);
                LoadFallbackTranslations();
            }
        }

        private void LoadLanguageFile(string filePath, Language language)
        {
            try
            {
                var json = File.ReadAllText(filePath);
                var translations = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                
                if (translations != null)
                {
                    ProcessTranslations(translations, language);
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error loading language file '{filePath}': {ex.Message}", ex);
            }
        }

        private void ProcessTranslations(Dictionary<string, object> translations, Language language, string prefix = "")
        {
            foreach (var kvp in translations)
            {
                var key = string.IsNullOrEmpty(prefix) ? kvp.Key : $"{prefix}.{kvp.Key}";
                
                if (kvp.Value is JsonElement element)
                {
                    if (element.ValueKind == JsonValueKind.String)
                    {
                        AddTranslation(key, element.GetString() ?? string.Empty, language);
                    }
                    else if (element.ValueKind == JsonValueKind.Object)
                    {
                        var nestedDict = JsonSerializer.Deserialize<Dictionary<string, object>>(element.GetRawText());
                        if (nestedDict != null)
                        {
                            ProcessTranslations(nestedDict, language, key);
                        }
                    }
                }
                else if (kvp.Value is string stringValue)
                {
                    AddTranslation(key, stringValue, language);
                }
            }
        }

        private void AddTranslation(string key, string value, Language language)
        {
            if (!_translations.TryGetValue(key, out var languageDict))
            {
                languageDict = new Dictionary<Language, string>();
                _translations[key] = languageDict;
            }
            
            languageDict[language] = value;
        }

        private void LoadItemTranslations()
        {
            try
            {
                _itemTranslations[Language.Russian] = new Dictionary<string, string>();
                _itemTranslations[Language.English] = new Dictionary<string, string>();

                LoadItemTranslationsForLanguage("item_translations_ru.json", Language.Russian);
                LoadItemTranslationsForLanguage("item_translations_en.json", Language.English);

                LoggingService.LogInfo("Item translations loaded");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error loading item translations: {ex.Message}", ex);
            }
        }

        private void LoadItemTranslationsForLanguage(string fileName, Language language)
        {
            try
            {
                var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Localization", fileName);
                
                if (File.Exists(filePath))
                {
                    var json = File.ReadAllText(filePath);
                    var translations = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                    
                    if (translations != null)
                    {
                        _itemTranslations[language] = translations;
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error loading item translations for {language}: {ex.Message}", ex);
            }
        }

        private string GenerateItemKey(Item item)
        {
            var typePrefix = item.Type.ToString().ToLower();
            var materialPrefix = item.Material != ItemMaterial.None ? item.Material.ToString().ToLower() : "";
            var baseName = item.Name.Replace(" ", "_").ToLower();

            if (!string.IsNullOrEmpty(materialPrefix))
            {
                return $"{typePrefix}.{materialPrefix}.{baseName}";
            }

            return $"{typePrefix}.{baseName}";
        }

        private void UpdateCulture()
        {
            try
            {
                var culture = _currentLanguage switch
                {
                    Language.English => new CultureInfo("en-US"),
                    Language.Russian => new CultureInfo("ru-RU"),
                    _ => new CultureInfo("ru-RU")
                };

                CultureInfo.CurrentCulture = culture;
                CultureInfo.CurrentUICulture = culture;
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error updating culture: {ex.Message}", ex);
            }
        }

        private void LoadFallbackTranslations()
        {
            LoadFallbackRussianTranslations();
            LoadFallbackEnglishTranslations();
        }

        private void LoadFallbackRussianTranslations()
        {
            var fallbackRussian = new Dictionary<string, string>
            {
                ["MainMenu.NewGame"] = "Новая игра",
                ["MainMenu.Continue"] = "Продолжить",
                ["MainMenu.Options"] = "Настройки", 
                ["MainMenu.Exit"] = "Выход",
                ["Characters.Player"] = "Игрок",
                ["UI.Health"] = "Здоровье",
                ["UI.Attack"] = "Атака",
                ["UI.Defense"] = "Защита",
                ["UI.Level"] = "Уровень",
                ["UI.Gold"] = "Золото"
            };

            foreach (var kvp in fallbackRussian)
            {
                AddTranslation(kvp.Key, kvp.Value, Language.Russian);
            }
        }

        private void LoadFallbackEnglishTranslations()
        {
            var fallbackEnglish = new Dictionary<string, string>
            {
                ["MainMenu.NewGame"] = "New Game",
                ["MainMenu.Continue"] = "Continue",
                ["MainMenu.Options"] = "Options",
                ["MainMenu.Exit"] = "Exit",
                ["Characters.Player"] = "Player",
                ["UI.Health"] = "Health",
                ["UI.Attack"] = "Attack",
                ["UI.Defense"] = "Defense",
                ["UI.Level"] = "Level",
                ["UI.Gold"] = "Gold"
            };

            foreach (var kvp in fallbackEnglish)
            {
                AddTranslation(kvp.Key, kvp.Value, Language.English);
            }
        }

        #endregion
    }
} 