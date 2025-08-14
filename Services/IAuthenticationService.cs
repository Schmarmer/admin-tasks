using Admin_Tasks.Models;

namespace Admin_Tasks.Services;

public interface IAuthenticationService
{
    Task<User?> LoginAsync(string username, string password);
    Task<bool> RegisterUserAsync(User user, string password);
    Task LogoutAsync();
    User? CurrentUser { get; }
    bool IsLoggedIn { get; }
    event EventHandler<User?>? UserChanged;
}