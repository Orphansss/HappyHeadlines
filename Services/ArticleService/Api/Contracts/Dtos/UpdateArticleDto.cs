namespace ArticleService.Api.Contracts.Dtos
{
    public record UpdateArticleDto
    (
        string Title,
        string? Summary,
        string Content
    );
}
