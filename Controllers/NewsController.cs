using Microsoft.AspNetCore.Mvc;
using NewsSite1.DAL;
using NewsSite1.Models;
using NewsSite1.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NewsSite1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NewsController : ControllerBase
    {
        private readonly NewsApiService _newsApiService;
        private readonly DBServices _db;

        public NewsController(NewsApiService newsApiService)
        {
            _newsApiService = newsApiService;
            _db = new DBServices(); // יצירת מופע של DBServices
        }

        [HttpGet]
        public IActionResult GetNews()
        {
            List<Article> savedArticles = _db.GetAllSavedArticles(); // יותר קריא
            return Ok(savedArticles);
        }


        [HttpPost("ImportExternal")]
        public async Task<IActionResult> ImportExternalArticles()
        {
            // משתמש באותו שירות שמביא את החדשות (כמו ב-GetNews)
            List<Article> externalArticles = await _newsApiService.GetTopHeadlinesAsync();

            foreach (var article in externalArticles)
            {
                int id = _db.SaveArticleAndGetId(article);
                article.Id = id; // 🟢 עדכון ה-ID שחזר מה-DB
            }

            return Ok(externalArticles);
        }
    }
}
