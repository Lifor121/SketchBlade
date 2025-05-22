using System;
using System.Globalization;
using System.Windows.Data;
using System.Diagnostics;

namespace SketchBlade.Helpers.Converters
{
    public class CommandParameterConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Console.WriteLine($"CommandParameterConverter: Converting value={value}, parameter={parameter}");
            
            if (value == null && parameter != null)
            {
                Console.WriteLine($"CommandParameterConverter: Using parameter={parameter} instead of null value");
                return parameter;
            }
            
            if (value is string stringValue)
            {
                if (string.IsNullOrEmpty(stringValue) && parameter != null)
                {
                    Console.WriteLine($"CommandParameterConverter: Using parameter={parameter} instead of empty string");
                    return parameter;
                }
                
                if (parameter != null && parameter.ToString() == "ScreenName")
                {
                    string normalized = NormalizeScreenName(stringValue);
                    Console.WriteLine($"CommandParameterConverter: Normalized screen name from '{stringValue}' to '{normalized}'");
                    return normalized;
                }
            }
            
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
        
        private string NormalizeScreenName(string screenName)
        {
            try
            {
                if (string.IsNullOrEmpty(screenName))
                    return "MainMenuView";
                
                if (screenName.EndsWith("View", StringComparison.OrdinalIgnoreCase))
                {
                    return screenName;
                }
                else if (screenName.EndsWith("Screen", StringComparison.OrdinalIgnoreCase))
                {
                    return screenName.Substring(0, screenName.Length - 6) + "View";
                }
                else
                {
                    return screenName + "View";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in NormalizeScreenName: {ex.Message}");
                return screenName;
            }
        }
    }
} 