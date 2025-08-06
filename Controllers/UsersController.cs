using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using NewsSite1.DAL;
using NewsSite1.Models;
using System.Net.Http.Json;
using NewsSite1.Models.DTOs;
using NewsSite1.Models.DTOs.Requests;



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
            try
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
            catch (Exception ex)
            {
                return StatusCode(500, "Server error during registration: " + ex.Message);
            }
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
            try
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
            catch (Exception ex)
            {
                return StatusCode(500, "Login failed: " + ex.Message);
            }
        }

        // ✅ Gets user by ID and updates avatar levels
        [HttpGet("GetUserById/{id}")]
        public IActionResult GetUserById(int id)
        {
            try
            {
                db.ExecuteStoredProcedure("NewsSP_UpdateUserAvatarLevels");
                return Ok(db.GetUserById(id));
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error retrieving user: " + ex.Message);
            }
        }

        // ✅ Adds a tag to a user
        [HttpPost("AddTag")]
        public IActionResult AddTag([FromBody] AddTagRequest data)
        {
            try
            {
                db.AddUserTag(data.UserId, data.TagId);
                return Ok("Tag added to user");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error adding tag: " + ex.Message);
            }
        }

        // ✅ Gets all tags for a user
        [HttpGet("GetTags/{userId}")]
        public IActionResult GetTags(int userId)
        {
            return Ok(db.GetUserTags(userId));
        }

        [HttpPost("MarkSharedAsRead/{userId}")]
        public async Task<IActionResult> MarkSharedAsRead(int userId)
        {
            try
            {
                db.MarkSharedAsRead(userId);
                await UpdateInboxCountInFirebase(userId);
                return Ok();
            }
            catch
            {
                return StatusCode(500, "Error marking shared articles as read");
            }
        }

        // ✅ Checks if the user is allowed to share articles
        [HttpGet("CanShare/{userId}")]
        public IActionResult CanUserShare(int userId)
        {
            try
            {
                bool canShare = db.GetUserCanShare(userId);
                return Ok(new { canShare });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error checking sharing permission: " + ex.Message);
            }
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
            try
            {
                db.RemoveUserTag(data.UserId, data.TagId);
                return Ok("Tag removed");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error removing tag: " + ex.Message);
            }
        }

        // ✅ Updates the user's password
        [HttpPost("UpdatePassword")]
        public IActionResult UpdatePassword([FromBody] UpdatePasswordRequest data)
        {
            try
            {
                db.UpdatePassword(data.UserId, data.NewPassword);
                return Ok("Password updated");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error updating password: " + ex.Message);
            }
        }

        // ✅ Blocks another user by username
        [HttpPost("BlockUser")]
        public IActionResult BlockUser([FromBody] BlockUserRequest req)
        {
            try
            {
                if (string.IsNullOrEmpty(req.BlockedUsername))
                    return BadRequest("No user specified.");

                int? blockedId = db.GetUserIdByUsername(req.BlockedUsername);
                if (blockedId == null) return NotFound("User not found.");

                db.BlockUser(req.BlockerUserId, blockedId.Value);
                return Ok("Blocked");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error blocking user: " + ex.Message);
            }
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
            try
            {
                if (req == null || req.BlockerUserId <= 0 || req.BlockedUserId <= 0)
                    return BadRequest(new { message = "Invalid unblock request" });

                db.UnblockUser(req.BlockerUserId, req.BlockedUserId);

                // תמיד נחזיר הצלחה, גם אם לא הייתה חסימה בפועל
                return Ok(new { message = "User unblocked" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error unblocking user: " + ex.Message });
            }
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
            try
            {
                db.ToggleUserNotifications(req.UserId, req.Enable); 
                return Ok(new { message = "Notification preference updated" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error toggling notifications: " + ex.Message);
            }
        }


        // ✅ Removes a shared article by sharedId
        [HttpDelete("RemoveShared/{sharedId}")]
        public IActionResult RemoveSharedArticle(int sharedId)
        {
            try
            {
                db.RemoveSharedArticle(sharedId);
                return Ok("Shared article removed");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error removing shared article: " + ex.Message);
            }
        }


        // ✅ (Internal) Updates inbox count in Firebase Realtime DB
        [NonAction]
        private async Task UpdateInboxCountInFirebase(int userId)
        {
            try
            {
                int count = db.GetUnreadSharedArticlesCount(userId);

                using (var client = new HttpClient())
                {
                    string firebasePath = $"https://news-project-e6f1e-default-rtdb.europe-west1.firebasedatabase.app/userInboxCount/{userId}.json";
                    await client.PutAsJsonAsync(firebasePath, count);
                }
            }
            catch (Exception ex)
            {
                System.IO.File.AppendAllText("firebase_errors.log",
                    $"{DateTime.Now}: Failed to update inbox count for user {userId} - {ex.Message}\n");
            }
        }



        // ✅ Test endpoint to trigger an unhandled exception
        [HttpGet("fail")]
        public IActionResult Fail()
        {
            throw new Exception("Something went wrong from UsersController");
        }
    }
}






