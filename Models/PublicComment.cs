namespace NewsSite1.Models
{
    public class PublicComment
    {
        private int id;
        private int publicArticleId;
        private int userId;
        private string comment = "";
        private DateTime createdAt;
        private string username = "";

        public PublicComment() { }

        public PublicComment(int id, int publicArticleId, int userId, string comment, DateTime createdAt, string username)
        {
            this.id = id;
            this.publicArticleId = publicArticleId;
            this.userId = userId;
            this.comment = comment;
            this.createdAt = createdAt;
            this.username = username;
        }

        public int Id { get => id; set => id = value; }
        public int PublicArticleId { get => publicArticleId; set => publicArticleId = value; }
        public int UserId { get => userId; set => userId = value; }
        public string Comment { get => comment; set => comment = value; }
        public DateTime CreatedAt { get => createdAt; set => createdAt = value; }
        public string Username { get => username; set => username = value; }
    }
}
