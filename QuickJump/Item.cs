namespace QuickJump
{
    public class Item
    {
        public required string Id { get; set; }

        public string? Path { get; set; }

        public required string Name { get; set; }

        public string? Description { get; set; }

        public Types? Type { get; set; }

        public Categories? Category { get; set; }

        public string? Provider { get; set; }

        public string? Icon { get; set; }
    }

    public enum Types
    {
        Uri,
        File,
        ProcessId
    }

    public enum Categories
    {
        Azure,
        AzureDevOps,
        Solution,
        ProcessWindow,
        Bookmark
    }
}