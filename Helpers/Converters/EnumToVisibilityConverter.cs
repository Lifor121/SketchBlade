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
            // Если значение null или не enum, показываем меню
            if (value == null)
                return Visibility.Visible;
            
            // Пытаемся преобразовать в GameScreen
            if (value is GameScreen screen)
            {
                // Скрываем меню на главном экране и в настройках, иначе показываем
                return screen == GameScreen.MainMenu || screen == GameScreen.Settings
                    ? Visibility.Collapsed 
                    : Visibility.Visible;
            }
            
            // В случае ошибки просто показываем меню
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 