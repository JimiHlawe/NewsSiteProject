namespace NewsSite1.Models
{
    public class Article
    {
        private int id;
        private string title = "";
        private string description = "";
        private string content = "";
        private string author = "";
        private string sourceName = "";
        private string sourceUrl = "";
        private string imageUrl = "";
        private DateTime publishedAt;

        public Article() { }

        public Article(int id, string title, string description, string content,
                       string author, string sourceName, string sourceUrl, string imageUrl, DateTime publishedAt)
        {
            this.id = id;
            this.title = title;
            this.description = description;
            this.content = content;
            this.author = author;
            this.sourceName = sourceName;
            this.sourceUrl = sourceUrl;
            this.imageUrl = imageUrl;
            this.publishedAt = publishedAt;
        }

        public int Id { get => id; set => id = value; }
        public string Title { get => title; set => title = value; }
        public string Description { get => description; set => description = value; }
        public string Content { get => content; set => content = value; }
        public string Author { get => author; set => author = value; }
        public string SourceName { get => sourceName; set => sourceName = value; }
        public string SourceUrl { get => sourceUrl; set => sourceUrl = value; }
        public string ImageUrl { get => imageUrl; set => imageUrl = value; }
        public DateTime PublishedAt { get => publishedAt; set => publishedAt = value; }
    }
}
