using System;
using System.Globalization;
using System.Windows.Data;
using SketchBlade.Services;

namespace SketchBlade.Helpers.Converters
{
    public class TranslationConverter : IValueConverter
    {
        private static readonly object _lock = new object();
        private static int _version = 0;
        
        static TranslationConverter()
        {
            LanguageService.LanguageChanged += (s, e) =>
            {
                lock (_lock)
                {
                    _version++;
                }
            };
        }
        
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                string key;
                
                if (parameter != null && value != null)
                {
                    string format = parameter.ToString();
                    if (format.Contains("{0}"))
                    {
                        key = string.Format(format, value.ToString());
                    }
                    else
                    {
                        key = parameter.ToString();
                    }
                }
                else if (value == null)
                {
                    return string.Empty;
                }
                else
                {
                    key = value.ToString();
                }
                
                if (string.IsNullOrEmpty(key))
                {
                    return string.Empty;
                }
                
                int currentVersion;
                lock (_lock)
                {
                    currentVersion = _version;
                }
                
                string translation = LanguageService.GetTranslation(key);
                return translation;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"TranslationConverter: Error converting value: {ex.Message}");
                return value?.ToString() ?? string.Empty;
            }
        }
        
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 