using System.Net.Http;
using System.Text.Json;
using NewsSite1.Models;

public class NewsFetcher
{
    private static readonly HttpClient client = new HttpClient();
    private string apiKey = "4e9ec1f00eda4bc7800e28cef3d5bed3";

    public async Task<List<Article>> FetchTopHeadlinesAsync()
    {
        string url = $"https://newsapi.org/v2/top-headlines?country=us&apiKey={apiKey}";

        HttpResponseMessage response = await client.GetAsync(url);
        if (!response.IsSuccessStatusCode)
            return new List<Article>();

        string json = await response.Content.ReadAsStringAsync();
        using JsonDocument doc = JsonDocument.Parse(json);
        JsonElement root = doc.RootElement;
        JsonElement articles = root.GetProperty("articles");

        List<Article> result = new List<Article>();
        foreach (var item in articles.EnumerateArray())
        {
            var article = new Article
            {
                Title = item.GetProperty("title").GetString(),
                Description = item.GetProperty("description").GetString() ?? "",
                Content = item.GetProperty("content").GetString() ?? "",
                Author = item.GetProperty("author").GetString() ?? "",
                SourceName = item.GetProperty("source").GetProperty("name").GetString() ?? "",
                SourceUrl = item.GetProperty("url").GetString() ?? "",
                ImageUrl = item.TryGetProperty("urlToImage", out var img) ? img.GetString() ?? "" : "",
                PublishedAt = item.GetProperty("publishedAt").GetDateTime()
            };
            result.Add(article);
        }

        return result;
    }
}
