using Microsoft.AspNetCore.Mvc;
using NewsSite.Services;
using NewsSite1.DAL;
using NewsSite1.Models;
using NewsSite1.Models.DTOs.Requests;
using NewsSite1.Services;
using System;
using System.Threading.Tasks;

namespace NewsSite1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ArticlesController : ControllerBase
    {
        private readonly DBServices _db;
        private readonly NewsApiService _newsApiService;
        private readonly ImageGenerationService _openAiService;
        private readonly FirebaseRealtimeService _firebase;

        public ArticlesController(
            DBServices db,
            NewsApiService newsApiService,
            ImageGenerationService openAiService,
            FirebaseRealtimeService firebase
        )
        {
            _db = db;
            _newsApiService = newsApiService;
            _openAiService = openAiService;
            _firebase = firebase;
        }

        // ============================
        // == Discovery / Fetching   ==
        // ============================

        /// <summary>
        /// Gets filtered articles for a user (by interest tags); logs fetch (once per day).
        /// </summary>
        [HttpGet("AllFiltered")]
        public IActionResult GetAllFiltered(int userId)
        {
            try
            {
                var filtered = _db.GetArticlesFilteredByTags(userId);
                _db.LogArticleFetch(userId);
                return Ok(filtered);
            }
            catch (Exception)
            {
                return StatusCode(500, "Error fetching filtered articles");
            }
        }

        /// <summary>
        /// Gets sidebar articles with simple pagination.
        /// </summary>
        [HttpGet("Sidebar")]
        public IActionResult GetSidebarArticles(int page = 1, int pageSize = 6)
        {
            try
            {
                var paged = _db.GetSidebarArticles(page, pageSize);
                return Ok(paged);
            }
            catch (Exception)
            {
                return StatusCode(500, "Error loading sidebar articles");
            }
        }

        /// <summary>
        /// Returns all public threads (publicly shared articles) visible for the user.
        /// </summary>
        [HttpGet("Threads/{userId}")]
        public IActionResult GetThreads(int userId)
        {
            try
            {
                var threads = _db.GetAllThreads(userId);
                return Ok(threads);
            }
            catch (Exception)
            {
                return StatusCode(500, "Failed to load public threads");
            }
        }

        /// <summary>
        /// Returns all articles shared privately with the user (Inbox).
        /// </summary>
        [HttpGet("Inbox/{userId}")]
        public IActionResult GetInbox(int userId)
        {
            try
            {
                var inbox = _db.GetInboxArticles(userId);
                return Ok(inbox);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error fetching inbox articles: " + ex.Message);
            }
        }

        // ============================
        // == Saved Articles (MyList) ==
        // ============================

        /// <summary>
        /// Saves an article for a user.
        /// </summary>
        [HttpPost("SaveArticle")]
        public IActionResult SaveArticle([FromBody] SaveArticleRequest request)
        {
            try
            {
                _db.SaveArticle(request.UserId, request.ArticleId);
                return Ok("Article saved");
            }
            catch (Exception)
            {
                return StatusCode(500, "Server error while saving article");
            }
        }

        /// <summary>
        /// Gets all saved articles for a specific user.
        /// </summary>
        [HttpGet("GetSavedArticles/{userId}")]
        public IActionResult GetSavedArticles(int userId)
        {
            try
            {
                var list = _db.GetSavedArticles(userId);
                return Ok(list);
            }
            catch (Exception)
            {
                return StatusCode(500, "Error loading saved articles");
            }
        }

        /// <summary>
        /// Removes a saved article for a user.
        /// </summary>
        [HttpPost("RemoveSavedArticle")]
        public IActionResult RemoveSavedArticle([FromBody] SaveArticleRequest request)
        {
            try
            {
                _db.RemoveSavedArticle(request.UserId, request.ArticleId);
                return Ok("Removed successfully");
            }
            catch (Exception)
            {
                return StatusCode(500, "Error removing article");
            }
        }

        // ============================
        // == Sharing (Private/Public) ==
        // ============================

        /// <summary>
        /// Shares an article privately by usernames and updates Firebase inbox count.
        /// </summary>
        [HttpPost("ShareByUsernames")]
        public async Task<IActionResult> ShareByUsernames([FromBody] SharedArticleRequest req)
        {
            try
            {
                _db.ShareArticleByUsernames(req.SenderUsername, req.ToUsername, req.ArticleId, req.Comment);

                int? targetUserId = _db.GetUserIdByUsername(req.ToUsername);
                if (targetUserId != null)
                {
                    int newCount = _db.GetUnreadSharedArticlesCount(targetUserId.Value);
                    await _firebase.UpdateInboxCount(targetUserId.Value, newCount);
                }

                return Ok(new { message = "✅ Article shared by username and inbox updated." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "❌ Error sharing article: " + ex.Message);
            }
        }

        /// <summary>
        /// Shares an article publicly to Threads.
        /// </summary>
        [HttpPost("ShareToThreads")]
        public IActionResult ShareToThreads([FromBody] PublicArticleShareRequest req)
        {
            try
            {
                _db.ShareToThreads(req.UserId, req.ArticleId, req.Comment);
                return Ok(new { message = "Article shared publicly to threads." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error sharing to threads: " + ex.Message);
            }
        }

        // =================
        // == Reporting   ==
        // =================

        /// <summary>
        /// Reports content (article/comment/thread) with a reason.
        /// </summary>
        [HttpPost("Report")]
        public IActionResult ReportContent([FromBody] ReportRequest req)
        {
            if (req == null || req.UserId <= 0 || string.IsNullOrEmpty(req.ReferenceType))
                return BadRequest("Invalid report data");

            try
            {
                _db.ReportContent(req.UserId, req.ReferenceType, req.ReferenceId, req.Reason);
                return Ok("Reported");
            }
            catch (Exception)
            {
                return StatusCode(500, "Error reporting content");
            }
        }
    }
}
