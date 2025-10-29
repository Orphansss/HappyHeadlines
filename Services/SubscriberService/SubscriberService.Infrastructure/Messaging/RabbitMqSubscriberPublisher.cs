using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using SubscriberService.Application.Abstractions;
using SubscriberService.Application.DTOs;

namespace SubscriberService.Infrastructure.Messaging;

public class RabbitMqSubscriberPublisher : ISubscriberPublisher, IDisposable
{
    private readonly IConnection _conn;
    private readonly string _exchange;
    private readonly string _routingKey;
    private readonly ILogger<RabbitMqSubscriberPublisher> _logger;
    private bool _disposed;

    public RabbitMqSubscriberPublisher(IConfiguration config, ILogger<RabbitMqSubscriberPublisher> logger)
    {
        _logger = logger;
        _exchange = config["Rabbit:Exchange"] ?? "subscriber.events";
        _routingKey = config["Rabbit:RoutingKeyNewSubscriber"] ?? "subscriber.created";

        var uri = config["Rabbit:Uri"];
        var factory = new ConnectionFactory
        {
            DispatchConsumersAsync = true,
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(5),
            TopologyRecoveryEnabled = true
        };

        if (!string.IsNullOrWhiteSpace(uri))
        {
            factory.Uri = new Uri(uri);
        }
        else
        {
            factory.HostName = config["Rabbit:Host"] ?? "rabbitmq";
            factory.UserName = config["Rabbit:User"] ?? "guest";
            factory.Password = config["Rabbit:Pass"] ?? "guest";
        }

        _conn = factory.CreateConnection("subscriber-service-publisher");
    }

    public Task PublishNewSubscriberAsync(SubscriberDto dto, CancellationToken ct = default)
    {
        using var ch = _conn.CreateModel();

        // Ensure the exchange exists – idempotent
        ch.ExchangeDeclare(_exchange, ExchangeType.Topic, durable: true, autoDelete: false);

        var payload = JsonSerializer.Serialize(new
        {
            subscriberId = dto.Id,
            email = dto.Email,
            subscribedAtUtc = dto.SubscribedAtUtc
        });

        var body = Encoding.UTF8.GetBytes(payload);
        var props = ch.CreateBasicProperties();
        props.ContentType = "application/json";
        props.DeliveryMode = 2; // persistent

        // Log & publish
        _logger.LogInformation("Publishing NewSubscriber event for {Email} -> {Exchange}/{RoutingKey}", 
            dto.Email, _exchange, _routingKey);

        ch.BasicPublish(
            exchange: _exchange,
            routingKey: _routingKey,
            mandatory: true, // helpful for detecting misconfiguration
            basicProperties: props,
            body: body);

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _conn?.Dispose();
        _disposed = true;
    }
}
