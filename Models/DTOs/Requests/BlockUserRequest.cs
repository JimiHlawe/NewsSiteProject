namespace NewsSite1.Models.DTOs.Requests
{
    public class BlockUserRequest
    {
        public int BlockerUserId { get; set; }
        public string BlockedUsername { get; set; }
    }
}
