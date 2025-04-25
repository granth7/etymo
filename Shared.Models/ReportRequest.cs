using System.ComponentModel.DataAnnotations;

namespace Shared.Models
{
    public class ReportRequest
    {
        public required string ContentId { get; set; }
        [MaxLength(255)]
        public required string Reason { get; set; }

        public required string Details { get; set; }
    }
}