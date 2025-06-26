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

    public ArticlesController(DBServices db, NewsApiService newsApiService)
    {
        _db = db;
        _newsApiService = newsApiService;
    }

    [HttpPost("Add")]
    public IActionResult AddArticle([FromBody] Article article)
    {
        if (article == null)
            return BadRequest("Invalid article data");

        int id = _db.AddOrGetArticle(article);
        article.Id = id;

        return Ok(article); // מחזיר את הכתבה עם ID, גם אם הייתה קיימת
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
            int id = _db.AddOrGetArticle(article); // במקום _articleService.SaveArticleAndGetId
            article.Id = id;
        }

        return Ok(externalArticles);
    }
}
