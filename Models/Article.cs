namespace NewsSite1.Models
{
    public class Article
    {
        private int id;
        private string title = "";
        private string description = "";
        private string content = "";
        private string author = "";
        private string sourceUrl = "";
        private string imageUrl = "";
        private DateTime publishedAt;
        private List<string> tags = new List<string>();

        public Article() { }

        public Article(int id, string title, string description, string content,
                       string author, string sourceUrl,
                       string imageUrl, DateTime publishedAt, List<string> tags)
        {
            this.id = id;
            this.title = title;
            this.description = description;
            this.content = content;
            this.author = author;
            this.sourceUrl = sourceUrl;
            this.imageUrl = imageUrl;
            this.publishedAt = publishedAt;
            this.tags = tags;
        }

        public int Id { get => id; set => id = value; }
        public string Title { get => title; set => title = value; }
        public string Description { get => description; set => description = value; }
        public string Content { get => content; set => content = value; }
        public string Author { get => author; set => author = value; }
        public string SourceUrl { get => sourceUrl; set => sourceUrl = value; }
        public string ImageUrl { get => imageUrl; set => imageUrl = value; }
        public DateTime PublishedAt { get => publishedAt; set => publishedAt = value; }
        public List<string> Tags { get => tags; set => tags = value; }
    }
}
