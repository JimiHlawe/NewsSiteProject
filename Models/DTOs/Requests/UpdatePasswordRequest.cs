namespace NewsSite1.Models.DTOs.Requests
{
    public class UpdatePasswordRequest
    {
        public int UserId { get; set; }
        public string NewPassword { get; set; }
    }
}
