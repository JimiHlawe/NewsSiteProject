using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace NewsSite1.Services
{
    public class FirebaseRealtimeService
    {
        private readonly HttpClient _httpClient;
        private readonly string _firebaseBaseUrl = "https://news-project-e6f1e-default-rtdb.europe-west1.firebasedatabase.app";

        public FirebaseRealtimeService()
        {
            _httpClient = new HttpClient();
        }

        // ✅ Updates the inbox count for a specific user in Firebase Realtime Database
        public async Task UpdateInboxCount(int targetUserId, int count)
        {
            string url = $"{_firebaseBaseUrl}/userInboxCount/{targetUserId}.json";
            var content = new StringContent(JsonSerializer.Serialize(count), Encoding.UTF8, "application/json");
            await _httpClient.PutAsync(url, content);
        }

        // ✅ Updates the like count for a specific article in Firebase Realtime Database
        public async Task UpdateLikeCount(int articleId, int count)
        {
            string url = $"{_firebaseBaseUrl}/likes/article_{articleId}.json";
            var content = new StringContent(JsonSerializer.Serialize(count), Encoding.UTF8, "application/json");
            await _httpClient.PutAsync(url, content);
        }
    }
}
