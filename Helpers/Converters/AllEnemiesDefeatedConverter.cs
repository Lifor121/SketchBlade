using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using SketchBlade.Models;

namespace SketchBlade.Helpers.Converters
{
    public class AllEnemiesDefeatedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ObservableCollection<Character> enemies)
            {
                bool allDefeated = enemies.Count > 0 && enemies.All(e => e.IsDefeated);
                
                // Return Visible if all enemies are defeated, otherwise Collapsed
                if (targetType == typeof(Visibility))
                {
                    return allDefeated ? Visibility.Visible : Visibility.Collapsed;
                }
                
                // Return true if all enemies are defeated
                return allDefeated;
            }
            
            // Default to invisible/false
            if (targetType == typeof(Visibility))
            {
                return Visibility.Collapsed;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 