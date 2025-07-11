﻿namespace NewsSite1.Models
{
    public class SharedArticle : Article
    {
        private string comment = "";
        private DateTime sharedAt;
        private string senderName = "";

        public SharedArticle() { }

        public SharedArticle(int id, string title, string description, string content,
                             string author, string sourceUrl,
                             string imageUrl, DateTime publishedAt,
                             List<string> tags,
                             string comment, DateTime sharedAt, string senderName)
            : base(id, title, description, content, author, sourceUrl, imageUrl, publishedAt, tags)
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
