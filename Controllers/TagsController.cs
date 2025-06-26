using Microsoft.AspNetCore.Mvc;
using NewsSite1.DAL;

[ApiController]
[Route("api/[controller]")]
public class TagsController : ControllerBase
{
    private readonly DBServices _db;

    public TagsController(DBServices db)
    {
        _db = db;
    }

    [HttpGet("Article/{articleId}")]
    public IActionResult GetTagsForArticle(int articleId)
    {
        try
        {
            var tags = _db.GetTagsForArticle(articleId);
            return Ok(tags);
        }
        catch (Exception ex)
        {
            return StatusCode(500, "Error: " + ex.Message);
        }
    }

    [HttpGet("AllArticles")]
    public IActionResult GetArticlesWithTags()
    {
        try
        {
            var articles = _db.GetArticlesWithTags();
            return Ok(articles);
        }
        catch (Exception ex)
        {
            return StatusCode(500, "Error: " + ex.Message);
        }
    }
}
