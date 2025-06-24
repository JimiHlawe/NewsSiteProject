public class PublicArticle
{
    public int PublicArticleId { get; set; }
    public int ArticleId { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string Content { get; set; }
    public string Author { get; set; }
    public string SourceUrl { get; set; }
    public string ImageUrl { get; set; }
    public DateTime PublishedAt { get; set; }
    public string SenderName { get; set; }
    public string InitialComment { get; set; }
    public DateTime SharedAt { get; set; }
}
