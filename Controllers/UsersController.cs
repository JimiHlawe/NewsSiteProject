using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using NewsSite1.DAL;
using NewsSite1.Models;

namespace NewsSite1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly DBServices db;
        private readonly IWebHostEnvironment env;

        public UsersController(DBServices db, IWebHostEnvironment env)
        {
            this.db = db;
            this.env = env;
        }

        [HttpPost("Register")]
        public IActionResult Register([FromBody] UserWithTags user)
        {
            if (user == null)
                return BadRequest("Invalid user data");

            if (db.IsEmailExists(user.Email))
                return Conflict("email");

            if (db.IsNameExists(user.Name))
                return Conflict("name");

            bool success = db.RegisterUser(user);

            if (success)
                return Ok(user);
            else
                return StatusCode(500, "Registration failed.");
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
            if (user == null)
                return Unauthorized("Invalid email or password");

            if (!user.Active)
                return StatusCode(403, "Your account is blocked.");

            db.LogUserLogin(user.Id);

            return Ok(user);
        }

        [HttpGet("GetUserById/{id}")]
        public User GetUserById(int id)
        {
            db.ExecuteStoredProcedure("NewsSP_UpdateUserAvatarLevels");
            return db.GetUserById(id);
        }



        [HttpPost("AddTag")]
        public IActionResult AddTag([FromBody] AddTagRequest data)
        {
            db.AddUserTag(data.UserId, data.TagId);
            return Ok("Tag added to user");
        }

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

        [HttpPost("RemoveTag")]
        public IActionResult RemoveTag([FromBody] AddTagRequest data)
        {
            db.RemoveUserTag(data.UserId, data.TagId);
            return Ok("Tag removed");
        }

        [HttpPost("UpdatePassword")]
        public IActionResult UpdatePassword([FromBody] UpdatePasswordRequest data)
        {
            db.UpdatePassword(data.UserId, data.NewPassword);
            return Ok("Password updated");
        }

        [HttpPost("BlockUser")]
        public IActionResult BlockUser([FromBody] BlockUserRequest req)
        {
            if (string.IsNullOrEmpty(req.BlockedUsername))
                return BadRequest("No user specified.");

            int? blockedId = db.GetUserIdByUsername(req.BlockedUsername);
            if (blockedId == null) return NotFound("User not found.");

            db.BlockUser(req.BlockerUserId, blockedId.Value);
            return Ok("Blocked");
        }

        [HttpGet("BlockedByUser/{userId}")]
        public IActionResult GetBlockedUsers(int userId)
        {
            var blockedUsers = db.GetBlockedUsers(userId);
            return Ok(blockedUsers);
        }

        [HttpPost("UnblockUser")]
        public IActionResult UnblockUser([FromBody] UserBlockRequest req)
        {
            bool success = db.UnblockUser(req.BlockerUserId, req.BlockedUserId);
            if (success)
                return Ok();
            else
                return BadRequest();
        }

        [HttpPost("SetActiveStatus")]
        public IActionResult SetActiveStatus([FromBody] SetStatusRequest req)
        {
            db.SetUserActiveStatus(req.UserId, req.IsActive);
            return Ok("User status updated");
        }

        [HttpPost("SetSharingStatus")]
        public IActionResult SetSharingStatus([FromBody] SharingStatusRequest req)
        {
            db.SetUserSharingStatus(req.UserId, req.CanShare);
            return Ok("Sharing status updated");
        }

        [HttpPost("SetCommentingStatus")]
        public IActionResult SetCommentingStatus([FromBody] SharingStatusRequest req)
        {
            db.SetUserCommentingStatus(req.UserId, req.CanComment);
            return Ok("Commenting status updated");
        }



        [HttpGet("GetStatistics")]
        public IActionResult GetStatistics()
        {
            var stats = db.GetSiteStatistics();
            return Ok(stats);
        }

        // 🟢 אופציונלי: בדיקת יכולת שיתוף/תגובה בצד UsersController אם צריך
        private bool UserCanShare(int userId)
        {
            using (SqlConnection con = db.connect())
            {
                SqlCommand cmd = new SqlCommand("SELECT CanShare FROM News_Users WHERE Id = @Id", con);
                cmd.Parameters.AddWithValue("@Id", userId);
                return (bool)cmd.ExecuteScalar();
            }
        }

        private bool UserCanComment(int userId)
        {
            using (SqlConnection con = db.connect())
            {
                SqlCommand cmd = new SqlCommand("SELECT CanComment FROM News_Users WHERE Id = @Id", con);
                cmd.Parameters.AddWithValue("@Id", userId);
                return (bool)cmd.ExecuteScalar();
            }
        }

        [HttpPost("UploadProfileImage")]
        public async Task<IActionResult> UploadProfileImage(IFormFile file, [FromQuery] int userId)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded");

            string uploadsFolder = Path.Combine(env.WebRootPath, "uploads");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            string fileName = $"user_{userId}_{Guid.NewGuid().ToString().Substring(0, 8)}{Path.GetExtension(file.FileName)}";
            string filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            string relativePath = $"/uploads/{fileName}";
            await db.UpdateProfileImagePath(userId, relativePath);

            return Ok(new { imageUrl = relativePath });
        }

        [HttpPost("ToggleNotifications")]
        public IActionResult ToggleNotifications([FromBody] ToggleRequest req)
        {
            bool success = db.ToggleUserNotifications(req.UserId, req.Enable);
            if (success)
                return Ok(new { message = "Updated" });

            return BadRequest("Failed to update");
        }

        [NonAction]
        private async Task UpdateInboxCountInFirebase(int userId)
        {
            // 1. חשב את מספר הכתבות ששיתפו עם המשתמש שלא נקראו
            int count = db.GetUnreadSharedArticlesCount(userId);

            // 2. עדכן את Firebase
            using (var client = new HttpClient())
            {
                string firebasePath = $"https://news-project-e6f1e-default-rtdb.europe-west1.firebasedatabase.app/userInboxCount/{userId}.json";
                var response = await client.PutAsJsonAsync(firebasePath, count);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine("❌ Failed to update Firebase: " + response.StatusCode);
                }
            }
        }




    }

    public class ToggleRequest
    {
        public int UserId { get; set; }
        public bool Enable { get; set; }
    }

    public class SetStatusRequest
    {
        public int UserId { get; set; }
        public bool IsActive { get; set; }
    }

    public class SharingStatusRequest
    {
        public int UserId { get; set; }
        public bool CanShare { get; set; }
        public bool CanComment { get; set; }
    }

    public class BlockUserRequest
    {
        public int BlockerUserId { get; set; }
        public string BlockedUsername { get; set; }
    }
    public class UserBlockRequest
    {
        public int BlockerUserId { get; set; }
        public int BlockedUserId { get; set; }
    }


    public class UpdatePasswordRequest
    {
        public int UserId { get; set; }
        public string NewPassword { get; set; }
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
