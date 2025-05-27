using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using SketchBlade.Models;
using System.Windows.Threading;
using System.Windows;
using SketchBlade.Services;

namespace SketchBlade.ViewModels
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        private readonly GameData _gameState;
        private readonly Action<string> _navigateAction;
        
        public event PropertyChangedEventHandler? PropertyChanged;
        
        // Settings properties from GameData
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
        
        // Localized display text for selected language
        public string SelectedLanguageText
        {
            get
            {
                var language = Settings?.Language;
                if (language == null)
                    return "Language not set";
                
                var key = $"Language.{language}";
                return LocalizationService.Instance.GetTranslation(key);
            }
        }
        
        // Localized display text for selected difficulty
        public string SelectedDifficultyText
        {
            get
            {
                var difficulty = Settings?.Difficulty;
                if (difficulty == null)
                    return "Difficulty not set";
                
                var key = $"Difficulty.{difficulty}";
                return LocalizationService.Instance.GetTranslation(key);
            }
        }
        
        // Commands
        public ICommand NavigateCommand { get; private set; }
        public ICommand ResetToDefaultsCommand { get; private set; }
        
        // Constructor
        public SettingsViewModel(GameData GameData, Action<string> navigateAction)
        {
            _gameState = GameData;
            _navigateAction = navigateAction;
            
            // Initialize commands
            NavigateCommand = new RelayCommand<string>(NavigateToScreen);
            ResetToDefaultsCommand = new RelayCommand<object>(_ => ResetToDefaults());
            
            // Subscribe to settings property changes for immediate application
            Settings.PropertyChanged += Settings_PropertyChanged;
        }
        
        private void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Apply UI changes immediately for all settings for instant visual feedback
            ApplySettingChange(e.PropertyName);
            
            // Обновляем все связанные свойства
            OnPropertyChanged(e.PropertyName);
            OnPropertyChanged(nameof(Settings));
        }
        
        private void ApplySettingChange(string propertyName)
        {
            try
            {
                switch (propertyName)
                {
                    case nameof(Settings.Language):
                        ApplyLanguageChange();
                        break;
                    case nameof(Settings.Difficulty):
                        ApplyDifficultyChange();
                        break;
                    case nameof(Settings.ShowCombatDamageNumbers):
                        ApplyDamageNumbersSettingChange();
                        break;
                }
                
                // Save settings immediately after applying
                CoreGameService.Instance.SaveSettings();
                _gameState.SaveGame();
                
                // Принудительно обновляем все UI элементы
                ForceUIUpdate();
            }
            catch (Exception ex)
            {
                // Показываем уведомление только при ошибках
                MessageBox.Show(
                    LocalizationService.Instance.GetTranslation("Settings.ApplyError"),
                    LocalizationService.Instance.GetTranslation("Settings.Title"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        
        private void ApplyLanguageChange()
        {
            // Применяем изменение языка через сервис UI мгновенно
            UIService.Instance.ApplyLanguage(Settings.Language);
            
            // Обновляем весь экран настроек мгновенно
            RefreshSettingsScreen();
        }
        
        private void ApplyDifficultyChange()
        {
            // Сложность применяется без уведомлений - будет использована в следующих боях
        }
        
        private void ApplyDamageNumbersSettingChange()
        {
            // Уведомляем все элементы что настройка показа урона изменилась
            // Принудительно обновляем GameData чтобы боевой экран получил обновление
            _gameState.OnPropertyChanged(nameof(_gameState.Settings));
        }
        
        private void ForceUIUpdate()
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                // Принудительно обновляем все привязки данных для настроек
                OnPropertyChanged(nameof(Settings));
                OnPropertyChanged(nameof(Settings.Language));
                OnPropertyChanged(nameof(Settings.Difficulty));
                OnPropertyChanged(nameof(Settings.ShowCombatDamageNumbers));
                OnPropertyChanged(nameof(SelectedLanguageText));
                OnPropertyChanged(nameof(SelectedDifficultyText));
                
                // Принудительно обновляем все команды
                System.Windows.Input.CommandManager.InvalidateRequerySuggested();
                
                // Обновляем главное окно если оно доступно
                var mainWindow = Application.Current.MainWindow as MainWindow;
                if (mainWindow != null)
                {
                    mainWindow.RefreshCurrentScreen();
                    mainWindow.UpdateLayout();
                }
            });
        }
        
        private void RefreshSettingsScreen()
        {
            // Обновляем все привязки данных на экране настроек
            Application.Current?.Dispatcher.Invoke(() =>
            {
                OnPropertyChanged(nameof(Settings));
                OnPropertyChanged(nameof(AvailableLanguages));
                OnPropertyChanged(nameof(AvailableDifficulties));
                OnPropertyChanged(nameof(SelectedLanguageText));
                OnPropertyChanged(nameof(SelectedDifficultyText));
                
                // Принудительно обновляем все свойства настроек
                OnPropertyChanged(nameof(Settings.Language));
                OnPropertyChanged(nameof(Settings.Difficulty));
                OnPropertyChanged(nameof(Settings.ShowCombatDamageNumbers));
                
                // Принудительно обновляем все команды и привязки
                System.Windows.Input.CommandManager.InvalidateRequerySuggested();
                
                // Обновляем главное окно
                var mainWindow = Application.Current.MainWindow as MainWindow;
                if (mainWindow != null)
                {
                    mainWindow.RefreshCurrentScreen();
                    mainWindow.UpdateLayout();
                    mainWindow.InvalidateVisual();
                }
            });
        }
        
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
        private void NavigateToScreen(string screenName)
        {
            _navigateAction("MainMenuView");
        }
        
        private void ResetToDefaults()
        {
            try
            {
                // Reset all settings to defaults
                Settings.Language = Language.Russian;
                Settings.Difficulty = Difficulty.Normal;
                Settings.ShowCombatDamageNumbers = true;
                
                // Обновляем экран после сброса
                RefreshSettingsScreen();
                
                // Больше не показываем уведомление - настройки применяются мгновенно
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    LocalizationService.Instance.GetTranslation("Settings.ResetError"),
                    LocalizationService.Instance.GetTranslation("Settings.Title"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        
        // Cleanup method for proper resource disposal
        public void Cleanup()
        {
            if (Settings != null)
            {
                Settings.PropertyChanged -= Settings_PropertyChanged;
            }
        }
    }
} 
