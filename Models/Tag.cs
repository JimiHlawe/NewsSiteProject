namespace NewsSite1.Models
{
    public class Tag
    {
        private int id;
        private string name;

        public Tag() { }

        public Tag(int id, string name)
        {
            this.id = id;
            this.name = name;
        }

        public int Id { get => id; set => id = value; }
        public string Name { get => name; set => name = value; }
    }
}
