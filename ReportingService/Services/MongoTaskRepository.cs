using MongoDB.Driver;
using Microsoft.Extensions.Options;
using TaskService.Models;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace ReportingService.Repositories
{
    public class User
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        public string? Username { get; set; }
    }
    public class MongoTaskRepository : ITaskRepository
    {
        private readonly IMongoCollection<TaskItem> _tasks;
        private readonly IMongoCollection<User> _users;


        public MongoTaskRepository(IOptions<MongoSettings> settings)
        {
            var client = new MongoClient(settings.Value.ConnectionString);
            var database = client.GetDatabase(settings.Value.DatabaseName);
            _tasks = database.GetCollection<TaskItem>("Tasks");
            _users = database.GetCollection<User>("Users");
        }

        public async Task<string?> GetUsernameById(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return null;
            var user = await _users.Find(u => u.Id == userId).FirstOrDefaultAsync();
            return user?.Username;
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

        public async Task<List<TaskItem>> GetMyTasks(string id)
        {
            if (string.IsNullOrEmpty(id))
                return new List<TaskItem>();
            return await _tasks.Find(t => t.AssigneeId == id).ToListAsync();
        }
    }
}