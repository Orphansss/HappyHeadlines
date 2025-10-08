using ArticleService.Application.Interfaces;
using ArticleService.Domain.Entities;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Serilog;
using System.Text;
using System.Text.Json;
using ArticleService.Api.Contracts.Dtos;
using Microsoft.Extensions.Options;

namespace ArticleService.Infrastructure.Messaging;

/// <summary>
/// Consumes article publish/update messages, persists to the correct region DB,
/// and updates the cache.
/// </summary>
public sealed class ArticleQueueConsumerRabbit : BackgroundService, IArticleQueueConsumer
{
    private readonly IServiceProvider _serviceProvider;
    private readonly RabbitOptions _opts;

    private IConnection? _connection;
    private IModel? _channel;

    public ArticleQueueConsumerRabbit(
        IServiceProvider serviceProvider,
        IOptions<RabbitOptions> opts)
    {
        _serviceProvider = serviceProvider;
        _opts = opts.Value;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Log.Information("Starting RabbitMQ consumer...");

        try
        {
            var factory = new ConnectionFactory
            {
                Uri = new Uri(_opts.Uri),
                DispatchConsumersAsync = true,
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(5)
            };

            _connection = factory.CreateConnection("article-service-consumer");
            _channel = _connection.CreateModel();

            // Idempotent declare & bind
            _channel.ExchangeDeclare(_opts.Exchange, ExchangeType.Topic, durable: true, autoDelete: false);
            _channel.QueueDeclare(_opts.Queue, durable: true, exclusive: false, autoDelete: false);
            _channel.QueueBind(_opts.Queue, _opts.Exchange, _opts.RoutingKey);
            _channel.BasicQos(0, prefetchCount: 10, global: false);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.Received += async (_, ea) =>
            {
                try
                {
                    // Deserialize
                    var json = Encoding.UTF8.GetString(ea.Body.Span);
                    var envelope = JsonSerializer.Deserialize<ArticleEnvelope>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    if (envelope?.article is null)
                        throw new InvalidOperationException("Invalid article message payload.");

                    var p = envelope.article;

                    using var scope = _serviceProvider.CreateScope();
                    var cfg = scope.ServiceProvider.GetRequiredService<IConfiguration>();

                    // Persist to correct region DB
                    await using var db = RegionDbContextFactory.CreateDbContext(p.region, cfg);

                    var existing = await db.Articles.FindAsync(new object[] { p.id }, stoppingToken);
                    if (existing is null)
                    {
                        db.Articles.Add(new Article
                        {
                            Id = p.id,
                            AuthorId = p.authorId,
                            Title = p.title,
                            Summary = p.summary,
                            Content = p.content,
                            PublishedAt = p.publishedAt
                        });
                    }
                    else
                    {
                        existing.Title       = p.title;
                        existing.Summary     = p.summary;
                        existing.Content     = p.content;
                        existing.PublishedAt = p.publishedAt;
                    }

                    await db.SaveChangesAsync(stoppingToken);

                    // Write-through cache
                    var cache = scope.ServiceProvider.GetRequiredService<IArticleCache>();
                    var dto = new ArticleResponse(p.id, p.authorId, p.title, p.summary, p.content, p.publishedAt);
                    await cache.SetByIdAsync(dto); // default TTL

                    _channel!.BasicAck(ea.DeliveryTag, multiple: false);
                    Log.Information("Consumed article message. Id={Id}, Region={Region}", p.id, p.region ?? "Global");
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to process article message. NACK & requeue.");
                    _channel?.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
                }
            };

            _channel.BasicConsume(queue: _opts.Queue, autoAck: false, consumer);

            Log.Information("RabbitMQ consumer started. Queue={Queue}, Exchange={Exchange}, Uri={Uri}",
                _opts.Queue, _opts.Exchange, _opts.Uri);

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            // Keep it simple: log and keep service running (will retry on container restart)
            Log.Error(ex, "RabbitMQ consumer failed to start.");
            return Task.CompletedTask;
        }
    }

    public override void Dispose()
    {
        try { _channel?.Close(); _channel?.Dispose(); } catch { }
        try { _connection?.Close(); _connection?.Dispose(); } catch { }
        base.Dispose();
    }
}
