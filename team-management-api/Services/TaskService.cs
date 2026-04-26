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
                DueDate = t.DueDate
            });
    }

    public IQueryable<TaskActivity> GetTaskActivities(int taskId)
    {
        return _context.TaskActivities
            .Where(t => t.TaskId == taskId);

    }


    public void PublishUpdateEvents(TaskSnapshot task, UpdateTaskDto dto, TaskDto updatedTask)
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
            events.Add(CreateEvent(StatusEvents.TASK_TITLE_UPDATED, updatedTask.Id, originalTitle ?? "", dto.Title ?? ""));
        }

        if (originalDescription != dto.Description)
        {
            events.Add(CreateEvent(StatusEvents.TASK_DESCRIPTION_UPDATED, updatedTask.Id, originalDescription ?? "", dto.Description ?? ""));
        }

        if (originalPriority != dto.Priority)
        {
            events.Add(CreateEvent(StatusEvents.TASK_PRIORITY_UPDATED, updatedTask.Id, originalPriority.ToString(), dto.Priority.ToString()));
        }

        if (originalDueDate != dto.DueDate)
        {
            events.Add(CreateEvent(StatusEvents.TASK_DUE_DATE_UPDATED, updatedTask.Id, originalDueDate?.ToString() ?? "", dto.DueDate?.ToString() ?? ""));
        }

        if (originalAssignedTo != dto.AssignedTo)
        {
            events.Add(CreateEvent(StatusEvents.TASK_ASSIGNED, updatedTask.Id, originalName?.ToString() ?? "", updatedTask.AssignedToName ?? ""));
        }

        events.ForEach(e => Publish(e));
    }

    public void PublishEvents(string eventType, int taskId, string oldValue = "", string newValue = "")
    {
        Publish(CreateEvent(eventType, taskId, oldValue, newValue));
    }

    public int GetCurrentUserId()
    {
        return int.Parse(_httpContext.HttpContext?.Items["UserId"].ToString() ?? 0.ToString());
    }

    private TaskEvent CreateEvent(string eventType, int taskId, string oldValue, string newValue)
    {
        return new TaskEvent
        {
            EventType = eventType,
            TaskId = taskId,
            OldValue = oldValue,
            NewValue = newValue,
            Timestamp = DateTime.UtcNow,
            CorrelationId = _httpContext.HttpContext?.Items["CorrelationId"]?.ToString() ?? "",
            ChangedBy = int.Parse(_httpContext.HttpContext?.Items["UserId"].ToString() ?? 0.ToString()),
            ChangedByName = _httpContext.HttpContext?.Items["UserName"]?.ToString() ?? "Unknown"
        };
    }

    private void Publish(TaskEvent taskEvent)
    {
        _publisher.Publish(taskEvent);
    }
}
