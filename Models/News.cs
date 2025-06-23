namespace NewsSite1.Models
{
    public class News
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string Content { get; set; } = "";
        public string Author { get; set; } = "";
        public string SourceUrl { get; set; } = "";
        public string ImageUrl { get; set; } = "";
    }
}
