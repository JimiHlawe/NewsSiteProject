using Microsoft.AspNetCore.Mvc;
using NewsSite1.Models;
using NewsSite1.Services;

namespace NewsSite1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NewsController : ControllerBase
    {
        private readonly NewsApiService _newsApiService;

        public NewsController(NewsApiService newsApiService)
        {
            _newsApiService = newsApiService;
        }

        [HttpGet]
        public async Task<IActionResult> GetNews()
        {
            var articles = await _newsApiService.GetTopHeadlinesAsync();
            return Ok(articles);
        }
    }

}
