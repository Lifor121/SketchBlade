using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows.Threading;

namespace SketchBlade.Services
{
    public interface IConfigService
    {
        T GetValue<T>(string key, T defaultValue = default!);
        void SetValue<T>(string key, T value);
        void Save();
        void Load();
        bool HasKey(string key);
        void RemoveKey(string key);
        
        ObservableCollection<Notification> Notifications { get; }
        void ShowInfo(string message, TimeSpan? duration = null);
        void ShowSuccess(string message, TimeSpan? duration = null);
        void ShowWarning(string message, TimeSpan? duration = null);
        void ShowError(string message, TimeSpan? duration = null);
        void ClearNotifications();
        
        event PropertyChangedEventHandler? PropertyChanged;
    }

    public enum NotificationType
    {
        Info,
        Success,
        Warning,
        Error
    }

    public class Notification : INotifyPropertyChanged
    {
        private bool _isVisible = true;

        public string Message { get; set; } = string.Empty;
        public NotificationType Type { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(3);

        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                if (_isVisible != value)
                {
                    _isVisible = value;
                    OnPropertyChanged();
                }
            }
        }

        public string TypeIcon => Type switch
        {
            NotificationType.Success => "✓",
            NotificationType.Warning => "⚠",
            NotificationType.Error => "✗",
            _ => "ℹ"
        };

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class ConfigService : IConfigService, INotifyPropertyChanged
    {
        private static readonly Lazy<ConfigService> _instance = new(() => new ConfigService());
        public static ConfigService Instance => _instance.Value;

        private readonly Dictionary<string, object> _configuration = new();
        private readonly string _configFilePath;
        private readonly object _lockObject = new object();

        public ObservableCollection<Notification> Notifications { get; } = new();

        public event PropertyChangedEventHandler? PropertyChanged;

        public const string LOGGING_LEVEL = "LoggingLevel";
        public const string LANGUAGE = "Language";
        public const string AUTO_SAVE = "AutoSave";
        public const string SHOW_DEBUG_INFO = "ShowDebugInfo";
        public const string INVENTORY_SORT_MODE = "InventorySortMode";
        public const string ENABLE_NOTIFICATIONS = "EnableNotifications";
        public const string CACHE_SIZE_LIMIT = "CacheSizeLimit";
        public const string UI_SCALE_FACTOR = "UIScaleFactor";
        public const string THEME = "Theme";
        public const string NOTIFICATION_DURATION = "NotificationDuration";

        private ConfigService()
        {
            _configFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SketchBlade", "config.json");
            EnsureConfigDirectoryExists();
            Load();
            SetDefaultValues();
            LoggingService.LogDebug("ConfigService initialized");
        }

        #region Configuration Management

        public T GetValue<T>(string key, T defaultValue = default!)
        {
            try
            {
                lock (_lockObject)
                {
                    if (_configuration.TryGetValue(key, out var value))
                    {
                        if (value is JsonElement jsonElement)
                        {
                            return ConvertJsonElement<T>(jsonElement, defaultValue);
                        }

                        if (value is T directValue)
                        {
                            return directValue;
                        }

                        return (T)Convert.ChangeType(value, typeof(T));
                    }

                    return defaultValue;
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error getting configuration value for key '{key}': {ex.Message}", ex);
                return defaultValue;
            }
        }

        public void SetValue<T>(string key, T value)
        {
            try
            {
                lock (_lockObject)
                {
                    _configuration[key] = value!;
                    OnPropertyChanged(key);
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error setting configuration value for key '{key}': {ex.Message}", ex);
            }
        }

        public void Save()
        {
            try
            {
                lock (_lockObject)
                {
                    var options = new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    };

                    var json = JsonSerializer.Serialize(_configuration, options);
                    File.WriteAllText(_configFilePath, json);
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Error saving configuration", ex);
                ShowError("Failed to save configuration");
            }
        }

        public void Load()
        {
            try
            {
                lock (_lockObject)
                {
                    if (!File.Exists(_configFilePath))
                    {
                        return;
                    }

                    var json = File.ReadAllText(_configFilePath);
                    var loadedConfig = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

                    if (loadedConfig != null)
                    {
                        _configuration.Clear();
                        foreach (var kvp in loadedConfig)
                        {
                            _configuration[kvp.Key] = kvp.Value;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Error loading configuration", ex);
                ShowError("Failed to load configuration, using defaults");
            }
        }

        public bool HasKey(string key)
        {
            lock (_lockObject)
            {
                return _configuration.ContainsKey(key);
            }
        }

        public void RemoveKey(string key)
        {
            try
            {
                lock (_lockObject)
                {
                    if (_configuration.Remove(key))
                    {
                        OnPropertyChanged(key);
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error removing configuration key '{key}': {ex.Message}", ex);
            }
        }

        #endregion

        #region Notification System

        public void ShowInfo(string message, TimeSpan? duration = null)
        {
            if (GetValue(ENABLE_NOTIFICATIONS, true))
            {
                ShowNotification(message, NotificationType.Info, duration);
            }
        }

        public void ShowSuccess(string message, TimeSpan? duration = null)
        {
            if (GetValue(ENABLE_NOTIFICATIONS, true))
            {
                ShowNotification(message, NotificationType.Success, duration);
            }
        }

        public void ShowWarning(string message, TimeSpan? duration = null)
        {
            if (GetValue(ENABLE_NOTIFICATIONS, true))
            {
                ShowNotification(message, NotificationType.Warning, duration);
            }
        }

        public void ShowError(string message, TimeSpan? duration = null)
        {
            if (GetValue(ENABLE_NOTIFICATIONS, true))
            {
                ShowNotification(message, NotificationType.Error, duration ?? TimeSpan.FromSeconds(5));
            }
        }

        public void ClearNotifications()
        {
            try
            {
                if (System.Windows.Application.Current?.Dispatcher != null)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        Notifications.Clear();
                    });
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Error clearing notifications", ex);
            }
        }

        #endregion

        #region Private Methods

        private void ShowNotification(string message, NotificationType type, TimeSpan? duration = null)
        {
            try
            {
                var defaultDuration = GetValue(NOTIFICATION_DURATION, 3.0);
                var notification = new Notification
                {
                    Message = message,
                    Type = type,
                    Duration = duration ?? TimeSpan.FromSeconds(defaultDuration)
                };

                if (System.Windows.Application.Current?.Dispatcher != null)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        Notifications.Add(notification);
                        
                        var maxNotifications = GetValue("MaxNotifications", 5);
                        while (Notifications.Count > maxNotifications)
                        {
                            Notifications.RemoveAt(0);
                        }
                    });

                    var timer = new DispatcherTimer
                    {
                        Interval = notification.Duration
                    };

                    timer.Tick += (s, e) =>
                    {
                        timer.Stop();
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            if (Notifications.Contains(notification))
                            {
                                notification.IsVisible = false;
                                
                                var removeTimer = new DispatcherTimer
                                {
                                    Interval = TimeSpan.FromMilliseconds(300)
                                };
                                
                                removeTimer.Tick += (s2, e2) =>
                                {
                                    removeTimer.Stop();
                                    Notifications.Remove(notification);
                                };
                                
                                removeTimer.Start();
                            }
                        });
                    };

                    timer.Start();
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error showing notification: {ex.Message}", ex);
            }
        }

        private void EnsureConfigDirectoryExists()
        {
            try
            {
                var directory = Path.GetDirectoryName(_configFilePath);
                if (directory != null && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Error creating configuration directory", ex);
            }
        }

        private void SetDefaultValues()
        {
            try
            {
                // Set default values only if they're not already set
                SetDefaultIfNotExists(LOGGING_LEVEL, "Info");
                SetDefaultIfNotExists(LANGUAGE, "ru");
                SetDefaultIfNotExists(AUTO_SAVE, true);
                SetDefaultIfNotExists(SHOW_DEBUG_INFO, false);
                SetDefaultIfNotExists(INVENTORY_SORT_MODE, "ByType");
                SetDefaultIfNotExists(ENABLE_NOTIFICATIONS, true);
                SetDefaultIfNotExists(CACHE_SIZE_LIMIT, 200);
                SetDefaultIfNotExists(UI_SCALE_FACTOR, 1.0);
                SetDefaultIfNotExists(THEME, "Default");
                SetDefaultIfNotExists(NOTIFICATION_DURATION, 3.0);
                SetDefaultIfNotExists("MaxNotifications", 5);

                LoggingService.LogDebug("Default configuration values set");
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Error setting default configuration values", ex);
            }
        }

        private void SetDefaultIfNotExists<T>(string key, T defaultValue)
        {
            if (!HasKey(key))
            {
                SetValue(key, defaultValue);
            }
        }

        private T ConvertJsonElement<T>(JsonElement jsonElement, T defaultValue)
        {
            try
            {
                var targetType = typeof(T);

                if (targetType == typeof(string))
                {
                    return (T)(object)(jsonElement.GetString() ?? string.Empty);
                }
                else if (targetType == typeof(int))
                {
                    return (T)(object)jsonElement.GetInt32();
                }
                else if (targetType == typeof(double))
                {
                    return (T)(object)jsonElement.GetDouble();
                }
                else if (targetType == typeof(bool))
                {
                    return (T)(object)jsonElement.GetBoolean();
                }
                else if (targetType == typeof(float))
                {
                    return (T)(object)(float)jsonElement.GetDouble();
                }

                // For complex types, use standard deserialization
                return JsonSerializer.Deserialize<T>(jsonElement.GetRawText()) ?? defaultValue;
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error converting JsonElement to type {typeof(T).Name}: {ex.Message}", ex);
                return defaultValue;
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
} 