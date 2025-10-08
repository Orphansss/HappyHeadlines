namespace ArticleService.Infrastructure.Messaging;

public sealed class RabbitOptions
{
    public string Uri { get; init; } = "";
    public string Exchange { get; init; } = "articles";
    public string Queue { get; init; } = "article.publish.request.q";
    public string RoutingKey { get; init; } = "article.publish.request";
}
