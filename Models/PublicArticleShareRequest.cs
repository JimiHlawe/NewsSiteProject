using System.Text.Json.Serialization;

namespace NewsSite1.Models
{
    public class PublicArticleShareRequest
    {
        private int userId;
        private int articleId;
        private string comment = "";

        public PublicArticleShareRequest() { }

        public PublicArticleShareRequest(int userId, int articleId, string comment)
        {
            this.userId = userId;
            this.articleId = articleId;
            this.comment = comment;
        }

        [JsonPropertyName("userId")]
        public int UserId { get => userId; set => userId = value; }

        [JsonPropertyName("articleId")]
        public int ArticleId { get => articleId; set => articleId = value; }

        [JsonPropertyName("comment")]
        public string Comment { get => comment; set => comment = value; }
    }
}
