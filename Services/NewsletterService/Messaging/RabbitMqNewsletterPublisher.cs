// NewsletterService/Infrastructure/Messaging/RabbitMqNewsletterPublisher.cs
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using NewsletterService.Contracts.Dtos;
using NewsletterService.Interfaces;

public class RabbitMqNewsletterPublisher : INewsletterPublisher, IDisposable
{
  private readonly IConnection _conn;
  private readonly string _exchange;
  private readonly string _routingKeyWelcome;   
  private readonly ILogger<RabbitMqNewsletterPublisher> _logger;
  private bool _disposed;

  public RabbitMqNewsletterPublisher(IConfiguration cfg, ILogger<RabbitMqNewsletterPublisher> logger)
  {
    _logger = logger;
    _exchange = cfg["Rabbit:Exchange"] ?? "subscriber.events";
    _routingKeyWelcome = cfg["Rabbit:RoutingKeyWelcome"] ?? "subscriber.welcome.requested";

    var f = new ConnectionFactory
    {
      DispatchConsumersAsync = true,
      AutomaticRecoveryEnabled = true,
      NetworkRecoveryInterval = TimeSpan.FromSeconds(5),
      TopologyRecoveryEnabled = true
    };

    var uri = cfg["Rabbit:Uri"];
    if (!string.IsNullOrWhiteSpace(uri)) f.Uri = new Uri(uri);
    else
    {
      f.HostName = cfg["Rabbit:Host"] ?? "rabbitmq";
      f.UserName = cfg["Rabbit:User"] ?? "guest";
      f.Password = cfg["Rabbit:Pass"] ?? "guest";
    }

    _conn = f.CreateConnection("newsletter-service-publisher");
  }

  public Task PublishWelcomeAsync(WelcomeEmailRequested evt, CancellationToken ct = default)
  {
    using var ch = _conn.CreateModel();
    ch.ExchangeDeclare(_exchange, ExchangeType.Topic, durable: true, autoDelete: false);

    var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(evt));
    var props = ch.CreateBasicProperties();
    props.ContentType = "application/json";
    props.DeliveryMode = 2; 
    props.MessageId = Guid.NewGuid().ToString();

    _logger.LogInformation("Publishing WelcomeEmailRequested for {Email} -> {Exchange}/{RoutingKey}",
        evt.Email, _exchange, _routingKeyWelcome);

    ch.BasicPublish(exchange: _exchange, routingKey: _routingKeyWelcome,
                    mandatory: true, basicProperties: props, body: body);

    return Task.CompletedTask;
  }

  public void Dispose()
  {
    if (_disposed) return;
    _conn?.Dispose();
    _disposed = true;
  }
}
