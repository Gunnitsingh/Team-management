namespace Shared.Entities
{
    public class Notification
    {
        public int Id { get; set; }
        public string UserId { get; set; } = default!;
        public string Title { get; set; } = default!;
        public string Message { get; set; } = default!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}