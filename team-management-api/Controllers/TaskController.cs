using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
public class TasksController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IMessagePublisher _publisher;

    public TasksController(AppDbContext context, IMessagePublisher publisher)
    {
        _context = context;
        _publisher = publisher;
    }

    [HttpGet]
    public async Task<IActionResult> GetTasks()
    {
        var tasks = await GetTaskDtoQuery().ToListAsync();
        return Ok(tasks);
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
            Status = TaskStatus.BACKLOG,
            CreatedDate = DateTime.UtcNow
        };

        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        _publisher.Publish(new TaskEvent
        {
            EventType = "TASK_CREATED",
            TaskId = task.Id,
            Status = task.Status.ToString(),
            AssignedTo = task.AssignedTo,
            Timestamp = DateTime.UtcNow
        });
        var createdTask = await GetTaskDtoQuery()
        .FirstOrDefaultAsync(t => t.Id == task.Id);

        return Ok(createdTask);
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateTaskStatus(int id, UpdateTaskStatusDto dto)
    {
        var task = await _context.Tasks.FindAsync(id);

        if (task == null)
            return NotFound();

        task.Status = dto.Status;

        await _context.SaveChangesAsync();

        _publisher.Publish(new TaskEvent
        {
            EventType = "TASK_STATUS_UPDATED",
            TaskId = task.Id,
            Status = task.Status.ToString(),
            AssignedTo = task.AssignedTo,
            Timestamp = DateTime.UtcNow
        });

        var updatedTask = await GetTaskDtoQuery()
       .FirstOrDefaultAsync(t => t.Id == id);

        return Ok(updatedTask);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTask(int id, [FromBody] UpdateTaskDto dto)
    {
        var task = await _context.Tasks.FindAsync(id);

        if (task == null)
            return NotFound();

        // Update fields
        task.Title = dto.Title;
        task.Description = dto.Description;
        task.Priority = dto.Priority;
        task.AssignedTo = dto.AssignedTo;
        task.DueDate = dto.DueDate;

        await _context.SaveChangesAsync();


        _publisher.Publish(new TaskEvent
        {
            EventType = "TASK_UPDATED",
            TaskId = task.Id,
            Status = task.Status.ToString(),
            AssignedTo = task.AssignedTo,
            Timestamp = DateTime.UtcNow
        });

        // Return updated DTO (important)
        var updatedTask = await GetTaskDtoQuery()
            .FirstOrDefaultAsync(t => t.Id == id);

        return Ok(updatedTask);
    }
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTask(int id)
    {
        var task = await _context.Tasks.FindAsync(id);

        if (task == null)
            return NotFound();

        _context.Tasks.Remove(task);
        await _context.SaveChangesAsync();

        _publisher.Publish(new TaskEvent
        {
            EventType = "TASK_DELETED",
            TaskId = task.Id,
            Timestamp = DateTime.UtcNow
        });

        return NoContent();
    }


    private IQueryable<TaskDto> GetTaskDtoQuery()
    {
        return _context.Tasks
            .Select(t => new TaskDto
            {
                Id = t.Id,
                Title = t.Title,
                Status = t.Status,
                Priority = t.Priority,
                AssignedToId = t.AssignedTo,
                AssignedToName = t.AssignedToUser != null ? t.AssignedToUser.Name : null,
                CreatedDate = t.CreatedDate,
                Description = t.Description,
                DueDate = t.DueDate
            });
    }
}