using Microsoft.AspNetCore.Mvc;
using NewsSite.Services;
using NewsSite1.DAL;
using NewsSite1.Models;
using NewsSite1.Models.DTOs.Requests;
using NewsSite1.Services;
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


        // ✅ Gets filtered articles by user's interests (once per day)
        [HttpGet("AllFiltered")]
        public IActionResult GetAllFiltered(int userId)
        {
            try
            {
                var filtered = _db.GetArticlesFilteredByTags(userId); 
                _db.LogArticleFetch(userId);
                return Ok(filtered);
            }
            catch
            {
                return StatusCode(500, "Error fetching filtered articles");
            }
        }


        // ✅ Gets sidebar articles
        [HttpGet("Sidebar")]
        public IActionResult GetSidebarArticles(int page = 1, int pageSize = 6)
        {
            try
            {
                var paged = _db.GetSidebarArticles(page, pageSize);
                return Ok(paged);
            }
            catch
            {
                return StatusCode(500, "Error loading sidebar articles");
            }
        }




        // ✅ Returns all public threads (articles shared publicly with comment)
        [HttpGet("Threads/{userId}")]
        public IActionResult GetThreads(int userId)
        {
            try
            {
                var threads = _db.GetAllThreads(userId);
                return Ok(threads);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Failed to load public threads");
            }
        }


        // ✅ Returns all articles shared privately with the user (Inbox)
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


        // ✅ Shares an article privately using usernames and updates Firebase inbox count
        [HttpPost("ShareByUsernames")]
        public async Task<IActionResult> ShareByUsernames([FromBody] SharedArticleRequest req)
        {
            try
            {
                // Perform DB share action
                _db.ShareArticleByUsernames(req.SenderUsername, req.ToUsername, req.ArticleId, req.Comment);

                // Get receiver's ID by username
                int? targetUserId = _db.GetUserIdByUsername(req.ToUsername);

                // Update inbox count in Firebase
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





        // ✅ Shares an article publicly to Threads
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
            catch
            {
                return StatusCode(500, "Error reporting content");
            }
        }
    }
}





















