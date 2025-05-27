using System;
using System.Globalization;
using System.Windows.Data;
using SketchBlade.Services;

namespace SketchBlade.Helpers.Converters
{
    public class DebugLoggingConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string parameterStr = parameter?.ToString() ?? "unknown";
            LoggingService.LogDebug($"DebugLoggingConverter [{parameterStr}]: value = {value}");
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
} 