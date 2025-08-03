using System.Text.Json.Serialization;

namespace NewsSite1.Models.DTOs.Requests
{
    public class PublicArticleShareRequest
    {
        [JsonPropertyName("userId")]
        public int UserId { get; set; }

        [JsonPropertyName("articleId")]
        public int ArticleId { get; set; }

        [JsonPropertyName("comment")]
        public string Comment { get; set; } = "";
    }
}
