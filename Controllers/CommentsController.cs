using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using NewsSite1.DAL;
using NewsSite1.Models;
using NewsSite1.Models.DTOs.Requests;

namespace NewsSite1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CommentsController : ControllerBase
    {
        private readonly DBServices _db;

        public CommentsController(DBServices db)
        {
            _db = db;
        }

        // ===============================
        // == Article Comments (regular) ==
        // ===============================

        /// <summary>
        /// Adds a comment to a regular article (validates user can comment).
        /// </summary>
        [HttpPost("AddToArticle")]
        public IActionResult AddComment([FromBody] CommentRequest comment)
        {
            if (comment == null || comment.ArticleId <= 0 || comment.UserId <= 0 || string.IsNullOrWhiteSpace(comment.Comment))
                return BadRequest("Invalid comment data");

            if (!_db.GetUserCanComment(comment.UserId))
                return Forbid("Commenting is disabled for this user");

            try
            {
                _db.AddCommentToArticle(comment.ArticleId, comment.UserId, comment.Comment);
                return Ok("Comment added successfully");
            }
            catch (Exception)
            {
                return StatusCode(500, "Error adding comment");
            }
        }

        /// <summary>
        /// Gets all comments for a specific regular article.
        /// </summary>
        [HttpGet("GetForArticle/{articleId}")]
        public IActionResult GetComments(int articleId)
        {
            try
            {
                var comments = _db.GetCommentsForArticle(articleId);
                return Ok(comments);
            }
            catch (Exception)
            {
                return StatusCode(500, "Error loading comments");
            }
        }

        // ============================
        // == Public Threads Comments ==
        // ============================

        /// <summary>
        /// Adds a comment to a public thread article.
        /// </summary>
        [HttpPost("AddToThreads")]
        public IActionResult AddCommentToThreads([FromBody] PublicComment comment)
        {
            try
            {
                if (comment == null)
                    return BadRequest("Invalid comment");

                _db.AddCommentToThreads(comment.PublicArticleId, comment.UserId, comment.Comment);
                return Ok();
            }
            catch (Exception)
            {
                return StatusCode(500, "Error adding comment to thread");
            }
        }

        /// <summary>
        /// Loads all comments for a public thread article.
        /// </summary>
        [HttpGet("GetForThreads/{articleId}")]
        public IActionResult LoadThreadsComments(int articleId)
        {
            try
            {
                var comments = _db.LoadThreadsComments(articleId);
                return Ok(comments);
            }
            catch (Exception)
            {
                return StatusCode(500, "Error retrieving thread comments");
            }
        }

        // ======================
        // == Comment Like/Unlike ==
        // ======================

        /// <summary>
        /// Toggles like on a regular article comment.
        /// </summary>
        [HttpPost("ToggleLikeForArticles")]
        public IActionResult ToggleCommentLike([FromBody] CommentLikeRequest req)
        {
            try
            {
                _db.ToggleCommentLike(req.UserId, req.CommentId);
                return Ok();
            }
            catch (Exception)
            {
                return StatusCode(500, "Error toggling like for article comment");
            }
        }

        /// <summary>
        /// Toggles like on a public thread comment.
        /// </summary>
        [HttpPost("ToggleLikeForThreads")]
        public IActionResult TogglePublicCommentLike([FromBody] PublicCommentLikeRequest req)
        {
            try
            {
                _db.TogglePublicCommentLike(req.UserId, req.PublicCommentId);
                return Ok();
            }
            catch (Exception)
            {
                return StatusCode(500, "Error toggling like for thread comment");
            }
        }

        // =================
        // == Like Counts ==
        // =================

        /// <summary>
        /// Gets like count for a regular article comment.
        /// </summary>
        [HttpGet("ArticleCommentLikeCount/{commentId}")]
        public IActionResult GetArticleCommentLikeCount(int commentId)
        {
            try
            {
                int count = _db.GetArticleCommentLikeCount(commentId);
                return Ok(count);
            }
            catch (Exception)
            {
                return StatusCode(500, "Error getting like count for article comment");
            }
        }

        /// <summary>
        /// Gets total likes for a public thread comment.
        /// </summary>
        [HttpGet("ThreadsCommentLikeCount/{publicCommentId}")]
        public IActionResult GetPublicCommentLikeCount(int publicCommentId)
        {
            try
            {
                int count = _db.GetPublicCommentLikeCount(publicCommentId);
                return Ok(count);
            }
            catch (Exception)
            {
                return StatusCode(500, "Error getting like count for thread comment");
            }
        }
    }
}
