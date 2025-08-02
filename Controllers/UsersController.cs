using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using NewsSite1.DAL;
using NewsSite1.Models;
using System.Net.Http.Json;

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

        // ✅ Registers a new user
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

            return success ? Ok(user) : StatusCode(500, "Registration failed.");
        }

        // ✅ Returns all registered users
        [HttpGet("AllUsers")]
        public IActionResult GetAllUsers()
        {
            return Ok(db.GetAllUsers());
        }

        // ✅ Logs in a user
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

        // ✅ Gets user by ID and updates avatar levels
        [HttpGet("GetUserById/{id}")]
        public User GetUserById(int id)
        {
            db.ExecuteStoredProcedure("NewsSP_UpdateUserAvatarLevels");
            return db.GetUserById(id);
        }

        // ✅ Adds a tag to a user
        [HttpPost("AddTag")]
        public IActionResult AddTag([FromBody] AddTagRequest data)
        {
            db.AddUserTag(data.UserId, data.TagId);
            return Ok("Tag added to user");
        }

        // ✅ Gets all tags for a user
        [HttpGet("GetTags/{userId}")]
        public IActionResult GetTags(int userId)
        {
            return Ok(db.GetUserTags(userId));
        }

        // ✅ Saves an article for a user
        [HttpPost("SaveArticle")]
        public IActionResult SaveArticle([FromBody] SaveArticleRequest request)
        {
            try
            {
                db.SaveArticle(request.UserId, request.ArticleId);
                return Ok("Article saved");
            }
            catch
            {
                return StatusCode(500, "Server error while saving article");
            }
        }

        // ✅ Removes a saved article for a user
        [HttpPost("RemoveSavedArticle")]
        public IActionResult RemoveSavedArticle([FromBody] SaveArticleRequest request)
        {
            try
            {
                db.RemoveSavedArticle(request.UserId, request.ArticleId);
                return Ok("Removed successfully");
            }
            catch
            {
                return StatusCode(500, "Error removing article");
            }
        }

        // ✅ Gets all available tags
        [HttpGet("AllTags")]
        public IActionResult GetAllTags()
        {
            return Ok(db.GetAllTags());
        }

        // ✅ Gets filtered articles for user
        [HttpGet("All")]
        public IActionResult GetAll(int userId)
        {
            return Ok(db.GetArticlesFilteredByTags(userId));
        }

        // ✅ Gets all saved articles for user
        [HttpGet("GetSavedArticles/{userId}")]
        public IActionResult GetSavedArticles(int userId)
        {
            return Ok(db.GetSavedArticles(userId));
        }

        // ✅ Removes a tag from a user
        [HttpPost("RemoveTag")]
        public IActionResult RemoveTag([FromBody] AddTagRequest data)
        {
            db.RemoveUserTag(data.UserId, data.TagId);
            return Ok("Tag removed");
        }

        // ✅ Updates the user's password
        [HttpPost("UpdatePassword")]
        public IActionResult UpdatePassword([FromBody] UpdatePasswordRequest data)
        {
            db.UpdatePassword(data.UserId, data.NewPassword);
            return Ok("Password updated");
        }

        // ✅ Blocks another user by username
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

        // ✅ Gets all users blocked by a user
        [HttpGet("BlockedByUser/{userId}")]
        public IActionResult GetBlockedUsers(int userId)
        {
            return Ok(db.GetBlockedUsers(userId));
        }

        // ✅ Unblocks a previously blocked user
        [HttpPost("UnblockUser")]
        public IActionResult UnblockUser([FromBody] UserBlockRequest req)
        {
            bool success = db.UnblockUser(req.BlockerUserId, req.BlockedUserId);
            return success ? Ok() : BadRequest();
        }

        // ✅ Sets active status of a user
        [HttpPost("SetActiveStatus")]
        public IActionResult SetActiveStatus([FromBody] SetStatusRequest req)
        {
            db.SetUserActiveStatus(req.UserId, req.IsActive);
            return Ok("User status updated");
        }

        // ✅ Sets whether user can share articles
        [HttpPost("SetSharingStatus")]
        public IActionResult SetSharingStatus([FromBody] SharingStatusRequest req)
        {
            db.SetUserSharingStatus(req.UserId, req.CanShare);
            return Ok("Sharing status updated");
        }

        // ✅ Sets whether user can comment on articles
        [HttpPost("SetCommentingStatus")]
        public IActionResult SetCommentingStatus([FromBody] SharingStatusRequest req)
        {
            db.SetUserCommentingStatus(req.UserId, req.CanComment);
            return Ok("Commenting status updated");
        }

        // ✅ Gets overall site statistics
        [HttpGet("GetStatistics")]
        public IActionResult GetStatistics()
        {
            return Ok(db.GetSiteStatistics());
        }

        // ✅ Uploads user profile image and updates path in DB
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

        // ✅ Toggles email notifications for a user
        [HttpPost("ToggleNotifications")]
        public IActionResult ToggleNotifications([FromBody] ToggleRequest req)
        {
            bool success = db.ToggleUserNotifications(req.UserId, req.Enable);
            return success ? Ok(new { message = "Updated" }) : BadRequest("Failed to update");
        }

        // ✅ (Internal) Updates inbox count in Firebase Realtime DB
        [NonAction]
        private async Task UpdateInboxCountInFirebase(int userId)
        {
            int count = db.GetUnreadSharedArticlesCount(userId);

            using (var client = new HttpClient())
            {
                string firebasePath = $"https://news-project-e6f1e-default-rtdb.europe-west1.firebasedatabase.app/userInboxCount/{userId}.json";
                await client.PutAsJsonAsync(firebasePath, count);
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




