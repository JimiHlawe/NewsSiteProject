namespace NewsSite1.Models.DTOs
{
    public class ReportedArticleDTO
    {
        public string ReporterName { get; set; }
        public string TargetName { get; set; }
        public string ArticleTitle { get; set; }
        public string Reason { get; set; }
        public DateTime ReportedAt { get; set; }
    }
}
