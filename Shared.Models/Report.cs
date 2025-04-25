using System.ComponentModel.DataAnnotations;

namespace Shared.Models
{
    public class Report
    {
        public int Id { get; set; }

        public Guid ReportedContentId { get; set; }

        public required string ReporterUserId { get; set; }

        public required string Reason { get; set; }

        public required string Details { get; set; }

        public string? ContentTitle { get; set; }

        public string? ContentDescription { get; set; }

        public string[]? ContentTags { get; set; }

        public Guid? ReportedUserId { get; set; }

        public Guid? ContentWordsId { get; set; }

        public string Status { get; set; } = "pending";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ResolvedAt { get; set; }

        public string? ResolverUserId { get; set; }
    }
}