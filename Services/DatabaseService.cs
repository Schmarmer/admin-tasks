using Microsoft.EntityFrameworkCore;
using Admin_Tasks.Models;

namespace Admin_Tasks.Services;

public class DatabaseService : IDatabaseService
{
    private readonly AdminTasksDbContext _context;

    public DatabaseService(AdminTasksDbContext context)
    {
        _context = context;
    }

    public async Task InitializeDatabaseAsync()
    {
        try
        {
            // Ensure database is created
            await _context.Database.EnsureCreatedAsync();
            
            // Apply any pending migrations
            var pendingMigrations = await _context.Database.GetPendingMigrationsAsync();
            if (pendingMigrations.Any())
            {
                await _context.Database.MigrateAsync();
            }

            // Seed initial data if database is empty
            if (!await _context.Users.AnyAsync())
            {
                await SeedDataAsync();
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Fehler bei der Datenbankinitialisierung: {ex.Message}", ex);
        }
    }

    public async Task<bool> DatabaseExistsAsync()
    {
        try
        {
            return await _context.Database.CanConnectAsync();
        }
        catch
        {
            return false;
        }
    }

    public async Task CreateDatabaseAsync()
    {
        await _context.Database.EnsureCreatedAsync();
    }

    public async Task SeedDataAsync()
    {
        // Check if data already exists
        if (await _context.Users.AnyAsync())
            return;

        // Create default users
        var users = new List<User>
        {
            new User
            {
                Username = "admin",
                Email = "admin@company.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                Role = "Admin",
                FirstName = "System",
                LastName = "Administrator",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new User
            {
                Username = "manager",
                Email = "manager@company.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("manager123"),
                Role = "Manager",
                FirstName = "Team",
                LastName = "Manager",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new User
            {
                Username = "user1",
                Email = "user1@company.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("user123"),
                Role = "User",
                FirstName = "Max",
                LastName = "Mustermann",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new User
            {
                Username = "user2",
                Email = "user2@company.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("user123"),
                Role = "User",
                FirstName = "Anna",
                LastName = "Schmidt",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        };

        await _context.Users.AddRangeAsync(users);
        await _context.SaveChangesAsync();

        // Create sample tasks
        var adminUser = users.First(u => u.Username == "admin");
        var managerUser = users.First(u => u.Username == "manager");
        var user1 = users.First(u => u.Username == "user1");
        var user2 = users.First(u => u.Username == "user2");

        var sampleTasks = new List<TaskItem>
        {
            new TaskItem
            {
                Title = "System Setup abschließen",
                Description = "Finale Konfiguration des Admin-Task-Systems durchführen und alle Benutzerkonten testen.",
                Status = Models.TaskStatus.InProgress,
                Priority = TaskPriority.High,
                CreatedByUserId = adminUser.Id,
                AssignedToUserId = managerUser.Id,
                DueDate = DateTime.UtcNow.AddDays(3),
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            },
            new TaskItem
            {
                Title = "Benutzerhandbuch erstellen",
                Description = "Ein umfassendes Benutzerhandbuch für das Aufgabenverwaltungssystem erstellen.",
                Status = Models.TaskStatus.Open,
                Priority = TaskPriority.Medium,
                CreatedByUserId = managerUser.Id,
                AssignedToUserId = user1.Id,
                DueDate = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            },
            new TaskItem
            {
                Title = "Backup-Strategie implementieren",
                Description = "Eine automatische Backup-Lösung für die SQLite-Datenbank einrichten.",
                Status = Models.TaskStatus.Open,
                Priority = TaskPriority.High,
                CreatedByUserId = adminUser.Id,
                DueDate = DateTime.UtcNow.AddDays(5),
                CreatedAt = DateTime.UtcNow.AddHours(-12),
                UpdatedAt = DateTime.UtcNow.AddHours(-12)
            },
            new TaskItem
            {
                Title = "UI-Tests durchführen",
                Description = "Umfassende Tests der Benutzeroberfläche in verschiedenen Szenarien.",
                Status = Models.TaskStatus.Completed,
                Priority = TaskPriority.Medium,
                CreatedByUserId = managerUser.Id,
                AssignedToUserId = user2.Id,
                CompletedAt = DateTime.UtcNow.AddHours(-6),
                CreatedAt = DateTime.UtcNow.AddDays(-3),
                UpdatedAt = DateTime.UtcNow.AddHours(-6)
            },
            new TaskItem
            {
                Title = "Performance-Optimierung",
                Description = "Datenbankabfragen optimieren und Ladezeiten verbessern.",
                Status = Models.TaskStatus.OnHold,
                Priority = TaskPriority.Low,
                CreatedByUserId = adminUser.Id,
                AssignedToUserId = user1.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-4),
                UpdatedAt = DateTime.UtcNow.AddDays(-2)
            }
        };

        await _context.Tasks.AddRangeAsync(sampleTasks);
        await _context.SaveChangesAsync();

        // Add some sample comments
        var completedTask = sampleTasks.First(t => t.Status == Models.TaskStatus.Completed);
            var inProgressTask = sampleTasks.First(t => t.Status == Models.TaskStatus.InProgress);

        var sampleComments = new List<TaskComment>
        {
            new TaskComment
            {
                TaskId = completedTask.Id,
                UserId = user2.Id,
                Content = "Tests erfolgreich abgeschlossen. Alle Funktionen arbeiten wie erwartet.",
                CreatedAt = DateTime.UtcNow.AddHours(-7)
            },
            new TaskComment
            {
                TaskId = completedTask.Id,
                UserId = managerUser.Id,
                Content = "Ausgezeichnete Arbeit! Die Ergebnisse sehen sehr gut aus.",
                CreatedAt = DateTime.UtcNow.AddHours(-6)
            },
            new TaskComment
            {
                TaskId = inProgressTask.Id,
                UserId = managerUser.Id,
                Content = "Bitte bis Freitag abschließen. Bei Fragen gerne melden.",
                CreatedAt = DateTime.UtcNow.AddHours(-18)
            },
            new TaskComment
            {
                TaskId = inProgressTask.Id,
                UserId = adminUser.Id,
                Content = "Priorität wurde auf Hoch gesetzt aufgrund der Deadline.",
                CreatedAt = DateTime.UtcNow.AddHours(-12)
            }
        };

        await _context.TaskComments.AddRangeAsync(sampleComments);
        await _context.SaveChangesAsync();
    }
}