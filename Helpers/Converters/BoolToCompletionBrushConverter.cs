using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SketchBlade.Helpers.Converters
{
    public class BoolToCompletionBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isCompleted)
            {
                return isCompleted 
                    ? new SolidColorBrush(Color.FromRgb(76, 175, 80))  // Green for completed
                    : new SolidColorBrush(Color.FromRgb(158, 158, 158));  // Gray for not completed
            }
            
            return new SolidColorBrush(Color.FromRgb(158, 158, 158));  // Default to gray
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 