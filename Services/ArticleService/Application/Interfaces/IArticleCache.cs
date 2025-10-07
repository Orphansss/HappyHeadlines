using ArticleService.Domain.Entities;

namespace ArticleService.Application.Interfaces;

public interface IArticleCache
{
    Task<Article?> GetByIdAsync(int id, CancellationToken ct = default);
    Task SetByIdAsync(Article article, CancellationToken ct = default);
    Task RemoveByIdAsync(int id, CancellationToken ct = default);
}