using PublisherService.Application.Abstractions;
using PublisherService.Domain.Entities;
using Serilog;
using System.Diagnostics;

namespace PublisherService.Application.UseCases.PublishArticle;

// The orchestrator (application service) that executes the use case
public sealed class PublishArticleHandler
{
    // Activity source for manual spans in PublisherService
    private static readonly ActivitySource ActivitySource = new("PublisherService");

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
            cleanedContent: cleanedContent,
            region: cmd.Region
        );

        // 3) Publish to queue
        // Wrap publish in a PRODUCER span (so Jaeger shows the messaging hop)
        using (var activity = ActivitySource.StartActivity("RabbitMQ Publish article", ActivityKind.Producer))
        {
            // small tags for clarity in Jaeger
            activity?.SetTag("messaging.system", "rabbitmq");
            activity?.SetTag("messaging.destination", "articles");
            activity?.SetTag("messaging.operation", "publish");
            activity?.SetTag("article.id", article.Id);

            // The RabbitMQ publisher implementation should inject the current W3C trace context
            // (traceparent/tracestate) into IBasicProperties.Headers before BasicPublish.
            await _publisher.PublishAsync(article, cmd.IdempotencyKey, ct);
        }

        // 4) Ack
        Log.Information("Published article: {Article}", System.Text.Json.JsonSerializer.Serialize(article));

        return new PublishArticleResult(PublicationId: article.Id, AcceptedAt: DateTimeOffset.UtcNow);
    }
}
