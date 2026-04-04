public class TaskItem
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public Shared.Enums.TaskStatus? Status { get; set; }
    public required string Priority { get; set; }

    public int? AssignedTo { get; set; }

    public User? AssignedToUser { get; set; }

    public int ProjectId { get; set; }
    public bool IsDeleted { get; set; }
    public int DeletedBy { get; set; }
    public DateTime DeletedAt { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? DueDate { get; set; }
}