using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Microsoft.Extensions.Logging;

namespace Admin_Tasks.Services
{
    public class ThemeService : IThemeService
    {
        private const string THEME_SETTING_KEY = "isDarkMode";
        private readonly ISettingsService _settingsService;
        private readonly ILogger<ThemeService>? _logger;
        private bool _isDarkMode;
        private bool _isInitialized = false;
        
        public bool IsDarkMode 
        { 
            get => _isDarkMode;
            private set
            {
                if (_isDarkMode != value)
                {
                    _isDarkMode = value;
                    ApplyTheme();
                    ThemeChanged?.Invoke(this, _isDarkMode);
                    
                    // Theme-Einstellung automatisch speichern (nur wenn bereits initialisiert)
                    if (_isInitialized)
                    {
                        _ = Task.Run(async () => 
                        {
                            try
                            {
                                await _settingsService.SetSettingAsync(THEME_SETTING_KEY, _isDarkMode);
                                _logger?.LogDebug($"Theme-Einstellung gespeichert: {(_isDarkMode ? "Dark" : "Light")}");
                            }
                            catch (Exception ex)
                            {
                                _logger?.LogError(ex, "Fehler beim Speichern der Theme-Einstellung");
                            }
                        });
                    }
                }
            }
        }
        
        public event EventHandler<bool>? ThemeChanged;
        
        public ThemeService(ISettingsService settingsService, ILogger<ThemeService>? logger = null)
        {
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _logger = logger;
            
            // Standardmäßig Light Mode (wird durch InitializeThemeAsync überschrieben)
            _isDarkMode = false;
            ApplyTheme();
        }
        
        public void ToggleTheme()
        {
            IsDarkMode = !IsDarkMode;
        }
        
        public void SetTheme(bool isDarkMode)
        {
            IsDarkMode = isDarkMode;
        }
        
        public async Task InitializeThemeAsync()
        {
            try
            {
                _logger?.LogDebug("Initialisiere Theme aus gespeicherten Einstellungen...");
                
                // Gespeicherte Theme-Einstellung laden
                var savedTheme = await _settingsService.GetSettingAsync(THEME_SETTING_KEY, false); // Standard: Light Mode
                
                _logger?.LogInformation($"Gespeicherte Theme-Einstellung geladen: {(savedTheme ? "Dark" : "Light")}");
                
                // Theme anwenden ohne automatisches Speichern zu triggern
                _isDarkMode = savedTheme;
                ApplyTheme();
                ThemeChanged?.Invoke(this, _isDarkMode);
                
                // Initialisierung abgeschlossen - ab jetzt automatisches Speichern aktivieren
                _isInitialized = true;
                
                _logger?.LogDebug("Theme-Initialisierung abgeschlossen");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Fehler bei der Theme-Initialisierung, verwende Light Mode als Fallback");
                
                // Fallback auf Light Mode
                _isDarkMode = false;
                ApplyTheme();
                ThemeChanged?.Invoke(this, _isDarkMode);
                _isInitialized = true;
            }
        }
        
        private void ApplyTheme()
        {
            var app = Application.Current;
            if (app?.Resources == null) return;

            // Clear existing merged dictionaries
            app.Resources.MergedDictionaries.Clear();
            
            // Load the appropriate theme XAML file
            var themeUri = IsDarkMode 
                ? new Uri("pack://application:,,,/Themes/DarkTheme.xaml", UriKind.Absolute)
                : new Uri("pack://application:,,,/Themes/LightTheme.xaml", UriKind.Absolute);
            
            try
            {
                var themeDict = new ResourceDictionary { Source = themeUri };
                app.Resources.MergedDictionaries.Add(themeDict);
            }
            catch (Exception ex)
            {
                // Fallback to hardcoded colors if XAML loading fails
                System.Diagnostics.Debug.WriteLine($"Failed to load theme XAML: {ex.Message}");
                ApplyFallbackTheme();
            }
        }
        
