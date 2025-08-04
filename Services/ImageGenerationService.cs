using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System;
using System.Text.RegularExpressions;

namespace NewsSite.Services
{
    public class ImageGenerationService
    {
        private readonly HttpClient _httpClient;
        private readonly string _openAiApiKey;
        private const string DefaultImageUrl = "http://tbinfo.org/sites/default/files/gallery/News1.jpg";

        public ImageGenerationService(IConfiguration config)
        {
            _httpClient = new HttpClient();
            _openAiApiKey = config["OpenAI:ApiKey"] ?? throw new Exception("OpenAI API key is missing");
        }

        // ✅ Generates an image URL from the given title and description using OpenAI
        public async Task<string> GenerateImageUrlFromPrompt(string title, string description)
        {
            Console.WriteLine("✅ [ImageGenerationService] Generating image for:");
            Console.WriteLine($"   Title: {title}");
            Console.WriteLine($"   Description: {description}");

            string safePrompt = BuildSafePrompt(title, description);
            Console.WriteLine($"🧠 Prompt Sent: {safePrompt}");

            var requestBody = new
            {
                prompt = safePrompt,
                n = 1,
                size = "512x512"
            };

            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri("https://api.openai.com/v1/images/generations"),
                Headers =
                {
                    { "Authorization", $"Bearer {_openAiApiKey}" }
                },
                Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
            };

            try
            {
                var response = await _httpClient.SendAsync(request);
                var json = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"📦 Response Status: {response.StatusCode}");
                Console.WriteLine($"📦 Response JSON: {json}");

                if (!response.IsSuccessStatusCode)
                {
                    if (json.Contains("content_policy_violation", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine("⚠️ Content policy violation detected. Using default image.");
                        return DefaultImageUrl;
                    }

                    Console.WriteLine("❌ Request failed. Returning null.");
                    return null;
                }

                using var doc = JsonDocument.Parse(json);
                if (!doc.RootElement.TryGetProperty("data", out JsonElement dataArray) || dataArray.GetArrayLength() == 0)
                {
                    Console.WriteLine("❌ No data returned from OpenAI. Using default image.");
                    return DefaultImageUrl;
                }

                string url = dataArray[0].GetProperty("url").GetString();
                Console.WriteLine($"✅ Image URL generated: {url}");
                return url ?? DefaultImageUrl;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Exception occurred: {ex.Message}");
                return null;
            }
        }

        // ✅ Builds a safe prompt for the image generation request
        private string BuildSafePrompt(string title, string description)
        {
            string rawCombined = title + " " + description;

            if (IsSensitive(rawCombined))
            {
                Console.WriteLine("🚨 Sensitive content detected. Using fallback prompt.");
                return @"Create a visually compelling illustration suitable for a global news platform.
                        Depict a dynamic 'Breaking News' concept with abstract elements like world map overlays, digital grids, glowing headlines, or satellite imagery.
                        Avoid any political, violent, or controversial visuals.

                        Style: Clean, professional, editorial-style design with sharp lines, glowing highlights, and a modern tech-inspired color scheme.";
            }

            string safeTitle = Clean(title);
            string cleanDesc = string.IsNullOrWhiteSpace(description)
                ? "breaking news coverage of a significant event"
                : Clean(TrimToLength(description, 300));

            return @$"Create a modern, professional editorial-style illustration for a news article titled: ""{safeTitle}"".
                    The article is about: {cleanDesc}

                    The image should visually reflect the core message or emotion of the article, while staying appropriate for public media.

                    Avoid: political figures, explicit scenes, violence, or controversial symbols.

                    Style: Digital illustration with a clean, magazine-worthy layout, balanced colors, elegant lighting, and subtle depth.
                    Incorporate abstract visual cues (e.g., news tickers, glowing headlines, world icons) if literal imagery is not suitable.";
        }

        // ✅ Cleans the text by redacting forbidden and sensitive words
        private string Clean(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";

            string[] forbiddenWords = new[]
            {
                "bomb", "terror", "kill", "nude", "blood", "dead", "shoot", "weapon",
                "gun", "murder", "attack", "sex", "rape", "suicide", "explosion", "war",
                "drugs", "violence", "abuse", "politics", "death", "death toll",
                "hostage", "conflict", "military", "missile", "airstrike", "ceasefire"
            };

            string[] sensitiveNames = new[]
            {
                "Trump", "Biden", "Putin", "Xi Jinping", "Netanyahu", "Iran", "Israel",
                "Russia", "Ukraine", "Palestine", "Hamas", "Hezbollah", "China", "USA"
            };

            foreach (var word in forbiddenWords.Concat(sensitiveNames))
            {
                text = Regex.Replace(
                    text,
                    $@"\b{Regex.Escape(word)}\b",
                    "[REDACTED]",
                    RegexOptions.IgnoreCase
                );
            }

            return text;
        }

        // ✅ Checks if the input contains any sensitive or triggering keywords
        private bool IsSensitive(string input)
        {
            string[] triggers = new[]
            {
                "war", "ceasefire", "conflict", "explosion", "terror", "kill", "Trump", "Iran", "Israel",
                "attack", "violence", "nuclear", "missile", "hostage", "politics", "Putin", "Biden", "suicide"
            };

            foreach (var word in triggers)
            {
                if (Regex.IsMatch(input, $@"\b{Regex.Escape(word)}\b", RegexOptions.IgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        // ✅ Trims the text to a specific max length
        private string TrimToLength(string text, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";
            return text.Length > maxLength ? text.Substring(0, maxLength) + "..." : text;
        }
    }
}
