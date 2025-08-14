using Admin_Tasks.Models;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace Admin_Tasks.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly AdminTasksDbContext _context;
    private User? _currentUser;

    public AuthenticationService(AdminTasksDbContext context)
    {
        _context = context;
    }

    public User? CurrentUser
    {
        get => _currentUser;
        private set
        {
            _currentUser = value;
            UserChanged?.Invoke(this, value);
        }
    }

    public bool IsLoggedIn => CurrentUser != null;

    public event EventHandler<User?>? UserChanged;

    public async Task<User?> LoginAsync(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return null;

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == username && u.IsActive);

        if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            return null;

        // Update last login time
        user.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        CurrentUser = user;
        return user;
    }

    public async Task<bool> RegisterUserAsync(User user, string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
            return false;

        // Check if username or email already exists
        var existingUser = await _context.Users
            .AnyAsync(u => u.Username == user.Username || u.Email == user.Email);

        if (existingUser)
            return false;

        // Hash password
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
        user.CreatedAt = DateTime.UtcNow;

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return true;
    }

    public Task LogoutAsync()
    {
        CurrentUser = null;
        return Task.CompletedTask;
    }
}