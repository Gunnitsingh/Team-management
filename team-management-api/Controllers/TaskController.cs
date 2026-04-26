using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

[ApiController]
[Route("api/[controller]")]
public class TasksController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IMessagePublisher _publisher;
    private readonly ITaskService _taskService;
    private readonly ConcurrencySaveHelper _concurrencyHelper;
    public TasksController(AppDbContext context, IMessagePublisher publisher, ITaskService taskService, ConcurrencySaveHelper concurrencyHelper)
    {
        _context = context;
        _publisher = publisher;
        _taskService = taskService;
        _concurrencyHelper = concurrencyHelper;
    }

    [HttpGet]
    public async Task<IActionResult> GetTasks()
    {
        var tasks = await _taskService.GetTaskReadQuery().ToListAsync();
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
            Version = 1,
            AssignedTo = dto.AssignedTo,
            DueDate = dto.DueDate,
            Status = Shared.Enums.TaskStatus.BACKLOG,
            CreatedDate = DateTime.UtcNow
        };

        _context.Tasks.Add(task);
        await _concurrencyHelper.SaveWithConcurrencyRetry(async () =>
 {


     await _context.SaveChangesAsync();
 });

        _taskService.PublishEvents(StatusEvents.TASK_CREATED, task.Id, "", "Created", task.Version);
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


        await _concurrencyHelper.SaveWithConcurrencyRetry(async () =>
{
    task.Status = dto.Status;
    task.Version += 1;

    await _context.SaveChangesAsync();
});

        _taskService.PublishEvents(StatusEvents.TASK_STATUS_UPDATED, task.Id, oldStatus.ToString(), dto.Status.ToString(), task.Version);

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


        await _concurrencyHelper.SaveWithConcurrencyRetry(async () =>
{
    task.Title = dto.Title;
    task.Description = dto.Description;
    task.Priority = dto.Priority;
    task.AssignedTo = dto.AssignedTo;
    task.DueDate = dto.DueDate;
    task.Version += 1;

    await _context.SaveChangesAsync();
});
        // Return updated DTO
        var updatedTask = await _taskService.GetTaskDtoQuery()
            .FirstAsync(t => t.Id == id);

        _taskService.PublishUpdateEvents(originalTask, dto, updatedTask, updatedTask.Version);
        return Ok(updatedTask);
    }
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTask(int id)
    {
        var task = await _context.Tasks.FindAsync(id);

        if (task == null)
            return NotFound();


        await _concurrencyHelper.SaveWithConcurrencyRetry(async () =>
{
    task.IsDeleted = true;
    task.DeletedBy = _taskService.GetCurrentUserId();
    task.DeletedAt = DateTime.UtcNow;
    task.Version += 1;

    await _context.SaveChangesAsync();
});
        _taskService.PublishEvents(StatusEvents.TASK_DELETED, task.Id, task.DeletedAt.ToString(), task.DeletedBy.ToString(), task.Version);

        return NoContent();
    }

}
