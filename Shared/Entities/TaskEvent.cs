

namespace Shared.Entities
{
   public class TaskEvent
{
    public string EventType { get; set; } = default!;
    public int TaskId { get; set; }

    public string?  OldValue { get; set; } 
    public string? NewValue { get; set; } 

    public int? AssignedTo { get; set; }
    public DateTime Timestamp { get; set; }
} 
}