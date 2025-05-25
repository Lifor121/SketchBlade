using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using SketchBlade.Services;

namespace SketchBlade.ViewModels
{
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected void LogInfo(string message, [CallerMemberName] string? context = null)
        {
            LoggingService.LogInfo($"[{context ?? GetType().Name}] {message}");
        }

        protected void LogDebug(string message, [CallerMemberName] string? context = null)
        {
            LoggingService.LogDebug($"[{context ?? GetType().Name}] {message}");
        }

        protected void LogWarning(string message, [CallerMemberName] string? context = null)
        {
            LoggingService.LogError($"[{context ?? GetType().Name}] WARNING: {message}");
        }

        protected void LogError(string message, System.Exception? exception = null, [CallerMemberName] string? context = null)
        {
            var fullMessage = $"[{context ?? GetType().Name}] {message}";
            if (exception != null)
            {
                LoggingService.LogError(fullMessage, exception);
            }
            else
            {
                LoggingService.LogError(fullMessage);
            }
        }

        protected void ShowNotification(string message, NotificationType type = NotificationType.Info)
        {
            switch (type)
            {
                case NotificationType.Info:
                    NotificationService.Instance.ShowInfo(message);
                    break;
                case NotificationType.Success:
                    NotificationService.Instance.ShowSuccess(message);
                    break;
                case NotificationType.Warning:
                    NotificationService.Instance.ShowWarning(message);
                    break;
                case NotificationType.Error:
                    NotificationService.Instance.ShowError(message);
                    break;
            }
        }
    }
} 
