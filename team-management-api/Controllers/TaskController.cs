using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
public class TasksController : ControllerBase
{
    private readonly AppDbContext _context;

    public TasksController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
public async Task<IActionResult> GetTasks()
{
    var tasks = await _context.Tasks
        .Select(t => new TaskDto
        {
            Id = t.Id,
            Title = t.Title,
            Status = t.Status,
            Priority = t.Priority,
            AssignedToId = t.AssignedTo,
            AssignedToName = t.AssignedToUser != null ? t.AssignedToUser.Name : null,
            CreatedDate = t.CreatedDate,
            Description = t.Description ?? string.Empty
        })
        .ToListAsync();

    return Ok(tasks);
}

    [HttpPost]
    public async Task<IActionResult> CreateTask(CreateTaskDto dto)
    {
        var task = new TaskItem
        {
            Title = dto.Title,
            Description = dto.Description,
            Priority = dto.Priority,
            ProjectId = dto.ProjectId,
            AssignedTo = dto.AssignedTo,
            DueDate = dto.DueDate,
            Status = "BACKLOG",
            CreatedDate = DateTime.UtcNow
        };

        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        return Ok(task);
    }

    [HttpPut("{id}/status")]
public async Task<IActionResult> UpdateTaskStatus(int id, UpdateTaskStatusDto dto)
{
    var task = await _context.Tasks.FindAsync(id);

    if (task == null)
        return NotFound();

if (!IsValidStatus(dto.Status))
{
    return BadRequest("Invalid status");
}

    task.Status = dto.Status;

    await _context.SaveChangesAsync();

    return Ok(task);
}

private bool IsValidStatus(string status)
{
    var allowedStatuses = new[] { "BACKLOG", "TODO", "IN_PROGRESS", "REVIEW", "DONE" };
    return allowedStatuses.Contains(status);    
}

}