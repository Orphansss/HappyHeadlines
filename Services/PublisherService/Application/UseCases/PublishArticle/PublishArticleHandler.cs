using PublisherService.Application.Abstractions;
using PublisherService.Domain.Entities;

namespace PublisherService.Application.UseCases.PublishArticle;

// The orchestrator (application service) that executes the use case
public sealed class PublishArticleHandler
{
    private readonly IProfanityClient _profanity;
    private readonly IArticleQueuePublisher _publisher;
    private readonly IIdGenerator _ids;

    public PublishArticleHandler(IProfanityClient profanity, IArticleQueuePublisher publisher, IIdGenerator ids)
    {
        _profanity = profanity;
        _publisher = publisher;
        _ids = ids;
    }

    public async Task<PublishArticleResult> HandleAsync(PublishArticleCommand cmd, CancellationToken ct = default)
    {
        // 1) Filter text
        var cleanedTitle   = await _profanity.FilterAsync(cmd.Title, ct);
        var cleanedSummary = cmd.Summary is null ? null : await _profanity.FilterAsync(cmd.Summary, ct);
        var cleanedContent = await _profanity.FilterAsync(cmd.Content, ct);

        // 2) Create domain entity with generated int ID
        var publicationId = _ids.NextId();
        var article = Article.CreatePublished(
            id: publicationId,
            authorId: cmd.AuthorId,
            title: cleanedTitle,
            summary: cleanedSummary,
            cleanedContent: cleanedContent
        );

        // 3) Publish to queue
        await _publisher.PublishAsync(article, cmd.IdempotencyKey, ct);

        // 4) Ack
        Console.WriteLine("Published article: " + System.Text.Json.JsonSerializer.Serialize(article));
        return new PublishArticleResult(PublicationId: article.Id, AcceptedAt: DateTimeOffset.UtcNow);
    }
}
