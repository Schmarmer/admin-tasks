using System;

namespace Admin_Tasks.Services
{
    public interface IThemeService
    {
        bool IsDarkMode { get; }
        event EventHandler<bool>? ThemeChanged;
        void ToggleTheme();
        void SetTheme(bool isDarkMode);
    }
}