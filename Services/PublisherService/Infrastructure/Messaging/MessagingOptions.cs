namespace PublisherService.Infrastructure.Messaging;

public sealed class MessagingOptions
{
    public const string SectionName = "Rabbit";
    public string? Uri { get; set; }                     // e.g. amqp://guest:guest@rabbitmq:5672/
    public string Exchange { get; set; } = "articles";   // topic exchange
    public string RoutingKey { get; set; } = "article.publish.request";
}