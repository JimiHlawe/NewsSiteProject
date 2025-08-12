namespace NewsSite1.Models.DTOs.Requests
{
    public class DeleteReportedTargetRequest
    {
        // "Article" | "Comment" | "PublicComment"
        public string TargetKind { get; set; } = "";
        public int TargetId { get; set; }
    }
}
