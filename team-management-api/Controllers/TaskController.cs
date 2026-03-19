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
    [HttpGet]
public async Task<IActionResult> GetTasks()
{
    var tasks = await _context.Tasks
        .Include(t => t.AssignedToUser) // navigation property
        .Select(t => new TaskDto
        {
            Id = t.Id,
            Title = t.Title,
            Status = t.Status,
            Priority = t.Priority,
            AssignedToId = t.AssignedTo,
            AssignedToName = t.AssignedToUser != null ? t.AssignedToUser.Name : null,
            CreatedDate = t.CreatedDate
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
}