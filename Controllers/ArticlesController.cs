using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using NewsSite1.DAL;
using NewsSite1.Models;
using NewsSite1.Services;

[ApiController]
[Route("api/[controller]")]
public class ArticlesController : ControllerBase
{
    private readonly DBServices _db;
    private readonly NewsApiService _newsApiService;
    private readonly ArticleService _articleService;

    public ArticlesController(DBServices db, NewsApiService newsApiService, ArticleService articleService)
    {
        _db = db;
        _newsApiService = newsApiService;
        _articleService = articleService;
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

    [HttpGet("Public/{userId}")]
    public IActionResult GetPublicArticles(int userId)
    {
        var all = _db.GetAllPublicArticles(userId);
        return Ok(all);
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

        foreach (var article in externalArticles)
        {
            int id = _articleService.SaveArticleAndGetId(article);
            article.Id = id;
        }

        return Ok(externalArticles);
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
}
