using Microsoft.AspNetCore.Mvc;
using NewsSite1.DAL;
using NewsSite1.Models;
using NewsSite1.Models.DTOs;
using NewsSite1.Models.DTOs.Requests;




namespace NewsSite1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportsController : ControllerBase
    {
        private readonly DBServices _db;

        public ReportsController(DBServices db)
        {
            _db = db;
        }

        // ✅ Reports content (article or comment)
        [HttpPost("Report")]
        public IActionResult ReportContent([FromBody] ReportRequest req)
        {
            if (req == null || req.UserId <= 0 || string.IsNullOrEmpty(req.ReferenceType))
                return BadRequest("Invalid report data");

            try
            {
                _db.ReportContent(req.UserId, req.ReferenceType, req.ReferenceId, req.Reason);
                return Ok("Reported");
            }
            catch
            {
                return StatusCode(500, "Error reporting content");
            }
        }
    }
}
