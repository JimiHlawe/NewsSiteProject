namespace NewsSite1.Models.DTOs
{
    public class ReportEntryDTO
    {
        public int Id { get; set; }
        public int ReporterId { get; set; }
        public string ReporterName { get; set; }
        public string ReportType { get; set; }   // "Article" או "Comment"
        public int ReferenceId { get; set; }
        public string Reason { get; set; }
        public DateTime ReportedAt { get; set; }
        public string Content { get; set; }        // ReportedContent
        public string TargetName { get; set; }     // Name of the user who wrote the reported item
        public string ArticleKind { get; set; }    // "Thread" או "Article"
    }
}
