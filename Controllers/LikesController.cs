using Microsoft.AspNetCore.Mvc;
using NewsSite1.DAL;
using NewsSite1.Models;
using NewsSite1.Models.DTOs.Requests;
using NewsSite1.Models.DTOs;
using NewsSite1.Services; // ✅ הוספת namespace של FirebaseRealtimeService
using System.Threading.Tasks;

namespace NewsSite1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LikesController : ControllerBase
    {
        private readonly DBServices _db;
        private readonly FirebaseRealtimeService _firebase;

        public LikesController(DBServices db, FirebaseRealtimeService firebase)
        {
            _db = db;
            _firebase = firebase;
        }

        // ✅ Toggles like on a regular article and updates Firebase
        // ✅ Toggles like on a regular article and updates Firebase
        [HttpPost("ToggleArticleLike")]
        public async Task<IActionResult> ToggleArticleLike([FromBody] LikeRequest req)
        {
            bool liked = _db.ToggleArticleLike(req.UserId, req.ArticleId);

            int newCount = _db.GetLikesCount(req.ArticleId);
            await _firebase.UpdateLikeCount(req.ArticleId, newCount);

            return Ok(new { liked });
        }

        // ✅ Toggles like on a public thread article and updates Firebase
        [HttpPost("ToggleThreadLike")]
        public async Task<IActionResult> ToggleThreadLike([FromBody] LikeThreadRequest req)
        {
            bool liked = _db.ToggleThreadLike(req.UserId, req.PublicArticleId);

            int newCount = _db.GetThreadLikeCount(req.PublicArticleId);
            await _firebase.UpdateLikeCount(req.PublicArticleId, newCount);

            return Ok(new { liked });
        }


        //// ✅ Gets like count for an article
        //[HttpGet("Count/{articleId}")]
        //public IActionResult GetLikesCount(int articleId)
        //{
        //    int count = _db.GetLikesCount(articleId);
        //    return Ok(count);
        //}

        // ✅ Gets like count for a public thread article
        //[HttpGet("ThreadLikeCount/{publicArticleId}")]
        //public IActionResult GetThreadLikeCount(int publicArticleId)
        //{
        //    int count = _db.GetThreadLikeCount(publicArticleId);
        //    return Ok(count);
        //}

        // ✅ Checks if user liked a public thread article
        [HttpGet("Check/{userId}/{articleId}")]
        public IActionResult CheckUserLike(int userId, int articleId)
        {
            bool liked = _db.CheckIfUserLikedThread(userId, articleId);
            return Ok(liked);
        }

        // ✅ Endpoint for triggering test exception
        [HttpGet("fail")]
        public IActionResult Fail()
        {
            throw new Exception("Simulated failure from LikesController");
        }
    }
}
