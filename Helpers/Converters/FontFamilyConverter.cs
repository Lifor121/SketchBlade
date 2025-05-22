using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SketchBlade.Helpers.Converters
{
    public class FontFamilyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string fontName && !string.IsNullOrEmpty(fontName))
            {
                return FontHelper.GetFont(fontName);
            }
            
            if (parameter is string paramFontName && !string.IsNullOrEmpty(paramFontName))
            {
                return FontHelper.GetFont(paramFontName);
            }
            
            return FontHelper.GetDefaultFont();
        }
        
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return string.Empty;
        }
    }
} 