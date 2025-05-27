using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using SketchBlade.Models;

namespace SketchBlade.Services
{
    /// <summary>
    /// Консолидированный сервис пользовательского интерфейса
    /// Объединяет адаптивный дизайн, настройки UI и переходы экранов
    /// </summary>
    public interface IUIService
    {
        // Adaptive UI
        double GetSize(string sizeKey);
        void SetBaseSize(string sizeKey, double value);
        double GetScaleFactor();
        void SetScaleFactor(double factor);
        
        // Screen Transitions
        Task FadeTransitionAsync(Frame contentFrame, UserControl newScreen, double duration = 0.3);
        Task SlideTransitionAsync(Frame contentFrame, UserControl newScreen, SlideDirection direction = SlideDirection.Left, double duration = 0.3);
        Task SlideLocationTransitionAsync(Panel container, Image newImage, bool slideRight = true, double duration = 0.5);
        
        // UI Settings
        void ApplyLanguage(Language language);
        void ApplyTheme(string themeName);
        void RefreshUI();
        
        // Events
        event PropertyChangedEventHandler? PropertyChanged;
    }

    public enum SlideDirection
    {
        Left,
        Right,
        Up,
        Down
    }

    public class UIService : IUIService, INotifyPropertyChanged
    {
        private static readonly Lazy<UIService> _instance = new(() => new UIService());
        public static UIService Instance => _instance.Value;

        private readonly Dictionary<string, double> _baseSizes = new();
        private double _scaleFactor = 1.0;

        public event PropertyChangedEventHandler? PropertyChanged;

        // Default base sizes for UI elements
        private static readonly Dictionary<string, double> _defaultBaseSizes = new()
        {
            ["FontSize.Small"] = 10,
            ["FontSize.Normal"] = 12,
            ["FontSize.Medium"] = 14,
            ["FontSize.Large"] = 16,
            ["FontSize.Title"] = 18,
            ["FontSize.Header"] = 20,
            ["FontSize.BigHeader"] = 24,
            
            ["Button.Width.Small"] = 60,
            ["Button.Width.Normal"] = 80,
            ["Button.Width.Large"] = 120,
            ["Button.Width.XLarge"] = 140,
            
            ["Button.Height.Small"] = 25,
            ["Button.Height.Normal"] = 30,
            ["Button.Height.Large"] = 35,
            
            ["Margin.Small"] = 5,
            ["Margin.Normal"] = 10,
            ["Margin.Large"] = 15,
            ["Margin.XLarge"] = 20,
            
            ["Padding.Small"] = 5,
            ["Padding.Normal"] = 10,
            ["Padding.Large"] = 15,
            
            ["Border.Thickness"] = 1,
            ["Border.Radius"] = 3,
            
            ["Icon.Small"] = 16,
            ["Icon.Normal"] = 24,
            ["Icon.Large"] = 32,
            
            ["Slot.Size"] = 50,
            ["InventorySlot.Size"] = 60,
        };

        private UIService()
        {
            LoadDefaultSizes();
            // LoggingService.LogDebug("UIService initialized");
        }

        #region Adaptive UI

        public double GetSize(string sizeKey)
        {
            try
            {
                if (_baseSizes.TryGetValue(sizeKey, out var baseSize))
                {
                    return baseSize * _scaleFactor;
                }

                // LoggingService.LogDebug($"Size key not found: {sizeKey}");
                return 12.0; // Default fallback
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error getting size for key '{sizeKey}': {ex.Message}", ex);
                return 12.0;
            }
        }

        public void SetBaseSize(string sizeKey, double value)
        {
            try
            {
                if (value <= 0)
                {
                    LoggingService.LogError("Size value must be positive");
                    return;
                }

                _baseSizes[sizeKey] = value;
                OnPropertyChanged(sizeKey);
                // LoggingService.LogDebug($"Base size set: {sizeKey} = {value}");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error setting base size for '{sizeKey}': {ex.Message}", ex);
            }
        }

        public double GetScaleFactor()
        {
            return _scaleFactor;
        }

