using System.Text.Json.Serialization;

namespace NewsSite1.Models.DTOs.Requests
{
    public class SharedArticleRequest
    {
        public string SenderUsername { get; set; }
        public string ToUsername { get; set; }
        public int ArticleId { get; set; }
        public string Comment { get; set; }
    }

}
