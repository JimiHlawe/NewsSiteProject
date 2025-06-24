using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using NewsSite1.DAL;
using NewsSite1.Models;
using NewsSite1.Services;
using NewsSite1.Models;

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

    [HttpPost("Add")]
    public IActionResult AddArticle([FromBody] Article article)
    {
        if (article == null)
            return BadRequest("Invalid article data");

        _db.AddArticle(article);
        return Ok("Article added successfully");
    }

    [HttpGet("All")]
    public IActionResult GetAll()
    {
        List<Article> articles = _db.GetAllArticles();
        return Ok(articles);
    }

    [HttpGet("Filter")]
    public IActionResult Filter(string? sourceName, string? title, DateTime? from, DateTime? to)
    {
        List<Article> result = _db.FilterArticles(sourceName, title, from, to);
        return Ok(result);
    }

    [HttpGet("Saved")]
    public IActionResult GetSaved()
    {
        List<Article> savedArticles = _db.GetAllSavedArticles();
        return Ok(savedArticles);
    }

    [HttpPost("Share")]
    public IActionResult ShareArticle([FromBody] SharedArticleRequest request)
    {
        if (request == null ||
            string.IsNullOrEmpty(request.SenderUsername) ||
            string.IsNullOrEmpty(request.ToUsername))
            return BadRequest("Invalid request");

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
        try
        {
            int? senderUserId = _db.GetUserIdByUsername(request.Username);

            if (senderUserId == null)
                return BadRequest("User not found.");

            _db.ShareArticlePublic(senderUserId.Value, request.ArticleId, request.Comment);
            return Ok("Public share succeeded");
        }
        catch (Exception ex)
        {
            return StatusCode(500, "Error: " + ex.Message);
        }
    }






    [HttpGet("Public")]
    public IActionResult GetPublicArticles()
    {
        try
        {
            var articles = _db.GetAllPublicArticles(); // ← ודא שזו המתודה שקיימת
            return Ok(articles);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Server error: {ex.Message}");
        }
    }



    [HttpPost("AddPublicComment")]
    public IActionResult AddPublicComment([FromBody] PublicArticleComment req)
    {
        if (req == null || req.UserId == 0 || req.PublicArticleId == 0)
            return BadRequest("Invalid input");

        _db.AddCommentToPublicArticle(req.PublicArticleId, req.UserId, req.Comment);
        return Ok();
    }


    [HttpGet("GetComments/{publicArticleId}")]
    public IActionResult GetPublicComments(int publicArticleId)
    {
        var comments = _db.GetCommentsForPublicArticle(publicArticleId);
        return Ok(comments);
    }


    [HttpPost("ImportExternal")]
    public async Task<IActionResult> ImportExternalArticles()
    {
        List<Article> externalArticles = await _newsApiService.GetTopHeadlinesAsync();

        foreach (var article in externalArticles)
        {
            int id = _articleService.SaveArticleAndGetId(article); // ← שימוש בשירות
            article.Id = id;
        }

        return Ok(externalArticles);
    }
}
