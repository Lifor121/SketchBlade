using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SketchBlade.Helpers.Converters
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return parameter != null && parameter.ToString() == "hasContent" 
                    ? Visibility.Collapsed 
                    : Visibility.Visible;
            }
            
            if (parameter != null && parameter.ToString() == "hasContent")
            {
                if (value is string stringValue)
                {
                    return string.IsNullOrEmpty(stringValue) ? Visibility.Collapsed : Visibility.Visible;
                }
            }
            
            if (parameter != null && value is string stringParam)
            {
                string paramStr = parameter.ToString();
                
                return stringParam == paramStr ? Visibility.Collapsed : Visibility.Visible;
            }
            
            if (value is bool boolValue)
            {
                bool invert = parameter != null && parameter.ToString() == "inverse";
                bool result = invert ? !boolValue : boolValue;
                return result ? Visibility.Visible : Visibility.Collapsed;
            }
            
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                bool invert = parameter != null && parameter.ToString() == "inverse";
                bool result = visibility == Visibility.Visible;
                return invert ? !result : result;
            }
            
            return false;
        }
    }
} 