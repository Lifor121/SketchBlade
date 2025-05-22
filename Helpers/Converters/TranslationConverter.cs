using System;
using System.Globalization;
using System.Windows.Data;
using SketchBlade.Services;

namespace SketchBlade.Helpers.Converters
{
    public class TranslationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                string key;
                
                // Handle case where parameter is a format string and value should be inserted into it
                if (parameter != null && value != null)
                {
                    string format = parameter.ToString();
                    if (format.Contains("{0}"))
                    {
                        key = string.Format(format, value.ToString());
                        Console.WriteLine($"TranslationConverter: Formatted key '{key}' from parameter '{format}' and value '{value}'");
                    }
                    else
                    {
                        key = parameter.ToString();
                        Console.WriteLine($"TranslationConverter: Using parameter as key '{key}'");
                    }
                }
                else if (value == null)
                {
                    Console.WriteLine("TranslationConverter: value is null");
                    return string.Empty;
                }
                else
                {
                    key = value.ToString();
                    Console.WriteLine($"TranslationConverter: Using value as key '{key}'");
                }
                
                if (string.IsNullOrEmpty(key))
                {
                    Console.WriteLine("TranslationConverter: key is empty");
                    return string.Empty;
                }
                
                Console.WriteLine($"TranslationConverter: Converting key '{key}'");
                string translation = LanguageService.GetTranslation(key);
                Console.WriteLine($"TranslationConverter: Result for '{key}' is '{translation}'");
                return translation;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"TranslationConverter: Error converting value: {ex.Message}");
                return value?.ToString() ?? string.Empty;
            }
        }
        
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 