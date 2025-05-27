using System;
using System.Globalization;
using System.Windows.Data;
using SketchBlade.Models;

namespace SketchBlade.Helpers.Converters
{
    public class AttackingCharacterConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length != 2) return false;
            
            var character = values[0] as Character;
            var attackingCharacter = values[1] as Character;
            
            if (character == null || attackingCharacter == null) return false;
            
            return ReferenceEquals(character, attackingCharacter);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 