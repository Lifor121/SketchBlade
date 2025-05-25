using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using System.IO;
using System.Windows;
using SketchBlade.Utilities;

namespace SketchBlade.Helpers.Converters
{
    public class StringToImageConverter : IValueConverter
    {
        
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string path = value as string;
            
            if (string.IsNullOrEmpty(path))
            {
                return ImageHelper.GetImageWithFallback(AssetPaths.DEFAULT_IMAGE);
            }
            
            try
            { 
                return ImageHelper.GetImageWithFallback(path);
            }
            catch (Exception ex)
            {
                return ImageHelper.GetImageWithFallback(AssetPaths.DEFAULT_IMAGE);
            }
        }
        
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }
} 