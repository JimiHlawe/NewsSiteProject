using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace NewsSite1.Services
{
    /// <summary>
    /// Uses OpenAI Chat Completions to detect relevant tags for an article
    /// from a fixed allow-list. Returns only allowed tags (no new tags).
    /// </summary>
    public class OpenAiTagService
    {
        private readonly HttpClient _http;
        private readonly string _apiKey;

        /// <summary>
        /// The only tags we accept. Output is filtered strictly to this set.
        /// </summary>
        private static readonly List<string> AllowedTags = new List<string>
        {
            "politics", "technology", "sports", "economy", "health", "culture", "science", "education",
            "military", "world", "law", "crime", "environment", "business", "entertainment", "weather",
            "travel", "opinion", "local", "breaking"
        };

        public OpenAiTagService(IConfiguration config)
        {
            _http = new HttpClient();
            _apiKey = config["OpenAI:ApiKey"] ?? throw new Exception("OpenAI API key is missing");
        }

        /// <summary>
        /// Sends title/content to OpenAI and asks for comma-separated tags.
        /// Returns only tags that exist in AllowedTags (lowercased, deduplicated).
        /// Throws on HTTP errors (including a specific message on 502).
        /// </summary>
        public async Task<List<string>> DetectTagsAsync(string title, string content)
        {
            // Prompt instructs the model to pick only from AllowedTags
            string prompt = $@"
                            From the following list of allowed tags, choose all that are relevant to this article.
                            Do not invent new tags. Only use tags from this list:
                            {string.Join(", ", AllowedTags)}

                            ---
                            Title: {title}
                            Content: {content}

                            Tags (comma-separated):".Trim();

            // Build Chat Completions request (model kept as-is)
            var payload = new
            {
                model = "gpt-3.5-turbo-0125",
                messages = new[]
                {
                    new { role = "user", content = prompt }
                },
                temperature = 0.3
            };

            using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            using var res = await _http.SendAsync(req);

            if (!res.IsSuccessStatusCode)
            {
                var errorBody = await res.Content.ReadAsStringAsync();

                if ((int)res.StatusCode == 502)
                    throw new Exception("OpenAI API is temporarily unavailable (502 Bad Gateway). Please try again later.");

                throw new Exception($"OpenAI Error ({res.StatusCode}): {errorBody}");
            }

            // Parse the response JSON and extract the raw tag string
            using var stream = await res.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);

            string tagsText = "";
            if (doc.RootElement.TryGetProperty("choices", out var choices) &&
                choices.GetArrayLength() > 0 &&
                choices[0].TryGetProperty("message", out var message) &&
                message.TryGetProperty("content", out var contentEl))
            {
                tagsText = contentEl.GetString() ?? "";
            }

            // Normalize: split by comma, trim, lowercase, keep only allowed, distinct
            var result = tagsText
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim().ToLowerInvariant())
                .Where(t => AllowedTags.Contains(t))
                .Distinct()
                .ToList();

            return result;
        }
    }
}
