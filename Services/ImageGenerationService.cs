using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace NewsSite.Services
{
    /// <summary>
    /// Generates editorial-style landscape images via OpenAI Images (DALL·E 3).
    /// Returns a default image URL on any failure.
    /// </summary>
    public class ImageGenerationService
    {
        private readonly HttpClient _http;
        private readonly string _apiKey;

        // Fallback image used when generation fails
        private const string DefaultImageUrl = "https://as2.ftcdn.net/v2/jpg/02/09/53/11/1000_F_209531103_vL5MaF5fWcdpVcXk5yREBk3KMcXE0X7m.jpg";

        public ImageGenerationService(IConfiguration config)
        {
            _http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            _apiKey = config["OpenAI:ApiKey"] ?? throw new Exception("OpenAI API key is missing");
        }

        /// <summary>
        /// Builds a neutral, safe, editorial-style prompt from title/description.
        /// Trims overly long inputs to keep prompts concise.
        /// </summary>
        private static string BuildPrompt(string title, string description)
        {
            string safeTitle = (title ?? string.Empty).Trim();
            string safeDesc = (description ?? string.Empty).Trim();

            if (safeTitle.Length > 180) safeTitle = safeTitle[..180] + "...";
            if (safeDesc.Length > 280) safeDesc = safeDesc[..280] + "...";

            string about = string.IsNullOrWhiteSpace(safeDesc) ? "a significant news event" : safeDesc;

            return @$"Create a modern, professional editorial-style image for a news article titled ""{safeTitle}"".
                      The article is about: {about}.
                      Use abstract news cues (world map overlays, digital grids, subtle glowing headlines).
                      Avoid text in the image and avoid explicit or graphic content.
                      Style: clean, magazine-worthy composition, balanced colors, elegant lighting, subtle depth.
                      Aspect: landscape, suitable for a NEWS website.";
        }

        /// <summary>
        /// Calls OpenAI Images (DALL·E 3) to generate a 1792x1024 image URL.
        /// Returns DefaultImageUrl on any failure.
        /// </summary>
        public async Task<string> GenerateImageUrlFromPrompt(string title, string description)
        {
            Console.WriteLine("[ImageGenerationService] Generating image...");
            Console.WriteLine($"  Title: {title}");
            Console.WriteLine($"  Description: {description}");

            string prompt = BuildPrompt(title, description);
            Console.WriteLine($"  Prompt: {prompt}");

            var payload = new
            {
                model = "dall-e-3",
                prompt,
                size = "1792x1024", // landscape
                quality = "standard",
                n = 1
            };

            using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/images/generations");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            try
            {
                using var res = await _http.SendAsync(req);
                string json = await res.Content.ReadAsStringAsync();

                Console.WriteLine($"  Response Status: {(int)res.StatusCode}");

                if (!res.IsSuccessStatusCode)
                {
                    Console.WriteLine($"  Request failed. Body: {json}");
                    return DefaultImageUrl;
                }

                using var doc = JsonDocument.Parse(json);
                var data = doc.RootElement.GetProperty("data");
                if (data.GetArrayLength() == 0)
                {
                    Console.WriteLine("  No data returned from OpenAI. Using default image.");
                    return DefaultImageUrl;
                }

                string? url = data[0].GetProperty("url").GetString();
                Console.WriteLine($"  Image URL: {url}");
                return string.IsNullOrWhiteSpace(url) ? DefaultImageUrl : url;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Exception: {ex.Message}");
                return DefaultImageUrl;
            }
        }
    }
}
