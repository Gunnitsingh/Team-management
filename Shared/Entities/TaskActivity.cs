namespace Shared.Entities
{
    public class TaskActivity
    {
        public int Id { get; set; }
        public int TaskId { get; set; }
        public string EventType { get; set; } = null!;
        public string Description { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}