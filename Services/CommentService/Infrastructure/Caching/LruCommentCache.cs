using CommentService.Interfaces;
using CommentService.Models;
using global::CommentService.Models;
using Microsoft.Extensions.Caching.Distributed;
using Monitoring;
using System.Collections.Concurrent;

namespace CommentService.Infrastructure.Caching;

/// <summary>
/// LRU cache that stores all comments for the 30 most recently accessed articles.
/// Uses cache-miss approach: only populates cache when data is requested.
/// </summary>
public sealed class LruCommentCache : ICommentCache
{
    private readonly IDistributedCache _cache;
    private readonly ICacheMetrics _metrics;
    private readonly TimeSpan _expiry = TimeSpan.FromMinutes(15);
    private const int MaxArticles = 30;

    // Track article access order (articleId -> last access time)
    private readonly ConcurrentDictionary<int, DateTime> _articleAccessTimes = new();
    private readonly SemaphoreSlim _evictionSemaphore = new(1, 1);

    public LruCommentCache(IDistributedCache cache, ICacheMetrics metrics)
    {
        _cache = cache;
        _metrics = metrics;
    }

    public async Task<Comment?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var key = $"comment:{id}";
        var json = await _cache.GetStringAsync(key, ct);

        if (json == null)
        {
            _metrics.Miss("comment_by_id");
            return null;
        }

        _metrics.Hit("comment_by_id");
        var comment = System.Text.Json.JsonSerializer.Deserialize<Comment>(json);

        // Track article access for LRU
        if (comment != null)
            TouchArticle(comment.ArticleId);

        return comment;
    }

    public async Task SetByIdAsync(Comment comment, CancellationToken ct = default)
    {
        TouchArticle(comment.ArticleId);
        await EvictLruArticleIfNeededAsync(ct);

        var key = $"comment:{comment.Id}";
        var json = System.Text.Json.JsonSerializer.Serialize(comment);
        var options = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = _expiry };
        await _cache.SetStringAsync(key, json, options, ct);
    }

    public async Task RemoveByIdAsync(int id, CancellationToken ct = default)
    {
        var key = $"comment:{id}";
        await _cache.RemoveAsync(key, ct);
    }

    public async Task<IEnumerable<Comment>?> GetCommentsByArticleIdAsync(int articleId, CancellationToken ct = default)
    {
        TouchArticle(articleId);

        var key = $"article:{articleId}:comments";
        var json = await _cache.GetStringAsync(key, ct);

        if (json == null)
        {
            _metrics.Miss("comments_by_article");
            return null;
        }

        _metrics.Hit("comments_by_article");
        return System.Text.Json.JsonSerializer.Deserialize<List<Comment>>(json);
    }

    public async Task SetCommentsByArticleIdAsync(int articleId, IEnumerable<Comment> comments, CancellationToken ct = default)
    {
        TouchArticle(articleId);
        await EvictLruArticleIfNeededAsync(ct);

        var key = $"article:{articleId}:comments";
        var json = System.Text.Json.JsonSerializer.Serialize(comments);
        var options = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = _expiry };
        await _cache.SetStringAsync(key, json, options, ct);
        
        // Report cache size
        _metrics.SetSize("lru_articles", _articleAccessTimes.Count);
    }

    public async Task InvalidateArticleAsync(int articleId, CancellationToken ct = default)
    {
        var key = $"article:{articleId}:comments";
        await _cache.RemoveAsync(key, ct);
        _articleAccessTimes.TryRemove(articleId, out _);
        
        // Update cache size
        _metrics.SetSize("lru_articles", _articleAccessTimes.Count);
    }

    public Task<IEnumerable<Comment>?> GetAllAsync(CancellationToken ct = default)
    {
        // Not applicable for article-scoped LRU cache
        _metrics.Miss("comments_all");
        return Task.FromResult<IEnumerable<Comment>?>(null);
    }

    public Task SetAllAsync(IEnumerable<Comment> comments, CancellationToken ct = default)
    {
        // Not applicable for article-scoped LRU cache
        return Task.CompletedTask;
    }

    public Task InvalidateAllAsync(CancellationToken ct = default)
    {
        // Clear all article access tracking
        _articleAccessTimes.Clear();
        _metrics.SetSize("lru_articles", 0);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Records access to an article for LRU tracking.
    /// </summary>
    private void TouchArticle(int articleId)
    {
        _articleAccessTimes[articleId] = DateTime.UtcNow;
        _metrics.SetSize("lru_articles", _articleAccessTimes.Count);
    }

    /// <summary>
    /// Evicts the least recently used article if we exceed MaxArticles.
    /// </summary>
    private async Task EvictLruArticleIfNeededAsync(CancellationToken ct)
    {
        if (_articleAccessTimes.Count <= MaxArticles)
            return;

        await _evictionSemaphore.WaitAsync(ct);
        try
        {
            if (_articleAccessTimes.Count <= MaxArticles)
                return;

            // Find LRU article
            var lruArticle = _articleAccessTimes
                .OrderBy(kvp => kvp.Value)
                .First();

            // Remove from tracking
            _articleAccessTimes.TryRemove(lruArticle.Key, out _);

            // Track eviction
            _metrics.Evict("lru_articles");
            _metrics.SetSize("lru_articles", _articleAccessTimes.Count);

            // Remove from Redis
            var key = $"article:{lruArticle.Key}:comments";
            await _cache.RemoveAsync(key, ct);
            Console.WriteLine($"[LRU] Evicted article {lruArticle.Key} from cache");
        }
        finally
        {
            _evictionSemaphore.Release();
        }
    }
}
