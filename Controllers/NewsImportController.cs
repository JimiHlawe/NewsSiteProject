using Microsoft.AspNetCore.Mvc;
using NewsSite1.DAL;

[Route("api/[controller]")]
[ApiController]
public class NewsImportController : ControllerBase
{
    private readonly DBServices db = new DBServices();
    private readonly NewsFetcher fetcher = new NewsFetcher();

    [HttpPost("Import")]
    public async Task<IActionResult> ImportArticles()
    {
        var articles = await fetcher.FetchTopHeadlinesAsync();

        int addedCount = 0;
        foreach (var article in articles)
        {
            try
            {
                db.AddArticle(article);
                addedCount++;
            }
            catch
            {
                // אפשר לרשום ללוג במקום לזרוק שגיאה
            }
        }

        return Ok($"{addedCount} articles imported.");
    }
}
