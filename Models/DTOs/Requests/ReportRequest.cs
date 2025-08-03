namespace NewsSite1.Models.DTOs.Requests
{
    public class ReportRequest
    {
        public int UserId { get; set; }
        public string ReferenceType { get; set; } = "";
        public int ReferenceId { get; set; }
        public string Reason { get; set; } = "";
    }
}
