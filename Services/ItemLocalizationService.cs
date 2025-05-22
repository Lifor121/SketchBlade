using System;
using System.Collections.Generic;
using SketchBlade.Models;

namespace SketchBlade.Services
{
    public static class ItemLocalizationService
    {
        private static Dictionary<string, string> _itemLocalizationCache = new Dictionary<string, string>();
        
        public static string GetLocalizedItemName(string itemKey)
        {
            // Если в кэше уже есть локализованное название, возвращаем его
            if (_itemLocalizationCache.TryGetValue(itemKey, out string localizedName))
            {
                return localizedName;
            }
            
            // Пытаемся найти локализацию по ключу
            string localizationKey = $"Items.{itemKey}.Name";
            string translation = LanguageService.GetTranslation(localizationKey);
            
            // Если перевод найден и не совпадает с ключом (т.е. это не просто возврат ключа из-за отсутствия перевода)
            if (!string.IsNullOrEmpty(translation) && !translation.Contains(localizationKey))
            {
                _itemLocalizationCache[itemKey] = translation;
                return translation;
            }
            
            // Пытаемся найти перевод, удаляя пробелы из названия (например, "Iron Shield" -> "IronShield")
            string noSpacesKey = itemKey.Replace(" ", "");
            localizationKey = $"Items.{noSpacesKey}.Name";
            translation = LanguageService.GetTranslation(localizationKey);
            
            if (!string.IsNullOrEmpty(translation) && !translation.Contains(localizationKey))
            {
                _itemLocalizationCache[itemKey] = translation;
                return translation;
            }
            
            // Если перевод не найден, возвращаем оригинальное название
            _itemLocalizationCache[itemKey] = itemKey;
            return itemKey;
        }
        
        public static string GetLocalizedItemDescription(string itemKey)
        {
            string localizationKey = $"Items.{itemKey}.Description";
            string translation = LanguageService.GetTranslation(localizationKey);
            
            if (string.IsNullOrEmpty(translation) || translation == localizationKey)
            {
                return string.Empty;
            }
            
            return translation;
        }
        
        public static string GetOriginalItemKey(string localizedName)
        {
            // Ищем в кэше ключ по локализованному названию
            foreach (var pair in _itemLocalizationCache)
            {
                if (pair.Value == localizedName)
                {
                    return pair.Key;
                }
            }
            
            // Если не найдено, возвращаем исходное название
            return localizedName;
        }
        
        // Метод для локализации названия рецепта
        public static string GetLocalizedRecipeName(string recipeKey)
        {
            // Для рецептов используем тот же механизм, что и для предметов
            return GetLocalizedItemName(recipeKey);
        }
        
        // Метод для очистки кэша (например, при смене языка)
        public static void ClearCache()
        {
            _itemLocalizationCache.Clear();
        }
    }
} 