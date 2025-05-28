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
            try
            {
                if (values == null || values.Length != 2) 
                    return false;
                
                var character = values[0] as Character;
                var attackingCharacter = values[1] as Character;
                
                // Если любой из персонажей null, возвращаем false
                if (character == null || attackingCharacter == null) 
                    return false;
                
                return ReferenceEquals(character, attackingCharacter);
            }
            catch
            {
                // В случае любой ошибки возвращаем false
                return false;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // Новый конвертер для сравнения персонажа с параметром
    public class CharacterComparisonConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Безопасная проверка на null для избежания ошибок привязки
            if (value == null || parameter == null) 
                return false;
            
            var character = value as Character;
            var targetCharacter = parameter as Character;
            
            // Дополнительная проверка после приведения типов
            if (character == null || targetCharacter == null) 
                return false;
            
            return ReferenceEquals(character, targetCharacter);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 