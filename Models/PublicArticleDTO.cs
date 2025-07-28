namespace NewsSite1.Models
{
    public class PublicArticleDTO
    {
        public int PublicArticleId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Author { get; set; }
        public string ImageUrl { get; set; }
        public string SourceUrl { get; set; }
        public DateTime PublishedAt { get; set; }
        public string SenderName { get; set; }
        public string InitialComment { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
    }
}
