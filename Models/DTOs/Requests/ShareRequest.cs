namespace NewsSite1.Models.DTOs.Requests
{

    public class ShareRequest
    {
        public int UserId { get; set; }
        public int TargetUserId { get; set; }
        public int ArticleId { get; set; }
        public string Comment { get; set; }
    }
}
