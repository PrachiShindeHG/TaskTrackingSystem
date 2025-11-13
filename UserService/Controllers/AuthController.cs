using Microsoft.AspNetCore.Mvc;
using UserService.Models; // This is IUserRepository
using System.Threading.Tasks;

namespace UserService.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IUserRepository _repository;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IUserRepository repository, ILogger<AuthController> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            try
            {
                if (string.IsNullOrEmpty(model.Username) || string.IsNullOrEmpty(model.Password))
                    return BadRequest("Username and password are required");

                var user = await _repository.AuthenticateAsync(model.Username, model.Password);
                if (user == null)
                    return Unauthorized("Invalid username or password");

                // Stub JWT-like token: "userId_role"
                var token = $"{user.Id}_{user.Role}";

                return Ok(new { Token = token });
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                return StatusCode(500, "Internal server error");
            }
        }
    }

    public class LoginModel
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}