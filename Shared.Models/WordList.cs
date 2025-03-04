using Shared.Models.Interfaces;

namespace Shared.Models
{
    public class WordList : ICreatorOwned
    {
        public required Guid Guid { get; set; }
        public required Guid CreatorGuid { get; set; }
        public required Dictionary<string, string> Words { get; set; } // List of key-value pairs (words and their definitions)
    }
}