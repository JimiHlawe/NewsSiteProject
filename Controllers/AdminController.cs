using Microsoft.AspNetCore.Mvc;
using NewsSite1.DAL;
using NewsSite1.Models;
using System;
using System.Collections.Generic;
using NewsSite1.Models.DTOs;


namespace NewsSite1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly DBServices db;

        public AdminController(DBServices db)
        {
            this.db = db;
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

        // ✅ Returns reported articles for review
        [HttpGet("ReportedArticles")]
        public IActionResult GetReportedArticles()
        {
            try
            {
                return Ok(db.GetReportedArticles());
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Failed to load reported articles");
            }
        }

        // ✅ Returns reported comments for review
        [HttpGet("ReportedComments")]
        public IActionResult GetReportedComments()
        {
            try
            {
                return Ok(db.GetReportedComments());
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Failed to load reported comments");
            }
        }

        // ✅ Returns all reports (articles and comments)
        [HttpGet("AllReports")]
        public IActionResult GetAllReports()
        {
            try
            {
                var reports = db.GetAllReports();
                return Ok(reports ?? new List<ReportEntry>());
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Failed to load all reports");
            }
        }
    }
}
