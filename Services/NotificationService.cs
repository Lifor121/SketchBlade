using System.Windows;

namespace SketchBlade.Services
{
    /// <summary>
    /// Сервис уведомлений пользователя
    /// </summary>
    public interface INotificationService
    {
        void ShowInfo(string message, string title = "Информация");
        void ShowSuccess(string message, string title = "Успех");
        void ShowWarning(string message, string title = "Предупреждение");
        void ShowError(string message, string title = "Ошибка");
        bool ShowConfirmation(string message, string title = "Подтверждение");
    }

    public class NotificationService : INotificationService
    {
        private static readonly Lazy<NotificationService> _instance = new(() => new NotificationService());
        public static NotificationService Instance => _instance.Value;

        private NotificationService()
        {
            LoggingService.LogDebug("NotificationService initialized");
        }

        public void ShowInfo(string message, string title = "Информация")
        {
            ShowMessage(message, title, MessageBoxImage.Information);
        }

        public void ShowSuccess(string message, string title = "Успех")
        {
            ShowMessage(message, title, MessageBoxImage.Information);
        }

        public void ShowWarning(string message, string title = "Предупреждение")
        {
            ShowMessage(message, title, MessageBoxImage.Warning);
        }

        public void ShowError(string message, string title = "Ошибка")
        {
            ShowMessage(message, title, MessageBoxImage.Error);
            LoggingService.LogError($"Error notification: {message}");
        }

        public bool ShowConfirmation(string message, string title = "Подтверждение")
        {
            try
            {
                var result = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
                return result == MessageBoxResult.Yes;
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error showing confirmation dialog: {ex.Message}", ex);
                return false;
            }
        }

        private void ShowMessage(string message, string title, MessageBoxImage icon)
        {
            try
            {
                MessageBox.Show(message, title, MessageBoxButton.OK, icon);
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error showing message dialog: {ex.Message}", ex);
            }
        }
    }
} 