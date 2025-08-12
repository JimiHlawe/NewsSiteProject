using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
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

        // ============================
        // == Authentication & Users ==
        // ============================

        /// <summary>Registers a new user (with optional interest tags).</summary>
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

        /// <summary>Authenticates a user by email & password.</summary>
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

        /// <summary>Gets a user by ID (refreshes avatar levels first).</summary>
        [HttpGet("GetUserById/{id}")]
        public IActionResult GetUserById(int id)
        {
            try
            {
                db.ExecuteStoredProcedure("NewsSP_UpdateUserAvatarLevels");
                var user = db.GetUserById(id);
                return Ok(user);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error retrieving user: " + ex.Message);
            }
        }

        /// <summary>Updates the user's password.</summary>
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

        /// <summary>Toggles email notifications on/off for a user.</summary>
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

        /// <summary>Returns all registered users.</summary>
        [HttpGet("AllUsers")]
        public IActionResult GetAllUsers()
        {
            try
            {
                var users = db.GetAllUsers();
                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error retrieving users: " + ex.Message);
            }
        }

        // ============
        // ==  Tags  ==
        // ============

        /// <summary>Gets the full list of available tags.</summary>
        [HttpGet("AllTags")]
        public IActionResult GetAllTags()
        {
            try
            {
                var tags = db.GetAllTags();
                return Ok(tags);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error retrieving tags: " + ex.Message);
            }
        }

        /// <summary>Gets all tags assigned to a specific user.</summary>
        [HttpGet("GetTags/{userId}")]
        public IActionResult GetTags(int userId)
        {
            try
            {
                var tags = db.GetUserTags(userId);
                return Ok(tags);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error retrieving user tags: " + ex.Message);
            }
        }

        /// <summary>Adds a tag to a user.</summary>
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

        /// <summary>Removes a tag from a user.</summary>
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

        // ===============
        // == Blocking  ==
        // ===============

        /// <summary>Blocks another user by username.</summary>
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

        /// <summary>Unblocks a previously blocked user.</summary>
        [HttpPost("UnblockUser")]
        public IActionResult UnblockUser([FromBody] UserBlockRequest req)
        {
            try
            {
                if (req == null || req.BlockerUserId <= 0 || req.BlockedUserId <= 0)
                    return BadRequest(new { message = "Invalid unblock request" });

                db.UnblockUser(req.BlockerUserId, req.BlockedUserId);
                return Ok(new { message = "User unblocked" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error unblocking user: " + ex.Message });
            }
        }

        /// <summary>Gets all users blocked by the given user.</summary>
        [HttpGet("BlockedByUser/{userId}")]
        public IActionResult GetBlockedUsers(int userId)
        {
            try
            {
                var list = db.GetBlockedUsers(userId);
                return Ok(list);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error retrieving blocked users: " + ex.Message);
            }
        }

        // =======================
        // == Shared & Inbox    ==
        // =======================

        /// <summary>Marks all shared articles for a user as read and updates Firebase inbox count.</summary>
        [HttpPost("MarkSharedAsRead/{userId}")]
        public async Task<IActionResult> MarkSharedAsRead(int userId)
        {
            try
            {
                db.MarkSharedAsRead(userId);
                await UpdateInboxCountInFirebase(userId);
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error marking shared articles as read: " + ex.Message);
            }
        }

        /// <summary>Removes a shared article by its sharedId.</summary>
        [HttpDelete("RemoveShared/{sharedId}")]
        public IActionResult RemoveSharedArticle(int sharedId)
        {
            try
            {
                db.RemoveSharedArticle(sharedId);
                return Ok("Shared article removed");
            }
            catch (SqlException ex) when (ex.Number == 547)
            {
                // Example: FK violation
                return Conflict("Cannot remove this shared article due to related data.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error removing shared article: " + ex.Message);
            }
        }

        // ============
        // == Media  ==
        // ============

        /// <summary>Uploads a profile image and updates its path in the database.</summary>
        [HttpPost("UploadProfileImage")]
        public async Task<IActionResult> UploadProfileImage(IFormFile file, [FromQuery] int userId)
        {
            try
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
            catch (Exception ex)
            {
                return StatusCode(500, "Error uploading profile image: " + ex.Message);
            }
        }

        // ===========================
        // == Internal / Helper     ==
        // ===========================

        /// <summary>Internal helper: updates user's inbox count in Firebase.</summary>
        [NonAction]
        private async Task UpdateInboxCountInFirebase(int userId)
        {
            try
            {
                int count = db.GetUnreadSharedArticlesCount(userId);

                using (var client = new HttpClient())
                {
                    string firebasePath =
                        $"https://news-project-e6f1e-default-rtdb.europe-west1.firebasedatabase.app/userInboxCount/{userId}.json";
                    await client.PutAsJsonAsync(firebasePath, count);
                }
            }
            catch (Exception ex)
            {
                System.IO.File.AppendAllText("firebase_errors.log",
                    $"{DateTime.Now}: Failed to update inbox count for user {userId} - {ex.Message}\n");
            }
        }
    }
}
