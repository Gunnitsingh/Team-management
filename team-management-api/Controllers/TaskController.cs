using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;
using Shared.Entities;

[ApiController]
[Route("api/[controller]")]
public class TasksController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IMessagePublisher _publisher;
    private readonly ITaskService _taskService;

    public TasksController(AppDbContext context, IMessagePublisher publisher, ITaskService taskService)
    {
        _context = context;
        _publisher = publisher;
        _taskService = taskService;
    }

    [HttpGet]
    public async Task<IActionResult> GetTasks()
    {
        var tasks = await _taskService.GetTaskDtoQuery().ToListAsync();
        return Ok(tasks);
    }

    [HttpGet("{id}/activities")]
    public async Task<IActionResult> GetTasksActivities(int id)
    {
        var tasks = await _taskService.GetTaskActivities(id).ToListAsync();
        var groupedLogs = tasks
    .GroupBy(x => x.CorrelationId)
    .Select(g => new
    {
        CorrelationId = g.Key,
        ChangedByName = g.First().ChangedByName,
        Timestamp = g.First().CreatedAt,
        Changes = g.Select(x => new
        {
            x.Description,
            x.EventType
        })
    })
    .OrderByDescending(x => x.Timestamp)
    .ToList();
        return Ok(groupedLogs);
    }

    [HttpPost]
    public async Task<IActionResult> CreateTask([FromBody] CreateTaskDto dto)
    {
        var task = new TaskItem
        {
            Title = dto.Title,
            Description = dto.Description,
            Priority = dto.Priority,
            ProjectId = dto.ProjectId,
            AssignedTo = dto.AssignedTo,
            DueDate = dto.DueDate,
            Status = Shared.Enums.TaskStatus.BACKLOG,
            CreatedDate = DateTime.UtcNow
        };

        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        _taskService.PublishEvents(StatusEvents.TASK_CREATED, task.Id, "", "Created");
        var updatedTask = await _taskService.GetTaskDtoQuery()
            .FirstOrDefaultAsync(t => t.Id == task.Id);

        return Ok(updatedTask);
    }
    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateTaskStatus(int id, UpdateTaskStatusDto dto)
    {
        var task = await _context.Tasks.FindAsync(id);
        var oldStatus = task?.Status.ToString() ?? Shared.Enums.TaskStatus.BACKLOG.ToString();

        if (task == null)
            return NotFound();

        task.Status = dto.Status;

        await _context.SaveChangesAsync();

        _taskService.PublishEvents(StatusEvents.TASK_STATUS_UPDATED, task.Id, oldStatus.ToString(), dto.Status.ToString());

        var updatedTask = await _taskService.GetTaskDtoQuery()
       .FirstOrDefaultAsync(t => t.Id == id);


        return Ok(updatedTask);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTask(int id, [FromBody] UpdateTaskDto dto)
    {
        var task = await _context.Tasks.FindAsync(id);

        if (task == null)
            return NotFound();

        TaskSnapshot originalTask = new TaskSnapshot
        {
            Title = task.Title,
            Description = task.Description,
            Priority = task.Priority,
            DueDate = task.DueDate,
            AssignedTo = task.AssignedTo,
            AssignedToName = task.AssignedToUser?.Name
        };
        // Update fields
        task.Title = dto.Title;
        task.Description = dto.Description;
        task.Priority = dto.Priority;
        task.AssignedTo = dto.AssignedTo;
        task.DueDate = dto.DueDate;

        await _context.SaveChangesAsync();
        // Return updated DTO
        var updatedTask = await _taskService.GetTaskDtoQuery()
            .FirstAsync(t => t.Id == id);

        _taskService.PublishUpdateEvents(originalTask, dto, updatedTask);
        return Ok(updatedTask);
    }
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTask(int id)
    {
        var task = await _context.Tasks.FindAsync(id);

        if (task == null)
            return NotFound();

        task.IsDeleted = true;
        task.DeletedBy = _taskService.GetCurrentUserId();
        task.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        _taskService.PublishEvents(StatusEvents.TASK_DELETED, task.Id, task.DeletedAt.ToString(), task.DeletedBy.ToString());

        return NoContent();
    }

}