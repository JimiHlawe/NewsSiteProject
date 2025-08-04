namespace NewsSite1.Models
{
    public class UserWithTags
    {
        private int id;
        private string name;
        private string email;
        private string password;
        private int active = 1;
        private List<int> tags = new List<int>();
        private bool isAdmin;

        public UserWithTags() { }

        public UserWithTags(int id, string name, string email, string password, int active, List<int> tags, bool isAdmin)
        {
            this.id = id;
            this.name = name;
            this.email = email;
            this.password = password;
            this.active = active;
            this.tags = tags;
            this.isAdmin = isAdmin;
        }

        public int Id { get => id; set => id = value; }
        public string Name { get => name; set => name = value; }
        public string Email { get => email; set => email = value; }
        public string Password { get => password; set => password = value; }
        public int Active { get => active; set => active = value; }
        public List<int> Tags { get => tags; set => tags = value; }
        public bool IsAdmin { get => isAdmin; set => isAdmin = value; }
    }
}
