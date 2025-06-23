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

        // --- POST: api/Users/Register
        [HttpPost("Register")]
        public IActionResult Register([FromBody] User user)
        {
            if (user == null)
                return BadRequest("Invalid user data");

            bool success = db.RegisterUser(user);
            if (success)
                return Ok("User registered successfully");
            else
                return Conflict("Email already exists");
        }

        [HttpGet("AllUsers")]
        public IActionResult GetAllUsers()
        {
            var users = db.GetAllUsers();
            return Ok(users);
        }


        // --- POST: api/Users/Login
        [HttpPost("Login")]
        public IActionResult Login([FromBody] LoginRequest loginUser)
        {
            if (loginUser == null)
                return BadRequest("Invalid login data");

            User user = db.LoginUser(loginUser.Email, loginUser.Password);
            return user != null ? Ok(user) : Unauthorized("Invalid email or password");
        }


        // --- POST: api/Users/AddTag
        [HttpPost("AddTag")]
        public IActionResult AddTag([FromBody] AddTagRequest data)
        {
            db.AddUserTag(data.UserId, data.TagId);
            return Ok("Tag added to user");
        }

        // --- GET: api/Users/GetTags/5
        [HttpGet("GetTags/{userId}")]
        public IActionResult GetTags(int userId)
        {
            var tags = db.GetUserTags(userId);
            return Ok(tags);
        }
    }
    public class AddTagRequest
    {
        public int UserId { get; set; }
        public int TagId { get; set; }
    }

    public class LoginRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }
}
