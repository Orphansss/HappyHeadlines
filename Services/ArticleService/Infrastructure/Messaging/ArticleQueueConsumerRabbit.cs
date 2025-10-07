using ArticleService.Application.Interfaces;
using ArticleService.Domain.Entities;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Serilog;
using System.Text;
using System.Text.Json;
using ArticleService.Api.Contracts.Dtos;

namespace ArticleService.Infrastructure.Messaging;

public sealed class ArticleQueueConsumerRabbit : BackgroundService, IArticleQueueConsumer
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ArticleQueueConsumerRabbit> _logger;
    private readonly string _exchange = "articles";
    private readonly string _queueName = "article-service-consumer";
    private readonly string _routingKey = "article.publish.request.#";
    
    private IConnection? _connection;
    private IModel? _channel;

    public ArticleQueueConsumerRabbit(IServiceProvider serviceProvider, ILogger<ArticleQueueConsumerRabbit> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
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
                
                // Write-through cache using the DTO we already have
                var cache = scope.ServiceProvider.GetRequiredService<IArticleCache>();
                var dto = new ArticleResponse(
                    article.Id, article.AuthorId, article.Title, article.Summary, article.Content, article.PublishedAt);
                // Save consumed article in the cache
                await cache.SetByIdAsync(dto, ttl: null, stoppingToken); // use default TTL from options
                
                _channel.BasicAck(ea.DeliveryTag, multiple: false);
                
                Log.Information("Article queue consumed {article}", System.Text.Json.JsonSerializer.Serialize(article));
            }
            catch (Exception e)
            {
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

