using Microsoft.AspNetCore.Mvc;
using NewsSite1.DAL;
using NewsSite1.Models;

[ApiController]
[Route("api/[controller]")]
public class SharedArticlesController : ControllerBase
{
    private readonly DBServices _db;

    public SharedArticlesController(DBServices db)
    {
        _db = db;
    }

    [HttpPost("SharePrivate")]
    public IActionResult SharePrivate([FromBody] SharedArticleRequest request)
    {
        if (request == null ||
            string.IsNullOrEmpty(request.SenderUsername) ||
            string.IsNullOrEmpty(request.ToUsername))
            return BadRequest("Invalid request");

        try
        {
            _db.ShareArticleByUsernames(request.SenderUsername, request.ToUsername, request.ArticleId, request.Comment);
            return Ok("Article shared privately");
        }
        catch (Exception ex)
        {
            return StatusCode(500, "Server error: " + ex.Message);
        }
    }

    [HttpGet("SharedWithMe/{userId}")]
    public IActionResult GetSharedWithMe(int userId)
    {
        try
        {
            var result = _db.GetArticlesSharedWithUser(userId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, "Error: " + ex.Message);
        }
    }

    [HttpPost("SharePublic")]
    public IActionResult SharePublic([FromBody] PublicArticleShareRequest request)
    {
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

    [HttpGet("Public")]
    public IActionResult GetAllPublic()
    {
        try
        {
            var result = _db.GetAllPublicArticles();
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest("DB Error: " + ex.Message);
        }
    }
}
