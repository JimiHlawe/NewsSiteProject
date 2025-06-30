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
        public IActionResult Register([FromBody] UserWithTags user)
        {
            if (user == null)
                return BadRequest("Invalid user data");

            int newUserId = db.RegisterUser(user);

            if (newUserId > 0)
            {
                user.Id = newUserId;
                return Ok(user); // מחזיר את המשתמש עם ה-ID החדש
            }
            else
            {
                return Conflict("Email already exists");
            }
        }

        [HttpGet("AllUsers")]
        public IActionResult GetAllUsers()
        {
            var users = db.GetAllUsers();
            return Ok(users);
        }


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

        [HttpPost("SaveArticle")]
        public IActionResult SaveArticle([FromBody] SaveArticleRequest request)
        {
            try
            {
                db.SaveArticle(request.UserId, request.ArticleId);
                return Ok("Article saved");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "שגיאה בשרת: " + ex.Message);
            }
        }

        [HttpPost("RemoveSavedArticle")]
        public IActionResult RemoveSavedArticle([FromBody] SaveArticleRequest request)
        {
            try
            {
                db.RemoveSavedArticle(request.UserId, request.ArticleId);
                return Ok("Removed successfully");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error: " + ex.Message);
            }
        }

        [HttpGet("AllTags")]
        public IActionResult GetAllTags()
        {
            var tags = db.GetAllTags();
            return Ok(tags);
        }


        [HttpGet("All")]
        public IActionResult GetAll(int userId)
        {
            var articles = db.GetArticlesFilteredByTags(userId);
            return Ok(articles);
        }


        [HttpGet("GetSavedArticles/{userId}")]
        public IActionResult GetSavedArticles(int userId)
        {
            var articles = db.GetSavedArticles(userId);
            return Ok(articles);
        }

    }



    public class SaveArticleRequest
    {
        public int UserId { get; set; }
        public int ArticleId { get; set; }
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
