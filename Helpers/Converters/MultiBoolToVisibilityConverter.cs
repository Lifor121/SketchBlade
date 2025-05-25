using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace SketchBlade.Helpers.Converters
{
    public class MultiBoolToVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length == 0)
                return Visibility.Collapsed;

            foreach (var value in values)
            {
                if (value == null)
                    return Visibility.Collapsed;

                if (value is Visibility visibility)
                {
                    if (visibility != Visibility.Visible)
                        return Visibility.Collapsed;
                }
                else if (value is bool boolValue)
                {
                    if (!boolValue)
                        return Visibility.Collapsed;
                }
                else if (value is string stringValue)
                {
                    if (string.IsNullOrEmpty(stringValue))
                        return Visibility.Collapsed;
                }
                else if (value == null)
                {
                    return Visibility.Collapsed;
                }
            }
            
            return Visibility.Visible;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 