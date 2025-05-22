using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using SketchBlade.Models;
using System.Windows.Threading;
using System.Windows;

namespace SketchBlade.ViewModels
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        private readonly GameState _gameState;
        private readonly Action<string> _navigateAction;
        
        public event PropertyChangedEventHandler? PropertyChanged;
        
        // Settings properties from GameState
        public GameSettings Settings => _gameState.Settings;
        
        // Available languages
        public List<Language> AvailableLanguages { get; } = new List<Language>
        {
            Language.Russian,
            Language.English
        };
        
        // Available difficulties
        public List<Difficulty> AvailableDifficulties { get; } = new List<Difficulty>
        {
            Difficulty.Easy,
            Difficulty.Normal,
            Difficulty.Hard
        };
        
        // Свойства для состояния кнопки и индикации сохранения
        private bool _isSettingsSaved = false;
        public bool IsSettingsSaved 
        { 
            get => _isSettingsSaved; 
            set 
            { 
                if(_isSettingsSaved != value)
                {
                    _isSettingsSaved = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(SaveButtonText));
                    OnPropertyChanged(nameof(SaveButtonBackground));
                }
            }
        }
        
        private bool _hasUnsavedChanges = false;
        public bool HasUnsavedChanges
        {
            get => _hasUnsavedChanges;
            set
            {
                if(_hasUnsavedChanges != value)
                {
                    _hasUnsavedChanges = value;
                    OnPropertyChanged();
                    // Если появились несохраненные изменения, сбрасываем индикатор сохранения
                    if(value && IsSettingsSaved)
                    {
                        IsSettingsSaved = false;
                    }
                }
            }
        }
        
        // Текст кнопки сохранения меняется в зависимости от состояния
        public string SaveButtonText => IsSettingsSaved ? 
            Services.LanguageService.GetTranslation("Settings.SavedStatus") : 
            Services.LanguageService.GetTranslation("Settings.Save");
        
        // Цвет кнопки сохранения
        public string SaveButtonBackground => IsSettingsSaved ? "#4CAF50" : "#0078D7";
        
        // Таймер для сброса состояния сохранения
        private DispatcherTimer _savedStateTimer;
        
        // Commands
        public ICommand NavigateCommand { get; private set; }
        public ICommand SaveSettingsCommand { get; private set; }
        public ICommand ResetToDefaultsCommand { get; private set; }
        
        // Constructor
        public SettingsViewModel(GameState gameState, Action<string> navigateAction)
        {
            _gameState = gameState;
            _navigateAction = navigateAction;
            
            // Initialize timer for resetting saved state
            _savedStateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(3)
            };
            _savedStateTimer.Tick += (s, e) => 
            {
                _savedStateTimer.Stop();
                IsSettingsSaved = false;
            };
            
            // Initialize commands
            NavigateCommand = new RelayCommand<string>(NavigateToScreen);
            SaveSettingsCommand = new RelayCommand<object>(_ => SaveSettings());
            ResetToDefaultsCommand = new RelayCommand<object>(_ => ResetToDefaults());
            
            // Запоминаем исходное состояние настроек
            CaptureSettingsState();
            
            // Subscribe to settings property changes
            Settings.PropertyChanged += Settings_PropertyChanged;
        }
        
        private void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // При изменении любого свойства отмечаем, что есть несохраненные изменения
            HasUnsavedChanges = true;
            OnPropertyChanged(e.PropertyName);
        }
        
        // Хранилище исходных настроек для определения изменений
        private Dictionary<string, object> _originalSettings = new Dictionary<string, object>();
        
        private void CaptureSettingsState()
        {
            // Сохраняем текущие значения настроек
            _originalSettings.Clear();
            _originalSettings.Add(nameof(Settings.Language), Settings.Language);
            _originalSettings.Add(nameof(Settings.MusicVolume), Settings.MusicVolume);
            _originalSettings.Add(nameof(Settings.SoundEffectsVolume), Settings.SoundEffectsVolume);
            _originalSettings.Add(nameof(Settings.IsMusicEnabled), Settings.IsMusicEnabled);
            _originalSettings.Add(nameof(Settings.AreSoundEffectsEnabled), Settings.AreSoundEffectsEnabled);
            _originalSettings.Add(nameof(Settings.Difficulty), Settings.Difficulty);
            _originalSettings.Add(nameof(Settings.UIScale), Settings.UIScale);
            _originalSettings.Add(nameof(Settings.ShowItemDescriptionsOnHover), Settings.ShowItemDescriptionsOnHover);
            _originalSettings.Add(nameof(Settings.ShowCombatDamageNumbers), Settings.ShowCombatDamageNumbers);
            
            HasUnsavedChanges = false;
        }
        
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
        private void NavigateToScreen(string screenName)
        {
            if (HasUnsavedChanges)
            {
                var result = MessageBox.Show(
                    Services.LanguageService.GetTranslation("Settings.UnsavedChanges"), 
                    Services.LanguageService.GetTranslation("Settings.UnsavedChangesTitle"), 
                    MessageBoxButton.YesNoCancel);
                
                if (result == MessageBoxResult.Yes)
                {
                    SaveSettings();
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    return; // Отменяем навигацию
                }
            }
            
            _navigateAction("MainMenuView");
        }
        
        private void SaveSettings()
        {
            // Settings are automatically saved through the property changed event
            _gameState.SaveGame();
            
            // Явно сохраняем настройки в отдельный файл
            Services.SettingsSaveService.SaveSettings(Settings);
            
            // Обновляем индикацию сохранения
            IsSettingsSaved = true;
            HasUnsavedChanges = false;
            
            // Запоминаем новое состояние
            CaptureSettingsState();
            
            // Запускаем таймер для сброса индикации через 3 секунды
            _savedStateTimer.Start();
        }
        
        private void ResetToDefaults()
        {
            // Reset all settings to defaults
            Settings.Language = Language.Russian;
            Settings.MusicVolume = 0.7;
            Settings.SoundEffectsVolume = 0.8;
            Settings.IsMusicEnabled = true;
            Settings.AreSoundEffectsEnabled = true;
            Settings.Difficulty = Difficulty.Normal;
            Settings.UIScale = 1.0;
            Settings.ShowItemDescriptionsOnHover = true;
            Settings.ShowCombatDamageNumbers = true;
            
            // После сброса настроек отмечаем, что есть несохраненные изменения
            HasUnsavedChanges = true;
        }
    }
} 