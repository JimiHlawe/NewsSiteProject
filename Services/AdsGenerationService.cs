using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace NewsSite.Services
{
    public class AdResult
    {
        public string Text { get; set; }
        public string ImageUrl { get; set; }
    }

    public class AdsGenerationService
    {
        private readonly HttpClient _httpClient;
        private readonly string _openAiApiKey;

        public AdsGenerationService(IConfiguration config)
        {
            _httpClient = new HttpClient();
            _openAiApiKey = config["OpenAI:ApiKey"] ?? throw new Exception("OpenAI API key is missing");
        }

        // ✅ Sends a request to OpenAI's chat API and returns the response content as string
        private async Task<string?> SendChatRequestAsync(string prompt)
        {
            var chatRequest = new
            {
                model = "gpt-4",
                messages = new[] {
                    new { role = "user", content = prompt }
                }
            };

            var chatHttpReq = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
            chatHttpReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _openAiApiKey);
            chatHttpReq.Content = new StringContent(JsonSerializer.Serialize(chatRequest), Encoding.UTF8, "application/json");

            var chatResponse = await _httpClient.SendAsync(chatHttpReq);
            var chatContent = await chatResponse.Content.ReadAsStringAsync();

            if (!chatResponse.IsSuccessStatusCode)
            {
                return null;
            }

            return chatContent;
        }

        // ✅ Sends a request to OpenAI's image API and returns the image URL
        private async Task<string> SendImageRequestAsync(string imagePrompt)
        {
            var imageRequest = new
            {
                prompt = imagePrompt,
                n = 1,
                size = "512x512"
            };

            var imageHttpReq = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/images/generations");
            imageHttpReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _openAiApiKey);
            imageHttpReq.Content = new StringContent(JsonSerializer.Serialize(imageRequest), Encoding.UTF8, "application/json");

            var imageResponse = await _httpClient.SendAsync(imageHttpReq);
            var imageContent = await imageResponse.Content.ReadAsStringAsync();

            if (!imageResponse.IsSuccessStatusCode)
            {
                return "/images/news-placeholder.png";
            }

            var imageJson = JsonDocument.Parse(imageContent);
            if (imageJson.RootElement.TryGetProperty("data", out JsonElement dataArray) &&
                dataArray.GetArrayLength() > 0 &&
                dataArray[0].TryGetProperty("url", out JsonElement urlElement))
            {
                return urlElement.GetString() ?? "/images/news-placeholder.png";
            }

            return "/images/news-placeholder.png";
        }

        // ✅ Generates a short ad text and a matching image for a given category
        public async Task<AdResult?> GenerateAdWithImageAsync(string category)
        {
            string prompt = $"Write a short and engaging ad about: {category}";

            string? chatContent = await SendChatRequestAsync(prompt);
            if (string.IsNullOrWhiteSpace(chatContent))
            {
                return null;
            }

            var chatJson = JsonDocument.Parse(chatContent);

            string adText = "";
            if (chatJson.RootElement.TryGetProperty("choices", out JsonElement choicesArray) &&
                choicesArray.GetArrayLength() > 0 &&
                choicesArray[0].TryGetProperty("message", out JsonElement messageElement) &&
                messageElement.TryGetProperty("content", out JsonElement contentElement))
            {
                adText = contentElement.GetString()?.Trim() ?? "";
            }

            string imagePrompt = $@"
                Create a realistic, modern advertisement image for the following product or service: {category}.
                Avoid text in the image. Focus on photography-style composition, clean commercial lighting, product-focused visual, minimalistic background.
                Style: high-resolution professional marketing photo or rendered product shot for a real advertisement campaign.";

            string imageUrl = await SendImageRequestAsync(imagePrompt);

            return new AdResult
            {
                Text = adText,
                ImageUrl = imageUrl
            };
        }
    }
}
