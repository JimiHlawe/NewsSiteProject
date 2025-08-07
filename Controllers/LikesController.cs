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

        // ✅ Adds like to an article and updates Firebase
        [HttpPost("Like")]
        public async Task<IActionResult> Like([FromBody] LikeRequest req)
        {
            _db.AddLike(req.UserId, req.ArticleId);

            int newCount = _db.GetLikesCount(req.ArticleId);
            await _firebase.UpdateLikeCount(req.ArticleId, newCount);

            return Ok();
        }

        // ✅ Removes like from an article and updates Firebase
        [HttpPost("Unlike")]
        public async Task<IActionResult> Unlike([FromBody] LikeRequest req)
        {
            _db.RemoveLike(req.UserId, req.ArticleId);

            int newCount = _db.GetLikesCount(req.ArticleId);
            await _firebase.UpdateLikeCount(req.ArticleId, newCount);

            return Ok();
        }

        // ✅ Gets like count for an article
        [HttpGet("Count/{articleId}")]
        public IActionResult GetLikesCount(int articleId)
        {
            int count = _db.GetLikesCount(articleId);
            return Ok(count);
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


        // ✅ Checks if user liked a public thread article
        [HttpGet("Check/{userId}/{articleId}")]
        public IActionResult CheckUserLike(int userId, int articleId)
        {
            bool liked = _db.CheckIfUserLikedThread(userId, articleId);
            return Ok(liked);
        }





        // ✅ Gets like count for a public thread article
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
