namespace NewsSite1.Models
{
    public class SharedArticle : Article
    {
        private string comment = "";
        private DateTime sharedAt;
        private string senderName = "";

        public SharedArticle() { }

        public SharedArticle(int id, string title, string description, string content,
                             string author, string sourceName, string sourceUrl,
                             string imageUrl, DateTime publishedAt,
                             string comment, DateTime sharedAt, string senderName)
            : base(id, title, description, content, author, sourceName, sourceUrl, imageUrl, publishedAt)
        {
            this.comment = comment;
            this.sharedAt = sharedAt;
            this.senderName = senderName;
        }

        public string Comment { get => comment; set => comment = value; }
        public DateTime SharedAt { get => sharedAt; set => sharedAt = value; }
        public string SenderName { get => senderName; set => senderName = value; }
    }
}
