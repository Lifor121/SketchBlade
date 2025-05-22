using System;
using SketchBlade.Models;

namespace SketchBlade.Services
{
    /// <summary>
    /// Interface for image and sound service operations
    /// </summary>
    public interface IImageService
    {
        /// <summary>
        /// Play location-specific background music
        /// </summary>
        /// <param name="locationType">Type of location to play music for</param>
        void PlayLocationMusic(LocationType locationType);
        
        /// <summary>
        /// Play a sound effect
        /// </summary>
        /// <param name="soundType">Type of sound to play</param>
        void PlaySound(SoundType soundType);
        
        /// <summary>
        /// Flag indicating if music is enabled
        /// </summary>
        bool IsMusicEnabled { get; set; }
        
        /// <summary>
        /// Flag indicating if sound effects are enabled
        /// </summary>
        bool AreSoundEffectsEnabled { get; set; }
        
        /// <summary>
        /// Music volume level (0.0 - 1.0)
        /// </summary>
        double MusicVolume { get; set; }
        
        /// <summary>
        /// Sound effects volume level (0.0 - 1.0)
        /// </summary>
        double SoundEffectsVolume { get; set; }
    }
} 