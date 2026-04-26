public class TaskDto
{
    public int Id { get; set; }
    public string Title { get; set; } = default!;
    public Shared.Enums.TaskStatus? Status { get; set; }
    public string Priority { get; set; } = default!;

    public string Description { get; set; } = default!;
    public int? AssignedToId { get; set; }
    public string? AssignedToName { get; set; }

    public DateTime CreatedDate { get; set; }
    public DateTime? DueDate { get; set; }
    public int Version { get; set; }
}