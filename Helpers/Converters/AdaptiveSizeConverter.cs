using System;
using System.Globalization;
using System.Windows.Data;
using SketchBlade.Services;

namespace SketchBlade.Helpers.Converters
{
    public class AdaptiveSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter is string sizeKey)
            {
                return UIService.Instance.GetSize(sizeKey);
            }
            
            if (value is double baseSize)
            {
                return baseSize * UIService.Instance.GetScaleFactor();
            }
            
            if (double.TryParse(value?.ToString(), out var size))
            {
                return size * UIService.Instance.GetScaleFactor();
            }
            
            return 12.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("AdaptiveSizeConverter не поддерживает обратное преобразование");
        }
    }
} 