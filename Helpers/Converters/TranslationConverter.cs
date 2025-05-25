using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using SketchBlade.Services;
using System.ComponentModel;

namespace SketchBlade.Helpers.Converters
{
    public class TranslationConverter : IValueConverter, IMultiValueConverter, INotifyPropertyChanged
    {
        private static readonly object _lock = new object();
        private static int _version = 0;
        private static event PropertyChangedEventHandler? _staticPropertyChanged;
        
        public event PropertyChangedEventHandler? PropertyChanged
        {
            add { _staticPropertyChanged += value; }
            remove { _staticPropertyChanged -= value; }
        }
        
        static TranslationConverter()
        {
            LocalizationService.Instance.LanguageChanged += (s, e) =>
            {
                lock (_lock)
                {
                    _version++;
                }
                
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    _staticPropertyChanged?.Invoke(null, new PropertyChangedEventArgs("Translation"));
                    System.Windows.Input.CommandManager.InvalidateRequerySuggested();
                });
            };
        }
        
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                string key = GetTranslationKey(value, parameter);
                
                if (string.IsNullOrEmpty(key))
                {
                    return string.Empty;
                }
                
                string translation = LocalizationService.Instance.GetTranslation(key);
                return translation;
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"TranslationConverter error: {ex.Message}");
                return value?.ToString() ?? string.Empty;
            }
        }
        
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values?.Length > 0)
            {
                return Convert(values[0], targetType, parameter, culture);
            }
            return string.Empty;
        }
        
        private string GetTranslationKey(object value, object parameter)
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
            
            return key ?? string.Empty;
        }
        
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
        
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 