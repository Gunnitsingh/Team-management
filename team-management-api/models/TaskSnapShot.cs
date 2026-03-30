public class TaskSnapshot
{
    public string Title { get; set; }
    public string Description { get; set; }
    public string Priority { get; set; }
    public DateTime? DueDate { get; set; }
    public int? AssignedTo { get; set; }
    public string? AssignedToName { get; set; }
}