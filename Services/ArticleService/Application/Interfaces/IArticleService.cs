using ArticleService.Domain.Entities;

namespace ArticleService.Application.Interfaces;

public interface IArticleService
{
    Task<Article> CreateAsync(Article input, CancellationToken ct = default);
    Task<IReadOnlyList<Article>> GetAllAsync(CancellationToken ct = default);
    Task<Article?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Article?> UpdateAsync(int id, Article input, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}