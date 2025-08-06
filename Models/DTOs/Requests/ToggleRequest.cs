using System.Text.Json.Serialization;

namespace NewsSite1.Models.DTOs.Requests
{
    public class ToggleRequest
    {
        [JsonPropertyName("userId")]
        public int UserId { get; set; }

        [JsonPropertyName("enable")]
        public bool Enable { get; set; }
    }
}
