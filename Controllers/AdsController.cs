using Microsoft.AspNetCore.Mvc;
using NewsSite.Services;
using System;
using System.Threading.Tasks;

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

        // ✅ Generates an ad with an image for a given category
        [HttpGet("Generate")]
        public async Task<IActionResult> GenerateAd([FromQuery] string category)
        {
            try
            {
                var adResult = await _adsService.GenerateAdWithImageAsync(category);

                if (adResult == null)
                    return StatusCode(500, "Failed to generate ad");

                return Ok(adResult);
            }
            catch (Exception)
            {
                return StatusCode(500, "An error occurred while generating the ad");
            }
        }
    }
}
