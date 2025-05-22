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
                    
                    // Update language service when language changes
                    LanguageService.CurrentLanguage = value;
                }
            }
        }
        
        // Audio settings
        private double _musicVolume = 0.7;
        public double MusicVolume
        {
            get => _musicVolume;
            set
            {
                if (_musicVolume != value)
                {
                    _musicVolume = Math.Clamp(value, 0.0, 1.0);
                    OnPropertyChanged();
                }
            }
        }
        
        private double _soundEffectsVolume = 0.8;
        public double SoundEffectsVolume
        {
            get => _soundEffectsVolume;
            set
            {
                if (_soundEffectsVolume != value)
                {
                    _soundEffectsVolume = Math.Clamp(value, 0.0, 1.0);
                    OnPropertyChanged();
                }
            }
        }
        
        private bool _isMusicEnabled = true;
        public bool IsMusicEnabled
        {
            get => _isMusicEnabled;
            set
            {
                if (_isMusicEnabled != value)
                {
                    _isMusicEnabled = value;
                    OnPropertyChanged();
                }
            }
        }
        
        private bool _areSoundEffectsEnabled = true;
        public bool AreSoundEffectsEnabled
        {
            get => _areSoundEffectsEnabled;
            set
            {
                if (_areSoundEffectsEnabled != value)
                {
                    _areSoundEffectsEnabled = value;
                    OnPropertyChanged();
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
        
        // Interface settings
        private double _uiScale = 1.0;
        public double UIScale
        {
            get => _uiScale;
            set
            {
                if (_uiScale != value)
                {
                    _uiScale = Math.Clamp(value, 0.5, 1.5);
                    OnPropertyChanged();
                }
            }
        }
        
        private bool _showItemDescriptionsOnHover = true;
        public bool ShowItemDescriptionsOnHover
        {
            get => _showItemDescriptionsOnHover;
            set
            {
                if (_showItemDescriptionsOnHover != value)
                {
                    _showItemDescriptionsOnHover = value;
                    OnPropertyChanged();
                }
            }
        }
        
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
        
        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 