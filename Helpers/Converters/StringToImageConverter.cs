using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using System.IO;
using System.Windows;

namespace SketchBlade.Helpers.Converters
{
    /// <summary>
    /// Конвертер, преобразующий строковый путь к изображению в BitmapImage
    /// </summary>
    public class StringToImageConverter : IValueConverter
    {
        // Кэшированное изображение по умолчанию больше не нужно, так как оно уже кэшируется в ImageHelper
        
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string path = value as string;
            
            // Если путь не задан, возвращаем изображение по умолчанию из ImageHelper
            if (string.IsNullOrEmpty(path))
            {
                Console.WriteLine("StringToImageConverter: Path is null or empty");
                return ImageHelper.GetImageWithFallback(ImageHelper.DefaultSpritePath); // Используем def.png
            }
            
            try
            {
                Console.WriteLine($"StringToImageConverter: Converting path to image: {path}");
                
                // Всегда используем GetImageWithFallback вместо LoadImage, чтобы гарантировать использование def.png
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
            // Преобразование BitmapImage в строку не поддерживается
            return DependencyProperty.UnsetValue;
        }
    }
} 