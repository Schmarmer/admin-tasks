using Admin_Tasks.Models;
using Microsoft.EntityFrameworkCore;

namespace Admin_Tasks.Services;

public class CategoryService : ICategoryService
{
    private readonly AdminTasksDbContext _context;

    public CategoryService(AdminTasksDbContext context)
    {
        _context = context;
    }

    public async Task<List<TaskCategory>> GetAllActiveCategoriesAsync()
    {
        return await _context.TaskCategories
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<TaskCategory?> GetCategoryByIdAsync(int categoryId)
    {
        return await _context.TaskCategories
            .FirstOrDefaultAsync(c => c.Id == categoryId);
    }

    public async Task<TaskCategory?> GetCategoryByNameAsync(string name)
    {
        return await _context.TaskCategories
            .FirstOrDefaultAsync(c => c.Name.ToLower() == name.ToLower());
    }

    public async Task<TaskCategory> CreateCategoryAsync(string name, string? description = null, string? color = null)
    {
        // Prüfen ob Kategorie bereits existiert
        var existingCategory = await GetCategoryByNameAsync(name);
        if (existingCategory != null)
        {
            if (!existingCategory.IsActive)
            {
                // Reaktiviere deaktivierte Kategorie
                existingCategory.IsActive = true;
                existingCategory.Description = description ?? existingCategory.Description;
                existingCategory.Color = color ?? existingCategory.Color;
                await _context.SaveChangesAsync();
                return existingCategory;
            }
            throw new InvalidOperationException($"Eine Kategorie mit dem Namen '{name}' existiert bereits.");
        }

        var category = new TaskCategory
        {
            Name = name.Trim(),
            Description = description?.Trim(),
            Color = color ?? "#007ACC", // Standard-Blau
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _context.TaskCategories.Add(category);
        await _context.SaveChangesAsync();
        return category;
    }

    public async Task<TaskCategory> UpdateCategoryAsync(TaskCategory category)
    {
        var existingCategory = await GetCategoryByIdAsync(category.Id);
        if (existingCategory == null)
        {
            throw new ArgumentException($"Kategorie mit ID {category.Id} wurde nicht gefunden.");
        }

        // Prüfen ob Name bereits von anderer Kategorie verwendet wird
        var duplicateCategory = await _context.TaskCategories
            .FirstOrDefaultAsync(c => c.Name.ToLower() == category.Name.ToLower() && c.Id != category.Id);
        
        if (duplicateCategory != null)
        {
            throw new InvalidOperationException($"Eine andere Kategorie mit dem Namen '{category.Name}' existiert bereits.");
        }

        existingCategory.Name = category.Name.Trim();
        existingCategory.Description = category.Description?.Trim();
        existingCategory.Color = category.Color;
        existingCategory.IsActive = category.IsActive;

        await _context.SaveChangesAsync();
        return existingCategory;
    }

    public async Task<bool> DeleteCategoryAsync(int categoryId)
    {
        var category = await GetCategoryByIdAsync(categoryId);
        if (category == null)
        {
            return false;
        }

        // Prüfen ob Kategorie von Tasks verwendet wird
        var tasksUsingCategory = await _context.Tasks
            .AnyAsync(t => t.CategoryId == categoryId);

        if (tasksUsingCategory)
        {
            throw new InvalidOperationException("Die Kategorie kann nicht gelöscht werden, da sie von Tasks verwendet wird. Deaktivieren Sie sie stattdessen.");
        }

        _context.TaskCategories.Remove(category);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeactivateCategoryAsync(int categoryId)
    {
        var category = await GetCategoryByIdAsync(categoryId);
        if (category == null)
        {
            return false;
        }

        category.IsActive = false;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> CategoryExistsAsync(string name)
    {
        return await _context.TaskCategories
            .AnyAsync(c => c.Name.ToLower() == name.ToLower());
    }

    public async Task<List<TaskCategory>> GetCategoriesWithTaskCountAsync()
    {
        return await _context.TaskCategories
            .Include(c => c.Tasks)
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }
}