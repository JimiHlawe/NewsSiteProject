using Microsoft.AspNetCore.Mvc;
using NewsSite1.DAL;

namespace NewsSite1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SavedArticlesController : ControllerBase
    {
        private readonly DBServices db;

        public SavedArticlesController(DBServices db)
        {
            this.db = db;
        }

        [HttpPost("Save")]
        public IActionResult SaveArticle([FromBody] SaveArticleRequest request)
        {
            try
            {
                db.SaveArticle(request.UserId, request.ArticleId);
                return Ok("Article saved");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Server error: " + ex.Message);
            }
        }

        [HttpPost("Remove")]
        public IActionResult RemoveSavedArticle([FromBody] SaveArticleRequest request)
        {
            try
            {
                db.RemoveSavedArticle(request.UserId, request.ArticleId);
                return Ok("Removed successfully");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Server error: " + ex.Message);
            }
        }

        [HttpGet("GetAll/{userId}")]
        public IActionResult GetSavedArticles(int userId)
        {
            var articles = db.GetSavedArticles(userId);
            return Ok(articles);
        }
    }

    public class SaveArticleRequest
    {
        public int UserId { get; set; }
        public int ArticleId { get; set; }
    }
}
