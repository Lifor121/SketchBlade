using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using SketchBlade.Models;
using SketchBlade.ViewModels;

namespace SketchBlade.Helpers.Converters
{
    public class EnemyToSelectedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Character enemy)
            {
                var window = Application.Current.MainWindow;
                if (window.Content is System.Windows.Controls.Frame frame && 
                    frame.Content is System.Windows.Controls.UserControl userControl && 
                    userControl.DataContext is BattleViewModel battleViewModel)
                {
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