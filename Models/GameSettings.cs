using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using SketchBlade.Services;

namespace SketchBlade.Models
{
    public enum Language
    {
        Russian,
        English
    }
    
    public enum Difficulty
    {
        Easy,
        Normal,
        Hard
    }
    
    [Serializable]
    public class GameSettings : INotifyPropertyChanged
    {
        // Language settings
        private Language _language = Language.Russian;
        public Language Language
        {
            get => _language;
            set
            {
                if (_language != value)
                {
                    _language = value;
                    OnPropertyChanged();
                    
                    // Language service will be updated by SettingsViewModel to avoid duplication
                }
            }
        }
        
        // Gameplay settings
        private Difficulty _difficulty = Difficulty.Normal;
        public Difficulty Difficulty
        {
            get => _difficulty;
            set
            {
                if (_difficulty != value)
                {
                    _difficulty = value;
                    OnPropertyChanged();
                }
            }
        }
        
        // Combat settings
        private bool _showCombatDamageNumbers = true;
        public bool ShowCombatDamageNumbers
        {
            get => _showCombatDamageNumbers;
            set
            {
                if (_showCombatDamageNumbers != value)
                {
                    _showCombatDamageNumbers = value;
                    OnPropertyChanged();
                }
            }
        }

        // UI Settings
        private double _uiScale = 1.0;
        public double UIScale
        {
            get => _uiScale;
            set
            {
                if (Math.Abs(_uiScale - value) > 0.001)
                {
                    _uiScale = Math.Max(0.5, Math.Min(2.0, value)); // Clamp between 0.5x and 2.0x
                    OnPropertyChanged();
                }
            }
        }

        // Описания предметов всегда включены, но добавляем свойство для совместимости с существующим кодом
        public bool ShowItemDescriptionsOnHover => true;
        
        // Additional display settings for backward compatibility
        public bool ShowItemDescriptions => true;
        public bool ShowDamageNumbers => ShowCombatDamageNumbers;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 