using Microsoft.AspNetCore.Mvc;
using NewsSite1.DAL;
using NewsSite1.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NewsSite1.Models.DTOs;
using NewsSite1.Models.DTOs.Requests;
using NewsSite1.Services;
using NewsSite.Services;

namespace NewsSite1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly DBServices db;
        private readonly NewsApiService newsApiService;
        private readonly ImageGenerationService imageGen;

        public AdminController(DBServices db, NewsApiService newsApiService, ImageGenerationService imageGen)
        {
            this.db = db;
            this.newsApiService = newsApiService;
            this.imageGen = imageGen;
        }

        // ============================
        // == Stats & Reports (GET)  ==
        // ============================

        /// <summary>
        /// Returns like statistics for the site.
        /// </summary>
        [HttpGet("LikesStats")]
        public IActionResult GetLikesStats()
        {
            try
            {
                var stats = db.GetLikesStats();
                return Ok(stats);
            }
            catch (Exception)
            {
                return StatusCode(500, "Failed to load like statistics");
            }
        }

        /// <summary>
        /// Returns the number of article reports created today.
        /// </summary>
        [HttpGet("Reports/TodayCount")]
        public IActionResult GetTodayReportsCount()
        {
            try
            {
                int count = db.GetTodayArticleReportsCount();
                return Ok(count);
            }
            catch (Exception)
            {
                return StatusCode(500, "Failed to get report count");
            }
        }

        /// <summary>
        /// Returns all reports (articles and comments).
        /// </summary>
        [HttpGet("AllReports")]
        public IActionResult GetAllReports()
        {
            try
            {
                var reports = db.GetAllReports();
                return Ok(reports ?? new List<ReportEntryDTO>());
            }
            catch (Exception)
            {
                return StatusCode(500, "Failed to load all reports");
            }
        }

        /// <summary>
        /// Returns overall site statistics.
        /// </summary>
        [HttpGet("GetStatistics")]
        public IActionResult GetStatistics()
        {
            try
            {
                var stats = db.GetSiteStatistics();
                return Ok(stats);
            }
            catch (Exception)
            {
                return StatusCode(500, "Failed to load site statistics");
            }
        }

        // ======================================
        // == User Moderation Flags (POST)     ==
        // ======================================

        /// <summary>
        /// Sets a user's active status (enable/disable login).
        /// </summary>
        [HttpPost("SetActiveStatus")]
        public IActionResult SetActiveStatus([FromBody] SetStatusRequest req)
        {
            try
            {
                db.SetUserActiveStatus(req.UserId, req.IsActive);
                return Ok("User status updated");
            }
            catch (Exception)
            {
                return StatusCode(500, "Failed to update user active status");
            }
        }

        /// <summary>
        /// Sets whether a user can share articles.
        /// </summary>
        [HttpPost("SetSharingStatus")]
        public IActionResult SetSharingStatus([FromBody] SharingStatusRequest req)
        {
            try
            {
                db.SetUserSharingStatus(req.UserId, req.CanShare);
                return Ok("Sharing status updated");
            }
            catch (Exception)
            {
                return StatusCode(500, "Failed to update sharing status");
            }
        }

        /// <summary>
        /// Sets whether a user can comment on articles.
        /// </summary>
        [HttpPost("SetCommentingStatus")]
        public IActionResult SetCommentingStatus([FromBody] SharingStatusRequest req)
        {
            try
            {
                db.SetUserCommentingStatus(req.UserId, req.CanComment);
                return Ok("Commenting status updated");
            }
            catch (Exception)
            {
                return StatusCode(500, "Failed to update commenting status");
            }
        }

        // ======================================
        // == Article Admin (Add/Import/Fix/DeleteReports)   ==
        // ======================================

        /// <summary>
        /// Adds a new user-submitted article.
        /// </summary>
        [HttpPost("AddUserArticle")]
        public IActionResult AddUserArticle([FromBody] Article article)
        {
            if (article == null ||
                string.IsNullOrEmpty(article.Title) ||
                string.IsNullOrEmpty(article.Description) ||
                string.IsNullOrEmpty(article.Content) ||
                string.IsNullOrEmpty(article.Author) ||
                string.IsNullOrEmpty(article.SourceUrl) ||
                string.IsNullOrEmpty(article.ImageUrl) ||
                article.PublishedAt == default)
            {
                return BadRequest("Invalid article data");
            }

            article.Tags ??= new List<string>();

            try
            {
                int newId = db.AddUserArticle(article);

                if (newId == -1)
                    return Conflict("Article with the same URL already exists");

                article.Id = newId;
                return Ok(article);
            }
            catch (Exception)
            {
                return StatusCode(500, "Error adding user article");
            }
        }

        /// <summary>
        /// Imports external articles from NewsAPI and saves new ones.
        /// </summary>
        [HttpPost("GetFromNewsAPI")]
        public async Task<IActionResult> ImportExternal()
        {
            try
            {
                var articles = await newsApiService.GetNewsAPISAsync();
                var importedArticles = new List<Article>();

                foreach (var article in articles)
                {
                    int newId = db.AddUserArticle(article);
                    if (newId > 0)
                    {
                        article.Id = newId;
                        importedArticles.Add(article);
                    }
                }

                return Ok(importedArticles);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error importing articles: " + ex.Message);
            }
        }

        /// <summary>
        /// Generates images for articles missing an image (skips default/policy-blocked results).
        /// </summary>
        [HttpPost("FixMissingImages")]
        public async Task<IActionResult> FixMissingImages()
        {
            try
            {
                var allArticles = db.GetAllArticles();
                int success = 0, failed = 0, skippedDueToContentPolicy = 0;

                foreach (var article in allArticles.Where(a => string.IsNullOrWhiteSpace(a.ImageUrl)))
                {
                    try
                    {
                        var imageUrl = await imageGen.GenerateImageUrlFromPrompt(article.Title, article.Content);

                        if (imageUrl == null)
                        {
                            failed++;
                            continue;
                        }

                        // Skip default placeholder (treated as policy-violation/placeholder)
                        if (imageUrl.Contains("News1.jpg"))
                        {
                            skippedDueToContentPolicy++;
                            continue;
                        }

                        article.ImageUrl = imageUrl;
                        db.UpdateArticleImageUrl(article.Id, imageUrl);
                        success++;
                    }
                    catch
                    {
                        failed++;
                    }
                }

                return Ok(new
                {
                    success,
                    skippedDueToContentPolicy,
                    failed
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error fixing missing images: " + ex.Message);
            }
        }

        /// Handles deletion of a reported target (article or comment). 
        /// Normalizes a PublicArticleId to its ArticleId before deletion and returns 404 if nothing was removed.
        [HttpPost("DeleteReportedTarget")]
        public IActionResult DeleteReportedTarget([FromBody] DeleteReportedTargetRequest req)
        {
            if (req == null || req.TargetId <= 0) return BadRequest("Invalid request"); // basic guard
            try
            {
                int ok = 0; // rows affected / success flag from SP

                if (string.Equals(req.TargetKind, "Article", StringComparison.OrdinalIgnoreCase))
                {
                    // Normalize: if an admin sent a PublicArticleId, resolve to the owning ArticleId
                    var targetId = req.TargetId;
                    if (!db.ArticleExists(targetId) && db.PublicArticleExists(targetId))
                    {
                        var resolved = db.GetArticleIdByPublicArticle(targetId);
                        if (resolved > 0) targetId = resolved; // use ArticleId
                    }
                    ok = db.DeleteArticleAndReports(targetId); // delete article + all related data
                }
                else
                {
                    // Try regular comment first; if not found, try public (threads) comment
                    if (db.CommentExists(req.TargetId))
                        ok = db.DeleteCommentAndReports(req.TargetId);
                    else if (db.PublicCommentExists(req.TargetId))
                        ok = db.DeletePublicCommentAndReports(req.TargetId);
                    else
                        ok = 0; // nothing matched
                }

                if (ok == 0) return NotFound(); // no rows affected
                return Ok(new { deleted = true }); // success
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message }); // unexpected failure
            }
        }



    }



}
