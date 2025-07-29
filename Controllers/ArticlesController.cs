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


    [HttpGet("AllFiltered")]
    public IActionResult GetAllFiltered(int userId)
    {
        var filtered = _db.GetArticlesFilteredByTags(userId);

        // רשום Log של Fetch
        _db.LogArticleFetch(userId);

        return Ok(filtered);
    }


    [HttpGet("Filter")]
    public IActionResult Filter(string? sourceName, string? title, DateTime? from, DateTime? to)
    {
        List<Article> result = _db.FilterArticles(title, from, to);
        return Ok(result);
    }

    [HttpPost("Share")]
    public IActionResult ShareArticle([FromBody] SharedArticleRequest request)
    {
        if (request == null || string.IsNullOrEmpty(request.SenderUsername) || string.IsNullOrEmpty(request.ToUsername))
            return BadRequest("Invalid request");

        int? senderUserId = _db.GetUserIdByUsername(request.SenderUsername);
        if (senderUserId == null) return NotFound("Sender not found");

        if (!UserCanShare(senderUserId.Value))
            return Forbid("Sharing is disabled for this user");

        try
        {
            _db.ShareArticleByUsernames(request.SenderUsername, request.ToUsername, request.ArticleId, request.Comment);
            return Ok("Article shared successfully");
        }
        catch (Exception ex)
        {
            return StatusCode(500, "Server error: " + ex.Message);
        }
    }

    [HttpGet("SharedWithMe/{userId}")]
    public IActionResult GetSharedArticles(int userId)
    {
        var result = _db.GetArticlesSharedWithUser(userId);
        return Ok(result);
    }

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


    [HttpPost("ShareThreadToUser")]
    public IActionResult ShareThreadToUser([FromBody] ThreadShareRequest req)
    {
        if (req == null || string.IsNullOrWhiteSpace(req.toUsername))
            return BadRequest("Invalid data");

        try
        {
            _db.ShareArticleByUsernames(req.senderUsername, req.toUsername, req.publicArticleId, req.comment);
            return Ok("Thread shared via existing share mechanism.");
        }
        catch (Exception ex)
        {
            // שלב חשוב: הדפסת השגיאה ליומן
            Console.WriteLine("🔥 ShareThreadToUser failed: " + ex.Message);
            return StatusCode(500, $"❌ Error while sharing: {ex.Message}");
        }
    }


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
            Console.WriteLine("❌ שגיאה בהבאת כתבות ציבוריות: " + ex.Message);
            return StatusCode(500, "Server error: " + ex.Message); // תוכל לראות ב־Network > Response
        }
    }



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

    [HttpPost("ImportExternal")]
    public async Task<IActionResult> ImportExternalArticles()
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

    [HttpDelete("RemoveShared/{sharedId}")]
    public IActionResult RemoveSharedArticle(int sharedId)
    {
        try
        {
            _db.RemoveSharedArticle(sharedId); // זו אמורה לקרוא ל-SP
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }




    [HttpGet("WithTags")]
    public IActionResult GetArticlesWithTags(int page = 1, int pageSize = 20)
    {
        var articles = _db.GetArticlesWithTags(page, pageSize);
        return Ok(articles);
    }

    [HttpGet("GetTagsForArticle/{articleId}")]
    public IActionResult GetTagsForArticle(int articleId)
    {
        var tags = _db.GetTagsForArticle(articleId);
        return Ok(tags);
    }

    [HttpGet("Paginated")]
    public IActionResult GetPaginated(int page = 1, int pageSize = 6)
    {
        var paged = _db.GetArticlesPaginated(page, pageSize);
        return Ok(paged);
    }

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

    [HttpPost("Like")]
    public IActionResult Like([FromBody] LikeRequest req)
    {
        _db.AddLike(req.UserId, req.ArticleId);
        return Ok();
    }

    [HttpPost("Unlike")]
    public IActionResult Unlike([FromBody] LikeRequest req)
    {
        _db.RemoveLike(req.UserId, req.ArticleId);
        return Ok();
    }

    [HttpGet("LikesCount/{articleId}")]
    public IActionResult GetLikesCount(int articleId)
    {
        int count = _db.GetLikesCount(articleId);
        return Ok(count);
    }

    [HttpPost("AddThreadLike")]
    public IActionResult AddThreadLike([FromBody] LikeThreadRequest req)
    {
        _db.AddThreadLike(req.UserId, req.PublicArticleId);
        return Ok();
    }

    [HttpPost("RemoveThreadLike")]
    public IActionResult RemoveThreadLike([FromBody] LikeThreadRequest req)
    {
        _db.RemoveThreadLike(req.UserId, req.PublicArticleId);
        return Ok();
    }


    [HttpGet("GetThreadLikeCount/{articleId}")]
    public IActionResult GetThreadLikeCount(int articleId)
    {
        int count = _db.GetThreadLikeCount(articleId);
        return Ok(count);
    }

    [HttpPost("ToggleThreadLike")]
    public IActionResult ToggleThreadLike([FromBody] LikeThreadRequest req)
    {
        bool liked = _db.ToggleThreadLike(req.UserId, req.PublicArticleId);
        return Ok(new { liked });
    }

    [HttpGet("CheckUserLike/{userId}/{articleId}")]
    public IActionResult CheckUserLike(int userId, int articleId)
    {
        bool liked = _db.CheckIfUserLikedThread(userId, articleId);
        return Ok(liked);
    }

    [HttpPost("ToggleCommentLike")]
    public IActionResult ToggleCommentLike([FromBody] CommentLikeRequest req)
    {
        _db.ToggleCommentLike(req.UserId, req.CommentId);
        return Ok();
    }

    [HttpGet("CommentLikeCount/{commentId}")]
    public IActionResult GetCommentLikeCount(int commentId)
    {
        int count = _db.GetCommentLikeCount(commentId);
        return Ok(count);
    }

    [HttpPost("TogglePublicCommentLike")]
    public IActionResult TogglePublicCommentLike([FromBody] PublicCommentLikeRequest req)
    {
        _db.TogglePublicCommentLike(req.UserId, req.PublicCommentId);
        return Ok();
    }

    [HttpGet("PublicCommentLikeCount/{publicCommentId}")]
    public IActionResult GetPublicCommentLikeCount(int publicCommentId)
    {
        int count = _db.GetPublicCommentLikeCount(publicCommentId);
        return Ok(count);
    }


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

    // 🟢 עזר: בדיקת הרשאות שיתוף
    private bool UserCanShare(int userId)
    {
        using (SqlConnection con = _db.connect())
        {
            SqlCommand cmd = new SqlCommand("SELECT CanShare FROM News_Users WHERE Id = @Id", con);
            cmd.Parameters.AddWithValue("@Id", userId);
            return (bool)cmd.ExecuteScalar();
        }
    }

    // 🟢 עזר: בדיקת הרשאות תגובה
    private bool UserCanComment(int userId)
    {
        using (SqlConnection con = _db.connect())
        {
            SqlCommand cmd = new SqlCommand("SELECT CanComment FROM News_Users WHERE Id = @Id", con);
            cmd.Parameters.AddWithValue("@Id", userId);
            return (bool)cmd.ExecuteScalar();
        }
    }

    [HttpGet("UnreadCount/{userId}")]
    public int GetUnreadSharedCount(int userId)
    {
        return _db.GetUnreadSharedCount(userId);
    }

    [HttpPost("MarkSharedAsRead/{userId}")]
    public void MarkSharedAsRead(int userId)
    {
        _db.MarkSharedAsRead(userId);
    }

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



    [HttpPost("FixMissingImages")]
    public async Task<IActionResult> FixMissingImages()
    {
        var articles = _db.GetArticlesWithMissingImages();
        int success = 0, skipped = 0, failed = 0;

        foreach (var article in articles)
        {
            try
            {
                // שינה את הקריאה ל־title + description
                string imageUrl = await _openAiService.GenerateImageUrlFromPrompt(article.Title, article.Description);

                if (!string.IsNullOrEmpty(imageUrl))
                {
                    _db.UpdateArticleImageUrl(article.Id, imageUrl);
                    success++;
                }
                else
                {
                    skipped++; // נחסם או בעיה אחרת
                }

                // 🕒 המתן כדי לא לחרוג מהמכסה
                await Task.Delay(12000); // 12 שניות בין בקשות = מקסימום 5 לדקה
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error generating image for article {article.Id}: {ex.Message}");
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

}


