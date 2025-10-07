namespace ArticleService.Api.Contracts.Dtos;

public sealed record ArticleResponse(
    int Id,
    int AuthorId,
    string Title,
    string? Summary,
    string Content,
    DateTimeOffset PublishedAt
);