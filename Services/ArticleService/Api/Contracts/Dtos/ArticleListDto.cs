namespace ArticleService.Api.Contracts.Dtos
{
    public record ArticleListDto
    (   int Id,
        int AuthorId,
        string Title,
        string? Summary,
        DateTimeOffset PublishedAt
    );
       
    
}
