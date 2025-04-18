namespace Shared.Models
{
    public class UserUpvote
    {
        public int Id { get; set; }
        public Guid UserGuid { get; set; }
        public Guid WordListOverviewGuid { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}