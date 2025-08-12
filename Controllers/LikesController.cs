using Microsoft.AspNetCore.Mvc;
using NewsSite1.DAL;
using NewsSite1.Models.DTOs.Requests;
using NewsSite1.Services; // FirebaseRealtimeService
using System;
using System.Threading.Tasks;

namespace NewsSite1.Controllers
{
    /// <summary>
    /// Handles like/unlike actions for regular articles and public threads, with realtime updates.
    /// </summary>
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

        // ============================
        // == Regular Article Likes  ==
        // ============================

        /// <summary>
        /// Toggles like on a regular article and updates Firebase like count.
        /// </summary>
        [HttpPost("ToggleArticleLike")]
        public async Task<IActionResult> ToggleArticleLike([FromBody] LikeRequest req)
        {
            try
            {
                bool liked = _db.ToggleArticleLike(req.UserId, req.ArticleId);

                int newCount = _db.GetLikesCount(req.ArticleId);
                await _firebase.UpdateLikeCount(req.ArticleId, newCount);

                return Ok(new { liked });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error toggling article like: " + ex.Message);
            }
        }

        // ============================
        // == Public Thread Likes    ==
        // ============================

        /// <summary>
        /// Toggles like on a public thread article and updates Firebase like count.
        /// </summary>
        [HttpPost("ToggleThreadLike")]
        public async Task<IActionResult> ToggleThreadLike([FromBody] LikeThreadRequest req)
        {
            try
            {
                bool liked = _db.ToggleThreadLike(req.UserId, req.PublicArticleId);

                int newCount = _db.GetThreadLikeCount(req.PublicArticleId);
                await _firebase.UpdateLikeCount(req.PublicArticleId, newCount);

                return Ok(new { liked });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error toggling thread like: " + ex.Message);
            }
        }
    }
}
