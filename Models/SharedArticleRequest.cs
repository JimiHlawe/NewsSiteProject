using System.Text.Json.Serialization;

namespace NewsSite1.Models
{
    public class SharedArticleRequest
    {
        private string senderUsername = "";
        private string toUsername = "";
        private int articleId;
        private string comment = "";

        public SharedArticleRequest() { }

        public SharedArticleRequest(string senderUsername, string toUsername, int articleId, string comment)
        {
            this.senderUsername = senderUsername;
            this.toUsername = toUsername;
            this.articleId = articleId;
            this.comment = comment;
        }

        [JsonPropertyName("senderUsername")]
        public string SenderUsername { get => senderUsername; set => senderUsername = value; }

        [JsonPropertyName("toUsername")]
        public string ToUsername { get => toUsername; set => toUsername = value; }

        [JsonPropertyName("articleId")]
        public int ArticleId { get => articleId; set => articleId = value; }

        [JsonPropertyName("comment")]
        public string Comment { get => comment; set => comment = value; }
    }
}
