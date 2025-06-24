using NewsSite1.Models;

public class SharedArticle : Article
{
    public string Comment { get; set; }
    public DateTime SharedAt { get; set; }
    public string SenderName { get; set; }
}
