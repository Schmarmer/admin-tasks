using System.Windows;
using System.Windows.Controls;
using Admin_Tasks.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Admin_Tasks.Views
{
    /// <summary>
    /// Interaktionslogik für UserManagementView.xaml
    /// </summary>
    public partial class UserManagementView : Window
    {
        private readonly UserManagementViewModel _viewModel;
        
        public UserManagementView(UserManagementViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;
            
            // Event-Handler für PasswordBox-Bindung
            PasswordBox.PasswordChanged += OnPasswordChanged;
            ConfirmPasswordBox.PasswordChanged += OnConfirmPasswordChanged;
            
            Loaded += OnLoaded;
        }
        
        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            await _viewModel.InitializeAsync();
        }
        
        private void OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox passwordBox)
            {
                _viewModel.NewPassword = passwordBox.Password;
            }
        }
        
        private void OnConfirmPasswordChanged(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox passwordBox)
            {
                _viewModel.ConfirmPassword = passwordBox.Password;
            }
        }
        
        protected override void OnClosed(System.EventArgs e)
        {
            // Event-Handler entfernen
            PasswordBox.PasswordChanged -= OnPasswordChanged;
            ConfirmPasswordBox.PasswordChanged -= OnConfirmPasswordChanged;
            
            base.OnClosed(e);
        }
    }
}