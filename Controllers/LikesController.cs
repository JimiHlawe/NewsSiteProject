using Microsoft.AspNetCore.Mvc;
using NewsSite1.DAL;
using NewsSite1.Models;

namespace NewsSite1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LikesController : ControllerBase
    {
        private readonly DBServices _db;

        public LikesController(DBServices db)
        {
            _db = db;
        }

        // ✅ Adds like to an article
        [HttpPost("Like")]
        public IActionResult Like([FromBody] LikeRequest req)
        {
            _db.AddLike(req.UserId, req.ArticleId);
            return Ok();
        }

        // ✅ Removes like from an article
        [HttpPost("Unlike")]
        public IActionResult Unlike([FromBody] LikeRequest req)
        {
            _db.RemoveLike(req.UserId, req.ArticleId);
            return Ok();
        }

        // ✅ Gets like count for an article
        [HttpGet("Count/{articleId}")]
        public IActionResult GetLikesCount(int articleId)
        {
            int count = _db.GetLikesCount(articleId);
            return Ok(count);
        }

        // ✅ Toggles like on a public thread article
        [HttpPost("ToggleThreadLike")]
        public IActionResult ToggleThreadLike([FromBody] LikeThreadRequest req)
        {
            bool liked = _db.ToggleThreadLike(req.UserId, req.PublicArticleId);
            return Ok(new { liked });
        }

        // ✅ Checks if user liked a public thread article
        [HttpGet("Check/{userId}/{articleId}")]
        public IActionResult CheckUserLike(int userId, int articleId)
        {
            bool liked = _db.CheckIfUserLikedThread(userId, articleId);
            return Ok(liked);
        }

        // ✅ Toggle like for a public comment
        [HttpPost("TogglePublicCommentLike")]
        public IActionResult TogglePublicCommentLike([FromBody] PublicCommentLikeRequest req)
        {
            _db.TogglePublicCommentLike(req.UserId, req.PublicCommentId);
            return Ok();
        }

        // ✅ Get total likes for a public comment
        [HttpGet("PublicCommentLikeCount/{publicCommentId}")]
        public IActionResult GetPublicCommentLikeCount(int publicCommentId)
        {
            int count = _db.GetPublicCommentLikeCount(publicCommentId);
            return Ok(count);
        }


        [HttpGet("ThreadLikeCount/{publicArticleId}")]
        public IActionResult GetThreadLikeCount(int publicArticleId)
        {
            int count = _db.GetThreadLikeCount(publicArticleId);
            return Ok(count);
        }

        // ✅ Endpoint for triggering test exception
        [HttpGet("fail")]
        public IActionResult Fail()
        {
            throw new Exception("Simulated failure from LikesController");
        }

    }
}
