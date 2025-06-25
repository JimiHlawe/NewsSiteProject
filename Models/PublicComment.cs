public class PublicComment
{
    public int Id { get; set; }
    public int PublicArticleId { get; set; }
    public int UserId { get; set; }
    public string Comment { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Username { get; set; }
}