        public void SetScaleFactor(double factor)
        {
            try
            {
                if (factor <= 0)
                {
                    LoggingService.LogError("Scale factor must be positive");
                    return;
                }

                if (Math.Abs(_scaleFactor - factor) > 0.001)
                {
                    _scaleFactor = factor;
                    OnPropertyChanged(nameof(GetScaleFactor));
                    
                    // Notify all size changes
                    foreach (var key in _baseSizes.Keys)
                    {
                        OnPropertyChanged(key);
                    }

                    // LoggingService.LogDebug($"Scale factor changed to {factor}");
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error setting scale factor: {ex.Message}", ex);
            }
        }

        #endregion

        #region Screen Transitions

        public async Task FadeTransitionAsync(Frame contentFrame, UserControl newScreen, double duration = 0.3)
        {
            try
            {
                if (contentFrame == null || newScreen == null)
                {
                    LoggingService.LogError("Frame or screen cannot be null");
                    return;
                }

                // LoggingService.LogDebug($"Starting fade transition to {newScreen.GetType().Name}");

                // Create fade out animation
                var fadeOut = new DoubleAnimation
                {
                    From = 1.0,
                    To = 0.0,
                    Duration = TimeSpan.FromSeconds(duration / 2)
                };

                var fadeOutCompleted = false;
                fadeOut.Completed += (s, e) => fadeOutCompleted = true;

                // Start fade out
                contentFrame.BeginAnimation(UIElement.OpacityProperty, fadeOut);

                // Wait for fade out to complete
                while (!fadeOutCompleted)
                {
                    await Task.Delay(16); // ~60 FPS
                }

                // Change content
                contentFrame.Content = newScreen;

                // Create fade in animation
                var fadeIn = new DoubleAnimation
                {
                    From = 0.0,
                    To = 1.0,
                    Duration = TimeSpan.FromSeconds(duration / 2)
                };

                var fadeInCompleted = false;
                fadeIn.Completed += (s, e) => fadeInCompleted = true;

                // Start fade in
                contentFrame.BeginAnimation(UIElement.OpacityProperty, fadeIn);

                // Wait for fade in to complete
                while (!fadeInCompleted)
                {
                    await Task.Delay(16);
                }

                                        // LoggingService.LogDebug("Fade transition completed");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error in fade transition: {ex.Message}", ex);
                
                // Fallback: Direct content change
                if (contentFrame != null && newScreen != null)
                {
                    contentFrame.Content = newScreen;
                    contentFrame.Opacity = 1.0;
                }
            }
        }

        public async Task SlideTransitionAsync(Frame contentFrame, UserControl newScreen, SlideDirection direction = SlideDirection.Left, double duration = 0.3)
        {
            try
            {
                if (contentFrame == null || newScreen == null)
                {
                    LoggingService.LogError("Frame or screen cannot be null");
                    return;
                }

                // LoggingService.LogDebug($"Starting slide transition to {newScreen.GetType().Name}");

                var width = contentFrame.ActualWidth;
                var height = contentFrame.ActualHeight;

                if (width == 0 || height == 0)
                {
                    // Fallback to fade if dimensions not available
                    await FadeTransitionAsync(contentFrame, newScreen, duration);
                    return;
                }

                // Create transform for current content
                var currentTransform = new TranslateTransform();
                if (contentFrame.Content is FrameworkElement currentContent)
                {
                    currentContent.RenderTransform = currentTransform;
                }

                // Create transform for new content
                var newTransform = new TranslateTransform();
                newScreen.RenderTransform = newTransform;

                // Set initial position for new screen
                switch (direction)
                {
                    case SlideDirection.Left:
                        newTransform.X = width;
                        break;
                    case SlideDirection.Right:
                        newTransform.X = -width;
                        break;
                    case SlideDirection.Up:
                        newTransform.Y = height;
                        break;
                    case SlideDirection.Down:
                        newTransform.Y = -height;
                        break;
                }

                // Change content
                contentFrame.Content = newScreen;

                // Create animations
                var currentAnim = CreateSlideAnimation(currentTransform, direction, width, height, duration, true);
                var newAnim = CreateSlideAnimation(newTransform, direction, width, height, duration, false);

                var animationCompleted = false;
                newAnim.Completed += (s, e) => animationCompleted = true;

                // Start animations
                if (direction == SlideDirection.Left || direction == SlideDirection.Right)
                {
                    currentTransform.BeginAnimation(TranslateTransform.XProperty, currentAnim);
                    newTransform.BeginAnimation(TranslateTransform.XProperty, newAnim);
                }
                else
                {
                    currentTransform.BeginAnimation(TranslateTransform.YProperty, currentAnim);
                    newTransform.BeginAnimation(TranslateTransform.YProperty, newAnim);
                }

                // Wait for animation to complete
                while (!animationCompleted)
                {
                    await Task.Delay(16);
                }

                // Clean up transforms
                newScreen.RenderTransform = null;

                // LoggingService.LogDebug("Slide transition completed");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error in slide transition: {ex.Message}", ex);
                
                // Fallback to fade transition
                await FadeTransitionAsync(contentFrame, newScreen, duration);
            }
        }

        public async Task SlideLocationTransitionAsync(Panel container, Image newImage, bool slideRight = true, double duration = 0.5)
        {
            try
            {
                if (container == null || newImage == null)
                {
                    LoggingService.LogError("Container or new image cannot be null");
                    return;
                }

                // LoggingService.LogDebug($"Starting slide location transition to {newImage.GetType().Name}");

                var width = container.ActualWidth;
                var height = container.ActualHeight;

                if (width == 0 || height == 0)
                {
                    // Fallback: просто заменяем изображение
                    if (container.Children.Count > 0 && container.Children[0] is Image fallbackImage)
                    {
                        fallbackImage.Source = newImage.Source;
                    }
                    else
                    {
                        container.Children.Clear();
                        container.Children.Add(newImage);
                    }
                    // LoggingService.LogDebug("Used fallback image replacement due to zero dimensions");
                    return;
                }

                // Находим текущее изображение
                Image? existingImage = null;
                if (container.Children.Count > 0 && container.Children[0] is Image img)
                {
                    existingImage = img;
                }

                // Create transform for current content
                var currentTransform = new TranslateTransform();
                if (existingImage != null)
                {
                    existingImage.RenderTransform = currentTransform;
                }

                // Create transform for new content
                var newTransform = new TranslateTransform();
                newImage.RenderTransform = newTransform;

                // Set initial position for new image
                newTransform.X = slideRight ? width : -width;

                // Add new image to container
                container.Children.Add(newImage);

                // Create animations
                var direction = slideRight ? SlideDirection.Right : SlideDirection.Left;
                var animationTasks = new List<Task>();

                // Анимация для текущего изображения (если есть)
                if (existingImage != null)
                {
                    var currentAnimation = CreateSlideAnimation(currentTransform, direction, width, height, duration, true);
                    var tcs1 = new TaskCompletionSource<bool>();
                    currentAnimation.Completed += (s, e) => tcs1.SetResult(true);
                    currentTransform.BeginAnimation(TranslateTransform.XProperty, currentAnimation);
                    animationTasks.Add(tcs1.Task);
                }

                // Анимация для нового изображения
                var newAnimation = CreateSlideAnimation(newTransform, direction, width, height, duration, false);
                var tcs2 = new TaskCompletionSource<bool>();
                newAnimation.Completed += (s, e) => tcs2.SetResult(true);
                newTransform.BeginAnimation(TranslateTransform.XProperty, newAnimation);
                animationTasks.Add(tcs2.Task);

                // Wait for animations to complete
                await Task.WhenAll(animationTasks);

                // Clean up - remove only the old image, keep everything else
                if (existingImage != null && container.Children.Contains(existingImage))
                {
                    container.Children.Remove(existingImage);
                }

                // Reset transform
                newImage.RenderTransform = null;

                // LoggingService.LogDebug("Slide location transition completed");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error in slide location transition: {ex.Message}", ex);
            }
        }

        public void ApplyTheme(string themeName)
        {
            try
            {
                // LoggingService.LogDebug($"Applying theme: {themeName}");

                Application.Current?.Dispatcher.Invoke(() =>
                {
                    // Apply theme-specific resources
                    switch (themeName.ToLower())
                    {
                        case "dark":
                            ApplyDarkTheme();
                            break;
                        case "light":
                            ApplyLightTheme();
                            break;
                        default:
                            ApplyDefaultTheme();
                            break;
                    }

                    RefreshUI();
                });

                // LoggingService.LogDebug("Theme applied successfully");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error applying theme: {ex.Message}", ex);
            }
        }

        public void RefreshUI()
        {
            try
            {
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    // Force command re-evaluation
                    System.Windows.Input.CommandManager.InvalidateRequerySuggested();
                    
                    // Refresh main window
                    var mainWindow = Application.Current.MainWindow as MainWindow;
                    if (mainWindow != null)
                    {
                        mainWindow.RefreshCurrentScreen();
                        mainWindow.UpdateLayout();
                        mainWindow.InvalidateVisual();
                    }

                    // LoggingService.LogDebug("UI refreshed");
                });
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error refreshing UI: {ex.Message}", ex);
            }
        }

