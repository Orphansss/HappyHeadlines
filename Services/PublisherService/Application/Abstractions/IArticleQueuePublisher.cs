using System.Threading;
using System.Threading.Tasks;
using PublisherService.Domain.Entities;

namespace PublisherService.Application.Abstractions;

public interface IArticleQueuePublisher
{
    /// <summary>
    /// Publishes a "publish-article" command/event to the ArticleQueue.
    /// Implementations should set message id/correlation/idempotency headers.
    /// </summary>
    Task PublishAsync(Article article, string? idempotencyKey, CancellationToken ct = default);
}