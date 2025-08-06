using Microsoft.AspNetCore.Mvc;
using NewsSite1.DAL;
using NewsSite1.Models;
using System;
using System.Collections.Generic;
using NewsSite1.Models.DTOs;
using NewsSite1.Models.DTOs.Requests;
using NewsSite1.Services;


namespace NewsSite1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly DBServices db;
        private readonly NewsApiService newsApiService;

        public AdminController(DBServices db, NewsApiService newsApiService)
        {
            this.db = db;
            this.newsApiService = newsApiService;
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

        [HttpPost("ImportExternal")]
        public async Task<IActionResult> ImportExternal()
        {
            try
            {
                var articles = await newsApiService.GetTopHeadlinesAsync(); // שליפת כתבות מה-API
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

    }
}
