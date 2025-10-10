using ArticleService.Domain.Entities;

namespace ArticleService.Application.Interfaces
{
    public interface IArticleCache
    {
        Task<(Article? article, bool hit)> TryGetArticleAsync(int id, CancellationToken ct = default);
        Task SetArticleAsync(Article article, CancellationToken ct = default);
        Task InvalidateArticleAsync(int id, CancellationToken ct = default);

        Task<(IReadOnlyList<Article> items, bool hit)> TryGetLatestAsync(int take, CancellationToken ct = default);
        Task SetLatestAsync(IReadOnlyList<Article> items, CancellationToken ct = default);

        // Used by the offline warmup job
        Task UpsertHotSetAsync(IEnumerable<int> ids, CancellationToken ct = default);
    }
}
