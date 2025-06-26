namespace NewsSite1.Models
{
    public class ArticleWithTags
    {
        private int id;
        private string title = "";
        private string description = "";
        private string imageUrl = "";
        private string sourceUrl = "";
        private List<string> tags = new List<string>();

        public ArticleWithTags() { }

        public ArticleWithTags(int id, string title, string description, string imageUrl, string sourceUrl, List<string> tags)
        {
            this.id = id;
            this.title = title;
            this.description = description;
            this.imageUrl = imageUrl;
            this.sourceUrl = sourceUrl;
            this.tags = tags;
        }

        public int Id { get => id; set => id = value; }
        public string Title { get => title; set => title = value; }
        public string Description { get => description; set => description = value; }
        public string ImageUrl { get => imageUrl; set => imageUrl = value; }
        public string SourceUrl { get => sourceUrl; set => sourceUrl = value; }
        public List<string> Tags { get => tags; set => tags = value; }
    }
}
