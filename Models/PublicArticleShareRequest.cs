using System.Text.Json.Serialization;

public class PublicArticleShareRequest
{
    [JsonPropertyName("username")]
    public string Username { get; set; }

    [JsonPropertyName("articleId")]
    public int ArticleId { get; set; }

    [JsonPropertyName("comment")]
    public string Comment { get; set; }
}
