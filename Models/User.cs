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
        private string profileImagePath;
        private string avatarLevel;
        private bool receiveNotifications; 
        public User() { }

        public User(int id, string name, string email, string password, bool active, bool canShare, bool canComment, bool isAdmin, string profileImagePath = null, string avatarLevel = "BRONZE", bool receiveNotifications = false)
        {
            this.id = id;
            this.name = name;
            this.email = email;
            this.password = password;
            this.active = active;
            this.canShare = canShare;
            this.canComment = canComment;
            this.isAdmin = isAdmin;
            this.profileImagePath = profileImagePath;
            this.avatarLevel = avatarLevel;
            this.receiveNotifications = receiveNotifications;
        }

        public int Id { get => id; set => id = value; }
        public string Name { get => name; set => name = value; }
        public string Email { get => email; set => email = value; }
        public string Password { get => password; set => password = value; }
        public bool Active { get => active; set => active = value; }
        public bool CanShare { get => canShare; set => canShare = value; }
        public bool CanComment { get => canComment; set => canComment = value; }
        public bool IsAdmin { get => isAdmin; set => isAdmin = value; }
        public string ProfileImagePath { get => profileImagePath; set => profileImagePath = value; }
        public string AvatarLevel { get => avatarLevel; set => avatarLevel = value; }

        public bool ReceiveNotifications { get => receiveNotifications; set => receiveNotifications = value; } 
    }
}
