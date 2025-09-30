using System;

namespace PublisherService.Application.UseCases.PublishArticle;

// The input model for the use case
public sealed record PublishArticleCommand(
    int AuthorId,
    string Title,
    string? Summary,
    string Content,
    string? IdempotencyKey
);