namespace NewsSite1.Models
{
    public class NewsApiModels
    {
        private List<NewsApiArticle> articles = new List<NewsApiArticle>();

        public NewsApiModels() { }

        public NewsApiModels(List<NewsApiArticle> articles)
        {
            this.articles = articles;
        }

        public List<NewsApiArticle> Articles { get => articles; set => articles = value; }
    }

    public class NewsApiArticle
    {
        private string title = "";
        private string description = "";
        private string content = "";
        private string author = "";
        private NewsApiSource source = new NewsApiSource();
        private string url = "";
        private string urlToImage = "";
        private string publishedAt = "";

        public NewsApiArticle() { }

        public NewsApiArticle(string title, string description, string content, string author,
                              NewsApiSource source, string url, string urlToImage, string publishedAt)
        {
            this.title = title;
            this.description = description;
            this.content = content;
            this.author = author;
            this.source = source;
            this.url = url;
            this.urlToImage = urlToImage;
            this.publishedAt = publishedAt;
        }

        public string Title { get => title; set => title = value; }
        public string Description { get => description; set => description = value; }
        public string Content { get => content; set => content = value; }
        public string Author { get => author; set => author = value; }
        public NewsApiSource Source { get => source; set => source = value; }
        public string Url { get => url; set => url = value; }
        public string UrlToImage { get => urlToImage; set => urlToImage = value; }
        public string PublishedAt { get => publishedAt; set => publishedAt = value; }
    }

    public class NewsApiSource
    {
        private string name = "";

        public NewsApiSource() { }

        public NewsApiSource(string name)
        {
            this.name = name;
        }

        public string Name { get => name; set => name = value; }
    }
}
