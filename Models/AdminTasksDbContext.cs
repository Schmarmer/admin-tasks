using Microsoft.EntityFrameworkCore;

namespace Admin_Tasks.Models;

public class AdminTasksDbContext : DbContext
{
    public AdminTasksDbContext(DbContextOptions<AdminTasksDbContext> options) : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.ConfigureWarnings(warnings =>
            warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
    }

    public DbSet<User> Users { get; set; }
    public DbSet<TaskItem> Tasks { get; set; }
    public DbSet<TaskComment> TaskComments { get; set; }
    public DbSet<TaskAttachment> TaskAttachments { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<TaskCompletionDetails> TaskCompletionDetails { get; set; }
    public DbSet<TaskCategory> TaskCategories { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User Configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
            
            entity.Property(e => e.Role).HasDefaultValue("User");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            
            // Use different SQL functions based on database provider
            if (Database.IsNpgsql())
            {
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            }
            else if (Database.IsSqlite())
            {
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("datetime('now')");
            }
        });

        // TaskItem Configuration
        modelBuilder.Entity<TaskItem>(entity =>
        {
            entity.Property(e => e.Status).HasDefaultValue(Models.TaskStatus.Open);
            entity.Property(e => e.Priority)
                  .HasConversion<int>()
                  .HasDefaultValue(TaskPriority.Medium)
                  .HasSentinel(TaskPriority.Unspecified);
            
            // Use different SQL functions based on database provider
            if (Database.IsNpgsql())
            {
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("NOW()");
            }
            else if (Database.IsSqlite())
            {
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("datetime('now')");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("datetime('now')");
            }

            // Relationships
            entity.HasOne(t => t.CreatedByUser)
                  .WithMany(u => u.CreatedTasks)
                  .HasForeignKey(t => t.CreatedByUserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(t => t.AssignedToUser)
                  .WithMany(u => u.AssignedTasks)
                  .HasForeignKey(t => t.AssignedToUserId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // TaskComment Configuration
        modelBuilder.Entity<TaskComment>(entity =>
        {
            // Use different SQL functions based on database provider
            if (Database.IsNpgsql())
            {
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            }
            else if (Database.IsSqlite())
            {
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("datetime('now')");
            }

            entity.Property(e => e.Type).HasDefaultValue(CommentType.Normal);
            entity.Property(e => e.IsRead).HasDefaultValue(false);
            entity.Property(e => e.IsEdited).HasDefaultValue(false);

            entity.HasOne(tc => tc.Task)
                  .WithMany(t => t.Comments)
                  .HasForeignKey(tc => tc.TaskId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(tc => tc.User)
                  .WithMany()
                  .HasForeignKey(tc => tc.UserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(tc => tc.ParentComment)
                  .WithMany(pc => pc.Replies)
                  .HasForeignKey(tc => tc.ParentCommentId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Notification Configuration
        modelBuilder.Entity<Notification>(entity =>
        {
            // Use different SQL functions based on database provider
            if (Database.IsNpgsql())
            {
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            }
            else if (Database.IsSqlite())
            {
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("datetime('now')");
            }

            entity.Property(e => e.IsRead).HasDefaultValue(false);

            entity.HasOne(n => n.User)
                  .WithMany()
                  .HasForeignKey(n => n.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(n => n.Task)
                  .WithMany()
                  .HasForeignKey(n => n.TaskId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // TaskCompletionDetails Configuration
        modelBuilder.Entity<TaskCompletionDetails>(entity =>
        {
            // Use different SQL functions based on database provider
            if (Database.IsNpgsql())
            {
                entity.Property(e => e.CompletedAt).HasDefaultValueSql("NOW()");
            }
            else if (Database.IsSqlite())
            {
                entity.Property(e => e.CompletedAt).HasDefaultValueSql("datetime('now')");
            }

            entity.HasOne(tcd => tcd.Task)
                  .WithOne()
                  .HasForeignKey<TaskCompletionDetails>(tcd => tcd.TaskId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(tcd => tcd.CompletedByUser)
                  .WithMany()
                  .HasForeignKey(tcd => tcd.CompletedByUserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // TaskAttachment Configuration
        modelBuilder.Entity<TaskAttachment>(entity =>
        {
            // Use different SQL functions based on database provider
            if (Database.IsNpgsql())
            {
                entity.Property(e => e.UploadedAt).HasDefaultValueSql("NOW()");
            }
            else if (Database.IsSqlite())
            {
                entity.Property(e => e.UploadedAt).HasDefaultValueSql("datetime('now')");
            }

            entity.HasOne(ta => ta.Task)
                  .WithMany(t => t.Attachments)
                  .HasForeignKey(ta => ta.TaskId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ta => ta.User)
                   .WithMany()
                   .HasForeignKey(ta => ta.UploadedBy)
                   .OnDelete(DeleteBehavior.Restrict);
         });

        // Seed Data
        SeedData(modelBuilder);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        // Seed Admin User
        modelBuilder.Entity<User>().HasData(
            new User
            {
                Id = 1,
                Username = "admin",
                Email = "admin@company.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"), // Default password
                Role = "Admin",
                FirstName = "System",
                LastName = "Administrator",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new User
            {
                Id = 2,
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
                Id = 3,
                Username = "user",
                Email = "user@company.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("user123"),
                Role = "User",
                FirstName = "Test",
                LastName = "User",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        );
    }
}