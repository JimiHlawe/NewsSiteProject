using System.Text.Json.Serialization;

namespace NewsSite1.Models.DTOs.Requests
{
    public class UserBlockRequest
    {
        [JsonPropertyName("blockerUserId")]
        public int BlockerUserId { get; set; }

        [JsonPropertyName("blockedUserId")]
        public int BlockedUserId { get; set; }
    }

}
