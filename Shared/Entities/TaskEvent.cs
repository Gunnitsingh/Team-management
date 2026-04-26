

namespace Shared.Entities
{
    public class TaskEvent
    {
        public string EventType { get; set; } = default!;
        public int TaskId { get; set; }

        public string? OldValue { get; set; }
        public string? NewValue { get; set; }

        public int? AssignedTo { get; set; }
        public DateTime Timestamp { get; set; }
        public string CorrelationId { get; set; }

        public int ChangedBy { get; set; }
        public string ChangedByName { get; set; } = null!;
        public int Version { get; set; }
    }
}