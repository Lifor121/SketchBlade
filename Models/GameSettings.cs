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
                }
            }
        }
        
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

        private double _uiScale = 1.0;
        public double UIScale
        {
            get => _uiScale;
            set
            {
                if (Math.Abs(_uiScale - value) > 0.001)
                {
                    _uiScale = Math.Max(0.5, Math.Min(2.0, value));
                    OnPropertyChanged();
                }
            }
        }

        public bool ShowItemDescriptionsOnHover => true;
        
        public bool ShowItemDescriptions => true;
        public bool ShowDamageNumbers => ShowCombatDamageNumbers;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 