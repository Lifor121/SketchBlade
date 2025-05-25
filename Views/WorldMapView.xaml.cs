using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using SketchBlade.Services;
using SketchBlade.ViewModels;
using System.Threading.Tasks;
using System;
using SketchBlade;
using System.Windows.Input;
using SketchBlade.Views;
using System.Reflection;
using SketchBlade.Models;
using System.Windows.Data;
using System.Windows.Threading;
using SketchBlade.Helpers;
using SketchBlade.Utilities;
using SketchBlade.Views.Helpers;

namespace SketchBlade.Views
{
    /// <summary>
    /// Interaction logic for WorldMapView.xaml
    /// </summary>
    public partial class WorldMapView : UserControl
    {
        private LocationChangedEventArgs? _pendingLocationChange;
        private bool _isProcessingLocationChange = false; // Флаг для предотвращения множественных обработок
        
        public WorldMapView()
        {
            InitializeComponent();
            
            // Subscribe to the ViewModel's location changed event when loaded
            Loaded += WorldMapView_Loaded;
            
            // Subscribe to unloaded event to clean up
            Unloaded += WorldMapView_Unloaded;
        }
        
        private void WorldMapView_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                LoggingService.LogDebug("=== WorldMapView_Loaded START ===");
                
                // Subscribe to location change events if the data context is MapViewModel
                if (DataContext is MapViewModel viewModel)
                {
                    LoggingService.LogDebug("DataContext is MapViewModel, subscribing to events");
                    viewModel.LocationChanged += OnLocationChanged;
                    
                    // Load the current location image
                    LoadCurrentLocationImage();
                    
                    // Apply any pending location change
                    if (_pendingLocationChange != null)
                    {
                        LoggingService.LogDebug("Applying pending location change");
                        OnLocationChanged(this, _pendingLocationChange);
                        _pendingLocationChange = null;
                    }
                    
                    viewModel.RefreshLocations();
                    
                    // Log initial UI state
                    LogUIState("After initial load");
                }
                else
                {
                    LoggingService.LogDebug("DataContext is NOT MapViewModel");
                }
                
