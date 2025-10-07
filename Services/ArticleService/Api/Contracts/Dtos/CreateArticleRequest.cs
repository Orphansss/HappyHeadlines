namespace ArticleService.Api.Contracts.Dtos;

public record CreateArticleRequest(
    int AuthorId,
    string Title,
    string? Summary,
    string Content,
    string? Region
);