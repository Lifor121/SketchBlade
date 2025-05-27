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
        private bool _isProcessingLocationChange = false; // ���� ��� �������������� ������������� ���������
        
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
                // LoggingService.LogDebug("=== WorldMapView_Loaded START ===");
                
                // Subscribe to location change events if the data context is MapViewModel
                if (DataContext is MapViewModel viewModel)
                {
                    // LoggingService.LogDebug("DataContext is MapViewModel, subscribing to events");
                    viewModel.LocationChanged += OnLocationChanged;
                    
                    // Load the current location image
                    LoadCurrentLocationImage();
                    
                    // Apply any pending location change
                    if (_pendingLocationChange != null)
                    {
                        // LoggingService.LogDebug("Applying pending location change");
                        OnLocationChanged(this, _pendingLocationChange);
                        _pendingLocationChange = null;
                    }
                    
                    viewModel.RefreshLocations();
                    
                    // Log initial UI state
                    // LogUIState("After initial load"); // ��������� ��� ������������������
                }
                else
                {
                    // LoggingService.LogDebug("DataContext is NOT MapViewModel");
                }
                
                // LoggingService.LogDebug("=== WorldMapView_Loaded END ===");
            }
            catch (Exception ex)
            {
                // LoggingService.LogError($"Error in WorldMapView_Loaded: {ex.Message}", ex);
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
                // LoggingService.LogError($"Error in WorldMapView_Unloaded: {ex.Message}", ex);
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
                            // LoggingService.LogError($"Error setting image binding: {ex.Message}", ex);
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
                // LoggingService.LogError($"Error in LoadCurrentLocationImage: {ex.Message}", ex);
            }
        }
        
        private void OnLocationChanged(object sender, LocationChangedEventArgs e)
        {
            // ������������� ������������� ��������� ������ � ���� �� �������
            if (_isProcessingLocationChange)
            {
                // LoggingService.LogDebug("OnLocationChanged: ��� �������������� ����� �������, ����������");
                return;
            }
            
            _isProcessingLocationChange = true;
            
            try
            {
                // LoggingService.LogDebug("=== ��������� ����� ������� ===");
                // LoggingService.LogDebug($"�����������: {e.Direction}");
                // LoggingService.LogDebug($"����� �������: {e.NewLocation.Name} ({e.NewLocation.Type})");
                // LoggingService.LogDebug($"���� � ������� (�� ��������): {e.NewLocation.SpritePath}");
                
                if (string.IsNullOrEmpty(e.NewLocation.SpritePath))
                {
                    string locationTypeName = e.NewLocation.Type.ToString().ToLower();
                    string typePath = AssetPaths.Locations.GetLocationPath(locationTypeName);
                    
                    // LoggingService.LogDebug($"������ ���� ������, ������� �����: {typePath}");
                    
                    string fullPath = System.IO.Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory, 
                        typePath
                    );
                    
                    // LoggingService.LogDebug($"������ ����: {fullPath}");
                    // LoggingService.LogDebug($"���� ����������: {System.IO.File.Exists(fullPath)}");
                    
                    if (System.IO.File.Exists(fullPath))
                    {
                        e.NewLocation.SpritePath = typePath;
                        // LoggingService.LogDebug($"���������� ���� � �������: {typePath}");
                    }
                    else
                    {
                        e.NewLocation.SpritePath = AssetPaths.DEFAULT_IMAGE;
                        // LoggingService.LogDebug("���� �� ������, ���������� def.png");
                    }
                }
                
                // LoggingService.LogDebug($"��������� ���� � �������: {e.NewLocation.SpritePath}");
                
                // Preload the image to ensure it's available
                var image = ResourceService.Instance.GetImage(e.NewLocation.SpritePath);
                // LoggingService.LogDebug($"����������� ���������: {image != null}");
                
                // ��������� ���������� UI ������ ���� ���
                Dispatcher.BeginInvoke(new Action(() => {
                    try
                    {
                        // LogUIState("Before transition"); // ��������� ��� ������������������
                        
                        // ���������� ������: ������ ������ ����������� ��� ������� ��������
                        // ��� ������������� ������������ ��������� � ���������
                        // LoggingService.LogDebug("Using simplified image transition without complex animation");
                        LocationImage.Source = ResourceService.Instance.GetImage(e.NewLocation.SpritePath);
                        // LoggingService.LogDebug("Image source changed successfully");
                        
                        // LogUIState("After simplified transition"); // ��������� ��� ������������������
                    }
                    catch (Exception ex)
                    {
                        // LoggingService.LogError($"Error during transition: {ex.Message}", ex);
                        // Fallback to default image
                        try
                        {
                            LocationImage.Source = ResourceService.Instance.GetImage(AssetPaths.DEFAULT_IMAGE);
                        }
                        catch { }
                    }
                    finally
                    {
                        // ���������� ���� ��������� ����� ����������
                        _isProcessingLocationChange = false;
                        // LoggingService.LogDebug("Location change processing completed");
                    }
                }));
                
                // LoggingService.LogDebug("=== ����� ��������� ����� ������� ===");
            }
            catch (Exception ex)
            {
                // LoggingService.LogError($"Error in OnLocationChanged: {ex.Message}", ex);
                
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
            // ����� ��������� �������� ��� ��������� ������������������
            // ���������������� ��� ���� ������ ��� ������� ����������� UI �������
            return;
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
