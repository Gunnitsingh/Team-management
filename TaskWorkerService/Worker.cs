using System.Diagnostics.Eventing.Reader;
using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Constants;
using Shared.Entities;

public class Worker : BackgroundService
{
    private IConnection _connection;
    private IModel _channel;
    private readonly IServiceProvider _serviceProvider;

    private readonly IServiceScopeFactory _scopeFactory;

    public Worker(IServiceScopeFactory scopeFactory, IServiceProvider serviceProvider)
    {
        _scopeFactory = scopeFactory;
        _serviceProvider = serviceProvider;

        var factory = new ConnectionFactory()
        {
            HostName = "localhost"
        };
        var args = new Dictionary<string, object>
{
    { "x-dead-letter-exchange", "" },
    { "x-dead-letter-routing-key", "task-events-retry" }
};

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        _channel.QueueDeclare(
            queue: MqEvents.TASK_EVENTS,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: args
        );
        var retryArgs = new Dictionary<string, object>
{
    { "x-dead-letter-exchange", "" },
    { "x-dead-letter-routing-key", "task-events" },
    { "x-message-ttl", 5000 } // 5 sec delay
};

        _channel.QueueDeclare(
            queue: MqEvents.TASK_EVENTS_RETRY,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: retryArgs
        );
        _channel.QueueDeclare(
            queue: MqEvents.TASK_EVENTS_DLQ,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null
        );

    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumer = new EventingBasicConsumer(_channel);

        consumer.Received += async (model, ea) =>
        {
            int retryCount = 0;

            // 🧠 READ retry header
            if (ea.BasicProperties.Headers != null &&
                ea.BasicProperties.Headers.ContainsKey("x-retry"))
            {
                var value = ea.BasicProperties.Headers["x-retry"];
                retryCount = int.Parse(Encoding.UTF8.GetString((byte[])value));
            }

            Console.WriteLine($"🔁 Retry Count: {retryCount}");

            try
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                if (string.IsNullOrWhiteSpace(message))
                {
                    Console.WriteLine("⚠️ Empty message, skipping");
                    _channel.BasicAck(ea.DeliveryTag, false);
                    return;
                }

                var taskEvent = JsonSerializer.Deserialize<TaskEvent>(message);

                if (taskEvent == null)
                {
                    Console.WriteLine("❌ Null event, skipping");
                    _channel.BasicAck(ea.DeliveryTag, false);
                    return;
                }

                // 🧪 Force failure for testing (remove later)
                // throw new Exception("Test retry");

               await HandleEvent(taskEvent);

                // ✅ Success → ACK
                _channel.BasicAck(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");

                var body = ea.Body;

                // 🛑 MAX RETRY CHECK
                if (retryCount >= 3)
                {
                    Console.WriteLine("🚨 Max retries reached → Sending to DLQ");

                    _channel.BasicPublish(
                        exchange: "",
                        routingKey: MqEvents.TASK_EVENTS_DLQ,
                        basicProperties: null,
                        body: body
                    );

                    _channel.BasicAck(ea.DeliveryTag, false);
                    return;
                }

                // 🧠 CREATE HEADER FOR RETRY
                var properties = _channel.CreateBasicProperties();
                properties.Headers = new Dictionary<string, object>
                {
                { "x-retry", Encoding.UTF8.GetBytes((retryCount + 1).ToString()) }
                };

                Console.WriteLine($"🔁 Retrying... Attempt {retryCount + 1}");

                // 🔁 Send to retry queue
                _channel.BasicPublish(
                    exchange: "",
                    routingKey: MqEvents.TASK_EVENTS_RETRY,
                    basicProperties: properties,
                    body: body
                );

                // ✅ ACK original message
                _channel.BasicAck(ea.DeliveryTag, false);
            }
        };

        _channel.BasicConsume(
            queue: MqEvents.TASK_EVENTS,
            autoAck: false,
            consumer: consumer
        );

        return Task.CompletedTask;
    }

    private async Task HandleEvent(TaskEvent taskEvent)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var description = GetEventDescription(taskEvent);

        var activity = new TaskActivity
        {
            TaskId = taskEvent.TaskId,
            EventType = taskEvent.EventType,
            Description = description,
            CreatedAt = DateTime.UtcNow,
            CorrelationId = taskEvent.CorrelationId
        };

        context.TaskActivities.Add(activity);
        await context.SaveChangesAsync();
    }

    private static string GetEventDescription(TaskEvent taskEvent)
    {
        return taskEvent.EventType switch
        {
            StatusEvents.TASK_CREATED => "Task created",
            StatusEvents.TASK_ASSIGNED => $"Assigned to {taskEvent.NewValue}",
            StatusEvents.TASK_TITLE_UPDATED => $"Title changed from '{taskEvent.OldValue}' to '{taskEvent.NewValue}'",
            StatusEvents.TASK_STATUS_UPDATED => $"Status changed from '{taskEvent.OldValue}' to '{taskEvent.NewValue}'",
            StatusEvents.TASK_PRIORITY_UPDATED => $"Priority changed from '{taskEvent.OldValue}' to '{taskEvent.NewValue}'",
            StatusEvents.TASK_DESCRIPTION_UPDATED => $"Description updated from '{taskEvent.OldValue}' to '{taskEvent.NewValue}'",
            StatusEvents.TASK_DUE_DATE_UPDATED => $"Due date changed from '{taskEvent.OldValue}' to '{taskEvent.NewValue}'",
            StatusEvents.TASK_DELETED => "Task deleted by user ID " + taskEvent.NewValue + " at " + taskEvent.OldValue,
            _ => taskEvent.EventType
        };
    }
}