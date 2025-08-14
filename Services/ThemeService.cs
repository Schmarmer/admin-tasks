using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace Admin_Tasks.Services
{
    public class ThemeService : IThemeService
    {
        private bool _isDarkMode;
        
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
                }
            }
        }
        
        public event EventHandler<bool>? ThemeChanged;
        
        public ThemeService()
        {
            // Standardmäßig Light Mode
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
        
        private void ApplyTheme()
        {
            var app = Application.Current;
            if (app?.Resources == null) return;

            // Update existing color resources
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
                { "PrimaryBackgroundBrush", new SolidColorBrush(Color.FromRgb(255, 255, 255)) },
                { "SecondaryBackgroundBrush", new SolidColorBrush(Color.FromRgb(248, 249, 250)) },
                { "TertiaryBackgroundBrush", new SolidColorBrush(Color.FromRgb(233, 236, 239)) },
                { "PrimaryTextBrush", new SolidColorBrush(Color.FromRgb(33, 37, 41)) },
                { "SecondaryTextBrush", new SolidColorBrush(Color.FromRgb(108, 117, 125)) },
                { "AccentBrush", new SolidColorBrush(Color.FromRgb(13, 110, 253)) },
                { "AccentHoverBrush", new SolidColorBrush(Color.FromRgb(11, 94, 215)) },
                { "BorderBrush", new SolidColorBrush(Color.FromRgb(222, 226, 230)) },
                { "SuccessBrush", new SolidColorBrush(Color.FromRgb(25, 135, 84)) },
                { "WarningBrush", new SolidColorBrush(Color.FromRgb(255, 193, 7)) },
                { "DangerBrush", new SolidColorBrush(Color.FromRgb(220, 53, 69)) },
                { "InfoBrush", new SolidColorBrush(Color.FromRgb(13, 202, 240)) }
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