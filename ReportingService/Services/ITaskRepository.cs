using TaskService.Models; 
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ReportingService.Repositories
{
    public interface ITaskRepository
    {
        Task<List<TaskItem>> GetAllAsync();
        Task<TaskItem?> GetByIdAsync(string id);
        Task<List<TaskItem>> FilterAsync(string? status, string? assigneeId, DateTime? from, DateTime? to);
        Task DeleteAsync(string id);

        Task<List<TaskItem>> GetMyTasks(string id);

        Task<string?> GetUsernameById(string userId);

    }
}