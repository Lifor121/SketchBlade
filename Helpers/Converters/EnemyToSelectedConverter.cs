using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using SketchBlade.Models;
using SketchBlade.ViewModels;

namespace SketchBlade.Helpers.Converters
{
    /// <summary>
    /// Converter to determine if an enemy is the currently selected enemy.
    /// </summary>
    public class EnemyToSelectedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Character enemy)
            {
                // Get the BattleViewModel from the element's DataContext in the visual tree
                var window = Application.Current.MainWindow;
                if (window.Content is System.Windows.Controls.Frame frame && 
                    frame.Content is System.Windows.Controls.UserControl userControl && 
                    userControl.DataContext is BattleViewModel battleViewModel)
                {
                    // Compare the enemy with the selected enemy
                    return enemy == battleViewModel.SelectedEnemy;
                }
            }
            
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 