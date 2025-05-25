using System;
using System.Windows;
using System.Windows.Controls;
using SketchBlade.ViewModels;
using System.Windows.Input;
using System.Windows.Data;
using System.Windows.Threading;

namespace SketchBlade.Views
{
    /// <summary>
    /// Interaction logic for SettingsView.xaml
    /// </summary>
    public partial class SettingsView : UserControl
    {
        private SettingsViewModel? ViewModel => this.DataContext as SettingsViewModel;
        
        public SettingsView()
        {
            InitializeComponent();
        }
        
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var mainWindow = Application.Current.MainWindow as MainWindow;
                if (mainWindow != null)
                {
                    mainWindow.NavigateToScreen("MainMenuView");
                }
                else
                {
                    if (DataContext is SettingsViewModel viewModel && viewModel.NavigateCommand?.CanExecute("MainMenuView") == true)
                    {
                        viewModel.NavigateCommand.Execute("MainMenuView");
                    }
                    else
                    {
                        MessageBox.Show("Unable to navigate back. Please restart the application.");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error navigating back: {ex.Message}");
            }
        }
        
        private void SettingsView_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    BackButton_Click(sender, e);
                    e.Handled = true;
                    break;
            }
        }
        
        private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ViewModel != null)
            {
                // Force refresh of the UI after language selection
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    BindingOperations.GetBindingExpression(sender as ComboBox, ComboBox.SelectedItemProperty)?.UpdateTarget();
                    UpdateLayout();
                }), DispatcherPriority.DataBind);
            }
        }
        
        private void DifficultyComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ViewModel != null)
            {
                // Force refresh of the UI after difficulty selection
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    BindingOperations.GetBindingExpression(sender as ComboBox, ComboBox.SelectedItemProperty)?.UpdateTarget();
                    UpdateLayout();
                }), DispatcherPriority.DataBind);
            }
        }
    }
} 