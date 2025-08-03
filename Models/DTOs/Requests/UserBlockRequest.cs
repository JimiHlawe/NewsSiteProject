namespace NewsSite1.Models.DTOs.Requests
{
    public class UserBlockRequest
    {
        public int BlockerUserId { get; set; }
        public int BlockedUserId { get; set; }
    }

}
