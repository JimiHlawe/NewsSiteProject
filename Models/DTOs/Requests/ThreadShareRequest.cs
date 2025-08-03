namespace NewsSite1.Models.DTOs.Requests
{
    public class ThreadShareRequest
    {
        public int publicArticleId { get; set; }
        public string senderUsername { get; set; }
        public string toUsername { get; set; }
        public string comment { get; set; }
    }
}
