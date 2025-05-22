using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SketchBlade.Helpers.Converters
{
    public class NullToTransparentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null ? Brushes.Transparent : new SolidColorBrush(Colors.LightGray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 