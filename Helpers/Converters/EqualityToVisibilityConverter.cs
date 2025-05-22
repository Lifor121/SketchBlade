using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SketchBlade.Helpers.Converters
{
    public class EqualityToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null && parameter == null)
                return Visibility.Collapsed;
                
            if (value == null || parameter == null)
                return Visibility.Visible;
                
            bool isEqual = value.ToString() == parameter.ToString();
            
            // Returns Collapsed if the values are equal (hide nav bar on main menu)
            // Returns Visible if they are different (show nav bar on other screens)
            return isEqual ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Not implemented as it's not needed for this feature
            throw new NotImplementedException();
        }
    }
} 