using System.Diagnostics;
using ArticleService.Api.Contracts.Dtos;
using ArticleService.Application.Interfaces;
using ArticleService.Infrastructure.Caching;
using Monitoring;
using Serilog;

namespace ArticleService.Application.Services.Decorators;

public sealed class ArticleServiceCachingDecorator : IArticleService
{
    private readonly IArticleService _decorated;
    private readonly IArticleCache _articleCache;
    private readonly ArticleCacheOptions _options;

    private const string Layer = CacheLayers.Articles; // for metrics

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
        
        Log.Information("ArticleCache: WRITE-THROUGH articleId={ArticleId}", created.Id);
        
        return created;
    }

    public async Task<IReadOnlyList<ArticleResponse>> GetAllAsync(CancellationToken ct = default)
        => await _decorated.GetAllAsync(ct); // no caching for GetAll

    public async Task<IReadOnlyList<ArticleResponse>> GetLatestAsync(int count, CancellationToken ct = default)
    {
        var ctx = Log.ForContext("Count", count)
            .ForContext("TraceId", Activity.Current?.TraceId.ToString());
        
        // METRICS cache attempt
        CacheMetrics.Request(Layer);
        
        // HIT?
        var hit = await _articleCache.TryGetLatestAsync(count, ct);
        if (hit != null)
        {
            CacheMetrics.Hit(Layer); // METRICS cache hit
            ctx.Information("ArticleCache: HIT latest");
            return hit;
        }
        
        // METRICS cache miss
        CacheMetrics.Miss(Layer);
        
        // MISS -> go to DB -> set cache
        ctx.Information("ArticleCache: MISS latest (loading from DB)");
        var items = await _decorated.GetLatestAsync(count, ct);
        await _articleCache.SetLatestAsync(count, items, _options.ArticleTtl, ct);
        
        return items;
    }

    public async Task<ArticleResponse?> GetByIdAsync(int id, CancellationToken ct = default)
    {
   

        var ctx = Log.ForContext("ArticleId", id)
            .ForContext("TraceId", Activity.Current?.TraceId.ToString());
        
        // METRICS cache attempt
        CacheMetrics.Request(Layer);

        var cached = await _articleCache.TryGetByIdAsync(id, ct);
        if (cached != null) 
        {
           CacheMetrics.Hit(layer: Layer); // METRICS cache hit
            ctx.Information("ArticleCache: HIT");
            return cached;
        }
        
        // METRICS cache miss
        CacheMetrics.Miss(Layer);
        
        ctx.Information("ArticleCache: MISS (loading from DB)");
        var item = await _decorated.GetByIdAsync(id, ct);
        if (item is not null)
        {
            await _articleCache.SetByIdAsync(item, _options.ArticleTtl, ct);
            ctx.Information("ArticleCache: STORE after miss");
        }
        
        return item;
    }

    public async Task<ArticleResponse?> UpdateAsync(int id, UpdateArticleRequest input, CancellationToken ct = default)
    {
        var updated = await _decorated.UpdateAsync(id, input, ct);
        if (updated is null)
        {
            Log.ForContext("ArticleId", id)
                .Information("ArticleCache: INVALIDATE on not-found update");
            await _articleCache.RemoveByIdAsync(id, ct);
            return null;
        }
        
        await _articleCache.SetByIdAsync(updated, _options.ArticleTtl, ct);
        
        Log.ForContext("ArticleId", id)
            .Information("ArticleCache: WRITE-THROUGH on update");
        
        return updated;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var ok = await _decorated.DeleteAsync(id, ct);
        if (ok)
        {
            await _articleCache.RemoveByIdAsync(id, ct);
            Log.ForContext("ArticleId", id)
                .Information("ArticleCache: INVALIDATE on delete");
        }        
        return ok;
    }
}