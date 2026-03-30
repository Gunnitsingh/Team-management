using RabbitMQ.Client;
using Shared.Constants;
using Shared.Entities;
using System.Text;
using System.Text.Json;

public class RabbitMqPublisher : IMessagePublisher
{
    private readonly IConnection _connection;
    private readonly IModel _channel;

    public RabbitMqPublisher()
    {
        var factory = new ConnectionFactory()
        {
            HostName = "localhost"
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

    }

    public void Publish(TaskEvent taskEvent)
    {
        try
        {
            var message = JsonSerializer.Serialize(taskEvent);
            var body = Encoding.UTF8.GetBytes(message);
            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true;

            _channel.BasicPublish(
                exchange: "",
                routingKey: MqEvents.TASK_EVENTS,
                basicProperties: properties,
                body: body
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"RabbitMQ publish failed: {ex.Message}");
        }
    }
}