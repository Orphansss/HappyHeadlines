using ArticleService.Application.Interfaces;
using ArticleService.Domain.Entities;
using ArticleService.Infrastructure.Data;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Serilog;
using System.Text;
using System.Text.Json;
using System.Diagnostics;                          
using OpenTelemetry;                                 
using OpenTelemetry.Context.Propagation;             

namespace ArticleService.Infrastructure.Messaging;

public sealed class ArticleQueueConsumerRabbit : BackgroundService, IArticleQueueConsumer
{
    private readonly IServiceProvider _serviceProvider;
    private readonly string _exchange = "articles";
    private readonly string _queueName = "article-service-consumer";
    private readonly string _routingKey = "article.publish.request.#";
    
    private IConnection? _connection;
    private IModel? _channel;

    // NEW: activity source for manual spans in ArticleService
    private static readonly ActivitySource ActivitySource = new("ArticleService");
    private static readonly TextMapPropagator Propagator = Propagators.DefaultTextMapPropagator;

    public ArticleQueueConsumerRabbit(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Log.Information("Consuming articles from article queue...");

        var factory = new ConnectionFactory
        {
            Uri = new Uri("amqp://guest:guest@rabbitmq:5672/") // TODO: bind from config
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        
        _channel.ExchangeDeclare(_exchange, ExchangeType.Topic, durable: true);
        _channel.QueueDeclare(_queueName, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind(_queueName, _exchange, _routingKey);
        _channel.BasicQos(0, 10, false);

        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += async (_, ea) =>
        {
            // Extract parent trace context from RMQ headers (if present)
            var parentCtx = Propagator.Extract(default, ea.BasicProperties?.Headers, static (hdrs, key) =>
            {
                if (hdrs is null) return Array.Empty<string>();
                if (!hdrs.TryGetValue(key, out var obj) || obj is not byte[] bytes) return Array.Empty<string>();
                return new[] { Encoding.UTF8.GetString(bytes) }; // convert header bytes -> string
            });
            Baggage.Current = parentCtx.Baggage;

            // Start a CONSUMER span so Jaeger shows the queue hop
            using var activity = ActivitySource.StartActivity(
                "RabbitMQ Consume article",
                ActivityKind.Consumer,
                parentCtx.ActivityContext);

            activity?.SetTag("messaging.system", "rabbitmq"); // small metadata tags
            activity?.SetTag("messaging.destination", _exchange);
            activity?.SetTag("messaging.operation", "process");

            try
            {
                var body = Encoding.UTF8.GetString(ea.Body.Span);
                var envelope = JsonSerializer.Deserialize<ArticleEnvelope>(body, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (envelope == null) throw new Exception("Invalid article message");

                var article = new Article
                {
                    Id = envelope.article.id,
                    AuthorId = envelope.article.authorId,
                    Title = envelope.article.title,
                    Summary = envelope.article.summary,
                    Content = envelope.article.content,
                    PublishedAt = envelope.article.publishedAt
                };
                
                using var scope = _serviceProvider.CreateScope();
                var cfg = scope.ServiceProvider.GetRequiredService<IConfiguration>();

                var payload = envelope.article;
                await using var db = RegionDbContextFactory.CreateDbContext(payload.region, cfg);

                var existing = await db.Articles.FindAsync(new object[] { envelope.article.id }, stoppingToken);
                if (existing == null)
                {
                    db.Articles.Add(article);
                }
                else
                {
                    existing.Title = article.Title;
                    existing.Summary = article.Summary;
                    existing.Content = article.Content;
                    existing.PublishedAt = article.PublishedAt;
                }
                
                await db.SaveChangesAsync(stoppingToken);

                _channel.BasicAck(ea.DeliveryTag, multiple: false);

                // helpful correlation in logs
                Log.Information("Article queue consumed {article}", System.Text.Json.JsonSerializer.Serialize(article));
            }
            catch (Exception e)
            {
                activity?.SetStatus(ActivityStatusCode.Error, e.Message);   // NEW: mark span as error
                Log.Error(e, "Failed to consume article message...");
                _channel?.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
            }
        };
        
        _channel.BasicConsume(_queueName, autoAck: false, consumer);
        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}
