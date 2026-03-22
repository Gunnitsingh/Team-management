public class UpdateTaskDto
{
    public string Title { get; set; }
    public string Description { get; set; }
    public string Priority { get; set; }
    public int? AssignedTo { get; set; }
    public DateTime? DueDate { get; set; }
}