namespace NewsSite1.Models
{
    public class Comment
    {
        private int id;
        private int articleId;
        private int userId;
        private string commentText;
        private DateTime createdAt;
        private string username;

        public Comment() { }

        public Comment(int id, int articleId, int userId, string commentText, DateTime createdAt, string username)
        {
            this.id = id;
            this.articleId = articleId;
            this.userId = userId;
            this.commentText = commentText;
            this.createdAt = createdAt;
            this.username = username;
        }

        public int Id { get => id; set => id = value; }
        public int ArticleId { get => articleId; set => articleId = value; }
        public int UserId { get => userId; set => userId = value; }
        public string CommentText { get => commentText; set => commentText = value; }
        public DateTime CreatedAt { get => createdAt; set => createdAt = value; }
        public string Username { get => username; set => username = value; }
    }
}
