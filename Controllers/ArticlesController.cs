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
        private readonly ImageGenerationService _imageGen;


        public ArticlesController(
            DBServices db,
            NewsApiService newsApiService,
            ImageGenerationService openAiService,
            FirebaseRealtimeService firebase,
            ImageGenerationService imageGen
        )
        {
            _db = db;
            _newsApiService = newsApiService;
            _openAiService = openAiService;
            _firebase = firebase;
            _imageGen = imageGen;
        }


        // ✅ Gets articles with tags and supports pagination
        [HttpGet("WithTags")]
        public IActionResult GetArticlesWithTags(int page = 1, int pageSize = 20)
        {
            try
            {
                var articles = _db.GetArticlesWithTags(page, pageSize);
                return Ok(articles);
            }
            catch
            {
                return StatusCode(500, "Error loading articles with tags");
            }
        }

        // ✅ Gets all tags for a specific article
        [HttpGet("GetTagsForArticle/{articleId}")]
        public IActionResult GetTagsForArticle(int articleId)
        {
            try
            {
                var tags = _db.GetTagsForArticle(articleId);
                return Ok(tags);
            }
            catch
            {
                return StatusCode(500, "Error loading tags for article");
            }
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


        // ✅ Gets articles paginated
        [HttpGet("Paginated")]
        public IActionResult GetPaginated(int page = 1, int pageSize = 6)
        {
            try
            {
                var paged = _db.GetArticlesPaginated(page, pageSize);
                return Ok(paged);
            }
            catch
            {
                return StatusCode(500, "Error loading paginated articles");
            }
        }

        // ✅ Adds a new user article
        [HttpPost("AddUserArticle")]
        public IActionResult AddUserArticle([FromBody] Article article)
        {
            if (article == null ||
                string.IsNullOrEmpty(article.Title) ||
                string.IsNullOrEmpty(article.Description) ||
                string.IsNullOrEmpty(article.Content) ||
                string.IsNullOrEmpty(article.Author) ||
                string.IsNullOrEmpty(article.SourceUrl) ||
                string.IsNullOrEmpty(article.ImageUrl) ||
                article.PublishedAt == default)
            {
                return BadRequest("Invalid article data");
            }

            article.Tags ??= new List<string>();

            try
            {
                int newId = _db.AddUserArticle(article);
                article.Id = newId;
                return Ok(article);
            }
            catch
            {
                return StatusCode(500, "Error adding user article");
            }
        }

        // ✅ Returns all public articles shared with comment (Threads)
        [HttpGet("Public/{userId}")]
        public IActionResult GetPublicArticles(int userId)
        {
            try
            {
                var threads = _db.GetAllPublicArticles(userId);
                return Ok(threads);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Failed to load public threads");
            }
        }

        [HttpGet("SharedWithMe/{userId}")]
        public IActionResult GetSharedWithMe(int userId)
        {
            try
            {
                var shared = _db.GetSharedArticlesForUser(userId);
                return Ok(shared);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error fetching shared articles: " + ex.Message);
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
        [HttpPost("SharePublic")]
        public IActionResult SharePublicArticle([FromBody] PublicArticleShareRequest req)
        {
            try
            {
                _db.ShareArticlePublic(req.UserId, req.ArticleId, req.Comment);
                return Ok(new { message = "Article shared publicly." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error sharing article publicly: " + ex.Message);
            }
        }


        [HttpPost("FixMissingImages")]
        public async Task<IActionResult> FixMissingImages()
        {
            var allArticles = _db.GetAllArticles(); // שלוף את כל הכתבות
            int success = 0, failed = 0, skippedDueToContentPolicy = 0;

            foreach (var article in allArticles.Where(a => string.IsNullOrWhiteSpace(a.ImageUrl)))
            {
                try
                {
                    var imageUrl = await _imageGen.GenerateImageUrlFromPrompt(article.Title, article.Content);

                    if (imageUrl == null)
                    {
                        failed++;
                        continue;
                    }

                    if (imageUrl.Contains("News1.jpg")) // מזהה את ברירת המחדל -> content policy violation
                    {
                        skippedDueToContentPolicy++;
                        continue;
                    }

                    article.ImageUrl = imageUrl;
                    _db.UpdateArticleImageUrl(article.Id, imageUrl); // מימוש בצד DB
                    success++;
                }
                catch
                {
                    failed++;
                }
            }

            return Ok(new
            {
                success,
                skippedDueToContentPolicy,
                failed
            });
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





















