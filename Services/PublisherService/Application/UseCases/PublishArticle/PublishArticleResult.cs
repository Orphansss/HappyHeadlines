namespace PublisherService.Application.UseCases.PublishArticle;

// The output model for the use case
public sealed record PublishArticleResult(Guid PublicationId, DateTimeOffset AcceptedAt);