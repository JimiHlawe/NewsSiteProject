using Microsoft.AspNetCore.Mvc;
using NewsSite.Services;
using System;
using System.Threading.Tasks;

namespace NewsSite.Controllers
{
    /// <summary>
    /// Provides endpoints for generating ad creatives (text + image).
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AdsController : ControllerBase
    {
        private readonly AdsGenerationService _adsService;

        public AdsController(AdsGenerationService adsService)
        {
            _adsService = adsService;
        }

        // ============================
        // == Ad Generation ==
        // ============================

        /// <summary>
        /// Generates an ad (text + image) for the given category.
        /// </summary>
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
