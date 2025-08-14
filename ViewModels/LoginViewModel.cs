using System.Windows.Input;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.Input;
using Admin_Tasks.Services;
using Admin_Tasks.Models;

namespace Admin_Tasks.ViewModels;

public class LoginViewModel : BaseViewModel
{
    private readonly IAuthenticationService _authService;
    private string _username = string.Empty;
    private string _password = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _rememberMe;

    public LoginViewModel(IAuthenticationService authService)
    {
        _authService = authService;
        Title = "Anmeldung";
        
        LoginCommand = new AsyncRelayCommand<PasswordBox>(LoginAsync, CanLogin);
        ClearCommand = new RelayCommand(Clear);
    }

    public string Username
    {
        get => _username;
        set
        {
            SetProperty(ref _username, value);
            LoginCommand.NotifyCanExecuteChanged();
            if (!string.IsNullOrEmpty(ErrorMessage))
                ErrorMessage = string.Empty;
        }
    }

    public string Password
    {
        get => _password;
        set
        {
            SetProperty(ref _password, value);
            LoginCommand.NotifyCanExecuteChanged();
            if (!string.IsNullOrEmpty(ErrorMessage))
                ErrorMessage = string.Empty;
        }
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public bool RememberMe
    {
        get => _rememberMe;
        set => SetProperty(ref _rememberMe, value);
    }

    public IAsyncRelayCommand LoginCommand { get; }
    public ICommand ClearCommand { get; }

    public event EventHandler<User>? LoginSuccessful;

    private bool CanLogin(PasswordBox? passwordBox)
    {
        var password = passwordBox?.Password ?? Password;
        return !IsBusy && 
               !string.IsNullOrWhiteSpace(Username) && 
               !string.IsNullOrWhiteSpace(password);
    }

    private async Task LoginAsync(PasswordBox? passwordBox)
    {
        ErrorMessage = string.Empty;
        
        // Passwort aus PasswordBox extrahieren
        var password = passwordBox?.Password ?? Password;

        var user = await ExecuteAsync(async () =>
        {
            return await _authService.LoginAsync(Username.Trim(), password);
        });

        if (user != null)
        {
            LoginSuccessful?.Invoke(this, user);
            Clear();
        }
        else
        {
            ErrorMessage = "Ungültiger Benutzername oder Passwort.";
            if (passwordBox != null)
                passwordBox.Password = string.Empty;
            else
                Password = string.Empty;
        }
    }

    private void Clear()
    {
        Username = string.Empty;
        Password = string.Empty;
        ErrorMessage = string.Empty;
        RememberMe = false;
    }

    protected override void OnBusyChanged()
    {
        LoginCommand.NotifyCanExecuteChanged();
    }
}