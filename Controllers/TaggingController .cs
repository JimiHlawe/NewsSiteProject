using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace NewsSite1.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class TaggingController : ControllerBase
    {
        private readonly IConfiguration _config;

        public TaggingController(IConfiguration config)
        {
            _config = config;
        }

        /// <summary>
        /// Triggers the article-tagging process and sends notifications.
        /// </summary>
        [HttpPost("RunTagging")]
        public async Task<IActionResult> RunTagging()
        {
            try
            {
                TaggingRunner runner = new TaggingRunner(_config);
                await runner.RunAsync();
                return Ok("Tagging completed and notifications sent.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error running tagging: " + ex.Message);
            }
        }
    }
}
