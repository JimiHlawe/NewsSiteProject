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

        [HttpPost("Add")]
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
            catch
            {
                return StatusCode(500, "Error adding comment");
            }
        }

        // ✅ Gets comments for an article
        [HttpGet("Get/{articleId}")]
        public IActionResult GetComments(int articleId)
        {
            try
            {
                var comments = _db.GetCommentsForArticle(articleId);
                return Ok(comments);
            }
            catch
            {
                return StatusCode(500, "Error loading comments");
            }
        }

        // ✅ Toggles like on a regular comment
        [HttpPost("ToggleLike")]
        public IActionResult ToggleCommentLike([FromBody] CommentLikeRequest req)
        {
            _db.ToggleCommentLike(req.UserId, req.CommentId);
            return Ok();
        }

        // ✅ Toggles like on a public comment
        [HttpPost("TogglePublicLike")]
        public IActionResult TogglePublicCommentLike([FromBody] PublicCommentLikeRequest req)
        {
            _db.TogglePublicCommentLike(req.UserId, req.PublicCommentId);
            return Ok();
        }

        // ✅ Gets like count for a regular comment
        [HttpGet("LikeCount/{commentId}")]
        public IActionResult GetCommentLikeCount(int commentId)
        {
            int count = _db.GetCommentLikeCount(commentId);
            return Ok(count);
        }


        [HttpPost("AddPublic")]
        public IActionResult AddPublicComment([FromBody] PublicComment comment)
        {
            try
            {
                if (comment == null)
                    return BadRequest("Invalid comment");

                _db.AddPublicComment(comment.PublicArticleId, comment.UserId, comment.Comment);
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error adding comment");
            }
        }


        [HttpGet("Public/{articleId}")]
        public IActionResult GetCommentsForPublicArticle(int articleId)
        {
            try
            {
                var comments = _db.GetCommentsForPublicArticle(articleId);
                return Ok(comments);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error retrieving public comments");
            }
        }




        // ✅ Endpoint for triggering test exception
        [HttpGet("fail")]
        public IActionResult Fail()
        {
            throw new Exception("Simulated failure from CommentsController");
        }

    }

}
