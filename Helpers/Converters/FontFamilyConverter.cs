using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SketchBlade.Helpers.Converters
{
    /// <summary>
    /// Конвертер для получения шрифтов через FontHelper
    /// </summary>
    public class FontFamilyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string fontName && !string.IsNullOrEmpty(fontName))
            {
                return FontHelper.GetFont(fontName);
            }
            
            // Используем параметр как название шрифта, если значение не задано
            if (parameter is string paramFontName && !string.IsNullOrEmpty(paramFontName))
            {
                return FontHelper.GetFont(paramFontName);
            }
            
            // Возвращаем шрифт по умолчанию
            return FontHelper.GetDefaultFont();
        }
        
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Обратное преобразование не реализовано
            return string.Empty;
        }
    }
} 