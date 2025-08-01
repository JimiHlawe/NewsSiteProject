﻿using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace NewsSite1.Services
{
    public class OpenAiTagService
    {
        private readonly string _openAiApiKey;
        private readonly HttpClient _httpClient;

        private static readonly List<string> AllowedTags = new List<string>
        {
            "politics", "technology", "sports", "economy", "health", "culture", "science", "education",
            "military", "world", "law", "crime", "environment", "business", "entertainment", "weather",
            "travel", "opinion", "local", "breaking"
        };

        public OpenAiTagService(IConfiguration config)
        {
            _httpClient = new HttpClient();
            _openAiApiKey = config["OpenAI:ApiKey"] ?? throw new Exception("OpenAI API key is missing");
        }

        public async Task<List<string>> DetectTagsAsync(string title, string content)
        {
            string prompt = $@"
From the following list of allowed tags, choose all that are relevant to this article. 
Do not invent new tags. Only use tags from this list:
{string.Join(", ", AllowedTags)}

---
Title: {title}
Content: {content}

Tags (comma-separated):";

            var requestBody = new
            {
                model = "gpt-3.5-turbo-0125",
                messages = new[]
                {
                    new { role = "user", content = prompt }
                },
                temperature = 0.3
            };

            var requestJson = JsonSerializer.Serialize(requestBody);
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
            request.Headers.Add("Authorization", $"Bearer {_openAiApiKey}"); // ← תוקן כאן
            request.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();

                if ((int)response.StatusCode == 502)
                {
                    throw new Exception("OpenAI API is temporarily unavailable (502 Bad Gateway). Please try again later.");
                }

                throw new Exception($"OpenAI Error ({response.StatusCode}): {errorBody}");
            }

            using var contentStream = await response.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(contentStream);

            var tagsText = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            var resultTags = tagsText.Split(',')
                                     .Select(t => t.Trim().ToLower())
                                     .Where(t => AllowedTags.Contains(t))
                                     .Distinct()
                                     .ToList();

            return resultTags;
        }
    }
}
