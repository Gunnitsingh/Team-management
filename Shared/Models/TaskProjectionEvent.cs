namespace Shared.Models
{
    public class TaskProjectionEvent
    {
        public string EventType { get; set; } = default!;
        public Models.TaskReadDto? Task { get; set; }
        public int TaskId { get; set; }
        public bool IsDeleted { get; set; }
    }
}
