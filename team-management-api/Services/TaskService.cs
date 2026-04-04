using Microsoft.EntityFrameworkCore;
using Shared.Constants;
using Shared.Entities;

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

        if (originalTitle != dto.Title)
            Publish(StatusEvents.TASK_TITLE_UPDATED, updatedTask.Id, originalTitle ?? "", dto.Title ?? "");

        if (originalDescription != dto.Description)
            Publish(StatusEvents.TASK_DESCRIPTION_UPDATED, updatedTask.Id, originalDescription ?? "", dto.Description ?? "");

        if (originalPriority != dto.Priority)
            Publish(StatusEvents.TASK_PRIORITY_UPDATED, updatedTask.Id, originalPriority.ToString(), dto.Priority.ToString());

        if (originalDueDate != dto.DueDate)
            Publish(StatusEvents.TASK_DUE_DATE_UPDATED, updatedTask.Id, originalDueDate?.ToString() ?? "", dto.DueDate?.ToString() ?? "");

        if (originalAssignedTo != dto.AssignedTo)
            Publish(StatusEvents.TASK_ASSIGNED, updatedTask.Id, originalName?.ToString() ?? "", updatedTask.AssignedToName ?? "");
    }

    public void PublishEvents(string eventType, int taskId, string oldValue = "", string newValue = "")
    {
        Publish(eventType, taskId, oldValue, newValue);
    }

    public int GetCurrentUserId()
    {
        return int.Parse(_httpContext.HttpContext?.Items["UserId"].ToString());
    }

    private void Publish(string eventType, int taskId, string oldValue, string newValue)
    {
        var taskEvent = new TaskEvent
        {
            EventType = eventType,
            TaskId = taskId,
            OldValue = oldValue,
            NewValue = newValue,
            Timestamp = DateTime.UtcNow,
            CorrelationId = _httpContext.HttpContext?.Items["CorrelationId"]?.ToString(),
            ChangedBy = int.Parse(_httpContext.HttpContext?.Items["UserId"].ToString()),
            ChangedByName = _httpContext.HttpContext?.Items["UserName"]?.ToString() ?? "Unknown"
        };
        _publisher.Publish(taskEvent);
    }
}