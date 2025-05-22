using System;
using System.Collections.Generic;
using System.IO;
using System.Media;
using System.Windows.Media;
using System.Threading.Tasks;
using SketchBlade.Models;

namespace SketchBlade.Services
{
    // Updated SoundType enum with all needed sound types
    public enum SoundType
    {
        ButtonClick,
        MenuOpen,
        InventoryOpen,
        ItemPickup,
        ItemEquip,
        ItemDrop,
        PlayerAttack,
        PlayerDamage,
        EnemyAttack,
        EnemyDamage,
        CriticalHit,
        Victory,
        Defeat,
        LevelUp,
        BossDefeated,
        ItemUse,
        ItemCrafted,
        Error,
        Attack,
        Hit,
        BattleStart,
        CollectItem,
        MainTheme,
        BattleTheme,
        ShopTheme,
        MapTheme,
        VictorySound,
        DefeatSound
    }

    public enum MusicType
    {
        MainMenu,
        Battle,
        BossBattle,
        Village,
        Forest,
        Cave,
        Victory,
        Defeat
    }

    public class SoundService : IImageService
    {
        private readonly Dictionary<SoundType, string> _soundPaths = new Dictionary<SoundType, string>();
        private readonly Dictionary<LocationType, string> _musicPaths = new Dictionary<LocationType, string>();
        private readonly Dictionary<SoundType, MediaPlayer> _mediaPlayers = new Dictionary<SoundType, MediaPlayer>();
        private readonly Dictionary<SoundType, SoundPlayer> _soundPlayers = new Dictionary<SoundType, SoundPlayer>();
        
        private MediaPlayer _currentBackgroundMusic;
        private SoundType _currentBackgroundMusicType;
        
        // Properties
        public bool IsMusicEnabled 
        { 
            get => _isMusicEnabled;
            set
            {
                _isMusicEnabled = value;
                if (!_isMusicEnabled)
                {
                    StopMusic();
                }
                else if (_currentBackgroundMusicType != 0)
                {
                    // Resume with the last played music
                    PlayBackgroundMusic(_currentBackgroundMusicType);
                }
            }
        }
        
        public bool AreSoundEffectsEnabled
        {
            get => _areSoundEffectsEnabled;
            set => _areSoundEffectsEnabled = value;
        }
        
        public double MusicVolume
        {
            get => _musicVolume;
            set
            {
                _musicVolume = Math.Clamp(value, 0.0, 1.0);
                if (_currentBackgroundMusic != null)
                {
                    _currentBackgroundMusic.Volume = _musicVolume;
                }
            }
        }
        
        public double SoundEffectsVolume
        {
            get => _soundEffectsVolume;
            set => _soundEffectsVolume = Math.Clamp(value, 0.0, 1.0);
        }
        
        private bool _isMusicEnabled = true;
        private bool _areSoundEffectsEnabled = true;
        private double _musicVolume = 0.7;
        private double _soundEffectsVolume = 0.8;
        
        public SoundService()
        {
            InitializeSoundPaths();
            InitializeMusicPaths();
            PreloadSounds();
        }
        
