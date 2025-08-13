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
    /// Generates editorial-style landscape images via OpenAI Images (gpt-image-1).
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
            _http = new HttpClient { Timeout = TimeSpan.FromSeconds(120) }; // ↑ give image gen enough time
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

            // very light content softening to reduce policy blocks
            static string soften(string s) => s
                .Replace("kill", "harm")
                .Replace("terror", "attack")
                .Replace("blood", "injury")
                .Replace("shoot", "fire")
                .Replace("war", "conflict");

            safeTitle = soften(safeTitle);
            safeDesc = soften(safeDesc);

            string about = string.IsNullOrWhiteSpace(safeDesc) ? "a significant news event" : safeDesc;

            // one-line prompt helps avoid weird parsing
            return $"Create a modern, professional editorial-style image for a news article titled \"{safeTitle}\". " +
                   $"The article is about: {about}. Use abstract news cues (world map overlays, digital grids, subtle glowing headlines). " +
                   $"Avoid any text in the image and avoid explicit or graphic content. " +
                   $"Style: clean, magazine-worthy composition, balanced colors, elegant lighting, subtle depth. " +
                   $"Aspect: landscape, suitable for a NEWS website.";
        }

        /// <summary>
        /// Calls OpenAI Images (gpt-image-1) to generate a 1792x1024 image URL.
        /// Returns DefaultImageUrl on any failure.
        /// </summary>
        public async Task<string> GenerateImageUrlFromPrompt(string title, string description)
        {
            Console.WriteLine("[ImageGenerationService] Generating image...");
            Console.WriteLine($"  Title: {title}");
            Console.WriteLine($"  Description: {description}");

            var prompt = BuildPrompt(title, description);
            Console.WriteLine($"  Prompt: {prompt}");

            var payload = new
            {
                model = "gpt-image-1",
                prompt,
                size = "1792x1024",
                response_format = "url",   // ← ask for direct URL
                n = 1
            };

            using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/images");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            try
            {
                using var res = await _http.SendAsync(req);
                string body = await res.Content.ReadAsStringAsync();
                Console.WriteLine($"  Response Status: {(int)res.StatusCode}");

                if (!res.IsSuccessStatusCode)
                {
                    // surface error body – most common: safety policy (400) or invalid model/endpoint
                    Console.WriteLine("  Request failed. Body:");
                    Console.WriteLine(body);
                    return DefaultImageUrl;
                }

                using var doc = JsonDocument.Parse(body);
                if (!doc.RootElement.TryGetProperty("data", out var data) || data.GetArrayLength() == 0)
                {
                    Console.WriteLine("  No data returned from OpenAI. Using default image.");
                    return DefaultImageUrl;
                }

                string? url = null;
                var first = data[0];

                if (first.TryGetProperty("url", out var urlProp))
                    url = urlProp.GetString();
                else if (first.TryGetProperty("b64_json", out _))
                {
                    // you asked for url, but just in case: treat as failure here to avoid handling storage
                    Console.WriteLine("  Received b64_json instead of url. Using default image.");
                    return DefaultImageUrl;
                }

                Console.WriteLine($"  Image URL: {url}");
                return string.IsNullOrWhiteSpace(url) ? DefaultImageUrl : url;
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine("  Request timed out. Using default image.");
                return DefaultImageUrl;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Exception: {ex.GetType().Name}: {ex.Message}");
                return DefaultImageUrl;
            }
        }

        // Expose the default URL so controller can detect it (optional)
        public static string GetDefaultImageUrl() => DefaultImageUrl;
    }
}
