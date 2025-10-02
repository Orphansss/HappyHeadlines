namespace ArticleService.Application.Interfaces;

// Our “application port” for consuming articles
public interface IArticleQueueConsumer
{
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
}