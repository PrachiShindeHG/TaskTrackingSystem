using UserService.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface IUserRepository
{
    Task<List<User>> GetAllUserAsync();
    Task<User> GetUserByIdAsync(string id);
    Task<User> CreateUserAsync(User user);
    Task UpdateUserAsync(string id, User user);
    Task DeleteAsync(string id);
    Task<User> AuthenticateAsync(string username, string password);
}