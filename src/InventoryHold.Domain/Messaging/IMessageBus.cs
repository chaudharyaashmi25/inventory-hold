namespace InventoryHold.Domain.Messaging;

/// <summary>
/// Abstraction for publishing domain events to an external message broker.
/// Implementations should be resilient and non-blocking for the main request path.
/// </summary>
public interface IMessageBus
{
    /// <summary>
    /// Publishes a message payload to the message broker with a routing key.
    /// </summary>
    Task PublishAsync<T>(string routingKey, T payload, IDictionary<string, string>? headers = null);
}
