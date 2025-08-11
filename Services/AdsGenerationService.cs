using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace NewsSite.Services
{
    /// <summary>Holds the ad text and its image URL.</summary>
    public class AdResult
    {
        public string Text { get; set; }
        public string ImageUrl { get; set; }
    }

    /// <summary>Creates short ad copy and a matching wide image.</summary>
    public class AdsGenerationService
    {
        private readonly HttpClient _http;
        private readonly string _apiKey;

        // Last failure reason (for quick diagnostics)
        public string LastError { get; private set; } = "";

        // Local fallback image if generation fails
        private const string PlaceholderImage = "/images/news-placeholder.png";

        public AdsGenerationService(IConfiguration config)
        {
            _http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            _apiKey = config["OpenAI:ApiKey"] ?? throw new Exception("OpenAI API key is missing");
        }

        /// <summary>
        /// Asks the chat model for ad text. Returns null on failure.
        /// </summary>
        private async Task<string?> ChatAsync(string prompt)
        {
            var payload = new
            {
                model = "gpt-5",
                messages = new[] { new { role = "user", content = prompt } }
            };

            using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            using var res = await _http.SendAsync(req);
            var body = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
            {
                LastError = $"Chat failed: {(int)res.StatusCode} | {body}";
                return null;
            }

            try
            {
                using var doc = JsonDocument.Parse(body);
                return doc.RootElement
                          .GetProperty("choices")[0]
                          .GetProperty("message")
                          .GetProperty("content")
                          .GetString()
                          ?.Trim();
            }
            catch (Exception ex)
            {
                LastError = $"Chat JSON parse error: {ex.Message}";
                return null;
            }
        }

        /// <summary>
        /// Generates a wide banner image (1792x1024). Returns placeholder on failure.
        /// </summary>
        private async Task<string> ImageAsync(string prompt)
        {
            var payload = new
            {
                model = "dall-e-3",
                prompt,
                size = "1792x1024",
                quality = "standard",
                n = 1
            };

            using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/images/generations");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            using var res = await _http.SendAsync(req);
            var body = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
            {
                LastError = $"Image failed: {(int)res.StatusCode} | {body}";
                return PlaceholderImage;
            }

            try
            {
                using var doc = JsonDocument.Parse(body);
                var url = doc.RootElement.GetProperty("data")[0].GetProperty("url").GetString();
                return string.IsNullOrWhiteSpace(url) ? PlaceholderImage : url;
            }
            catch (Exception ex)
            {
                LastError = $"Image JSON parse error: {ex.Message}";
                return PlaceholderImage;
            }
        }

        /// <summary>
        /// Public API: returns ad text + image. Returns null if chat fails.
        /// </summary>
        public async Task<AdResult?> GenerateAdWithImageAsync(string category)
        {
            var adText = await ChatAsync(
                $"Write a short, direct, and engaging advertisement about: {category}. " +
                "Address the reader directly, be persuasive, and avoid emojis. " +
                "Limit the ad to a maximum of 3 short sentences."
            );

            if (string.IsNullOrWhiteSpace(adText))
                return null; // No text -> stop (same behavior)

            var imagePrompt =
                $"Create a realistic, modern advertisement image for: {category}. " +
                "No text in the image. Photography-style composition, clean commercial lighting, " +
                "product-focused subject, minimal background. High-resolution marketing photo.";

            var imageUrl = await ImageAsync(imagePrompt);

            return new AdResult { Text = adText, ImageUrl = imageUrl };
        }
    }
}
