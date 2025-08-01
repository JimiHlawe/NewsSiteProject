using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

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

        [HttpPost("RunTagging")]
        public async Task<IActionResult> RunTagging()
        {
            TaggingRunner runner = new TaggingRunner(_config);
            await runner.RunAsync();
            return Ok("Tagging completed and notifications sent.");
        }

    }
}
