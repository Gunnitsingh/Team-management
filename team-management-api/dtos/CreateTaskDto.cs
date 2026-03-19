public class CreateTaskDto
{
    public required string Title { get; set; }
    public required string Description { get; set; }
    public required string Priority { get; set; }
    public int ProjectId { get; set; }
    public  int? AssignedTo { get; set; }
    public DateTime? DueDate { get; set; }
}