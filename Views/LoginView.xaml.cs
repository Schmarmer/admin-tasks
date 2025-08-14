using Admin_Tasks.ViewModels;
using Admin_Tasks.Models;
using System.Windows;
using System.Windows.Controls;

namespace Admin_Tasks.Views
{
    /// <summary>
    /// Interaktionslogik für LoginView.xaml
    /// </summary>
    public partial class LoginView : Window
    {
        public LoginView(LoginViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            
            // Event-Handler für erfolgreiche Anmeldung
            viewModel.LoginSuccessful += OnLoginSuccessful;
            
            // Focus auf Username-Feld setzen
            Loaded += (s, e) => UsernameTextBox.Focus();
            
            // Enter-Taste im PasswordBox behandeln
            PasswordBox.KeyDown += (s, e) =>
            {
                if (e.Key == System.Windows.Input.Key.Enter)
                {
                    viewModel.LoginCommand.Execute(PasswordBox);
                }
            };
            
            // PasswordBox-Änderungen an ViewModel weiterleiten
            PasswordBox.PasswordChanged += (s, e) =>
            {
                viewModel.Password = PasswordBox.Password;
            };
        }
        
        private void OnLoginSuccessful(object? sender, User user)
        {
            try
            {
                // MainView öffnen
                var app = (App)Application.Current;
                app.ShowMainWindow();
                
                // LoginView schließen
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Öffnen der Hauptansicht: {ex.Message}\n\nStackTrace: {ex.StackTrace}", 
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        protected override void OnClosed(EventArgs e)
        {
            // Event-Handler entfernen
            if (DataContext is LoginViewModel viewModel)
            {
                viewModel.LoginSuccessful -= OnLoginSuccessful;
            }
            base.OnClosed(e);
        }
    }
}