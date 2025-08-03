namespace NewsSite1.Models.DTOs.Requests
{
    public class SetStatusRequest
    {
        public int UserId { get; set; }
        public bool IsActive { get; set; }
    }
}
