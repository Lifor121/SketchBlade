using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using System.IO;
using System.Windows;

namespace SketchBlade.Helpers.Converters
{
    public class StringToImageConverter : IValueConverter
    {
        
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string path = value as string;
            
            if (string.IsNullOrEmpty(path))
            {
                Console.WriteLine("StringToImageConverter: Path is null or empty");
                return ImageHelper.GetImageWithFallback(ImageHelper.DefaultSpritePath);
            }
            
            try
            {
                Console.WriteLine($"StringToImageConverter: Converting path to image: {path}");
                
                return ImageHelper.GetImageWithFallback(path);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"StringToImageConverter: Error loading image from path {path}: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return ImageHelper.GetImageWithFallback(ImageHelper.DefaultSpritePath); // Используем def.png
            }
        }
        
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }
} 