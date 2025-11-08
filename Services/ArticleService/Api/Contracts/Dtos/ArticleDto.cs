namespace ArticleService.Api.Contracts.Dtos
{
    public record ArticleDto
    (
        int Id,
        int AuthorId,
        string Title,
        string? Summary,
        string Content,
        DateTimeOffset PublishedAt

    );

}
