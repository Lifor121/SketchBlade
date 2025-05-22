using System;
using System.Windows;
using System.Windows.Controls;
using SketchBlade.Models;
using SketchBlade.ViewModels;

namespace SketchBlade.Views
{
    public partial class MainMenuView : UserControl
    {
        private MainViewModel? ViewModel => this.DataContext as MainViewModel;
        
        public MainMenuView()
        {
            InitializeComponent();
            
            this.Loaded += MainMenuView_Loaded;
        }
        
        private void MainMenuView_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            var window = Window.GetWindow(this);
            
            if (window != null)
            {
                if (window.DataContext is MainViewModel)
                {
                    this.DataContext = window.DataContext;
                }
            }
        }
        
        private void NewGameButton_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null)
            {
                ViewModel.NewGameCommand.Execute(null);
            }
            else
            {
                var window = Window.GetWindow(this);
                if (window?.DataContext is MainViewModel vm)
                {
                    vm.NewGameCommand.Execute(null);
                }
            }
        }
        
        private void ContinueGameButton_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null)
            {
                ViewModel.ContinueGameCommand.Execute(null);
            }
            else
            {
                var window = Window.GetWindow(this);
                if (window?.DataContext is MainViewModel vm)
                {
                    vm.ContinueGameCommand.Execute(null);
                }
            }
        }
        
        private void OptionsButton_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null)
            {
                ViewModel.OptionsCommand.Execute(null);
            }
            else
            {
                var window = Window.GetWindow(this);
                if (window?.DataContext is MainViewModel vm)
                {
                    vm.OptionsCommand.Execute(null);
                }
            }
        }
        
        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null)
            {
                ViewModel.ExitGameCommand.Execute(null);
            }
            else
            {
                var window = Window.GetWindow(this);
                if (window?.DataContext is MainViewModel vm)
                {
                    vm.ExitGameCommand.Execute(null);
                }
                else
                {
                    Application.Current.Shutdown();
                }
            }
        }
    }
}
