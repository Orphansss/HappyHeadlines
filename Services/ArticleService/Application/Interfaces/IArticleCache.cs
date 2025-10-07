using ArticleService.Api.Contracts.Dtos;

namespace ArticleService.Application.Interfaces;

public interface IArticleCache
{
    Task<ArticleResponse?> TryGetByIdAsync(int id, CancellationToken ct = default);
    Task SetByIdAsync(ArticleResponse article, TimeSpan? ttl = null, CancellationToken ct = default);
    Task RemoveByIdAsync(int id, CancellationToken ct = default);

    Task<IReadOnlyList<ArticleResponse>?> TryGetLatestAsync(int count, CancellationToken ct = default);
    Task SetLatestAsync(int count, IReadOnlyList<ArticleResponse> items, TimeSpan? ttl = null, CancellationToken ct = default);

    // Batch helpers
    Task RefreshSingleAsync(int id, CancellationToken ct = default);
    Task WarmLast14DaysAsync(CancellationToken ct = default);
}
