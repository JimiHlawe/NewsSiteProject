using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace NewsSite1.Services
{
    public class FirebaseRealtimeService
    {
        private readonly HttpClient _http;
        private readonly string _baseUrl = "https://news-project-e6f1e-default-rtdb.europe-west1.firebasedatabase.app";

        public FirebaseRealtimeService()
        {
            // Reuse a single HttpClient instance for the service lifetime.
            _http = new HttpClient();
        }

        /// <summary>
        /// Updates the inbox count for a specific user.
        /// Sends a JSON integer via HTTP PUT to:
        /// {baseUrl}/userInboxCount/{targetUserId}.json
        /// (Does not throw on non-success; caller can add checks if needed.)
        /// </summary>
        public async Task UpdateInboxCount(int targetUserId, int count)
        {
            string url = $"{_baseUrl}/userInboxCount/{targetUserId}.json";
            var content = new StringContent(JsonSerializer.Serialize(count), Encoding.UTF8, "application/json");
            await _http.PutAsync(url, content);
        }

        /// <summary>
        /// Updates the like count for a specific article.
        /// Sends a JSON integer via HTTP PUT to:
        /// {baseUrl}/likes/article_{articleId}.json
        /// (Does not throw on non-success; caller can add checks if needed.)
        /// </summary>
        public async Task UpdateLikeCount(int articleId, int count)
        {
            string url = $"{_baseUrl}/likes/article_{articleId}.json";
            var content = new StringContent(JsonSerializer.Serialize(count), Encoding.UTF8, "application/json");
            await _http.PutAsync(url, content);
        }
    }
}
