namespace Admin_Tasks.Services;

public interface IDatabaseService
{
    Task InitializeDatabaseAsync();
    Task<bool> DatabaseExistsAsync();
    Task CreateDatabaseAsync();
    Task SeedDataAsync();
}