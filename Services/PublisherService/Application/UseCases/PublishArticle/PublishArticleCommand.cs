namespace PublisherService.Application.UseCases.PublishArticle;

// The input model for the use case
public sealed record PublishArticleCommand(
    Guid AuthorId,
    string Title,
    string? Summary,
    string Content,
    string? IdempotencyKey
);