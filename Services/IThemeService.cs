using System;
using System.Threading.Tasks;

namespace Admin_Tasks.Services
{
    public interface IThemeService
    {
        bool IsDarkMode { get; }
        event EventHandler<bool>? ThemeChanged;
        void ToggleTheme();
        void SetTheme(bool isDarkMode);
        
        /// <summary>
        /// Initialisiert das Theme aus den gespeicherten Einstellungen
        /// </summary>
        Task InitializeThemeAsync();
    }
}