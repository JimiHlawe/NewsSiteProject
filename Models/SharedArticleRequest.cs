using System.Text.Json.Serialization;

public class SharedArticleRequest
{
    [JsonPropertyName("senderUsername")]
    public string SenderUsername { get; set; }

    [JsonPropertyName("toUsername")]
    public string ToUsername { get; set; }

    [JsonPropertyName("articleId")]
    public int ArticleId { get; set; }

    [JsonPropertyName("comment")]
    public string Comment { get; set; }
}
