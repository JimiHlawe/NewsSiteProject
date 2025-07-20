using Microsoft.AspNetCore.Mvc;
using NewsSite1.DAL;
using NewsSite1.Models;

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

        [HttpGet("LikesStats")]
        public IActionResult GetLikesStats()
        {
            var stats = db.GetLikesStats();
            return Ok(stats);
        }

        [HttpGet("ReportedArticles")]
        public IActionResult GetReportedArticles()
        {
            return Ok(db.GetReportedArticles());
        }

        [HttpGet("ReportedComments")]
        public IActionResult GetReportedComments()
        {
            return Ok(db.GetReportedComments());
        }


        [HttpGet("AllReports")]
        public IActionResult GetAllReports()
        {
            var reports = db.GetAllReports(); // מחזיר List<ReportModel>
            return Ok(db.GetAllReports() ?? new List<ReportEntry>());
        }

    }
}
