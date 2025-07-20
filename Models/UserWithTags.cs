namespace NewsSite1.Models
{
    public class UserWithTags
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public int Active { get; set; } = 1;
        public List<int> Tags { get; set; }
        public bool IsAdmin { get; set; }
    }

}
