using Microsoft.AspNetCore.Mvc;
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
        var articles = _db.GetAllArticles();
        return Ok(articles);
    }

    [HttpGet("Filter")]
    public IActionResult Filter(string? sourceName, string? title, DateTime? from, DateTime? to)
    {
        var result = _db.FilterArticles(sourceName, title, from, to);
        return Ok(result);
    }

    [HttpPost("ImportExternal")]
    public async Task<IActionResult> ImportExternalArticles()
    {
        var externalArticles = await _newsApiService.GetTopHeadlinesAsync();

        foreach (var article in externalArticles)
        {
            int id = _articleService.SaveArticleAndGetId(article);
            article.Id = id;
        }

        return Ok(externalArticles);
    }
}
