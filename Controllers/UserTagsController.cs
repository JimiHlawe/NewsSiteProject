using Microsoft.AspNetCore.Mvc;
using NewsSite1.DAL;

namespace NewsSite1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserTagsController : ControllerBase
    {
        private readonly DBServices db;

        public UserTagsController(DBServices db)
        {
            this.db = db;
        }

        [HttpPost("Add")]
        public IActionResult AddTag([FromBody] AddTagRequest data)
        {
            db.AddUserTag(data.UserId, data.TagId);
            return Ok("Tag added to user");
        }

        [HttpGet("Get/{userId}")]
        public IActionResult GetTags(int userId)
        {
            var tags = db.GetUserTags(userId);
            return Ok(tags);
        }
    }

    public class AddTagRequest
    {
        public int UserId { get; set; }
        public int TagId { get; set; }
    }
}