        private void InitializeSoundPaths()
        {
            // Map sound types to their file paths
            _soundPaths[SoundType.MenuOpen] = "Assets/Sounds/UI/menu_open.wav";
            _soundPaths[SoundType.ButtonClick] = "Assets/Sounds/UI/button_click.wav";
            _soundPaths[SoundType.InventoryOpen] = "Assets/Sounds/UI/inventory_open.wav";
            _soundPaths[SoundType.ItemPickup] = "Assets/Sounds/UI/item_pickup.wav";
            _soundPaths[SoundType.ItemEquip] = "Assets/Sounds/UI/item_equip.wav";
            _soundPaths[SoundType.ItemDrop] = "Assets/Sounds/UI/item_drop.wav";
            _soundPaths[SoundType.PlayerAttack] = "Assets/Sounds/Battle/player_attack.wav";
            _soundPaths[SoundType.PlayerDamage] = "Assets/Sounds/Battle/player_damage.wav";
            _soundPaths[SoundType.EnemyAttack] = "Assets/Sounds/Battle/enemy_attack.wav";
            _soundPaths[SoundType.EnemyDamage] = "Assets/Sounds/Battle/enemy_damage.wav";
            _soundPaths[SoundType.CriticalHit] = "Assets/Sounds/Battle/critical_hit.wav";
            _soundPaths[SoundType.Victory] = "Assets/Sounds/Battle/victory.wav";
            _soundPaths[SoundType.Defeat] = "Assets/Sounds/Battle/defeat.wav";
            _soundPaths[SoundType.BossDefeated] = "Assets/Sounds/Battle/boss_defeated.wav";
            _soundPaths[SoundType.LevelUp] = "Assets/Sounds/UI/level_up.wav";
            _soundPaths[SoundType.ItemUse] = "Assets/Sounds/Battle/item_use.wav";
            _soundPaths[SoundType.ItemCrafted] = "Assets/Sounds/UI/item_crafted.wav";
            _soundPaths[SoundType.Error] = "Assets/Sounds/UI/error.wav";
            _soundPaths[SoundType.MainTheme] = "Sounds/main_theme.mp3";
            _soundPaths[SoundType.BattleTheme] = "Sounds/battle_theme.mp3";
            _soundPaths[SoundType.ShopTheme] = "Sounds/shop_theme.mp3";
            _soundPaths[SoundType.MapTheme] = "Sounds/map_theme.mp3";
            _soundPaths[SoundType.VictorySound] = "Sounds/victory.mp3";
            _soundPaths[SoundType.DefeatSound] = "Sounds/defeat.mp3";
        }
        
        private void InitializeMusicPaths()
        {
            // Map location types to their music file paths
            _musicPaths[LocationType.Village] = "Assets/Sounds/Music/village_theme.mp3";
            _musicPaths[LocationType.Forest] = "Assets/Sounds/Music/forest_theme.mp3";
            _musicPaths[LocationType.Cave] = "Assets/Sounds/Music/cave_theme.mp3";
            _musicPaths[LocationType.Castle] = "Assets/Sounds/Music/castle_theme.mp3";
            _musicPaths[LocationType.Ruins] = "Assets/Sounds/Music/ruins_theme.mp3";
            
            // Add forest music as fallback for any missing types
            string defaultMusic = "Assets/Sounds/Music/forest_theme.mp3";
            
            // Ensure all location types have music
            foreach (LocationType locType in Enum.GetValues(typeof(LocationType)))
            {
                if (!_musicPaths.ContainsKey(locType))
                {
                    _musicPaths[locType] = defaultMusic;
                }
            }
        }
        
