using Microsoft.EntityFrameworkCore;
using Shared.Constants;
using Shared.Entities;
using Shared.Models;

public class TaskService : ITaskService
{
    private readonly AppDbContext _context;
    private readonly IMessagePublisher _publisher;

    private readonly IHttpContextAccessor _httpContext;
    public TaskService(AppDbContext context, IMessagePublisher publisher, IHttpContextAccessor httpContext)
    {
        _context = context;
        _publisher = publisher;
        _httpContext = httpContext;
    }

    public IQueryable<TaskDto> GetTaskDtoQuery()
    {
        return _context.Tasks
            .Include(t => t.AssignedToUser)
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
                DueDate = t.DueDate,
                Version = t.Version
            });
    }

    public IQueryable<TaskReadDto> GetTaskReadQuery()
    {
        return _context.TaskReadModels
            .OrderByDescending(t => t.CreatedDate)
            .Select(t => new TaskReadDto
            {
                Id = t.Id,
                Title = t.Title,
                Status = t.Status,
                Priority = t.Priority,
                AssignedToId = t.AssignedToId,
                AssignedToName = t.AssignedToName,
                CreatedDate = t.CreatedDate,
                Description = t.Description,
                DueDate = t.DueDate,
                Version = t.Version
            });
    }

    public IQueryable<TaskActivity> GetTaskActivities(int taskId)
    {
        return _context.TaskActivities
            .Where(t => t.TaskId == taskId);

    }


    public void PublishUpdateEvents(TaskSnapshot task, UpdateTaskDto dto, TaskDto updatedTask, int version)
    {
        var originalAssignedTo = task.AssignedTo;
        var originalTitle = task.Title;
        var originalDescription = task.Description;
        var originalPriority = task.Priority;
        var originalName = task.AssignedToName;
        var originalDueDate = task.DueDate;
        var events = new List<TaskEvent>();

        if (originalTitle != dto.Title)
        {
            events.Add(CreateEvent(StatusEvents.TASK_TITLE_UPDATED, updatedTask.Id, originalTitle ?? "", dto.Title ?? "", version));
        }

        if (originalDescription != dto.Description)
        {
            events.Add(CreateEvent(StatusEvents.TASK_DESCRIPTION_UPDATED, updatedTask.Id, originalDescription ?? "", dto.Description ?? "", version));
        }

        if (originalPriority != dto.Priority)
        {
            events.Add(CreateEvent(StatusEvents.TASK_PRIORITY_UPDATED, updatedTask.Id, originalPriority.ToString(), dto.Priority.ToString(), version));
        }

        if (originalDueDate != dto.DueDate)
        {
            events.Add(CreateEvent(StatusEvents.TASK_DUE_DATE_UPDATED, updatedTask.Id, originalDueDate?.ToString() ?? "", dto.DueDate?.ToString() ?? "", version));
        }

        if (originalAssignedTo != dto.AssignedTo)
        {
            events.Add(CreateEvent(StatusEvents.TASK_ASSIGNED, updatedTask.Id, originalName?.ToString() ?? "", updatedTask.AssignedToName ?? "", version));
        }

        events.ForEach(e => Publish(e));
    }

    public void PublishEvents(string eventType, int taskId, string oldValue = "", string newValue = "", int version = 1)
    {
        Publish(CreateEvent(eventType, taskId, oldValue, newValue, version));
    }

    public int GetCurrentUserId()
    {
        return int.Parse((_httpContext.HttpContext?.Items["UserId"]?.ToString()) ?? "0");
    }

    private TaskEvent CreateEvent(string eventType, int taskId, string oldValue, string newValue, int version = 1)
    {
        return new TaskEvent
        {
            EventType = eventType,
            TaskId = taskId,
            OldValue = oldValue,
            NewValue = newValue,
            Version = version,
            Timestamp = DateTime.UtcNow,
            CorrelationId = _httpContext.HttpContext?.Items["CorrelationId"]?.ToString() ?? "",
            ChangedBy = int.Parse((_httpContext.HttpContext?.Items["UserId"]?.ToString()) ?? "0"),
            ChangedByName = _httpContext.HttpContext?.Items["UserName"]?.ToString() ?? "Unknown"
        };
    }

    private void Publish(TaskEvent taskEvent)
    {
        _publisher.Publish(taskEvent);
    }
}
