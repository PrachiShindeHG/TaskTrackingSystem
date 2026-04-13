using Microsoft.AspNetCore.Mvc;
using UserService.Models;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace UserService.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UsersController : ControllerBase
    {
        private readonly IUserRepository _repository;
        private readonly ILogger<UsersController> _logger;

        public UsersController(IUserRepository repository, ILogger<UsersController> logger)
        {
            _repository = repository;
            _logger = logger;
        }
        /// <summary>
        /// Get all users
        /// </summary>
        /// <returns>Users</returns>
        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
            try
            {
                var users = await _repository.GetAllUserAsync();
                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get user by userid
        /// </summary>
        /// <param name="id"></param>
        /// <returns>User</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(string id)
        {
            try
            {
                var user = await _repository.GetUserByIdAsync(id);
                if (user == null) return NotFound();
                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by ID");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Creates user
        /// </summary>
        /// <param name="user"></param>
        /// <returns>Created user id</returns>
        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] User user)
        {
            try
            {
                if (user == null) return BadRequest("Invalid user data");
                var createdUser = await _repository.CreateUserAsync(user);
                return CreatedAtAction(nameof(GetUserById), new { id = createdUser.Id }, createdUser);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Update user
        /// </summary>
        /// <param name="id"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(string id, [FromBody] User user)
        {
            try
            {
                if (user == null) return BadRequest("Invalid user data");
                var existing = await _repository.GetUserByIdAsync(id);
                if (existing == null) return NotFound();
                user.Id = id; // Ensure ID matches
                if (string.IsNullOrEmpty(user.Password))
                {
                    user.Password = existing.Password;
                }
                await _repository.UpdateUserAsync(id, user);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Deletes user
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            try
            {
                var existing = await _repository.GetUserByIdAsync(id);
                if (existing == null)
                    return NotFound($"User with id {id} not found");

                //Admin role check
                if (!Request.Headers.TryGetValue("Authorization", out var authHeader))
                    return Unauthorized("Authorization header is missing");

                var token = authHeader.ToString().Trim();
                if (!token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    return Unauthorized("Invalid token format");

                token = token["Bearer ".Length..].Trim(); // Remove "Bearer "

                var tokenParts = token.Split('_');
                var tokenRole = tokenParts.Length >= 2 ? tokenParts[^1] : string.Empty;
                if (!tokenRole.Equals("Admin", System.StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Delete attempt by non-Admin. Token: {Token}", token);
                    return Forbid("Only Admin can delete users");
                }

                await _repository.DeleteAsync(id);
                _logger.LogInformation("User {UserId} deleted by Admin", id);
                return NoContent(); // 204
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId}", id);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}