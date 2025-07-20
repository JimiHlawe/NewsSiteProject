namespace NewsSite1.Models
{
    public class User
    {
        private int id;
        private string name;
        private string email;
        private string password;
        private bool active;
        private bool canShare;
        private bool canComment;
        private bool isAdmin;

        public User() { }

        public User(int id, string name, string email, string password, bool active, bool canShare, bool canComment, bool isAdmin)
        {
            this.id = id;
            this.name = name;
            this.email = email;
            this.password = password;
            this.active = active;
            this.canShare = canShare;
            this.canComment = canComment;
            this.isAdmin = isAdmin;
        }

        public int Id { get => id; set => id = value; }
        public string Name { get => name; set => name = value; }
        public string Email { get => email; set => email = value; }
        public string Password { get => password; set => password = value; }
        public bool Active { get => active; set => active = value; }
        public bool CanShare { get => canShare; set => canShare = value; }
        public bool CanComment { get => canComment; set => canComment = value; }
        public bool IsAdmin { get => isAdmin; set => isAdmin = value; } 
    }
}
