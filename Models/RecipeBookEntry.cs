using System;
using System.ComponentModel;
using System.Windows.Media.Imaging;

namespace SketchBlade.Models
{
    public class RecipeBookEntry : INotifyPropertyChanged
    {
        private CraftingRecipe _recipe;
        private bool _canCraft;
        private string _iconPath;
        private BitmapImage _icon;
        
        public CraftingRecipe Recipe
        {
            get => _recipe;
            set
            {
                if (_recipe != value)
                {
                    _recipe = value;
                    OnPropertyChanged(nameof(Recipe));
                }
            }
        }
        
        public bool CanCraft
        {
            get => _canCraft;
            set
            {
                if (_canCraft != value)
                {
                    _canCraft = value;
                    OnPropertyChanged(nameof(CanCraft));
                }
            }
        }
        
        public string IconPath
        {
            get => _iconPath;
            set
            {
                if (_iconPath != value)
                {
                    _iconPath = value;
                    // При изменении пути к иконке загружаем новую иконку
                    LoadIcon();
                    OnPropertyChanged(nameof(IconPath));
                }
            }
        }
        
        public BitmapImage Icon
        {
            get
            {
                if (_icon == null)
                {
                    LoadIcon();
                }
                return _icon;
            }
        }
        
        // Метод для загрузки иконки
        private void LoadIcon()
        {
            try
            {
                if (string.IsNullOrEmpty(_iconPath))
                {
                    _icon = Helpers.ImageHelper.GetDefaultImage();
                }
                else
                {
                    _icon = Helpers.ImageHelper.GetImageWithFallback(_iconPath);
                }
                OnPropertyChanged(nameof(Icon));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при загрузке иконки для рецепта: {ex.Message}");
                _icon = Helpers.ImageHelper.GetDefaultImage();
            }
        }
        
        // Реализация INotifyPropertyChanged
        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 