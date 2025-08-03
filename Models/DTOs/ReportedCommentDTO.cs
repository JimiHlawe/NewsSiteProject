namespace NewsSite1.Models.DTOs
{
    public class ReportedCommentDTO
    {
        public string ReporterName { get; set; }
        public string TargetName { get; set; }
        public string CommentText { get; set; }
        public string Reason { get; set; }
        public DateTime ReportedAt { get; set; }
    }
}
