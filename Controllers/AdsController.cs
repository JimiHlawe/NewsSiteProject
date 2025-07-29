using Microsoft.AspNetCore.Mvc;
using NewsSite.Services;

namespace NewsSite.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdsController : ControllerBase
    {
        private readonly AdsGenerationService _adsService;

        public AdsController(AdsGenerationService adsService)
        {
            _adsService = adsService;
        }

        [HttpGet("Generate")]
        public async Task<IActionResult> GenerateAd([FromQuery] string category)
        {
            var adResult = await _adsService.GenerateAdWithImageAsync(category);
            if (adResult == null)
                return StatusCode(500, "Failed to generate ad");

            return Ok(adResult);
        }
    }
}
