using Microsoft.AspNetCore.Mvc;
using NewsSite1.DAL;
using NewsSite1.Models;
using System;
using System.Collections.Generic;
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



        // ✅ Returns like statistics for the site
        [HttpGet("LikesStats")]
        public IActionResult GetLikesStats()
        {
            try
            {
                var stats = db.GetLikesStats();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Failed to load like statistics");
            }
        }

        [HttpGet("Reports/TodayCount")]
        public IActionResult GetTodayReportsCount()
        {
            try
            {
                int count = db.GetTodayArticleReportsCount();
                return Ok(count);
            }
            catch
            {
                return StatusCode(500, "Failed to get report count");
            }
        }

        // ✅ Returns all reports (articles and comments)
        [HttpGet("AllReports")]
        public IActionResult GetAllReports()
        {
            try
            {
                var reports = db.GetAllReports();
                return Ok(reports ?? new List<ReportEntryDTO>());
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Failed to load all reports");
            }
        }

        // ✅ Sets active status of a user
        [HttpPost("SetActiveStatus")]
        public IActionResult SetActiveStatus([FromBody] SetStatusRequest req)
        {
            db.SetUserActiveStatus(req.UserId, req.IsActive);
            return Ok("User status updated");
        }

        // ✅ Sets whether user can share articles
        [HttpPost("SetSharingStatus")]
        public IActionResult SetSharingStatus([FromBody] SharingStatusRequest req)
        {
            db.SetUserSharingStatus(req.UserId, req.CanShare);
            return Ok("Sharing status updated");
        }

        // ✅ Sets whether user can comment on articles
        [HttpPost("SetCommentingStatus")]
        public IActionResult SetCommentingStatus([FromBody] SharingStatusRequest req)
        {
            db.SetUserCommentingStatus(req.UserId, req.CanComment);
            return Ok("Commenting status updated");
        }

        // ✅ Gets overall site statistics
        [HttpGet("GetStatistics")]
        public IActionResult GetStatistics()
        {
            return Ok(db.GetSiteStatistics());
        }

        // ✅ Adds a new user article
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
            catch
            {
                return StatusCode(500, "Error adding user article");
            }
        }

        [HttpPost("GetFromNewsAPI")] 
        public async Task<IActionResult> ImportExternal()
        {
            try
            {
                var articles = await newsApiService.GetNewsAPISAsync(); // שליפת כתבות מה-API
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

        [HttpPost("FixMissingImages")]
        public async Task<IActionResult> FixMissingImages()
        {
            var allArticles = db.GetAllArticles(); // שלוף את כל הכתבות
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

                    if (imageUrl.Contains("News1.jpg")) // מזהה את ברירת המחדל -> content policy violation
                    {
                        skippedDueToContentPolicy++;
                        continue;
                    }

                    article.ImageUrl = imageUrl;
                    db.UpdateArticleImageUrl(article.Id, imageUrl); // מימוש בצד DB
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

    }
}
