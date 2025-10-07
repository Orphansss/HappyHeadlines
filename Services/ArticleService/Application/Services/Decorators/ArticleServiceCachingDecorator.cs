using ArticleService.Api.Contracts.Dtos;
using ArticleService.Application.Interfaces;
using ArticleService.Infrastructure.Caching;

namespace ArticleService.Application.Services.Decorators;

public sealed class ArticleServiceCachingDecorator : IArticleService
{
    private readonly IArticleService _decorated;
    private readonly IArticleCache _articleCache;
    private readonly ArticleCacheOptions _options;


    public ArticleServiceCachingDecorator(IArticleService decorated, IArticleCache articleCache, ArticleCacheOptions options)
    {
        _decorated = decorated;
        _articleCache = articleCache;
        _options = options;
    }

    public async Task<ArticleResponse> CreateAsync(CreateArticleRequest input, CancellationToken ct = default)
    {
        var created = await _decorated.CreateAsync(input, ct);
        // Write-through: refresh the single article key
        await _articleCache.SetByIdAsync(created, _options.ArticleTtl, ct);
        
        return created;
    }

    public async Task<IReadOnlyList<ArticleResponse>> GetAllAsync(CancellationToken ct = default)
        => await _decorated.GetAllAsync(ct); // no caching for GetAll

    public async Task<IReadOnlyList<ArticleResponse>> GetLatestAsync(int count, CancellationToken ct = default)
    {
        // HIT?
        var hit = await _articleCache.TryGetLatestAsync(count, ct);
        if (hit != null) 
            return hit;
        
        // MISS -> go to DB -> set cache
        var items = await _decorated.GetLatestAsync(count, ct);
        await _articleCache.SetLatestAsync(count, items, _options.ArticleTtl, ct);
        
        return items;
    }

    public async Task<ArticleResponse?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var cached = await _articleCache.TryGetByIdAsync(id, ct);
        if (cached != null) 
            return cached;
        
        var item = await _decorated.GetByIdAsync(id, ct);
        if (item != null)
            await _articleCache.SetByIdAsync(item, _options.ArticleTtl, ct);
        
        return item;
    }

    public async Task<ArticleResponse?> UpdateAsync(int id, UpdateArticleRequest input, CancellationToken ct = default)
    {
        var updated = await _decorated.UpdateAsync(id, input, ct);
        if (updated is null)
        {
            // Ensure no stale value remains
            await _articleCache.RemoveByIdAsync(id, ct);
            return null;
        }
        await _articleCache.SetByIdAsync(updated, _options.ArticleTtl, ct);
        
        return updated;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var ok = await _decorated.DeleteAsync(id, ct);
        if (ok) await _articleCache.RemoveByIdAsync(id, ct);
        
        return ok;
    }
}