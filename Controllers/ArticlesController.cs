using Microsoft.AspNetCore.Mvc;
using NewsSite1.DAL;
using NewsSite1.Models;

[ApiController]
[Route("api/[controller]")]
public class ArticlesController : ControllerBase
{
    private readonly DBServices db;

    public ArticlesController(DBServices db)
    {
        this.db = db;
    }

    // POST: api/Articles/Add
    [HttpPost("Add")]
    public IActionResult AddArticle([FromBody] Article article)
    {
        if (article == null)
            return BadRequest("Invalid article data");

        db.AddArticle(article);
        return Ok("Article added successfully");
    }

    [HttpGet("All")]
    public IActionResult GetAll()
    {
        List<Article> articles = db.GetAllArticles();
        return Ok(articles);
    }


    [HttpGet("Filter")]
    public IActionResult Filter(string? sourceName, string? title, DateTime? from, DateTime? to)
    {
        List<Article> result = db.FilterArticles(sourceName, title, from, to);
        return Ok(result);
    }

}
