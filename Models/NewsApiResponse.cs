public class NewsApiResponse
{
    public List<NewsApiArticle> Articles { get; set; }
}

public class NewsApiArticle
{
    public string Title { get; set; }
    public string Description { get; set; }
    public string Content { get; set; }
    public string Author { get; set; }
    public NewsApiSource Source { get; set; }
    public string Url { get; set; }
    public string UrlToImage { get; set; }
    public string PublishedAt { get; set; }
}

public class NewsApiSource
{
    public string Name { get; set; }
}