        private void PreloadSounds()
        {
            try
            {
                // Preload short sound effects for better performance
                foreach (var pair in _soundPaths)
                {
                    if (File.Exists(pair.Value) && pair.Key < SoundType.Victory)
                    {
                        var player = new SoundPlayer(pair.Value);
                        try
                        {
                            player.LoadAsync();
                            _soundPlayers[pair.Key] = player;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error loading sound {pair.Key}: {ex.Message}");
                        }
                    }
                    else if (!File.Exists(pair.Value))
                    {
                        Console.WriteLine($"Sound file not found: {pair.Value}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error preloading sounds: {ex.Message}");
            }
        }
        
        // Play background music with looping
        public void PlayBackgroundMusic(string musicPath)
        {
            try
            {
                if (!AreSoundEffectsEnabled || !IsMusicEnabled)
                    return;
                    
                // Stop any currently playing music
                if (_currentBackgroundMusic != null)
                {
                    _currentBackgroundMusic.Stop();
                }
                
                // Create a new media player
                _currentBackgroundMusic = new MediaPlayer();
                
                // Set the volume
                _currentBackgroundMusic.Volume = MusicVolume;
                
                // Open the sound file
                _currentBackgroundMusic.Open(new Uri(musicPath, UriKind.Relative));
                
                // Add an event handler for when the music ends to loop it
                _currentBackgroundMusic.MediaEnded += MediaPlayer_MediaEnded;
                
                // Play the music
                _currentBackgroundMusic.Play();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error playing background music: {ex.Message}");
            }
        }
        
        public void PlayLocationMusic(LocationType locationType)
        {
            try
            {
                // Choose appropriate music based on location type
                MusicType musicType;
                
                switch (locationType)
                {
                    case LocationType.Village:
                        musicType = MusicType.Village;
                        break;
                    case LocationType.Forest:
                        musicType = MusicType.Forest;
                        break;
                    case LocationType.Cave:
                        musicType = MusicType.Cave;
                        break;
                    default:
                        musicType = MusicType.MainMenu;
                        break;
                }
                
                // Play the selected music
                PlayMusic(musicType);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error playing location music: {ex.Message}");
            }
        }
        
        public void PlaySound(SoundType soundType)
        {
            if (!AreSoundEffectsEnabled)
                return;
                
            try
            {
                if (_soundPlayers.TryGetValue(soundType, out var player))
                {
                    player.Play();
                }
                else
                {
                    // Try to load on demand if not preloaded
                    if (_soundPaths.TryGetValue(soundType, out var path) && File.Exists(path))
                    {
                        try
                        {
                            var newPlayer = new SoundPlayer(path);
                            newPlayer.Play();
                            _soundPlayers[soundType] = newPlayer;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error playing sound {soundType} from {path}: {ex.Message}");
                        }
                    }
                    else
                    {
                        // Sound file doesn't exist, just log and continue silently
                        Console.WriteLine($"Sound file not found for {soundType}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error playing sound {soundType}: {ex.Message}");
            }
        }
        
        private void MediaPlayer_MediaEnded(object sender, EventArgs e)
        {
            // Loop music when it ends
            if (sender is MediaPlayer player)
            {
                player.Position = TimeSpan.Zero;
                player.Play();
            }
        }
        
        public void StopMusic()
        {
            if (_currentBackgroundMusic != null)
            {
                _currentBackgroundMusic.Stop();
                _currentBackgroundMusic = null;
                _currentBackgroundMusicType = 0;
            }
        }

        // Add the PlayMusic method
        public void PlayMusic(MusicType musicType)
        {
            try
            {
                string musicPath = GetMusicPath(musicType);
                PlayBackgroundMusic(musicPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error playing music: {ex.Message}");
            }
        }

        // Add the GetMusicPath helper method
        private string GetMusicPath(MusicType musicType)
        {
            switch (musicType)
            {
                case MusicType.MainMenu:
                    return "Assets/Sounds/Music/main_menu.mp3";
                case MusicType.Battle:
                    return "Assets/Sounds/Music/battle.mp3";
                case MusicType.BossBattle:
                    return "Assets/Sounds/Music/boss_battle.mp3";
                case MusicType.Village:
                    return "Assets/Sounds/Music/village.mp3";
                case MusicType.Forest:
                    return "Assets/Sounds/Music/forest.mp3";
                case MusicType.Cave:
                    return "Assets/Sounds/Music/cave.mp3";
                case MusicType.Victory:
                    return "Assets/Sounds/Music/victory.mp3";
                case MusicType.Defeat:
                    return "Assets/Sounds/Music/defeat.mp3";
                default:
                    return "Assets/Sounds/Music/main_menu.mp3";
            }
        }

        // Method to get path from SoundType
        private string GetSoundPath(SoundType soundType)
        {
            switch (soundType)
            {
                case SoundType.MainTheme:
                    return "Sounds/main_theme.mp3";
                case SoundType.BattleTheme:
                    return "Sounds/battle_theme.mp3";
                case SoundType.ShopTheme:
                    return "Sounds/shop_theme.mp3";
                case SoundType.MapTheme:
                    return "Sounds/map_theme.mp3";
                case SoundType.VictorySound:
                    return "Sounds/victory.mp3";
                case SoundType.DefeatSound:
                    return "Sounds/defeat.mp3";
                default:
                    return "Sounds/main_theme.mp3";
            }
        }

        // Method overload to handle SoundType parameters
        public void PlayBackgroundMusic(SoundType soundType)
        {
            PlayBackgroundMusic(GetSoundPath(soundType));
            _currentBackgroundMusicType = soundType;
        }
    }
} 