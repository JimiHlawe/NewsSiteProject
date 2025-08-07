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
            catch
            {
                return StatusCode(500, "Error adding comment");
            }
        }

        // ✅ Gets comments for an article
        [HttpGet("GetForArticle/{articleId}")]
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
            catch (Exception ex)
            {
                return StatusCode(500, "Error adding comment to thread");
            }
        }

        [HttpGet("GetForThreads/{articleId}")]
        public IActionResult LoadThreadsComments(int articleId)
        {
            try
            {
                var comments = _db.LoadThreadsComments(articleId);
                return Ok(comments);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error retrieving thread comments");
            }
        }

        // ✅ Toggles like on a regular comment
        [HttpPost("ToggleLikeForArticles")]
        public IActionResult ToggleCommentLike([FromBody] CommentLikeRequest req)
        {
            _db.ToggleCommentLike(req.UserId, req.CommentId);
            return Ok();
        }

        [HttpPost("ToggleLikeForThreads")]
        public IActionResult TogglePublicCommentLike([FromBody] PublicCommentLikeRequest req)
        {
            _db.TogglePublicCommentLike(req.UserId, req.PublicCommentId);
            return Ok();
        }

        // ✅ Gets like count for a regular comment
        [HttpGet("ArticleCommentLikeCount/{commentId}")]
        public IActionResult GetArticleCommentLikeCount(int commentId)
        {
            int count = _db.GetArticleCommentLikeCount(commentId);
            return Ok(count);
        }

        // ✅ Get total likes for a public comment
        [HttpGet("ThreadsCommentLikeCount/{publicCommentId}")]
        public IActionResult GetPublicCommentLikeCount(int publicCommentId)
        {
            int count = _db.GetPublicCommentLikeCount(publicCommentId);
            return Ok(count);
        }











        // ✅ Endpoint for triggering test exception
        [HttpGet("fail")]
        public IActionResult Fail()
        {
            throw new Exception("Simulated failure from CommentsController");
        }

    }

}
