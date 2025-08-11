using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using NewsSite1.Models;
using System;
using System.Linq;
using NewsSite.Models;

namespace NewsSite1.Services
{
    public class NewsApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey = "4e9ec1f00eda4bc7800e28cef3d5bed3";

        public NewsApiService()
        {
            _httpClient = new HttpClient();
        }

        // ✅ Fetches top US news headlines and maps them to internal Article model
        public async Task<List<Article>> GetNewsAPISAsync()
        {
            try
            {
                string url = $"https://newsapi.org/v2/top-headlines?country=us&apiKey={_apiKey}";

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("User-Agent", "NewsSite1App/1.0");

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var news = await response.Content.ReadFromJsonAsync<NewsApi>();

                return news?.Articles.Select(a => new Article
                {
                    Title = a.Title ?? "",
                    Description = a.Description ?? "",
                    Content = a.Content ?? "",
                    Author = a.Author ?? "",
                    SourceUrl = a.Url ?? "",
                    ImageUrl = a.UrlToImage ?? "",
                    PublishedAt = DateTime.TryParse(a.PublishedAt, out DateTime dt) ? dt : DateTime.MinValue
                }).ToList() ?? new List<Article>();
            }
            catch (Exception ex)
            {
                throw new Exception("NewsAPI call failed: " + ex.Message);
            }
        }
    }
}
