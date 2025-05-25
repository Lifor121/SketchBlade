using System;
using System.Globalization;
using System.Windows.Data;
using SketchBlade.Models;
using SketchBlade.Services;

namespace SketchBlade.Helpers.Converters
{
    public class EnumToLocalizedStringConverter : IValueConverter, IMultiValueConverter
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
            
            return LocalizationService.Instance.GetTranslation(key);
        }
        
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values?.Length > 0 && values[0] != null)
            {
                return Convert(values[0], targetType, parameter, culture);
            }
            return string.Empty;
        }
        
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
        
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 