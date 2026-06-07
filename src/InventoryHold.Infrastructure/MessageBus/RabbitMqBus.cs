namespace InventoryHold.Infrastructure.MessageBus;

using System.Text.Json;
using InventoryHold.Domain.Messaging;
using RabbitMQ.Client;

/// <summary>
/// Production-ready RabbitMQ publisher. Maintains a single connection and
/// opens a channel per publish. Messages are published as persistent JSON.
/// </summary>
public class RabbitMqBus : IMessageBus, IDisposable
{
    private readonly IConnection _connection;
    private readonly string _exchange;

    public RabbitMqBus(string connectionString, string exchange = "inventory.holds")
    {
        _exchange = exchange;

        var factory = new ConnectionFactory();

        // Accept either a full AMQP URI or a hostname
        if (Uri.TryCreate(connectionString, UriKind.Absolute, out var uri) && (uri.Scheme == "amqp" || uri.Scheme == "amqps"))
        {
            factory.Uri = uri;
        }
        else
        {
            factory.HostName = connectionString;
        }

        int retries = 5;
        while (true)
        {
            try
            {
                _connection = factory.CreateConnection();
                break;
            }
            catch (Exception ex) when (ex is RabbitMQ.Client.Exceptions.BrokerUnreachableException || ex is System.Net.Sockets.SocketException)
            {
                if (retries-- <= 0) throw;
                System.Threading.Thread.Sleep(3000);
            }
        }

        // Ensure exchange exists
        using var ch = _connection.CreateModel();
        ch.ExchangeDeclare(_exchange, ExchangeType.Topic, durable: true, autoDelete: false);
    }

    public Task PublishAsync<T>(string routingKey, T payload, IDictionary<string, string>? headers = null)
    {
        var body = JsonSerializer.SerializeToUtf8Bytes(payload, typeof(T));

        return Task.Run(() =>
        {
            using var channel = _connection.CreateModel();
            var props = channel.CreateBasicProperties();
            props.DeliveryMode = 2; // persistent
            props.ContentType = "application/json";

            if (headers != null && headers.Count > 0)
            {
                props.Headers = headers.ToDictionary(kv => kv.Key, kv => (object)kv.Value);
            }

            channel.BasicPublish(exchange: _exchange, routingKey: routingKey, basicProperties: props, body: body);
        });
    }

    public void Dispose()
    {
        try
        {
            _connection?.Close();
            _connection?.Dispose();
        }
        catch { }
    }
}
