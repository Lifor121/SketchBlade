using System;
using System.Globalization;
using System.Windows.Data;
using SketchBlade.Models;
using SketchBlade.Services;

namespace SketchBlade.Helpers.Converters
{
    public class EnumToLocalizedStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return string.Empty;
            
            string prefix = parameter as string ?? string.Empty;
            string enumType = value.GetType().Name;
            string enumValue = value.ToString();
            
            string key = string.IsNullOrEmpty(prefix) 
                ? $"{enumType}.{enumValue}" 
                : $"{prefix}.{enumValue}";
            
            return LanguageService.GetTranslation(key);
        }
        
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 