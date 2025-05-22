using System;
using System.Globalization;
using System.Windows.Data;
using System.Diagnostics;

namespace SketchBlade.Helpers.Converters
{
    /// <summary>
    /// Конвертер для обработки параметров команд, особенно для навигации.
    /// Обеспечивает более надежную передачу параметров при переходе между экранами.
    /// </summary>
    public class CommandParameterConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Console.WriteLine($"CommandParameterConverter: Converting value={value}, parameter={parameter}");
            
            // Если значение null, но есть параметр, используем параметр
            if (value == null && parameter != null)
            {
                Console.WriteLine($"CommandParameterConverter: Using parameter={parameter} instead of null value");
                return parameter;
            }
            
            // Для строковых значений проверяем, что строка не пуста
            if (value is string stringValue)
            {
                if (string.IsNullOrEmpty(stringValue) && parameter != null)
                {
                    Console.WriteLine($"CommandParameterConverter: Using parameter={parameter} instead of empty string");
                    return parameter;
                }
                
                // Нормализация имен экранов для навигации
                if (parameter != null && parameter.ToString() == "ScreenName")
                {
                    string normalized = NormalizeScreenName(stringValue);
                    Console.WriteLine($"CommandParameterConverter: Normalized screen name from '{stringValue}' to '{normalized}'");
                    return normalized;
                }
            }
            
            // По умолчанию возвращаем исходное значение
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Обратное преобразование в большинстве случаев не нужно
            return value;
        }
        
        /// <summary>
        /// Нормализует имена экранов для совместимой работы навигации
        /// </summary>
        private string NormalizeScreenName(string screenName)
        {
            try
            {
                if (string.IsNullOrEmpty(screenName))
                    return "MainMenuView"; // Безопасное значение по умолчанию
                
                // Проверка суффиксов и нормализация
                if (screenName.EndsWith("View", StringComparison.OrdinalIgnoreCase))
                {
                    // Если уже заканчивается на View, оставляем как есть
                    return screenName;
                }
                else if (screenName.EndsWith("Screen", StringComparison.OrdinalIgnoreCase))
                {
                    // Если заканчивается на Screen, заменяем на View
                    return screenName.Substring(0, screenName.Length - 6) + "View";
                }
                else
                {
                    // В других случаях добавляем View
                    return screenName + "View";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in NormalizeScreenName: {ex.Message}");
                return screenName; // В случае ошибки возвращаем исходное имя
            }
        }
    }
} 