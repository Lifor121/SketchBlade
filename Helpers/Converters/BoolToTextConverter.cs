using System;
using System.Globalization;
using System.Windows.Data;

namespace SketchBlade.Helpers.Converters
{
    public class BoolToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                if (parameter is string textOptions)
                {
                    string[] options = textOptions.Split('|');
                    
                    if (options.Length == 2)
                    {
                        return boolValue ? options[0] : options[1];
                    }
                }
                
                return boolValue ? "True" : "False";
            }
            
            return "Undefined";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 