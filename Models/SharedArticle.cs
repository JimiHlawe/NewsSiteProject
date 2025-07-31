namespace NewsSite1.Models
{
    public class SharedArticle : Article
    {
        // שדות קיימים
        private string comment = "";
        private DateTime sharedAt;
        private string senderName = "";

        // ✅ שדה תגיות
        public List<string> Tags { get; set; } = new List<string>();

        public SharedArticle() { }

        public SharedArticle(int id, string title, string description, string content,
                             string author, string sourceUrl,
                             string imageUrl, DateTime publishedAt,
                             List<string> tags,
                             string comment, DateTime sharedAt, string senderName, int sharedId)
            : base(id, title, description, content, author, sourceUrl, imageUrl, publishedAt, tags)
        {
            this.comment = comment;
            this.sharedAt = sharedAt;
            this.senderName = senderName;
            this.SharedId = sharedId;

            this.Tags = tags; // ← להשלמה גם כאן
        }

        public string Comment { get => comment; set => comment = value; }
        public DateTime SharedAt { get => sharedAt; set => sharedAt = value; }
        public string SenderName { get => senderName; set => senderName = value; }
        public int SharedId { get; set; }
    }

}
