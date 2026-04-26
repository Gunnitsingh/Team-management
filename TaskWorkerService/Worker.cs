using System.Diagnostics.Eventing.Reader;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Constants;
using Shared.Entities;
using Shared.Models;

public class Worker : BackgroundService
{
    private static readonly JsonSerializerOptions EnumAsStringJsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    private IConnection _connection;
    private IModel _channel;
    private readonly IServiceProvider _serviceProvider;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly string _notificationApiBaseUrl;
    private readonly ConnectionFactory _rabbitMqFactory;

    public Worker(
        IServiceScopeFactory scopeFactory,
        IServiceProvider serviceProvider,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _serviceProvider = serviceProvider;
        _httpClientFactory = httpClientFactory;
        _notificationApiBaseUrl = (configuration["NotificationApi:BaseUrl"] ?? "https://localhost:5001").TrimEnd('/');

        var rabbitMqHost = configuration["RabbitMq:HostName"] ?? "localhost";
        var rabbitMqPortValue = configuration["RabbitMq:Port"];
        var rabbitMqPort = int.TryParse(rabbitMqPortValue, out var parsedPort) ? parsedPort : AmqpTcpEndpoint.UseDefaultPort;

        _rabbitMqFactory = new ConnectionFactory()
        {
            HostName = rabbitMqHost,
            Port = rabbitMqPort
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await EnsureRabbitMqReady(stoppingToken);

        var args = new Dictionary<string, object>
{
    { "x-dead-letter-exchange", "" },
    { "x-dead-letter-routing-key", "task-events-retry" }
};

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
                Console.WriteLine("📥 Event received from RabbitMQ");
                await HandleEvent(taskEvent);
                await SyncTaskProjection(taskEvent);
                var notification = new Notification
                {
                    UserId = taskEvent.ChangedBy.ToString() ?? "1",
                    Title = taskEvent.EventType,
                    Message = $"Task ID: {taskEvent.TaskId} by {taskEvent.ChangedByName}",
                    CreatedAt = DateTime.UtcNow
                };

                await SaveNotification(notification);

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

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task EnsureRabbitMqReady(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _connection = _rabbitMqFactory.CreateConnection();
                _channel = _connection.CreateModel();
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Waiting for RabbitMQ: {ex.Message}");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
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
            CorrelationId = taskEvent.CorrelationId,
            ChangedBy = taskEvent.ChangedBy,
            ChangedByName = taskEvent.ChangedByName
        };

        context.TaskActivities.Add(activity);
        await context.SaveChangesAsync();
    }

    private async Task SyncTaskProjection(TaskEvent taskEvent)
    {
        TaskProjectionEvent projectionEvent;

        using (var scope = _serviceProvider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var sourceTask = await context.Tasks
                .IgnoreQueryFilters()
                .Include(t => t.AssignedToUser)
                .FirstOrDefaultAsync(t => t.Id == taskEvent.TaskId);

            var readModel = await context.TaskReadModels
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Id == taskEvent.TaskId);

            if (sourceTask == null)
            {
                if (readModel == null)
                {
                    return;
                }

                readModel.IsDeleted = true;
                readModel.LastSyncedAt = DateTime.UtcNow;
                await context.SaveChangesAsync();

                projectionEvent = new TaskProjectionEvent
                {
                    EventType = taskEvent.EventType,
                    TaskId = taskEvent.TaskId,
                    IsDeleted = true
                };
            }
            else
            {
                if (readModel == null)
                {
                    readModel = new TaskReadModel
                    {
                        Id = sourceTask.Id
                    };
                    context.TaskReadModels.Add(readModel);
                }

                readModel.Title = sourceTask.Title;
                readModel.Status = sourceTask.Status;
                readModel.Priority = sourceTask.Priority;
                readModel.Description = sourceTask.Description;
                readModel.AssignedToId = sourceTask.AssignedTo;
                readModel.AssignedToName = sourceTask.AssignedToUser?.Name;
                readModel.CreatedDate = sourceTask.CreatedDate;
                readModel.DueDate = sourceTask.DueDate;
                readModel.IsDeleted = sourceTask.IsDeleted;
                readModel.LastSyncedAt = DateTime.UtcNow;

                await context.SaveChangesAsync();

                projectionEvent = new TaskProjectionEvent
                {
                    EventType = taskEvent.EventType,
                    TaskId = sourceTask.Id,
                    IsDeleted = sourceTask.IsDeleted,
                    Task = new TaskReadDto
                    {
                        Id = readModel.Id,
                        Title = readModel.Title,
                        Status = readModel.Status,
                        Priority = readModel.Priority,
                        Description = readModel.Description,
                        AssignedToId = readModel.AssignedToId,
                        AssignedToName = readModel.AssignedToName,
                        CreatedDate = readModel.CreatedDate,
                        DueDate = readModel.DueDate
                    }
                };
            }
        }

        await SendTaskProjectionToHub(projectionEvent);
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

    private async Task SaveNotification(Notification notification)
    {
        if (notification == null)
        {
            Console.WriteLine("❌ Notification is null");
            return;
        }

        // 🟢 Save to DB FIRST
        using (var scope = _scopeFactory.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            context.Notifications.Add(notification);
            await context.SaveChangesAsync();
        }
        await SendNotificationToHub(notification);

    }


    private async Task SendNotificationToHub(Notification notification)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();

            var response = await client.PostAsJsonAsync(
                $"{_notificationApiBaseUrl}/api/notifications/push",
                notification,
                EnumAsStringJsonOptions);

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("✅ Notification sent to API");
            }
            else
            {
                Console.WriteLine($"⚠️ API failed: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ API Call Failed: {ex.Message}");
        }
    }

    private async Task SendTaskProjectionToHub(TaskProjectionEvent projectionEvent)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();

            var response = await client.PostAsJsonAsync(
                $"{_notificationApiBaseUrl}/api/notifications/tasks/sync",
                projectionEvent,
                EnumAsStringJsonOptions);

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("✅ Task projection synced to SignalR");
            }
            else
            {
                Console.WriteLine($"⚠️ Task sync API failed: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Task sync API call failed: {ex.Message}");
        }
    }
}
