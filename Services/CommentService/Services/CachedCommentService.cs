using CommentService.Interfaces;
using CommentService.Models;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using Serilog;
using StackExchange.Redis;
using Monitoring;

namespace CommentService.Services;

public class CachedCommentService : ICommentService
{
    private readonly ICommentService _decorated;
    private readonly IDistributedCache _cache;
    private readonly IConnectionMultiplexer _redis;

    // Set time-to-live: LRU will constrain memory by latest accessed articles (30)
    private static readonly DistributedCacheEntryOptions CommentTtl =
        new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10) };

    public CachedCommentService(
        ICommentService decorated,
        IDistributedCache cache,
        IConnectionMultiplexer redis)
    {
        _decorated = decorated;
        _cache = cache;
        _redis = redis;
    }
    
    // READ (On-miss + LRU)
    public async Task<IEnumerable<Comment>> GetCommentsForArticle(int articleId, CancellationToken ct = default)
    {
        var key = CacheKeys.CommentsByArticle(articleId); //  "hh:v1:comments:article:{articleId}";

        // METRIC: cache lookup attempts
        CacheMetrics.Request("comments");

        try
        {
            // HIT
            var cached = await _cache.GetStringAsync(key, ct);
            if (!string.IsNullOrEmpty(cached))
            {
                var comments = JsonConvert.DeserializeObject<List<Comment>>(cached)!;

                // METRIC: cache returned a value
                CacheMetrics.Hit("comments");

                Log.Debug("CommentCache HIT for article {ArticleId} key {Key} count {Count}",
                    articleId, key, comments.Count);

                await BumpLruAsync(articleId);
                return comments;
            }

            Log.Information("CommentCache MISS for article {ArticleId} key {Key}", articleId, key);
        }
        catch (Exception ex)
        {
            // On cache fail, continue to DB.
            Log.Warning(ex, "CommentCache ERROR on read for article {ArticleId} key {Key}. Falling back to DB.",
                articleId, key);
        }
        
        // MISS or cache error, go to DB
        var fromDb = (await _decorated.GetCommentsForArticle(articleId, ct)).ToList();
        
        if (fromDb.Count == 0)
        {
            // Cache “empty” briefly to protect DB on empties, but don’t add to LRU
            var shortTtl = new DistributedCacheEntryOptions
                { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(45) };

            try
            {
                await _cache.SetStringAsync(key, "[]", shortTtl, ct);
                Log.Information("CommentCache SET EMPTY (short TTL) for article {ArticleId} key {Key}", articleId, key);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "CommentCache ERROR writing EMPTY for article {ArticleId} key {Key}", articleId, key);
            }

            return fromDb;
        }

        // Complete payloads only
        try
        {
            var payload = JsonConvert.SerializeObject(fromDb);
            await _cache.SetStringAsync(key, payload, CommentTtl, ct);
            Log.Information("CommentCache SET for article {ArticleId} key {Key} count {Count}",
                articleId, key, fromDb.Count);
            
            await BumpLruAsync(articleId);

            var evicted = await EnforceLruLimitAsync();
            if (evicted > 0)
            {
                // METRIC: count LRU cleanups
                CacheMetrics.Evicted("comments");
                Log.Information("CommentCache LRU evicted {EvictedCount} article thread(s)", evicted);
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "CommentCache ERROR on write for article {ArticleId} key {Key}", articleId, key);
        }

        return fromDb;
    }

    //  WRITE path
    public async Task<Comment> CreateComment(Comment comment, CancellationToken ct = default)
    {
        // Persist first
        var created = await _decorated.CreateComment(comment, ct);
        
        // Update cache for that article if present (read-modify-write)
        var key = CacheKeys.CommentsByArticle(comment.ArticleId);
        try
        {
            var cached = await _cache.GetStringAsync(key, ct);
            if (!string.IsNullOrEmpty(cached))
            {
                var list = JsonConvert.DeserializeObject<List<Comment>>(cached)!;
                
                // Keep newest-first ordering
                list.Insert(0, created);
                list = list
                    .OrderByDescending(c => c.PublishedAt)
                    .ToList();

                await _cache.SetStringAsync(key, JsonConvert.SerializeObject(list), CommentTtl, ct);
                
                Log.Information("CommentCache UPDATE (append) for article {ArticleId} key {Key} newCount {Count}",
                    created.ArticleId, key, list.Count);
            }
            else
            {
                // If not present in the cache, do nothing (it will be filled on next read)
                Log.Information("CommentCache MISS for article {ArticleId} key {Key}", comment.ArticleId, key);
            }

            await BumpLruAsync(created.ArticleId);

            var evicted = await EnforceLruLimitAsync();
            if (evicted > 0)
            {
                // METRIC: count LRU cleanups
                CacheMetrics.Evicted("comments");
                Log.Information("CommentCache LRU evicted {EvictedCount} article thread(s) after create", evicted);
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "CommentCache ERROR after create for article {ArticleId} key {Key}",
                created.ArticleId, key);      
        }

        return created;
    }
    
    public async Task<Comment?> UpdateComment(int id, Comment comment, CancellationToken ct = default)
    {
        var updated = await _decorated.UpdateComment(id, comment, ct);
        if (updated is null) return null;

        var key = CacheKeys.CommentsByArticle(updated.ArticleId);
        try
        {
            // Simplest + safe: next read will refresh
            await _cache.RemoveAsync(key, ct);
            Log.Information("CommentCache INVALIDATE after update for article {ArticleId} key {Key}",
                updated.ArticleId, key);

            await BumpLruAsync(updated.ArticleId);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "CommentCache ERROR invalidating after update for article {ArticleId} key {Key}",
                updated.ArticleId, key);
        }

        return updated;
    }
    
    public async Task<bool> DeleteComment(int id, CancellationToken ct = default)
    {
        // Need the articleId to invalidate the right thread
        var existing = await _decorated.GetCommentById(id, ct);
        var ok = await _decorated.DeleteComment(id, ct);
        if (!ok || existing is null) return ok;

        var key = CacheKeys.CommentsByArticle(existing.ArticleId);
        try
        {
            await _cache.RemoveAsync(key, ct);
            Log.Information("CommentCache INVALIDATE after delete for article {ArticleId} key {Key}",
                existing.ArticleId, key);

            await BumpLruAsync(existing.ArticleId);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "CommentCache ERROR invalidating after delete for article {ArticleId} key {Key}",
                existing.ArticleId, key);
        }

        return ok;
    }

    // Pass-through
    public Task<Comment?> GetCommentById(int id, CancellationToken ct = default) => _decorated.GetCommentById(id, ct);

    // LRU helpers (Sorted Set)
    
    // Update "recency" for one article
    private async Task BumpLruAsync(int articleId)
    {
        // If Redis is unavailable, this should not break the request
        try
        {
            var db = _redis.GetDatabase();
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            await db.SortedSetAddAsync(CacheKeys.CommentsLru, articleId, now);
            Log.Debug("CommentCache LRU bump for article {ArticleId}", articleId);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "CommentCache ERROR bumping LRU for article {ArticleId}", articleId);
        }
    }

    // Ensure we keep at most 30 article threads in cache
    private async Task<long> EnforceLruLimitAsync()
    {
        try
        {
            var db = _redis.GetDatabase();
            
            // Count members in the ZSET
            long count = await db.SortedSetLengthAsync(CacheKeys.CommentsLru);
            if (count <= 30) return 0;

            long toRemove = count - 30;
            // victims: the list of oldest articleIds that we plan to remove
            var victims = await db.SortedSetRangeByRankAsync(
                CacheKeys.CommentsLru, 0, toRemove - 1, Order.Ascending);
            
            // Foreach vimtim articleId, delete it's comment cache key
            long evicted = 0;
            foreach (var victim in victims)
            {
                if (int.TryParse(victim!, out var articleId))
                {
                    await _cache.RemoveAsync(CacheKeys.CommentsByArticle(articleId));
                    evicted++;
                }
            }
            // Delete oldest members from the ZSET
            await db.SortedSetRemoveRangeByRankAsync(
                CacheKeys.CommentsLru, 0, toRemove - 1);
            
            // evicted: a counter of how many we actually removed from the comment cache
            return evicted;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "CommentCache ERROR enforcing LRU limit");
            return 0;
        }
    }
}
