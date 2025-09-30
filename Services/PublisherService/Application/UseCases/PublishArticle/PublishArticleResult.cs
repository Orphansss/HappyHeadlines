using System;

namespace PublisherService.Application.UseCases.PublishArticle;

// The output model for the use case
public sealed record PublishArticleResult(int PublicationId, DateTimeOffset AcceptedAt);
