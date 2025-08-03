using Microsoft.AspNetCore.Mvc;
using NewsSite.Services;
using NewsSite1.DAL;
using NewsSite1.Models;
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

        public ArticlesController(DBServices db, NewsApiService newsApiService, ImageGenerationService openAiService)
        {
            _db = db;
            _newsApiService = newsApiService;
            _openAiService = openAiService;
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

        // ✅ Fixes missing images using OpenAI image generation
        [HttpPost("FixMissingImages")]
        public async Task<IActionResult> FixMissingImages()
        {
            var articles = _db.GetArticlesWithMissingImages();
            int success = 0, skipped = 0, failed = 0;

            foreach (var article in articles)
            {
                try
                {
                    string imageUrl = await _openAiService.GenerateImageUrlFromPrompt(article.Title, article.Description);

                    if (!string.IsNullOrEmpty(imageUrl))
                    {
                        _db.UpdateArticleImageUrl(article.Id, imageUrl);
                        success++;
                    }
                    else
                    {
                        skipped++;
                    }

                    await Task.Delay(12000);
                }
                catch
                {
                    failed++;
                }
            }

            return Ok(new
            {
                Total = articles.Count,
                Success = success,
                SkippedDueToContentPolicy = skipped,
                Failed = failed
            });
        }
    }
}




public class LikeRequest
    {
        public int UserId { get; set; }
        public int ArticleId { get; set; }
    }

    public class LikeThreadRequest
    {
        public int UserId { get; set; }
        public int PublicArticleId { get; set; }
    }

    public class ShareRequest
    {
        public int UserId { get; set; }
        public int TargetUserId { get; set; }
        public int ArticleId { get; set; }
        public string Comment { get; set; }
    }

    public class CommentRequest
    {
        public int ArticleId { get; set; }
        public int UserId { get; set; }
        public string Comment { get; set; } = "";
    }

    public class ReportRequest
    {
        public int UserId { get; set; }
        public string ReferenceType { get; set; } = "";
        public int ReferenceId { get; set; }
        public string Reason { get; set; } = "";
    }

    public class ThreadShareRequest
    {
        public int publicArticleId { get; set; }
        public string senderUsername { get; set; }
        public string toUsername { get; set; }
        public string comment { get; set; }
    }

    public class CommentLikeRequest
    {
        public int UserId { get; set; }
        public int CommentId { get; set; }
    }

    public class PublicCommentLikeRequest
    {
        public int UserId { get; set; }
        public int PublicCommentId { get; set; }
    }



