using Shared.Models.Interfaces;

namespace Shared.Models
{
    public class WordListOverview : ICreatorOwned
    {
        public required Guid Guid { get; set; }
        public required Guid CreatorGuid { get; set; }
        public required Guid WordListGuid { get; set; }
        public required string Title { get; set; }

        public bool IsPublic { get; set; }
        public bool IsHidden { get; set; }
        public bool UserHasUpvoted { get; set; } 
        public int Upvotes { get; set; }
        //public string? CreatorName { get; set; }
        public string? Description { get; set; }
        public string[]? Tags { get; set; }
        public Dictionary<string, string>? WordSample { get; set; }
        public DateTime CreatedDate
        {
            get; set;
        }
        public DateTime LastModifiedDate
        {
            get; set;
        }
    }
}