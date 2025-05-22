using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Threading.Tasks;
using System.Windows.Media;
using SketchBlade.Views;

namespace SketchBlade.Services
{
    /// <summary>
    /// Класс для анимированного перехода между экранами
    /// </summary>
    public class ScreenTransitionService
    {
        private const double DefaultTransitionDuration = 0.3; // seconds
        
        /// <summary>
        /// Выполняет анимированный переход между экранами
        /// </summary>
        /// <param name="contentFrame">The frame that hosts the content</param>
        /// <param name="newScreen">The new screen to show</param>
        /// <param name="duration">The duration of the transition in seconds</param>
        /// <returns>Task that completes when the transition is done</returns>
        public async Task FadeTransitionAsync(Frame contentFrame, UserControl newScreen, double duration = DefaultTransitionDuration)
        {
            Console.WriteLine("FadeTransitionAsync: начало");
            
            if (contentFrame == null || newScreen == null)
            {
                Console.WriteLine($"FadeTransitionAsync: ошибка - contentFrame или newScreen равны null");
                return;
            }
            
            try
            {
                // Специальная обработка для перехода с BattleView на WorldMapView
                // Проверяем тип перехода
                if (contentFrame.Content is UserControl currentScreen && 
                    currentScreen.GetType().Name == "BattleView" && 
                    newScreen.GetType().Name == "WorldMapView")
                {
                    Console.WriteLine("FadeTransitionAsync: обнаружен переход BattleView -> WorldMapView");
                    
                    // Принудительно очищаем текущее содержимое
                    currentScreen.Visibility = Visibility.Collapsed;
                    Console.WriteLine("FadeTransitionAsync: скрыт BattleView");
                    
                    // Устанавливаем новое содержимое без анимации
                    contentFrame.Content = newScreen;
                    newScreen.Visibility = Visibility.Visible;
                    newScreen.Opacity = 1.0;
                    
                    // Обновляем разметку для гарантии видимости
                    contentFrame.UpdateLayout();
                    newScreen.UpdateLayout();
                    
                    Console.WriteLine("FadeTransitionAsync: выполнен прямой переход без анимации");
                    return; // Выходим из метода, переход уже выполнен
                }
                
                // Сохраняем старое содержимое
                object oldContent = contentFrame.Content;
                
                // Устанавливаем начальную прозрачность нового экрана
                newScreen.Opacity = 0;
                
                // Проверка: если это обычный переход (не с боевого экрана)
                if (oldContent is UserControl oldControl && oldControl.GetType().Name == "BattleView")
                {
                    // Сразу скрываем BattleView, чтобы гарантировать его невидимость
                    oldControl.Visibility = Visibility.Collapsed;
                    Console.WriteLine("FadeTransitionAsync: скрыт BattleView в случае обычного перехода");
                }
                
                // Устанавливаем новое содержимое в contentFrame
                contentFrame.Content = newScreen;
                
                // Задаем полную видимость нового экрана
                newScreen.Visibility = Visibility.Visible;
                
                // Создаем анимацию появления
                DoubleAnimation fadeInAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(duration)
                };
                
                // Запускаем анимацию и ждем ее завершения
                newScreen.BeginAnimation(UIElement.OpacityProperty, fadeInAnimation);
                
                // Дожидаемся завершения анимации
                await Task.Delay(TimeSpan.FromSeconds(duration));
                
                // Установка окончательной прозрачности (чтобы не зависеть от анимации)
                newScreen.Opacity = 1;
                
                // Обновляем разметку для гарантии видимости
                contentFrame.UpdateLayout();
                newScreen.UpdateLayout();
                
                Console.WriteLine($"FadeTransitionAsync: переход успешно завершен на {newScreen.GetType().Name}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FadeTransitionAsync: ошибка при переходе - {ex.Message}");
                
                // Аварийная установка содержимого без анимации
                try
                {
                    contentFrame.Content = newScreen;
                    newScreen.Opacity = 1;
                    Console.WriteLine("FadeTransitionAsync: аварийная установка содержимого без анимации");
                }
                catch (Exception fallbackEx)
                {
                    Console.WriteLine($"FadeTransitionAsync: критическая ошибка - {fallbackEx.Message}");
                }
            }
        }
        
