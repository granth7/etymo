namespace Shared.Models
{
    public class WordList
    {
        public required Guid Guid { get; set; }
        public required Dictionary<string, string> Words { get; set; } // List of key-value pairs (words and their definitions)
    }
}