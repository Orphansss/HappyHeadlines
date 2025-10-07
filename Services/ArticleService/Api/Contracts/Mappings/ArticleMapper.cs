using ArticleService.Api.Contracts.Dtos;
using ArticleService.Domain.Entities;

namespace ArticleService.Api.Contracts.Mappings;

public static class ArticleMapper
{
    public static Article ToEntity(this CreateArticleRequest dto) =>
        new()
        {
            AuthorId = dto.AuthorId,
            Title = dto.Title,
            Summary = dto.Summary,
            Content = dto.Content,
            PublishedAt = DateTimeOffset.UtcNow
        };

    public static ArticleResponse ToResponse(this Article entity) =>
        new(entity.Id, entity.AuthorId, entity.Title, entity.Summary, entity.Content, entity.PublishedAt);
}
