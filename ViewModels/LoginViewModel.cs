using System.Windows.Input;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.Input;
using Admin_Tasks.Services;
using Admin_Tasks.Models;
using System.Security.Cryptography;
using System.Text;

namespace Admin_Tasks.ViewModels;

public class LoginViewModel : BaseViewModel
{
    private readonly IAuthenticationService _authService;
    private readonly ISettingsService _settingsService;
    private string _username = string.Empty;
    private string _password = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _rememberMe;

    private const string REMEMBER_USERNAME_KEY = "rememberUsername";
    private const string REMEMBER_PASSWORD_KEY = "rememberPassword";
    private const string REMEMBER_ME_KEY = "rememberMe";

    public LoginViewModel(IAuthenticationService authService, ISettingsService settingsService)
    {
        _authService = authService;
        _settingsService = settingsService;
        Title = "Anmeldung";
        
        LoginCommand = new AsyncRelayCommand<PasswordBox>(LoginAsync, CanLogin);
        ClearCommand = new RelayCommand(Clear);
        
        // Gespeicherte Anmeldedaten laden
        _ = LoadSavedCredentialsAsync();
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
    public event EventHandler<string>? PasswordRestored;

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
            // Anmeldedaten speichern wenn "Angemeldet bleiben" aktiviert ist
            if (RememberMe)
            {
                await SaveCredentialsAsync(Username.Trim(), password);
            }
            else
            {
                await ClearSavedCredentialsAsync();
            }
            
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

    public async Task LoadSavedCredentialsAsync()
    {
        try
        {
            var rememberMe = await _settingsService.GetSettingAsync(REMEMBER_ME_KEY, false);
            if (rememberMe)
            {
                var savedUsername = await _settingsService.GetSettingAsync<string>(REMEMBER_USERNAME_KEY, string.Empty);
                var encryptedPassword = await _settingsService.GetSettingAsync<string>(REMEMBER_PASSWORD_KEY, string.Empty);
                
                if (!string.IsNullOrEmpty(savedUsername) && !string.IsNullOrEmpty(encryptedPassword))
                {
                    var decryptedPassword = DecryptPassword(encryptedPassword);
                    Username = savedUsername;
                    Password = decryptedPassword;
                    RememberMe = true;
                    
                    // Event auslösen, damit die View das Passwort in die PasswordBox setzen kann
                    PasswordRestored?.Invoke(this, decryptedPassword);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Fehler beim Laden der gespeicherten Anmeldedaten: {ex.Message}");
        }
    }

    private async Task SaveCredentialsAsync(string username, string password)
    {
        try
        {
            var encryptedPassword = EncryptPassword(password);
            await _settingsService.SetSettingAsync(REMEMBER_USERNAME_KEY, username);
            await _settingsService.SetSettingAsync(REMEMBER_PASSWORD_KEY, encryptedPassword);
            await _settingsService.SetSettingAsync(REMEMBER_ME_KEY, true);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Fehler beim Speichern der Anmeldedaten: {ex.Message}");
        }
    }

    private async Task ClearSavedCredentialsAsync()
    {
        try
        {
            await _settingsService.RemoveSettingAsync(REMEMBER_USERNAME_KEY);
            await _settingsService.RemoveSettingAsync(REMEMBER_PASSWORD_KEY);
            await _settingsService.SetSettingAsync(REMEMBER_ME_KEY, false);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Fehler beim Löschen der gespeicherten Anmeldedaten: {ex.Message}");
        }
    }

    private string EncryptPassword(string password)
    {
        try
        {
            // Einfache Verschlüsselung mit DPAPI (Data Protection API)
            // Nur für den aktuellen Benutzer entschlüsselbar
            var data = Encoding.UTF8.GetBytes(password);
            var encryptedData = ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(encryptedData);
        }
        catch
        {
            return string.Empty;
        }
    }

    private string DecryptPassword(string encryptedPassword)
    {
        try
        {
            var encryptedData = Convert.FromBase64String(encryptedPassword);
            var data = ProtectedData.Unprotect(encryptedData, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(data);
        }
        catch
        {
            return string.Empty;
        }
    }
}