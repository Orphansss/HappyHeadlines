using System.Text.Json;
using PublisherService.Application.Abstractions;
using PublisherService.Domain.Entities;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace PublisherService.Infrastructure.Messaging;

/// <summary>
/// Publishes articles to RabbitMQ (ArticleQueue).
/// This is the infrastructure adapter that implements the IArticleQueuePublisher port.
/// </summary>
public sealed class ArticleQueuePublisherRabbit : IArticleQueuePublisher, IDisposable
{
    // Represents the TCP connection to the RabbitMQ broker
    private readonly IConnection _connection;

    // Represents a channel (a lightweight "session") used for publishing
    private readonly IModel _channel;

    // Strongly typed options bound from appsettings.json → section "Rabbit"
    private readonly MessagingOptions _options;

    public ArticleQueuePublisherRabbit(IOptions<MessagingOptions> options)
    {
        _options = options.Value;

        // 1. Create a connection factory configured with broker URI
        var factory = new ConnectionFactory
        {
            Uri = new Uri(_options.Uri ?? throw new InvalidOperationException("Rabbit:Uri missing"))
        };

        // 2. Establish a TCP connection to the broker
        _connection = factory.CreateConnection();

        // 3. Create a channel on top of the connection
        _channel = _connection.CreateModel();

        // 4. Declare the exchange if it doesn't already exist
        _channel.ExchangeDeclare(
            exchange: _options.Exchange,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false
        );
        // Note: Consumers (e.g. ArticleService) will bind their queues to this exchange.
    }

    /// <summary>
    /// Publishes the given Article to RabbitMQ.
    /// </summary>
    public Task PublishAsync(Article article, string? idempotencyKey, CancellationToken ct = default)
    {
        // Build a simple DTO to send over the wire
        var message = new
        {
            publicationId = article.Id,
            occurredAt = DateTimeOffset.UtcNow,
            article = new
            {
                id = article.Id,
                authorId = article.AuthorId,
                title = article.Title,
                summary = article.Summary,
                content = article.Content,
                region = article.Region,
                publishedAt = article.PublishedAt
            },
            idempotencyKey
        };

        var body = JsonSerializer.SerializeToUtf8Bytes(message);

        // Set RabbitMQ message properties
        var props = _channel.CreateBasicProperties();
        props.Persistent = true; // survive broker restarts
        props.ContentType = "application/json";
        props.MessageId = article.Id.ToString();
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
            props.CorrelationId = idempotencyKey;

        // Publish to the exchange with the configured routing key
        _channel.BasicPublish(
            exchange: _options.Exchange,
            routingKey: _options.RoutingKey,
            basicProperties: props,
            body: body
        );

        return Task.CompletedTask;
    }

    /// <summary>
    /// Dispose channel + connection when the service shuts down.
    /// </summary>
    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }
}
