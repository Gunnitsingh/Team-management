using Shared.Entities;

public interface ITaskService
{
    IQueryable<TaskDto> GetTaskDtoQuery();
    IQueryable<TaskActivity> GetTaskActivities(int taskId);
    void PublishUpdateEvents(TaskSnapshot task, UpdateTaskDto dto, TaskDto updatedTask);

    void PublishEvents(string eventType, int taskId, string oldValue, string newValue);
}