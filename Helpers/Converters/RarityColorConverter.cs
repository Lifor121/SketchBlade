using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using SketchBlade.Models;

namespace SketchBlade.Helpers.Converters
{
    public class RarityColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ItemRarity rarity)
            {
                return GetBrushForRarity(rarity);
            }
            
            return Brushes.White;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
        
        private SolidColorBrush GetBrushForRarity(ItemRarity rarity)
        {
            return rarity switch
            {
                ItemRarity.Common => new SolidColorBrush(Colors.White),
                ItemRarity.Uncommon => new SolidColorBrush(Colors.Green),
                ItemRarity.Rare => new SolidColorBrush(Colors.Blue),
                ItemRarity.Epic => new SolidColorBrush(Colors.Purple),
                ItemRarity.Legendary => new SolidColorBrush(Colors.Orange),
                _ => new SolidColorBrush(Colors.White)
            };
        }
    }
} 