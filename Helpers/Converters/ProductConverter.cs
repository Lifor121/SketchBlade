using System;
using System.Globalization;
using System.Windows.Data;

namespace SketchBlade.Helpers.Converters
{
    public class ProductConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 3 || values[0] is not double width || values[1] is not double value || values[2] is not double maximum)
            {
                return 0.0;
            }

            if (maximum == 0)
            {
                return 0.0;
            }

            return width * (value / maximum);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 