using MongoDB.Driver;
using Microsoft.Extensions.Options;
using TaskService.Models;

namespace ReportingService.Repositories
{
    public class MongoTaskRepository : ITaskRepository
    {
        private readonly IMongoCollection<TaskItem> _tasks;

        public MongoTaskRepository(IOptions<MongoSettings> settings)
        {
            var client = new MongoClient(settings.Value.ConnectionString);
            var database = client.GetDatabase(settings.Value.DatabaseName);
            _tasks = database.GetCollection<TaskItem>("Tasks");
        }

        public async Task<List<TaskItem>> GetAllAsync() =>
            await _tasks.Find(_ => true).ToListAsync();

        public async Task<TaskItem?> GetByIdAsync(string id) =>
            await _tasks.Find(t => t.Id == id).FirstOrDefaultAsync();

        public async Task<List<TaskItem>> FilterAsync(string? status, string? assigneeId, DateTime? from, DateTime? to)
        {
            var filter = Builders<TaskItem>.Filter.Empty;

            if (!string.IsNullOrEmpty(status))
                filter &= Builders<TaskItem>.Filter.Eq(t => t.Status, status);
            if (!string.IsNullOrEmpty(assigneeId))
                filter &= Builders<TaskItem>.Filter.Eq(t => t.AssigneeId, assigneeId);
            if (from.HasValue)
                filter &= Builders<TaskItem>.Filter.Gte(t => t.CreatedAt, from.Value);
            if (to.HasValue)
                filter &= Builders<TaskItem>.Filter.Lte(t => t.CreatedAt, to.Value);

            return await _tasks.Find(filter).ToListAsync();
        }

        public async Task DeleteAsync(string id) =>
            await _tasks.DeleteOneAsync(t => t.Id == id);
    }
}