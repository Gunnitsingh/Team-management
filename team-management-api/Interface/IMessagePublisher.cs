using Shared.Entities;

public interface IMessagePublisher
{
    void Publish(TaskEvent taskEvent);
}