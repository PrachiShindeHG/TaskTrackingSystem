using TaskService.Models;

public interface ITaskRepository
{
    Task<List<TaskItem>> GetAllAsync();
    Task<TaskItem?> GetByIdAsync(string id);
    Task<TaskItem> CreateAsync(TaskItem task);
    Task UpdateAsync(string id, TaskItem task);
    Task<List<TaskItem>> FilterAsync(string? status, string? assigneeId, DateTime? from, DateTime? to);

    Task DeleteAsync(string id);

    Task<string?> GetUsernameById(string id);
}