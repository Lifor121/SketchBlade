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

namespace SketchBlade.Views
{
    /// <summary>
    /// Interaction logic for WorldMapView.xaml
    /// </summary>
    public partial class WorldMapView : UserControl
    {
        private readonly ScreenTransitionService _transitionService;
        
        public WorldMapView()
        {
            InitializeComponent();
            _transitionService = new ScreenTransitionService();
            
            // Subscribe to the ViewModel's location changed event when loaded
            Loaded += WorldMapView_Loaded;
            
            // Subscribe to unloaded event to clean up
            Unloaded += WorldMapView_Unloaded;
        }
        
        private void WorldMapView_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is MapViewModel viewModel)
            {
                // First unsubscribe to avoid duplicate handlers
                viewModel.LocationChanged -= OnLocationChanged;
                // Then subscribe
                viewModel.LocationChanged += OnLocationChanged;
                
                // Ensure we're not working with null values
                if (viewModel.GameState != null && 
                    viewModel.GameState.Locations != null && 
                    viewModel.GameState.Locations.Count > 0)
                {
                    // Make sure CurrentLocation is set properly
                    if (viewModel.GameState.CurrentLocation == null)
                    {
                        viewModel.GameState.CurrentLocation = viewModel.GameState.Locations[viewModel.GameState.CurrentLocationIndex];
                    }
                    
                    // Update all location statuses
                    foreach (var location in viewModel.GameState.Locations)
                    {
                        location.CheckAvailability(viewModel.GameState);
                    }
                }
                
                // Refresh view when loaded
                viewModel.RefreshView();
                
                // Load the current location image safely
                LoadCurrentLocationImage();
            }
        }
        
        private void WorldMapView_Unloaded(object sender, RoutedEventArgs e)
        {
            // Unsubscribe when control is unloaded
            if (DataContext is MapViewModel viewModel)
            {
                viewModel.LocationChanged -= OnLocationChanged;
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
                        string typePath = $"Assets/Images/Locations/{locationTypeName}.png";
                        
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
                            viewModel.CurrentLocation.SpritePath = "Assets/Images/def.png";
                        }
                    }
                    
                    ImageHelper.GetImageWithFallback(viewModel.CurrentLocation.SpritePath);
                    
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
                            MessageBox.Show($"Error setting image binding: {ex.Message}");
                            try
                            {
                                LocationImage.Source = ImageHelper.GetImageWithFallback(ImageHelper.DefaultSpritePath);
                            }
                            catch { }
                        }
                    }));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in LoadCurrentLocationImage: {ex.Message}");
                MessageBox.Show($"Stack trace: {ex.StackTrace}");
            }
        }
        
        private void OnLocationChanged(object sender, LocationChangedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(e.NewLocation.SpritePath))
                {
                    string locationTypeName = e.NewLocation.Type.ToString().ToLower();
                    string typePath = $"Assets/Images/Locations/{locationTypeName}.png";
                    
                    string fullPath = System.IO.Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory, 
                        typePath
                    );
                    
                    if (System.IO.File.Exists(fullPath))
                    {
                        e.NewLocation.SpritePath = typePath;
                    }
                    else
                    {
                        e.NewLocation.SpritePath = "Assets/Images/def.png";
                    }
                }
                
                BitmapImage newLocationImage = ImageHelper.GetImageWithFallback(e.NewLocation.SpritePath);
                
                BitmapImage tempBitmap = null;
                
                if (LocationImage.Source is BitmapImage currentSource && currentSource.IsDownloading == false)
                {
                    tempBitmap = currentSource;
                }
                else
                {
                    tempBitmap = newLocationImage;
                }
                
                Image tempImage = new Image
                {
                    Source = tempBitmap,
                    Stretch = Stretch.Uniform
                };
                
                Dispatcher.BeginInvoke(new Func<Task>(async () => {
                    try
                    {
                        try
                        {
                            await _transitionService.SlideLocationTransitionAsync(
                                LocationImageContainer, 
                                tempImage, 
                                e.Direction == NavigationDirection.Next);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Error during transition animation: {ex.Message}");
                        }
                        
                        try
                        {
                            LocationImage.Source = ImageHelper.GetImageWithFallback(e.NewLocation.SpritePath);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Error setting new image source: {ex.Message}");
                            LocationImage.Source = ImageHelper.GetImageWithFallback(ImageHelper.DefaultSpritePath);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error updating UI after location change: {ex.Message}");
                    }
                }));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in OnLocationChanged: {ex.Message}");
                MessageBox.Show($"Stack trace: {ex.StackTrace}");
                
                try
                {
                    Dispatcher.BeginInvoke(new Action(() => {
                        try
                        {
                            LocationImage.Source = ImageHelper.GetImageWithFallback(ImageHelper.DefaultSpritePath);
                        }
                        catch { }
                    }));
                }
                catch { }
            }
        }
    }
} 