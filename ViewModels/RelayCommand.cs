using System;
using System.Windows.Input;
using System.Windows;
using SketchBlade.Services;

namespace SketchBlade.ViewModels
{
    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Predicate<object?>? _canExecute;
        private readonly string _commandName;

        public RelayCommand(Action execute, string commandName = "Unnamed") 
            : this(p => execute(), null, commandName)
        {
        }

        public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null, string commandName = "Unnamed")
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
            _commandName = commandName;
        }

        public bool CanExecute(object? parameter)
        {
            bool result = _canExecute == null || _canExecute(parameter);
            return result;
        }

        public void Execute(object? parameter)
        {
            try
            {
                _execute(parameter);
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"RelayCommand({_commandName}).Execute({parameter}) - ERROR: {ex.Message}", ex);
                
                MessageBox.Show($"Ошибка выполнения команды: {ex.Message}", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }

    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T?> _execute;
        private readonly Predicate<T?>? _canExecute;
        private readonly string _commandName;

        public RelayCommand(Action<T?> execute, Predicate<T?>? canExecute = null, string commandName = "Unnamed")
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
            _commandName = commandName;
        }

        public bool CanExecute(object? parameter)
        {
            bool result = parameter == null || 
                   parameter is T t && (_canExecute == null || _canExecute(t));
            return result;
        }

        public void Execute(object? parameter)
        {
            try
            {
                if (parameter == null)
                {
                    _execute(default);
                }
                else if (parameter is T t)
                {
                    _execute(t);
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"RelayCommand<{typeof(T).Name}>({_commandName}).Execute({parameter}) - ERROR: {ex.Message}", ex);
                
                MessageBox.Show($"Ошибка выполнения команды: {ex.Message}", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
} 