                LoggingService.LogDebug("=== WorldMapView_Loaded END ===");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error in WorldMapView_Loaded: {ex.Message}", ex);
            }
        }
        
        private void WorldMapView_Unloaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Unsubscribe from events to prevent memory leaks
                if (DataContext is MapViewModel viewModel)
                {
                    viewModel.LocationChanged -= OnLocationChanged;
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error in WorldMapView_Unloaded: {ex.Message}", ex);
            }
        }
        
        private void LoadCurrentLocationImage()
        {
            try
            {
                if (DataContext is MapViewModel viewModel && viewModel.CurrentLocation != null)
                {
                    if (string.IsNullOrEmpty(viewModel.CurrentLocation.SpritePath))
                    {
                        string locationTypeName = viewModel.CurrentLocation.Type.ToString().ToLower();
                        string typePath = AssetPaths.Locations.GetLocationPath(locationTypeName);
                        
                        string fullPath = System.IO.Path.Combine(
                            AppDomain.CurrentDomain.BaseDirectory, 
                            typePath
                        );
                        
                        if (System.IO.File.Exists(fullPath))
                        {
                            viewModel.CurrentLocation.SpritePath = typePath;
                        }
                        else
                        {
                            viewModel.CurrentLocation.SpritePath = AssetPaths.DEFAULT_IMAGE;
                        }
                    }
                    
                    ResourceService.Instance.GetImage(viewModel.CurrentLocation.SpritePath);
                    
                    Dispatcher.BeginInvoke(new Action(() => {
                        try
                        {
                            LocationImage.SetBinding(Image.SourceProperty, new Binding("CurrentLocation.SpritePath")
                            {
                                Converter = FindResource("StringToImageConverter") as IValueConverter,
                                FallbackValue = FindResource("DefaultImage"),
                                TargetNullValue = FindResource("DefaultImage")
                            });
                        }
                        catch (Exception ex)
                        {
                            LoggingService.LogError($"Error setting image binding: {ex.Message}", ex);
                            try
                            {
                                LocationImage.Source = ResourceService.Instance.GetImage(AssetPaths.DEFAULT_IMAGE);
                            }
                            catch { }
                        }
                    }));
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error in LoadCurrentLocationImage: {ex.Message}", ex);
            }
        }
        
        private void OnLocationChanged(object sender, LocationChangedEventArgs e)
        {
            // Предотвращаем множественные обработки одного и того же события
            if (_isProcessingLocationChange)
            {
                LoggingService.LogDebug("OnLocationChanged: Уже обрабатывается смена локации, пропускаем");
                return;
            }
            
            _isProcessingLocationChange = true;
            
            try
            {
                LoggingService.LogDebug("=== ОБРАБОТКА СМЕНЫ ЛОКАЦИИ ===");
                LoggingService.LogDebug($"Направление: {e.Direction}");
                LoggingService.LogDebug($"Новая локация: {e.NewLocation.Name} ({e.NewLocation.Type})");
                LoggingService.LogDebug($"Путь к спрайту (до проверки): {e.NewLocation.SpritePath}");
                
                if (string.IsNullOrEmpty(e.NewLocation.SpritePath))
                {
                    string locationTypeName = e.NewLocation.Type.ToString().ToLower();
                    string typePath = AssetPaths.Locations.GetLocationPath(locationTypeName);
                    
                    LoggingService.LogDebug($"Спрайт путь пустой, создаем новый: {typePath}");
                    
                    string fullPath = System.IO.Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory, 
                        typePath
                    );
                    
                    LoggingService.LogDebug($"Полный путь: {fullPath}");
                    LoggingService.LogDebug($"Файл существует: {System.IO.File.Exists(fullPath)}");
                    
                    if (System.IO.File.Exists(fullPath))
                    {
                        e.NewLocation.SpritePath = typePath;
                        LoggingService.LogDebug($"Установлен путь к спрайту: {typePath}");
                    }
                    else
                    {
                        e.NewLocation.SpritePath = AssetPaths.DEFAULT_IMAGE;
                        LoggingService.LogDebug("Файл не найден, используем def.png");
                    }
                }
                
                LoggingService.LogDebug($"Финальный путь к спрайту: {e.NewLocation.SpritePath}");
                
                // Preload the image to ensure it's available
                var image = ResourceService.Instance.GetImage(e.NewLocation.SpritePath);
                LoggingService.LogDebug($"Изображение загружено: {image != null}");
                
                // Выполняем обновление UI только один раз
                Dispatcher.BeginInvoke(new Action(() => {
                    try
                    {
                        LogUIState("Before transition");
                        
                        // Упрощенный подход: просто меняем изображение без сложной анимации
                        // Это предотвращает исчезновение кружочков и стрелочек
                        LoggingService.LogDebug("Using simplified image transition without complex animation");
                        LocationImage.Source = ResourceService.Instance.GetImage(e.NewLocation.SpritePath);
                        LoggingService.LogDebug("Image source changed successfully");
                        
                        LogUIState("After simplified transition");
                    }
                    catch (Exception ex)
                    {
                        LoggingService.LogError($"Error during transition: {ex.Message}", ex);
                        // Fallback to default image
                        try
                        {
                            LocationImage.Source = ResourceService.Instance.GetImage(AssetPaths.DEFAULT_IMAGE);
                        }
                        catch { }
                    }
                    finally
                    {
                        // Сбрасываем флаг обработки после завершения
                        _isProcessingLocationChange = false;
                        LoggingService.LogDebug("Location change processing completed");
                    }
                }));
                
                LoggingService.LogDebug("=== КОНЕЦ ОБРАБОТКИ СМЕНЫ ЛОКАЦИИ ===");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error in OnLocationChanged: {ex.Message}", ex);
                
                try
                {
                    Dispatcher.BeginInvoke(new Action(() => {
                        try
                        {
                            LocationImage.Source = ResourceService.Instance.GetImage(AssetPaths.DEFAULT_IMAGE);
                        }
                        catch { }
                        finally
                        {
                            _isProcessingLocationChange = false;
                        }
                    }));
                }
                catch 
                { 
                    _isProcessingLocationChange = false;
                }
            }
        }
        
        private void LogUIState(string context)
        {
            try
            {
                LoggingService.LogDebug($"=== UI STATE: {context} ===");
                
                // Log LocationImage state
                LoggingService.LogDebug($"LocationImage.ActualWidth: {LocationImage.ActualWidth}");
                LoggingService.LogDebug($"LocationImage.ActualHeight: {LocationImage.ActualHeight}");
                LoggingService.LogDebug($"LocationImage.Width: {LocationImage.Width}");
                LoggingService.LogDebug($"LocationImage.Height: {LocationImage.Height}");
                LoggingService.LogDebug($"LocationImage.MaxWidth: {LocationImage.MaxWidth}");
                LoggingService.LogDebug($"LocationImage.MaxHeight: {LocationImage.MaxHeight}");
                LoggingService.LogDebug($"LocationImage.Stretch: {LocationImage.Stretch}");
                LoggingService.LogDebug($"LocationImage.Visibility: {LocationImage.Visibility}");
                
                // Log parent Viewbox state
                if (LocationImage.Parent is Border border)
                {
                    LoggingService.LogDebug($"Border.ActualWidth: {border.ActualWidth}");
                    LoggingService.LogDebug($"Border.ActualHeight: {border.ActualHeight}");
                    LoggingService.LogDebug($"Border.MaxWidth: {border.MaxWidth}");
                    LoggingService.LogDebug($"Border.MaxHeight: {border.MaxHeight}");
                    LoggingService.LogDebug($"Border.HorizontalAlignment: {border.HorizontalAlignment}");
                    LoggingService.LogDebug($"Border.VerticalAlignment: {border.VerticalAlignment}");
                }
                else if (LocationImage.Parent is Viewbox viewbox)
                {
                    LoggingService.LogDebug($"Viewbox.ActualWidth: {viewbox.ActualWidth}");
                    LoggingService.LogDebug($"Viewbox.ActualHeight: {viewbox.ActualHeight}");
                    LoggingService.LogDebug($"Viewbox.Stretch: {viewbox.Stretch}");
                }
                else
                {
                    LoggingService.LogDebug($"LocationImage parent type: {LocationImage.Parent?.GetType().Name ?? "null"}");
                }
                
                // Find and log indicators state
                var indicatorsControl = FindName("LocationIndicators") as ItemsControl;
                if (indicatorsControl == null)
                {
                    // Try to find it in the visual tree
                    indicatorsControl = FindVisualChild<ItemsControl>(this);
                }
                
                if (indicatorsControl != null)
                {
                    LoggingService.LogDebug($"LocationIndicators found");
                    LoggingService.LogDebug($"LocationIndicators.Visibility: {indicatorsControl.Visibility}");
                    LoggingService.LogDebug($"LocationIndicators.ActualWidth: {indicatorsControl.ActualWidth}");
                    LoggingService.LogDebug($"LocationIndicators.ActualHeight: {indicatorsControl.ActualHeight}");
                    LoggingService.LogDebug($"LocationIndicators.ItemsSource count: {(indicatorsControl.ItemsSource as System.Collections.ICollection)?.Count ?? 0}");
                }
                else
                {
                    LoggingService.LogDebug("LocationIndicators NOT found");
                }
                
                // Find and log navigation buttons
                var navigationPanel = FindVisualChild<StackPanel>(this, sp => sp.Orientation == Orientation.Horizontal);
                if (navigationPanel != null)
                {
                    LoggingService.LogDebug($"Navigation panel found");
                    LoggingService.LogDebug($"Navigation panel.Visibility: {navigationPanel.Visibility}");
                    LoggingService.LogDebug($"Navigation panel.ActualWidth: {navigationPanel.ActualWidth}");
                    LoggingService.LogDebug($"Navigation panel.ActualHeight: {navigationPanel.ActualHeight}");
                    LoggingService.LogDebug($"Navigation panel children count: {navigationPanel.Children.Count}");
                }
                else
                {
                    LoggingService.LogDebug("Navigation panel NOT found");
                }
                
                LoggingService.LogDebug($"=== END UI STATE: {context} ===");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error in LogUIState: {ex.Message}", ex);
            }
        }
        
        private T FindVisualChild<T>(DependencyObject parent, Func<T, bool> predicate = null) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild && (predicate == null || predicate(typedChild)))
                {
                    return typedChild;
                }
                
                var result = FindVisualChild<T>(child, predicate);
                if (result != null)
                {
                    return result;
                }
            }
            return null;
        }
    }
} 
