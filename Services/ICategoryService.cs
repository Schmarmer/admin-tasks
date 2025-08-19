using Admin_Tasks.Models;

namespace Admin_Tasks.Services;

public interface ICategoryService
{
    Task<List<TaskCategory>> GetAllActiveCategoriesAsync();
    Task<TaskCategory?> GetCategoryByIdAsync(int categoryId);
    Task<TaskCategory?> GetCategoryByNameAsync(string name);
    Task<TaskCategory> CreateCategoryAsync(string name, string? description = null, string? color = null);
    Task<TaskCategory> UpdateCategoryAsync(TaskCategory category);
    Task<bool> DeleteCategoryAsync(int categoryId);
    Task<bool> DeactivateCategoryAsync(int categoryId);
    Task<bool> CategoryExistsAsync(string name);
    Task<List<TaskCategory>> GetCategoriesWithTaskCountAsync();
}