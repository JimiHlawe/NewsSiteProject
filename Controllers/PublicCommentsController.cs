using Microsoft.AspNetCore.Mvc;
using NewsSite1.DAL;
using NewsSite1.Models;

[ApiController]
[Route("api/[controller]")]
public class PublicCommentsController : ControllerBase
{
    private readonly DBServices _db;

    public PublicCommentsController(DBServices db)
    {
        _db = db;
    }

    [HttpPost("Add")]
    public IActionResult AddComment([FromBody] PublicCommentRequest comment)
    {
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

    [HttpGet("GetForArticle/{articleId}")]
    public IActionResult GetComments(int articleId)
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
}
