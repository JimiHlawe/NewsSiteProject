using Microsoft.AspNetCore.Mvc;
using NewsSite1.DAL;
using NewsSite1.Models;

namespace NewsSite1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly DBServices db;

        public UsersController(DBServices db)
        {
            this.db = db;
        }

        [HttpPost("Register")]
        public IActionResult Register([FromBody] User user)
        {
            if (user == null)
                return BadRequest("Invalid user data");

            bool success = db.RegisterUser(user);
            return success ? Ok("User registered successfully") : Conflict("Email already exists");
        }

        [HttpPost("Login")]
        public IActionResult Login([FromBody] LoginRequest loginUser)
        {
            if (loginUser == null)
                return BadRequest("Invalid login data");

            User user = db.LoginUser(loginUser.Email, loginUser.Password);
            return user != null ? Ok(user) : Unauthorized("Invalid email or password");
        }

        [HttpGet("All")]
        public IActionResult GetAllUsers()
        {
            var users = db.GetAllUsers();
            return Ok(users);
        }
    }

    public class LoginRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }
}
