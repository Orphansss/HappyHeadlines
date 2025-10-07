namespace ArticleService.Api.Contracts.Dtos;

public sealed record UpdateArticleRequest(
    string? Title,
    string? Summary,
    string? Content
);
