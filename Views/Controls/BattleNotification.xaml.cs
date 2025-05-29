using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace SketchBlade.Views.Controls
{
    public partial class BattleNotification : UserControl
    {
        private Storyboard _showStoryboard;
        private Storyboard _hideStoryboard;
        
        public BattleNotification()
        {
            InitializeComponent();
            Loaded += BattleNotification_Loaded;
        }
        
        private void BattleNotification_Loaded(object sender, RoutedEventArgs e)
        {
            CreateAnimations();
        }
        
        private void CreateAnimations()
        {
            // Создаем анимацию появления
            _showStoryboard = new Storyboard();
            
            var showOpacityAnimation = new DoubleAnimation
            {
                From = 0,
                To = 0.85,
                Duration = new Duration(TimeSpan.FromMilliseconds(400)),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(showOpacityAnimation, NotificationBorder);
            Storyboard.SetTargetProperty(showOpacityAnimation, new PropertyPath("Opacity"));
            _showStoryboard.Children.Add(showOpacityAnimation);
            
            // Создаем анимацию скрытия
            _hideStoryboard = new Storyboard();
            
            var hideOpacityAnimation = new DoubleAnimation
            {
                From = 0.85,
                To = 0,
                Duration = new Duration(TimeSpan.FromMilliseconds(600)),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };
            Storyboard.SetTarget(hideOpacityAnimation, NotificationBorder);
            Storyboard.SetTargetProperty(hideOpacityAnimation, new PropertyPath("Opacity"));
            _hideStoryboard.Children.Add(hideOpacityAnimation);
            
            // Обработчик завершения анимации скрытия
            _hideStoryboard.Completed += (s, args) => {
                this.Visibility = Visibility.Collapsed;
            };
        }
        
        public async Task ShowNotification(string message, BattleNotificationType type = BattleNotificationType.Info, int durationMs = 1800)
        {
            if (string.IsNullOrEmpty(message)) return;
            
            try
            {
                // Устанавливаем текст и цвет в зависимости от типа
                NotificationText.Text = message;
                SetNotificationStyle(type);
                
                // Показываем контрол
                this.Visibility = Visibility.Visible;
                
                // Запускаем анимацию появления
                _showStoryboard?.Begin();
                
                // Ждем указанное время, затем скрываем
                await Task.Delay(durationMs);
                
                // Запускаем анимацию скрытия
                _hideStoryboard?.Begin();
            }
            catch (Exception ex)
            {
                // В случае ошибки просто скрываем уведомление
                HideImmediately();
            }
        }
        
        public void HideImmediately()
        {
            try
            {
                _showStoryboard?.Stop();
                _hideStoryboard?.Stop();
                this.Visibility = Visibility.Collapsed;
                if (NotificationBorder != null)
                {
                    NotificationBorder.Opacity = 0;
                }
            }
            catch
            {
                // Игнорируем ошибки при принудительном скрытии
            }
        }
        
        private void SetNotificationStyle(BattleNotificationType type)
        {
            try
            {
                switch (type)
                {
                    case BattleNotificationType.Damage:
                        NotificationBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(180, 80, 80)); // Приглушенный красный
                        NotificationBorder.Background = new SolidColorBrush(Color.FromArgb(153, 60, 20, 20)); // Темно-красный с прозрачностью
                        break;
                        
                    case BattleNotificationType.Healing:
                        NotificationBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(100, 160, 100)); // Приглушенный зеленый
                        NotificationBorder.Background = new SolidColorBrush(Color.FromArgb(153, 20, 60, 20)); // Темно-зеленый с прозрачностью
                        break;
                        
                    case BattleNotificationType.Critical:
                        NotificationBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(200, 180, 100)); // Приглушенный золотой
                        NotificationBorder.Background = new SolidColorBrush(Color.FromArgb(153, 80, 60, 20)); // Темно-золотой с прозрачностью
                        break;
                        
                    case BattleNotificationType.Victory:
                        NotificationBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(120, 180, 120)); // Мягкий зеленый
                        NotificationBorder.Background = new SolidColorBrush(Color.FromArgb(153, 30, 70, 30)); // Зеленый с прозрачностью
                        break;
                        
                    case BattleNotificationType.Defeat:
                        NotificationBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(160, 80, 80)); // Приглушенный красный
                        NotificationBorder.Background = new SolidColorBrush(Color.FromArgb(153, 70, 20, 20)); // Темно-красный с прозрачностью
                        break;
                        
                    case BattleNotificationType.Info:
                    default:
                        NotificationBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(120, 120, 120)); // Серый
                        NotificationBorder.Background = new SolidColorBrush(Color.FromArgb(153, 40, 40, 40)); // Темно-серый с прозрачностью
                        break;
                }
            }
            catch
            {
                // Игнорируем ошибки стилизации
            }
        }
    }
    
    public enum BattleNotificationType
    {
        Info,
        Damage,
        Healing,
        Critical,
        Victory,
        Defeat
    }
} 