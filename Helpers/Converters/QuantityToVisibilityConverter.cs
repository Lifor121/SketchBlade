using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SketchBlade.Helpers.Converters
{
    public class QuantityToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int quantity)
            {
                // Показываем количество только если оно больше 1
                return quantity > 1 ? Visibility.Visible : Visibility.Collapsed;
            }
            
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 