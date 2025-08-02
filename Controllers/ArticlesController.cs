using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using NewsSite.Services;
using NewsSite1.DAL;
using NewsSite1.Models;
using NewsSite1.Services;

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

    // ✅ Get articles filtered by tags and log the fetch
    [HttpGet("AllFiltered")]
    public IActionResult GetAllFiltered(int userId)
    {
        var filtered = _db.GetArticlesFilteredByTags(userId);
        _db.LogArticleFetch(userId);
        return Ok(filtered);
    }

    // ✅ Filter articles by optional parameters
    [HttpGet("Filter")]
    public IActionResult Filter(string? sourceName, string? title, DateTime? from, DateTime? to)
    {
        List<Article> result = _db.FilterArticles(title, from, to);
        return Ok(result);
    }

    // ✅ Share article privately and update real-time inbox
    [HttpPost("Share")]
    public async Task<IActionResult> ShareArticle([FromBody] SharedArticleRequest request)
    {
        if (request == null || string.IsNullOrEmpty(request.SenderUsername) || string.IsNullOrEmpty(request.ToUsername))
            return BadRequest("Invalid request");

        int? senderUserId = _db.GetUserIdByUsername(request.SenderUsername);
        int? targetUserId = _db.GetUserIdByUsername(request.ToUsername);

        if (senderUserId == null || targetUserId == null)
            return NotFound("User not found");

        if (!UserCanShare(senderUserId.Value))
            return Forbid("Sharing is disabled for this user");

        try
        {
            _db.ShareArticleByUsernames(request.SenderUsername, request.ToUsername, request.ArticleId, request.Comment);
            await UpdateInboxCountInFirebase(targetUserId.Value);
            return Ok("Article shared successfully");
        }
        catch (Exception ex)
        {
            return StatusCode(500, "Server error: " + ex.Message);
        }
    }

    // ✅ Update Firebase inbox count for a specific user
    [HttpPost("UpdateInboxFirebase/{userId}")]
    public async Task<IActionResult> UpdateInbox(int userId)
    {
        await UpdateInboxCountInFirebase(userId);
        return Ok();
    }

    // ✅ Helper function: updates Firebase with current unread count
    [NonAction]
    private async Task UpdateInboxCountInFirebase(int userId)
    {
        int count = _db.GetUnreadSharedArticlesCount(userId);
        using (var client = new HttpClient())
        {
            string firebasePath = $"https://news-project-e6f1e-default-rtdb.europe-west1.firebasedatabase.app/userInboxCount/{userId}.json";
            await client.PutAsJsonAsync(firebasePath, count);
        }
    }

    // ✅ Get list of articles shared with the current user
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
            return StatusCode(500, "Server error: " + ex.Message);
        }
    }

    // ✅ Share article publicly with optional comment
    [HttpPost("SharePublic")]
    public IActionResult ShareArticlePublic([FromBody] PublicArticleShareRequest request)
    {
        if (!UserCanShare(request.UserId))
            return Forbid("Sharing is disabled for this user");

        try
        {
            _db.ShareArticlePublic(request.UserId, request.ArticleId, request.Comment);
            return Ok("Public share succeeded");
        }
        catch (Exception ex)
        {
            return StatusCode(500, "Error: " + ex.Message);
        }
    }

    // ✅ Get all public articles, optionally filtered by userId
    [HttpGet("Public/{userId}")]
    public IActionResult GetPublicArticles(int userId)
    {
        try
        {
            var articles = _db.GetAllPublicArticles(userId);
            return Ok(articles);
        }
        catch (Exception ex)
        {
            return StatusCode(500, "Server error: " + ex.Message);
        }
    }

    // ✅ Add comment to a public article
    [HttpPost("AddPublicComment")]
    public IActionResult AddPublicComment([FromBody] PublicCommentRequest comment)
    {
        if (!UserCanComment(comment.UserId))
            return Forbid("Commenting is disabled for this user");

        try
        {
            _db.AddPublicComment(comment.PublicArticleId, comment.UserId, comment.Comment);
            return Ok("Comment added successfully");
        }
        catch (Exception ex)
        {
            return StatusCode(500, "Error: " + ex.Message);
        }
    }

    // ✅ Get comments on a public article
    [HttpGet("GetPublicComments/{articleId}")]
    public IActionResult GetPublicComments(int articleId)
    {
        try
        {
            var comments = _db.GetCommentsForPublicArticle(articleId);
            return Ok(comments);
        }
        catch (Exception ex)
        {
            return StatusCode(500, "Error: " + ex.Message);
        }
    }

    // ✅ Import articles from external news API, skipping duplicates
    [HttpPost("ImportExternal")]
    public async Task<IActionResult> ImportExternalArticles()
    {
        try
        {
            List<Article> externalArticles = await _newsApiService.GetTopHeadlinesAsync();
            List<Article> addedArticles = new List<Article>();

            foreach (var article in externalArticles)
            {
                if (_db.ArticleExists(article.SourceUrl))
                    continue;

                int id = _db.AddUserArticle(article);
                article.Id = id;
                addedArticles.Add(article);
            }

            return Ok(addedArticles);
        }
        catch (Exception ex)
        {
            return StatusCode(500, "Error importing articles: " + ex.Message);
        }
    }


    // ✅ Remove a shared article by sharedId
    [HttpDelete("RemoveShared/{sharedId}")]
    public IActionResult RemoveSharedArticle(int sharedId)
    {
        try
        {
            _db.RemoveSharedArticle(sharedId);
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    // ✅ Get paginated articles with associated tags
    [HttpGet("WithTags")]
    public IActionResult GetArticlesWithTags(int page = 1, int pageSize = 20)
    {
        var articles = _db.GetArticlesWithTags(page, pageSize);
        return Ok(articles);
    }

    // ✅ Get tags for specific article
    [HttpGet("GetTagsForArticle/{articleId}")]
    public IActionResult GetTagsForArticle(int articleId)
    {
        var tags = _db.GetTagsForArticle(articleId);
        return Ok(tags);
    }

    // ✅ Get paginated articles (basic)
    [HttpGet("Paginated")]
    public IActionResult GetPaginated(int page = 1, int pageSize = 6)
    {
        var paged = _db.GetArticlesPaginated(page, pageSize);
        return Ok(paged);
    }

    // ✅ Add a new article submitted by the user
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

        if (article.Tags == null)
        {
            article.Tags = new List<string>();
        }

        try
        {
            int newId = _db.AddUserArticle(article);
            article.Id = newId;
            return Ok(article);
        }
        catch (Exception ex)
        {
            return StatusCode(500, "Error: " + ex.Message);
        }
    }

    // ✅ Report inappropriate content
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
        catch (Exception ex)
        {
            return StatusCode(500, "Error: " + ex.Message);
        }
    }

    // ✅ Like an article
    [HttpPost("Like")]
    public IActionResult Like([FromBody] LikeRequest req)
    {
        _db.AddLike(req.UserId, req.ArticleId);
        return Ok();
    }

    // ✅ Unlike an article
    [HttpPost("Unlike")]
    public IActionResult Unlike([FromBody] LikeRequest req)
    {
        _db.RemoveLike(req.UserId, req.ArticleId);
        return Ok();
    }

    // ✅ Get total likes for an article
    [HttpGet("LikesCount/{articleId}")]
    public IActionResult GetLikesCount(int articleId)
    {
        int count = _db.GetLikesCount(articleId);
        return Ok(count);
    }

    // ✅ Add like to a thread (public article)
    [HttpPost("AddThreadLike")]
    public IActionResult AddThreadLike([FromBody] LikeThreadRequest req)
    {
        _db.AddThreadLike(req.UserId, req.PublicArticleId);
        return Ok();
    }

    // ✅ Remove like from a thread
    [HttpPost("RemoveThreadLike")]
    public IActionResult RemoveThreadLike([FromBody] LikeThreadRequest req)
    {
        _db.RemoveThreadLike(req.UserId, req.PublicArticleId);
        return Ok();
    }

    // ✅ Get total thread likes
    [HttpGet("GetThreadLikeCount/{articleId}")]
    public IActionResult GetThreadLikeCount(int articleId)
    {
        int count = _db.GetThreadLikeCount(articleId);
        return Ok(count);
    }

    // ✅ Toggle thread like (add or remove)
    [HttpPost("ToggleThreadLike")]
    public IActionResult ToggleThreadLike([FromBody] LikeThreadRequest req)
    {
        bool liked = _db.ToggleThreadLike(req.UserId, req.PublicArticleId);
        return Ok(new { liked });
    }

    // ✅ Check if user liked a thread
    [HttpGet("CheckUserLike/{userId}/{articleId}")]
    public IActionResult CheckUserLike(int userId, int articleId)
    {
        bool liked = _db.CheckIfUserLikedThread(userId, articleId);
        return Ok(liked);
    }

    // ✅ Toggle like for a comment
    [HttpPost("ToggleCommentLike")]
    public IActionResult ToggleCommentLike([FromBody] CommentLikeRequest req)
    {
        _db.ToggleCommentLike(req.UserId, req.CommentId);
        return Ok();
    }

    // ✅ Get total likes for a comment
    [HttpGet("CommentLikeCount/{commentId}")]
    public IActionResult GetCommentLikeCount(int commentId)
    {
        int count = _db.GetCommentLikeCount(commentId);
        return Ok(count);
    }

    // ✅ Toggle like for a public comment
    [HttpPost("TogglePublicCommentLike")]
    public IActionResult TogglePublicCommentLike([FromBody] PublicCommentLikeRequest req)
    {
        _db.TogglePublicCommentLike(req.UserId, req.PublicCommentId);
        return Ok();
    }

    // ✅ Get total likes for a public comment
    [HttpGet("PublicCommentLikeCount/{publicCommentId}")]
    public IActionResult GetPublicCommentLikeCount(int publicCommentId)
    {
        int count = _db.GetPublicCommentLikeCount(publicCommentId);
        return Ok(count);
    }

    // ✅ Add a comment to a regular article
    [HttpPost("AddComment")]
    public IActionResult AddComment([FromBody] CommentRequest comment)
    {
        if (comment == null || comment.ArticleId <= 0 || comment.UserId <= 0 || string.IsNullOrWhiteSpace(comment.Comment))
            return BadRequest("Invalid comment data");

        if (!UserCanComment(comment.UserId))
            return Forbid("Commenting is disabled for this user");

        try
        {
            _db.AddCommentToArticle(comment.ArticleId, comment.UserId, comment.Comment);
            return Ok("Comment added successfully");
        }
        catch (Exception ex)
        {
            return StatusCode(500, "Error: " + ex.Message);
        }
    }

    // ✅ Get comments for an article
    [HttpGet("GetComments/{articleId}")]
    public IActionResult GetComments(int articleId)
    {
        try
        {
            var comments = _db.GetCommentsForArticle(articleId);
            return Ok(comments);
        }
        catch (Exception ex)
        {
            return StatusCode(500, "Error: " + ex.Message);
        }
    }

    // ✅ Check if the user has permission to share articles
    private bool UserCanShare(int userId)
    {
        using (SqlConnection con = _db.connect())
        {
            SqlCommand cmd = new SqlCommand("SELECT CanShare FROM News_Users WHERE Id = @Id", con);
            cmd.Parameters.AddWithValue("@Id", userId);
            return (bool)cmd.ExecuteScalar();
        }
    }

    // ✅ Check if the user has permission to comment
    private bool UserCanComment(int userId)
    {
        using (SqlConnection con = _db.connect())
        {
            SqlCommand cmd = new SqlCommand("SELECT CanComment FROM News_Users WHERE Id = @Id", con);
            cmd.Parameters.AddWithValue("@Id", userId);
            return (bool)cmd.ExecuteScalar();
        }
    }

    // ✅ Mark shared articles as read and update inbox count in Firebase
    [HttpPost("MarkSharedAsRead/{userId}")]
    public async Task<IActionResult> MarkSharedAsRead(int userId)
    {
        try
        {
            _db.MarkSharedAsRead(userId);
            await UpdateInboxCountInFirebase(userId);
            return Ok();
        }
        catch (Exception ex)
        {
            return StatusCode(500, "Server error: " + ex.Message);
        }
    }

    // ✅ Generate an ad based on category using OpenAI
    [Route("api/[controller]")]
    [ApiController]
    public class AdsController : ControllerBase
    {
        private readonly AdsGenerationService _adsService;

        public AdsController(AdsGenerationService adsService)
        {
            _adsService = adsService;
        }

        [HttpGet("Generate")]
        public async Task<IActionResult> GenerateAd([FromQuery] string category)
        {
            if (string.IsNullOrWhiteSpace(category))
                return BadRequest("Category is required");

            var ad = await _adsService.GenerateAdWithImageAsync(category);
            if (ad == null)
                return StatusCode(500, "Failed to generate ad");

            return Ok(ad);
        }
    }

    // ✅ Fix missing images for articles using OpenAI image generation
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

                await Task.Delay(12000); // Wait between requests to respect rate limits
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

    // ✅ Global error handler for unhandled exceptions
    [HttpGet("fail")]
    public IActionResult Fail()
    {
        throw new Exception("Something went wrong");
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



