using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public class FirebaseRealtimeService
{
    private readonly HttpClient _httpClient;
    private readonly string _firebaseBaseUrl = "https://news-project-e6f1e-default-rtdb.europe-west1.firebasedatabase.app";

    public FirebaseRealtimeService()
    {
        _httpClient = new HttpClient();
    }

    public async Task UpdateInboxCount(int targetUserId, int count)
    {
        string url = $"{_firebaseBaseUrl}/userInboxCount/{targetUserId}.json";

        var content = new StringContent(JsonSerializer.Serialize(count), Encoding.UTF8, "application/json");

        await _httpClient.PutAsync(url, content);
    }
}
