using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using Shared.Constants;
using Shared.Entities;

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
            Status = Shared.Enums.TaskStatus.BACKLOG,
            CreatedDate = DateTime.UtcNow
        };

        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        PublishEvent(StatusEvents.TASK_CREATED, task.Id, "", "Created");


        return Ok(task);
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

        PublishEvent(StatusEvents.TASK_STATUS_UPDATED, task.Id, oldStatus.ToString(), dto.Status.ToString());


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

        // Capture original values for change detection
        var originalAssignedTo = task.AssignedTo;
        var originalTitle = task.Title;
        var originalDescription = task.Description;
        var originalPriority = task.Priority;
        var originalName = task.AssignedToUser;
        var originalDueDate = task.DueDate;

        // Update fields
        task.Title = dto.Title;
        task.Description = dto.Description;
        task.Priority = dto.Priority;
        task.AssignedTo = dto.AssignedTo;
        task.DueDate = dto.DueDate;

        await _context.SaveChangesAsync();

        // Publish events for changed fields


        if (originalTitle != dto.Title)
            PublishEvent(StatusEvents.TASK_TITLE_UPDATED, task.Id, originalTitle, dto.Title);

        if (originalDescription != dto.Description)
            PublishEvent(StatusEvents.TASK_DESCRIPTION_UPDATED, task.Id, originalDescription, dto.Description);

        if (originalPriority != dto.Priority)
            PublishEvent(StatusEvents.TASK_PRIORITY_UPDATED, task.Id, originalPriority.ToString(), dto.Priority.ToString());

        if (originalDueDate != dto.DueDate)
            PublishEvent(StatusEvents.TASK_DUE_DATE_UPDATED, task.Id, originalDueDate?.ToString() ?? null, dto.DueDate?.ToString() ?? null);

        // Return updated DTO
        var updatedTask = await GetTaskDtoQuery()
            .FirstOrDefaultAsync(t => t.Id == id);

        if (originalAssignedTo != dto.AssignedTo)
            PublishEvent(StatusEvents.TASK_ASSIGNED, task.Id, originalName.ToString(), updatedTask.AssignedToName);

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

        PublishEvent(StatusEvents.TASK_DELETED, task.Id, "", "Deleted");

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

    private void PublishEvent(string eventType, int taskId, string oldValue, string newValue)
    {
        _publisher.Publish(new TaskEvent
        {
            EventType = eventType,
            TaskId = taskId,
            OldValue = oldValue ?? null,
            NewValue = newValue,
            Timestamp = DateTime.UtcNow
        });
    }
}