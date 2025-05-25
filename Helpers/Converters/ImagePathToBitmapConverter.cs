using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace SketchBlade.Helpers.Converters
{
    public class ImagePathToBitmapConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value is string imagePath && !string.IsNullOrEmpty(imagePath))
                {
                    var result = ImageHelper.GetImageWithFallback(imagePath);                    
                    return result;
                }
                
                return ImageHelper.GetDefaultImage();
            }
            catch (Exception ex)
            {
                return ImageHelper.GetDefaultImage();
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 