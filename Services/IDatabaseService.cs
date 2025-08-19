using Admin_Tasks.Models;

namespace Admin_Tasks.Services;

public interface IDatabaseService
{
    Task InitializeDatabaseAsync();
    Task<bool> DatabaseExistsAsync();
    Task CreateDatabaseAsync();
    Task SeedDataAsync();
    
    // User Management
    Task<IEnumerable<User>> GetAllUsersAsync();
    Task<User?> GetUserByIdAsync(int userId);
    Task<User> CreateUserAsync(User user);
    Task<User> UpdateUserAsync(User user);
    Task DeleteUserAsync(int userId);
}