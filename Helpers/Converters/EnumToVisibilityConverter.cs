using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using SketchBlade.Models;

namespace SketchBlade.Helpers.Converters
{
    public class EnumToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return Visibility.Visible;
            
            if (value is GameScreen screen)
            {
                return screen == GameScreen.MainMenu || screen == GameScreen.Settings
                    ? Visibility.Collapsed 
                    : Visibility.Visible;
            }
            
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 