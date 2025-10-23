using System;
using System.Text.Json;
using System.Collections.Generic; // ← needed for IDictionary<string, object>

public interface IEventBus
{
    void Publish<T>(string topic, T message);
}

public sealed class RabbitMqBus : IEventBus, IDisposable
{
    private readonly RabbitMQ.Client.IConnection _conn;
    private readonly RabbitMQ.Client.IModel _ch; // if you're on v7, change to IChannel

    public RabbitMqBus(string host = "rabbitmq")
    {
        var factory = new RabbitMQ.Client.ConnectionFactory { HostName = host };

        _conn = factory.CreateConnection();
        _ch   = _conn.CreateModel(); // v7: use CreateChannel()

        // v6/v7-safe: provide all args explicitly
        _ch.ExchangeDeclare(
            exchange: "events",
            type: RabbitMQ.Client.ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            arguments: null
        );
    }

    public void Publish<T>(string topic, T message)
    {
        var body = JsonSerializer.SerializeToUtf8Bytes(message);

        var props = _ch.CreateBasicProperties();
        props.ContentType = "application/json";
        props.DeliveryMode = 2; // persistent

        // v6/v7-safe overload: include 'mandatory' and IBasicProperties
        _ch.BasicPublish(
            exchange: "events",
            routingKey: topic,
            mandatory: false,
            basicProperties: props,
            body: body // byte[] implicitly converts to ReadOnlyMemory<byte>
        );
    }

    public void Dispose()
    {
        try { _ch?.Close(); } catch { }
        _ch?.Dispose();
        try { _conn?.Close(); } catch { }
        _conn?.Dispose();
    }
}
