namespace NewsSite1.Models
{
    public class UserWithTags : User
    {
        public List<int> Tags { get; set; }
    }
}
