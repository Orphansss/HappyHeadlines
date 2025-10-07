// Caching/ArticleCache.cs
using ArticleService.Domain.Entities;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace ArticleService.Caching;

public class ArticleCache
{
    private readonly IDistributedCache _cache;
    private readonly TimeSpan _ttl;
    public ArticleCache(IDistributedCache cache, IConfiguration cfg)
    {
        _cache = cache;
        var minutes = int.TryParse(cfg["Cache:TtlMinutes"], out var m) ? m : 20160;
        _ttl = TimeSpan.FromMinutes(minutes);
    }

    private static string Key(int id) => $"article:{id}";

    public async Task<Article?> GetAsync(int id, CancellationToken ct)
    {
        var raw = await _cache.GetStringAsync(Key(id), ct);
        if (raw is null) { ArticleCacheMetrics.Miss.Inc(); return null; }
        ArticleCacheMetrics.Hits.Inc();
        return JsonSerializer.Deserialize<Article>(raw);
    }

    public Task SetAsync(Article a, CancellationToken ct) =>
        _cache.SetStringAsync(Key(a.Id), JsonSerializer.Serialize(a),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = _ttl }, ct);
}
