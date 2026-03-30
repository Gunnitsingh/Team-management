public interface ITaskService
{
    IQueryable<TaskDto> GetTaskDtoQuery();
    void PublishUpdateEvents(TaskSnapshot task, UpdateTaskDto dto, TaskDto updatedTask);

    void PublishEvents(string eventType, int taskId, string oldValue, string newValue);
}