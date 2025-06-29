namespace NewsSite1.Models
{
    public class PublicArticle
    {
        private int publicArticleId;
        private int articleId;
        private string title = "";
        private string description = "";
        private string content = "";
        private string author = "";
        private string sourceUrl = "";
        private string imageUrl = "";
        private DateTime publishedAt;
        private string senderName = "";
        private string initialComment = "";
        private DateTime sharedAt;

        private List<string> tags = new List<string>(); 
        private List<PublicComment> publicComments = new List<PublicComment>();

        public PublicArticle() { }

        public PublicArticle(int publicArticleId, int articleId, string title, string description, string content,
                             string author, string sourceUrl, string imageUrl, DateTime publishedAt,
                             string senderName, string initialComment, DateTime sharedAt,
                             List<string> tags, List<PublicComment> publicComments)
        {
            this.publicArticleId = publicArticleId;
            this.articleId = articleId;
            this.title = title;
            this.description = description;
            this.content = content;
            this.author = author;
            this.sourceUrl = sourceUrl;
            this.imageUrl = imageUrl;
            this.publishedAt = publishedAt;
            this.senderName = senderName;
            this.initialComment = initialComment;
            this.sharedAt = sharedAt;
            this.tags = tags ?? new List<string>(); 
            this.publicComments = publicComments ?? new List<PublicComment>();
        }

        public int PublicArticleId { get => publicArticleId; set => publicArticleId = value; }
        public int ArticleId { get => articleId; set => articleId = value; }
        public string Title { get => title; set => title = value; }
        public string Description { get => description; set => description = value; }
        public string Content { get => content; set => content = value; }
        public string Author { get => author; set => author = value; }
        public string SourceUrl { get => sourceUrl; set => sourceUrl = value; }
        public string ImageUrl { get => imageUrl; set => imageUrl = value; }
        public DateTime PublishedAt { get => publishedAt; set => publishedAt = value; }
        public string SenderName { get => senderName; set => senderName = value; }
        public string InitialComment { get => initialComment; set => initialComment = value; }
        public DateTime SharedAt { get => sharedAt; set => sharedAt = value; }

        public List<string> Tags { get => tags; set => tags = value; }  
        public List<PublicComment> PublicComments { get => publicComments; set => publicComments = value; }
    }
}
