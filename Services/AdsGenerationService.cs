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

        public async Task<AdResult> GenerateAdWithImageAsync(string category)
        {
            string prompt = $"Write a short and engaging ad about: {category}";

            // 🧠 שליחת בקשה טקסטואלית ל־GPT
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

            Console.WriteLine("📥 GPT Response: " + chatContent);

            if (!chatResponse.IsSuccessStatusCode)
            {
                Console.WriteLine("❌ Failed to generate ad text.");
                return null;
            }

            var chatJson = JsonDocument.Parse(chatContent);
            string adText = chatJson.RootElement
                                    .GetProperty("choices")[0]
                                    .GetProperty("message")
                                    .GetProperty("content")
                                    .GetString();

            // 🖼 יצירת תמונה תואמת לפרסומת
            string imagePrompt = $@"
Create a realistic, modern advertisement image for the following product or service: {category}.
Avoid text in the image. Focus on photography-style composition, clean commercial lighting, product-focused visual, minimalistic background.
Style: high-resolution professional marketing photo or rendered product shot for a real advertisement campaign.";


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

            Console.WriteLine("🖼 Image Response: " + imageContent);

            if (!imageResponse.IsSuccessStatusCode)
            {
                Console.WriteLine("❌ Failed to generate ad image.");
                return new AdResult { Text = adText?.Trim(), ImageUrl = "/images/news-placeholder.png" };
            }

            var imageJson = JsonDocument.Parse(imageContent);
            string imageUrl = imageJson.RootElement
                                       .GetProperty("data")[0]
                                       .GetProperty("url")
                                       .GetString();

            return new AdResult
            {
                Text = adText?.Trim(),
                ImageUrl = imageUrl
            };
        }
    }
}
