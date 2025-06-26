namespace NewsSite1.Models
{
    public class PublicCommentRequest
    {
        private int publicArticleId;
        private int userId;
        private string comment = "";

        public PublicCommentRequest() { }

        public PublicCommentRequest(int publicArticleId, int userId, string comment)
        {
            this.publicArticleId = publicArticleId;
            this.userId = userId;
            this.comment = comment;
        }

        public int PublicArticleId { get => publicArticleId; set => publicArticleId = value; }
        public int UserId { get => userId; set => userId = value; }
        public string Comment { get => comment; set => comment = value; }
    }
}
