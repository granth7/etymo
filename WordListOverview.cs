namespace Shared.Models
{
    public class WordListOverview
    {
        public required string Guid { get; set; }
        public required string CreatedByUserGuid { get; set; }
        public int WordListId { get; set; }
        public bool IsPublic { get; set; }
        public int Upvotes { get; set; }
        public required string Title { get; set; }
        public string? Description { get; set; }
        public string[]? Tags { get; set; }
        public KeyValuePair<string, string>[]? WordSample { get; set; }
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