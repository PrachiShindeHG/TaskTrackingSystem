using MongoDB.Driver;
using Microsoft.Extensions.Options;
using UserService.Models; // For MongoSettings

public class MongoUserRepository : IUserRepository
{
    private readonly IMongoCollection<User> _users;

    public MongoUserRepository(IOptions<MongoSettings> mongoSettings)
    {
        var client = new MongoClient(mongoSettings.Value.ConnectionString);
        var database = client.GetDatabase(mongoSettings.Value.DatabaseName);
        _users = database.GetCollection<User>("Users");
    }

    public async Task<List<User>> GetAllUserAsync() => await _users.Find(_ => true).ToListAsync();

    public async Task<User> GetUserByIdAsync(string id) => await _users.Find(u => u.Id == id).FirstOrDefaultAsync();

    public async Task<User> CreateUserAsync(User user)
    {
        await _users.InsertOneAsync(user);
        return user;
    }

    public async Task UpdateUserAsync(string id, User user)
    {
        await _users.ReplaceOneAsync(u => u.Id == id, user);
    }

    public async Task DeleteAsync(string id)
    {
        await _users.DeleteOneAsync(u => u.Id == id);
    }

    public async Task<User> AuthenticateAsync(string username, string password)
    {
        return await _users.Find(u => u.Username == username && u.Password == password).FirstOrDefaultAsync();
    }
}