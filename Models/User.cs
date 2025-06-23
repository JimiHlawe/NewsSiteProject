namespace NewsSite1.Models
{
    public class User
    {
        private int id;
        private string name;
        private string email;
        private string password;
        private bool active;

        public User() { }

        public User(int id, string name, string email, string password, bool active)
        {
            this.id = id;
            this.name = name;
            this.email = email;
            this.password = password;
            this.active = active;
        }

        public int Id { get => id; set => id = value; }
        public string Name { get => name; set => name = value; }
        public string Email { get => email; set => email = value; }
        public string Password { get => password; set => password = value; }
        public bool Active { get => active; set => active = value; }
    }
}
