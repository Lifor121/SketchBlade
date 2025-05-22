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
            // Проверка на null для исходного значения
            if (value == null)
            {
                // Если параметр hasContent, то null означает что контента нет
                return parameter != null && parameter.ToString() == "hasContent" 
                    ? Visibility.Collapsed 
                    : Visibility.Visible;
            }
            
            // Если параметр hasContent, проверяем, есть ли содержимое в строке
            if (parameter != null && parameter.ToString() == "hasContent")
            {
                if (value is string stringValue)
                {
                    return string.IsNullOrEmpty(stringValue) ? Visibility.Collapsed : Visibility.Visible;
                }
            }
            
            // Проверка на равенство строк (для сравнения CurrentScreen с MainMenuView)
            if (parameter != null && value is string stringParam)
            {
                string paramStr = parameter.ToString();
                
                // Для навигационной панели: если CurrentScreen равен MainMenuView,
                // то скрываем (Collapsed), иначе показываем (Visible)
                return stringParam == paramStr ? Visibility.Collapsed : Visibility.Visible;
            }
            
            // Стандартное поведение для bool значений
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