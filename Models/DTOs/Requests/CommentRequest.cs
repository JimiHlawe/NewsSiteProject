namespace NewsSite1.Models.DTOs.Requests
{
    public class CommentRequest
    {
        public int ArticleId { get; set; }
        public int UserId { get; set; }
        public string Comment { get; set; } = "";
    }
}
