namespace NewsSite1.Models.DTOs.Requests
{
    public class SharingStatusRequest
    {
        public int UserId { get; set; }
        public bool CanShare { get; set; }
        public bool CanComment { get; set; }
    }
}
