using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SketchBlade.Helpers.Converters
{
    public class BoolToLocationIndicatorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isSelected)
            {
                return isSelected 
                    ? new SolidColorBrush(Color.FromRgb(255, 152, 0))  // Orange for selected
                    : new SolidColorBrush(Color.FromRgb(224, 224, 224));  // Light gray for not selected
            }
            
            return new SolidColorBrush(Color.FromRgb(224, 224, 224));  // Default to light gray
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 