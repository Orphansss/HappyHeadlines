using PublisherService.Application.Abstractions;
using PublisherService.Application.Common;
using PublisherService.Domain.Entities;

namespace PublisherService.Application.UseCases.PublishArticle;

// The orchestrator (application service) that executes the use case
public sealed class PublishArticleHandler
{
    private readonly IProfanityClient _profanity;
    private readonly IArticleQueuePublisher _publisher;

    public PublishArticleHandler(IProfanityClient profanity, IArticleQueuePublisher publisher)
    {
        _profanity = profanity;
        _publisher = publisher;
    }

    public async Task<PublishArticleResult> HandleAsync(PublishArticleCommand cmd, CancellationToken ct = default)
    {
        // 1) Filter profanity in *all* user-facing fields
        string cleanedTitle, cleanedSummary, cleanedContent;

        try
        {
            cleanedTitle   = await _profanity.FilterAsync(cmd.Title,   ct);
            cleanedSummary = cmd.Summary is null ? string.Empty : await _profanity.FilterAsync(cmd.Summary, ct);
            cleanedContent = await _profanity.FilterAsync(cmd.Content, ct);
        }
        catch (TaskCanceledException ex)       { throw new ProfanityUnavailableException(inner: ex); }
        catch (HttpRequestException ex)        { throw new ProfanityUnavailableException(inner: ex); }
        catch (Exception ex) when (ex.GetType().Name.Contains("BrokenCircuit", StringComparison.OrdinalIgnoreCase))
        { throw new ProfanityUnavailableException(inner: ex); }

        // 2) Build a valid, published Article (entity)
        var publicationId = Guid.NewGuid();
        var article = Article.CreatePublished(
            id: publicationId,
            authorId: cmd.AuthorId,
            title: cleanedTitle,
            summary: string.IsNullOrWhiteSpace(cleanedSummary) ? null : cleanedSummary,
            cleanedContent: cleanedContent
        );

        // 3) Publish to the queue (hand off to Infrastructure)
        await _publisher.PublishAsync(article, cmd.IdempotencyKey, ct);

        // 4) Return an ack to the caller
        return new PublishArticleResult(PublicationId: article.Id, AcceptedAt: DateTimeOffset.UtcNow);
    }
}
