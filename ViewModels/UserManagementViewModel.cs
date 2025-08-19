using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using Admin_Tasks.Models;
using Admin_Tasks.Services;
using BCrypt.Net;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Admin_Tasks.ViewModels
{
    public partial class UserManagementViewModel : BaseViewModel
    {
        private readonly IAuthenticationService _authService;
        private readonly IDatabaseService _databaseService;
        private readonly ILogger<UserManagementViewModel>? _logger;
        
        [ObservableProperty]
        private ObservableCollection<User> _users = new();
        
        [ObservableProperty]
        private User? _selectedUser;
        
        [ObservableProperty]
        private User _newUser = new();
        
        [ObservableProperty]
        private string _newPassword = string.Empty;
        
        [ObservableProperty]
        private string _confirmPassword = string.Empty;
        
        [ObservableProperty]
        private bool _isEditing;
        
        [ObservableProperty]
        private string _searchText = string.Empty;
        
        [ObservableProperty]
        private bool _isLoading;
        
        private ICollectionView? _usersView;
        
        public ICollectionView UsersView
        {
            get
            {
                if (_usersView == null)
                {
                    _usersView = CollectionViewSource.GetDefaultView(Users);
                    _usersView.Filter = FilterUsers;
                }
                return _usersView;
            }
        }
        
        public string[] AvailableRoles { get; } = { "Admin", "Manager", "User" };
        
        public UserManagementViewModel(
            IAuthenticationService authService,
            IDatabaseService databaseService,
            ILogger<UserManagementViewModel>? logger = null)
        {
            _authService = authService;
            _databaseService = databaseService;
            _logger = logger;
            
            Title = "Benutzerverwaltung";
            
            // Commands initialisieren
            LoadUsersCommand = new AsyncRelayCommand(LoadUsersAsync);
            AddUserCommand = new AsyncRelayCommand(AddUserAsync, CanAddUser);
            EditUserCommand = new CommunityToolkit.Mvvm.Input.RelayCommand<User>(EditUser, CanEditUser);
            SaveUserCommand = new AsyncRelayCommand(SaveUserAsync, CanSaveUser);
            DeleteUserCommand = new CommunityToolkit.Mvvm.Input.AsyncRelayCommand<User>(DeleteUserAsync, CanDeleteUser);
            CancelEditCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(CancelEdit);
            SearchCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(PerformSearch);
            
            // Property Changed Events
            PropertyChanged += OnPropertyChanged;
        }
        
        public IAsyncRelayCommand LoadUsersCommand { get; }
        public IAsyncRelayCommand AddUserCommand { get; }
        public IRelayCommand<User> EditUserCommand { get; }
        public IAsyncRelayCommand SaveUserCommand { get; }
        public IAsyncRelayCommand<User> DeleteUserCommand { get; }
        public IRelayCommand CancelEditCommand { get; }
        public IRelayCommand SearchCommand { get; }
        
        public async Task InitializeAsync()
        {
            await LoadUsersAsync();
        }
        
        private async Task LoadUsersAsync()
        {
            try
            {
                IsLoading = true;
                
                var users = await _databaseService.GetAllUsersAsync();
                
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Users.Clear();
                    foreach (var user in users.OrderBy(u => u.Username))
                    {
                        Users.Add(user);
                    }
                });
                
                _logger?.LogInformation($"Loaded {users.Count()} users");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error loading users");
                MessageBox.Show(
                    "Fehler beim Laden der Benutzer. Bitte versuchen Sie es erneut.",
                    "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }
        
        private bool CanAddUser() => !IsLoading && !IsEditing;
        
        private async Task AddUserAsync()
        {
            try
            {
                if (!ValidateNewUser())
                    return;
                
                IsLoading = true;
                
                // Passwort hashen
                var hashedPassword = BCrypt.Net.BCrypt.HashPassword(NewPassword);
                
                var user = new User
                {
                    Username = NewUser.Username.Trim(),
                    Email = NewUser.Email.Trim(),
                    FirstName = NewUser.FirstName.Trim(),
                    LastName = NewUser.LastName.Trim(),
                    Role = NewUser.Role,
                    PasswordHash = hashedPassword,
                    IsActive = NewUser.IsActive,
                    CreatedAt = DateTime.UtcNow
                };
                
                var createdUser = await _databaseService.CreateUserAsync(user);
                
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Users.Add(createdUser);
                    UsersView.Refresh();
                });
                
                ResetNewUser();
                
                MessageBox.Show(
                    $"Benutzer '{createdUser.Username}' wurde erfolgreich erstellt.",
                    "Benutzer erstellt",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                
                _logger?.LogInformation($"User created: {createdUser.Username}");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error creating user");
                MessageBox.Show(
                    "Fehler beim Erstellen des Benutzers. Bitte überprüfen Sie die Eingaben.",
                    "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }
        
        private bool CanEditUser(User? user) => user != null && !IsLoading;
        
        private void EditUser(User? user)
        {
            if (user == null) return;
            
            SelectedUser = user;
            NewUser = new User
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = user.Role,
                IsActive = user.IsActive
            };
            
            NewPassword = string.Empty;
            ConfirmPassword = string.Empty;
            IsEditing = true;
        }
        
        private bool CanSaveUser() => IsEditing && !IsLoading;
        
        private async Task SaveUserAsync()
        {
            try
            {
                if (!ValidateEditUser())
                    return;
                
                IsLoading = true;
                
                if (SelectedUser == null) return;
                
                // Benutzer aktualisieren
                SelectedUser.Username = NewUser.Username.Trim();
                SelectedUser.Email = NewUser.Email.Trim();
                SelectedUser.FirstName = NewUser.FirstName.Trim();
                SelectedUser.LastName = NewUser.LastName.Trim();
                SelectedUser.Role = NewUser.Role;
                SelectedUser.IsActive = NewUser.IsActive;
                
                // Passwort aktualisieren falls angegeben
                if (!string.IsNullOrWhiteSpace(NewPassword))
                {
                    SelectedUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(NewPassword);
                }
                
                await _databaseService.UpdateUserAsync(SelectedUser);
                
                Application.Current.Dispatcher.Invoke(() =>
                {
                    UsersView.Refresh();
                });
                
                MessageBox.Show(
                    $"Benutzer '{SelectedUser.Username}' wurde erfolgreich aktualisiert.",
                    "Benutzer aktualisiert",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                
                CancelEdit();
                
                _logger?.LogInformation($"User updated: {SelectedUser.Username}");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating user");
                MessageBox.Show(
                    "Fehler beim Aktualisieren des Benutzers. Bitte überprüfen Sie die Eingaben.",
                    "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }
        
        private bool CanDeleteUser(User? user) => user != null && !IsLoading;
        
        private async Task DeleteUserAsync(User? user)
        {
            if (user == null) return;
            
            var result = MessageBox.Show(
                $"Sind Sie sicher, dass Sie den Benutzer '{user.Username}' löschen möchten?\n\nDieser Vorgang kann nicht rückgängig gemacht werden.",
                "Benutzer löschen",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            
            if (result != MessageBoxResult.Yes)
                return;
            
            try
            {
                IsLoading = true;
                
                await _databaseService.DeleteUserAsync(user.Id);
                
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Users.Remove(user);
                    UsersView.Refresh();
                });
                
                MessageBox.Show(
                    $"Benutzer '{user.Username}' wurde erfolgreich gelöscht.",
                    "Benutzer gelöscht",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                
                _logger?.LogInformation($"User deleted: {user.Username}");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error deleting user");
                MessageBox.Show(
                    "Fehler beim Löschen des Benutzers. Der Benutzer könnte noch Aufgaben zugewiesen haben.",
                    "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }
        
        private void CancelEdit()
        {
            IsEditing = false;
            SelectedUser = null;
            ResetNewUser();
        }
        
        private void PerformSearch()
        {
            UsersView?.Refresh();
        }
        
        private bool FilterUsers(object obj)
        {
            if (obj is not User user)
                return false;
            
            if (string.IsNullOrWhiteSpace(SearchText))
                return true;
            
            var searchTerm = SearchText.ToLower();
            
            return user.Username.ToLower().Contains(searchTerm) ||
                   user.Email.ToLower().Contains(searchTerm) ||
                   user.FirstName.ToLower().Contains(searchTerm) ||
                   user.LastName.ToLower().Contains(searchTerm) ||
                   user.Role.ToLower().Contains(searchTerm);
        }
        
        private bool ValidateNewUser()
        {
            if (string.IsNullOrWhiteSpace(NewUser.Username))
            {
                MessageBox.Show("Benutzername ist erforderlich.", "Validierungsfehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            
            if (string.IsNullOrWhiteSpace(NewUser.Email))
            {
                MessageBox.Show("E-Mail ist erforderlich.", "Validierungsfehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            
            if (string.IsNullOrWhiteSpace(NewUser.FirstName))
            {
                MessageBox.Show("Vorname ist erforderlich.", "Validierungsfehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            
            if (string.IsNullOrWhiteSpace(NewUser.LastName))
            {
                MessageBox.Show("Nachname ist erforderlich.", "Validierungsfehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            
            if (string.IsNullOrWhiteSpace(NewPassword))
            {
                MessageBox.Show("Passwort ist erforderlich.", "Validierungsfehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            
            if (NewPassword != ConfirmPassword)
            {
                MessageBox.Show("Passwörter stimmen nicht überein.", "Validierungsfehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            
            if (Users.Any(u => u.Username.Equals(NewUser.Username, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show("Benutzername bereits vorhanden.", "Validierungsfehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            
            return true;
        }
        
        private bool ValidateEditUser()
        {
            if (string.IsNullOrWhiteSpace(NewUser.Username))
            {
                MessageBox.Show("Benutzername ist erforderlich.", "Validierungsfehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            
            if (string.IsNullOrWhiteSpace(NewUser.Email))
            {
                MessageBox.Show("E-Mail ist erforderlich.", "Validierungsfehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            
            if (string.IsNullOrWhiteSpace(NewUser.FirstName))
            {
                MessageBox.Show("Vorname ist erforderlich.", "Validierungsfehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            
            if (string.IsNullOrWhiteSpace(NewUser.LastName))
            {
                MessageBox.Show("Nachname ist erforderlich.", "Validierungsfehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            
            if (!string.IsNullOrWhiteSpace(NewPassword) && NewPassword != ConfirmPassword)
            {
                MessageBox.Show("Passwörter stimmen nicht überein.", "Validierungsfehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            
            if (Users.Any(u => u.Id != NewUser.Id && u.Username.Equals(NewUser.Username, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show("Benutzername bereits vorhanden.", "Validierungsfehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            
            return true;
        }
        
        private void ResetNewUser()
        {
            NewUser = new User { Role = "User", IsActive = true };
            NewPassword = string.Empty;
            ConfirmPassword = string.Empty;
        }
        
        private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(SearchText):
                    PerformSearch();
                    break;
                case nameof(NewUser):
                case nameof(NewPassword):
                case nameof(ConfirmPassword):
                    AddUserCommand.NotifyCanExecuteChanged();
                    SaveUserCommand.NotifyCanExecuteChanged();
                    break;
            }
        }
    }
}