        /// <summary>
        /// Выполняет анимированный переход между локациями
        /// </summary>
        /// <param name="contentControl">The control hosting the content</param>
        /// <param name="newContent">The new content to display</param>
        /// <param name="slideFromRight">True for right-to-left, False for left-to-right</param>
        /// <param name="duration">The duration of the transition in seconds</param>
        /// <returns>Task that completes when the transition is done</returns>
        public async Task SlideLocationTransitionAsync(ContentControl contentControl, object newContent, bool slideFromRight, double duration = DefaultTransitionDuration)
        {
            if (contentControl == null || newContent == null)
            {
                Console.WriteLine("SlideLocationTransitionAsync: contentControl or newContent is null");
                if (contentControl != null && newContent != null)
                {
                    contentControl.Content = newContent;
                }
                return;
            }

            try
            {
                // Check if content control has actual width
                if (contentControl.ActualWidth <= 0)
                {
                    // If the control doesn't have a width yet, just set the content and return
                    contentControl.Content = newContent;
                    return;
                }
            
                // Создаем контейнер для нового содержимого
                ContentControl tempContainer = new ContentControl
                {
                    Content = newContent,
                    RenderTransform = new TranslateTransform(slideFromRight ? contentControl.ActualWidth : -contentControl.ActualWidth, 0)
                };

                // Сохраняем старое содержимое (если оно есть)
                object oldContent = contentControl.Content;
                
                // Skip animation if old content is null or same as new content
                if (oldContent == null || ReferenceEquals(oldContent, newContent))
                {
                    contentControl.Content = newContent;
                    return;
                }

                // Создаем анимации для обоих контейнеров
                DoubleAnimation oldContentAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = slideFromRight ? -contentControl.ActualWidth : contentControl.ActualWidth,
                    Duration = new Duration(TimeSpan.FromSeconds(duration))
                };

                DoubleAnimation newContentAnimation = new DoubleAnimation
                {
                    From = slideFromRight ? contentControl.ActualWidth : -contentControl.ActualWidth,
                    To = 0,
                    Duration = new Duration(TimeSpan.FromSeconds(duration))
                };

                ContentControl oldContainer = new ContentControl
                {
                    Content = oldContent,
                    RenderTransform = new TranslateTransform(0, 0)
                };

                // Создаем Grid для размещения старого и нового содержимого
                Grid animationGrid = new Grid();
                animationGrid.Children.Add(oldContainer);
                animationGrid.Children.Add(tempContainer);

                // Заменяем содержимое на наш grid с анимацией
                contentControl.Content = animationGrid;

                // Запускаем анимации
                TranslateTransform oldTransform = (TranslateTransform)oldContainer.RenderTransform;
                TranslateTransform newTransform = (TranslateTransform)tempContainer.RenderTransform;

                oldTransform.BeginAnimation(TranslateTransform.XProperty, oldContentAnimation);
                newTransform.BeginAnimation(TranslateTransform.XProperty, newContentAnimation);

                // Ждем завершения анимации
                await Task.Delay(TimeSpan.FromMilliseconds(duration * 1000));

                // Устанавливаем окончательное содержимое
                contentControl.Content = newContent;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SlideLocationTransitionAsync: ошибка - {ex.Message}");
                Console.WriteLine($"SlideLocationTransitionAsync: стек вызовов - {ex.StackTrace}");
                // В случае ошибки просто показываем новое содержимое без анимации
                contentControl.Content = newContent;
            }
        }
        
        /// <summary>
        /// Создает анимированное появление специальных эффектов для боя
        /// </summary>
        /// <param name="targetElement">Элемент для анимации</param>
        /// <param name="effectType">Тип эффекта</param>
        /// <returns>Task that completes when the animation is done</returns>
        public async Task AnimateBattleEffectAsync(FrameworkElement targetElement, string effectType, double duration = 0.5)
        {
            if (targetElement == null)
                return;

            try
            {
                Storyboard storyboard = new Storyboard();

                switch (effectType.ToLower())
                {
                    case "critical":
                        // Красное свечение для критических ударов
                        ColorAnimation colorAnimation = new ColorAnimation
                        {
                            From = Colors.White,
                            To = Colors.Red,
                            Duration = new Duration(TimeSpan.FromSeconds(duration / 2)),
                            AutoReverse = true
                        };

                        Storyboard.SetTarget(colorAnimation, targetElement);
                        Storyboard.SetTargetProperty(colorAnimation, new PropertyPath("(Border.Background).(SolidColorBrush.Color)"));
                        storyboard.Children.Add(colorAnimation);
                        break;

                    case "victory":
                        // Увеличение и подпрыгивание для победы
                        DoubleAnimation scaleXAnimation = new DoubleAnimation
                        {
                            From = 1.0,
                            To = 1.2,
                            Duration = new Duration(TimeSpan.FromSeconds(duration / 2)),
                            AutoReverse = true,
                            RepeatBehavior = new RepeatBehavior(2)
                        };

                        DoubleAnimation scaleYAnimation = new DoubleAnimation
                        {
                            From = 1.0,
                            To = 1.2,
                            Duration = new Duration(TimeSpan.FromSeconds(duration / 2)),
                            AutoReverse = true,
                            RepeatBehavior = new RepeatBehavior(2)
                        };

                        Storyboard.SetTarget(scaleXAnimation, targetElement);
                        Storyboard.SetTarget(scaleYAnimation, targetElement);
                        Storyboard.SetTargetProperty(scaleXAnimation, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));
                        Storyboard.SetTargetProperty(scaleYAnimation, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));

                        storyboard.Children.Add(scaleXAnimation);
                        storyboard.Children.Add(scaleYAnimation);
                        break;

                    // Другие типы эффектов...
                }

                storyboard.Begin();
                await Task.Delay(TimeSpan.FromSeconds(duration));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AnimateBattleEffectAsync: ошибка - {ex.Message}");
            }
        }
    }
} 