        #endregion

        #region Private Methods

        private void LoadDefaultSizes()
        {
            try
            {
                foreach (var kvp in _defaultBaseSizes)
                {
                    _baseSizes[kvp.Key] = kvp.Value;
                }

                // LoggingService.LogDebug("Default sizes loaded");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error loading default sizes: {ex.Message}", ex);
            }
        }

        private DoubleAnimation CreateSlideAnimation(TranslateTransform transform, SlideDirection direction, double width, double height, double duration, bool isExiting)
        {
            double targetValue = 0;

            if (isExiting)
            {
                // Current content slides out
                switch (direction)
                {
                    case SlideDirection.Left:
                        targetValue = -width;
                        break;
                    case SlideDirection.Right:
                        targetValue = width;
                        break;
                    case SlideDirection.Up:
                        targetValue = -height;
                        break;
                    case SlideDirection.Down:
                        targetValue = height;
                        break;
                }
            }
            else
            {
                // New content slides in to center (0)
                targetValue = 0;
            }

            return new DoubleAnimation
            {
                To = targetValue,
                Duration = TimeSpan.FromSeconds(duration),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };
        }

        private void ApplyDarkTheme()
        {
            try
            {
                // Apply dark theme colors
                var app = Application.Current;
                if (app?.Resources != null)
                {
                    app.Resources["BackgroundBrush"] = new SolidColorBrush(Color.FromRgb(30, 30, 30));
                    app.Resources["ForegroundBrush"] = new SolidColorBrush(Colors.White);
                    app.Resources["AccentBrush"] = new SolidColorBrush(Color.FromRgb(70, 130, 180));
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error applying dark theme: {ex.Message}", ex);
            }
        }

        private void ApplyLightTheme()
        {
            try
            {
                // Apply light theme colors
                var app = Application.Current;
                if (app?.Resources != null)
                {
                    app.Resources["BackgroundBrush"] = new SolidColorBrush(Colors.White);
                    app.Resources["ForegroundBrush"] = new SolidColorBrush(Colors.Black);
                    app.Resources["AccentBrush"] = new SolidColorBrush(Color.FromRgb(0, 120, 215));
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error applying light theme: {ex.Message}", ex);
            }
        }

        private void ApplyDefaultTheme()
        {
            try
            {
                // Apply default theme
                var app = Application.Current;
                if (app?.Resources != null)
                {
                    app.Resources["BackgroundBrush"] = new SolidColorBrush(Color.FromRgb(240, 240, 240));
                    app.Resources["ForegroundBrush"] = new SolidColorBrush(Colors.Black);
                    app.Resources["AccentBrush"] = new SolidColorBrush(Color.FromRgb(0, 120, 215));
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error applying default theme: {ex.Message}", ex);
            }
        }

        public void ApplyLanguage(Language language)
        {
            try
            {
                // LoggingService.LogDebug($"Applying language: {language}");
                
                // Set language in localization service
                LocalizationService.Instance.CurrentLanguage = language;
                
                // Refresh UI on main thread
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    RefreshUI();
                });

                // LoggingService.LogDebug("Language applied successfully");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error applying language: {ex.Message}", ex);
            }
        }

        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
} 
