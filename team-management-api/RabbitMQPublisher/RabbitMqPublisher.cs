using RabbitMQ.Client;
using Shared.Constants;
using Shared.Entities;
using System.Text;
using System.Text.Json;

public class RabbitMqPublisher : IMessagePublisher
{
    private readonly ConnectionFactory _factory;
    private IConnection? _connection;
    private IModel? _channel;

    public RabbitMqPublisher(IConfiguration configuration)
    {
        var hostName = configuration["RabbitMq:HostName"] ?? "localhost";
        var portValue = configuration["RabbitMq:Port"];
        var port = int.TryParse(portValue, out var parsedPort) ? parsedPort : AmqpTcpEndpoint.UseDefaultPort;

        _factory = new ConnectionFactory()
        {
            HostName = hostName,
            Port = port
        };
    }

    public void Publish(TaskEvent taskEvent)
    {
        try
        {
            EnsureConnected();

            var message = JsonSerializer.Serialize(taskEvent);
            var body = Encoding.UTF8.GetBytes(message);
            var properties = _channel!.CreateBasicProperties();
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

    private void EnsureConnected()
    {
        if (_connection is { IsOpen: true } && _channel is { IsOpen: true })
        {
            return;
        }

        _connection?.Dispose();
        _channel?.Dispose();

        _connection = _factory.CreateConnection();
        _channel = _connection.CreateModel();
    }
}
