﻿namespace Shared.Models
{
    public class WordListOverview
    {
        public required Guid Guid { get; set; }
        public required Guid CreatedByUserGuid { get; set; }
        public required Guid WordListGuid { get; set; }
        public required string Title { get; set; }

        public bool IsPublic { get; set; }
        public int Upvotes { get; set; }
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