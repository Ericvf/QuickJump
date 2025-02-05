namespace QuickJump
{
    public class Item
    {
        public string Id { get; set; }

        public string Path { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public Types Type { get; set; }

        public enum Types
        {
            Uri,
            File,
        }
    }
}