        private void ApplyFallbackTheme()
        {
            var app = Application.Current;
            if (app?.Resources == null) return;

            var colors = IsDarkMode ? GetDarkThemeColors() : GetLightThemeColors();
            
            foreach (var color in colors)
            {
                if (app.Resources.Contains(color.Key))
                {
                    app.Resources[color.Key] = color.Value;
                }
                else
                {
                    app.Resources.Add(color.Key, color.Value);
                }
            }
        }
        
        private Dictionary<string, SolidColorBrush> GetLightThemeColors()
        {
            return new Dictionary<string, SolidColorBrush>
            {
                { "PrimaryBackgroundBrush", new SolidColorBrush(Color.FromRgb(250, 251, 252)) },
                { "SecondaryBackgroundBrush", new SolidColorBrush(Color.FromRgb(246, 248, 250)) },
                { "TertiaryBackgroundBrush", new SolidColorBrush(Color.FromRgb(234, 238, 242)) },
                { "PrimaryTextBrush", new SolidColorBrush(Color.FromRgb(31, 35, 40)) },
                { "SecondaryTextBrush", new SolidColorBrush(Color.FromRgb(101, 109, 118)) },
                { "AccentBrush", new SolidColorBrush(Color.FromRgb(9, 105, 218)) },
                { "AccentHoverBrush", new SolidColorBrush(Color.FromRgb(5, 80, 174)) },
                { "BorderBrush", new SolidColorBrush(Color.FromRgb(209, 217, 224)) },
                { "SuccessBrush", new SolidColorBrush(Color.FromRgb(26, 127, 55)) },
                { "WarningBrush", new SolidColorBrush(Color.FromRgb(191, 135, 0)) },
                { "DangerBrush", new SolidColorBrush(Color.FromRgb(207, 34, 46)) },
                { "InfoBrush", new SolidColorBrush(Color.FromRgb(9, 105, 218)) },
                { "HoverBackgroundBrush", new SolidColorBrush(Color.FromRgb(243, 244, 246)) },
                { "SelectedBackgroundBrush", new SolidColorBrush(Color.FromRgb(221, 244, 255)) },
                { "FocusBorderBrush", new SolidColorBrush(Color.FromRgb(9, 105, 218)) },
                { "ShadowBrush", new SolidColorBrush(Color.FromArgb(38, 31, 35, 40)) }
            };
        }
        
        private Dictionary<string, SolidColorBrush> GetDarkThemeColors()
        {
            return new Dictionary<string, SolidColorBrush>
            {
                { "PrimaryBackgroundBrush", new SolidColorBrush(Color.FromRgb(30, 30, 30)) },
                { "SecondaryBackgroundBrush", new SolidColorBrush(Color.FromRgb(45, 45, 48)) },
                { "TertiaryBackgroundBrush", new SolidColorBrush(Color.FromRgb(62, 62, 66)) },
                { "PrimaryTextBrush", new SolidColorBrush(Color.FromRgb(255, 255, 255)) },
                { "SecondaryTextBrush", new SolidColorBrush(Color.FromRgb(204, 204, 204)) },
                { "AccentBrush", new SolidColorBrush(Color.FromRgb(0, 120, 212)) },
                { "AccentHoverBrush", new SolidColorBrush(Color.FromRgb(16, 110, 190)) },
                { "BorderBrush", new SolidColorBrush(Color.FromRgb(70, 70, 71)) },
                { "SuccessBrush", new SolidColorBrush(Color.FromRgb(16, 124, 65)) },
                { "WarningBrush", new SolidColorBrush(Color.FromRgb(202, 80, 16)) },
                { "DangerBrush", new SolidColorBrush(Color.FromRgb(209, 52, 56)) },
                { "InfoBrush", new SolidColorBrush(Color.FromRgb(0, 188, 242)) }
            };
        }
    }
}