using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

public class Worker : BackgroundService
{
    private IConnection _connection;
    private IModel _channel;

    public Worker()
    {
        var factory = new ConnectionFactory()
        {
            HostName = "localhost"
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        _channel.QueueDeclare(
            queue: "task-events",
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null
        );
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumer = new EventingBasicConsumer(_channel);

        consumer.Received += (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                var taskEvent = JsonSerializer.Deserialize<TaskEvent>(message);

                HandleEvent(taskEvent);

                _channel.BasicAck(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing message: {ex.Message}");
            }
        };

        _channel.BasicConsume(
            queue: "task-events",
            autoAck: false,
            consumer: consumer
        );

        return Task.CompletedTask;
    }

    private void HandleEvent(TaskEvent? taskEvent)
    {
        if (taskEvent == null) return;

        switch (taskEvent.EventType)
        {
            case "TASK_CREATED":
                Console.WriteLine($"[Worker] Task Created → ID: {taskEvent.TaskId}");
                break;

            case "TASK_UPDATED":
                Console.WriteLine($"[Worker] Task Updated → ID: {taskEvent.TaskId}, Status: {taskEvent.Status}");
                break;

            case "TASK_DELETED":
                Console.WriteLine($"[Worker] Task Deleted → ID: {taskEvent.TaskId}");
                break;

            default:
                Console.WriteLine($"[Worker] Unknown event: {taskEvent.EventType}");
                break;
        }
    }
}