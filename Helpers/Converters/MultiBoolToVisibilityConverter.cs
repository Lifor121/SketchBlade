using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace SketchBlade.Helpers.Converters
{
    /// <summary>
    /// Converts multiple boolean values to a Visibility. 
    /// All values must be true for the element to be visible.
    /// </summary>
    public class MultiBoolToVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // If any value is null, not a boolean, or false, return Collapsed
            if (values == null || values.OfType<bool>().Any(b => b == false) || values.Any(v => !(v is bool)))
            {
                return Visibility.Collapsed;
            }
            
            // All values are true
            return Visibility.Visible;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 