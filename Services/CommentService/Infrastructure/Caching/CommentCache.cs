using System.Text.Json;
using CommentService.Application.Interfaces;
using CommentService.Domain.Entities;
using StackExchange.Redis;
using CommentService.Infrastructure.Caching;

namespace CommentService.Infrastructure.Caching;

public class CommentCache : ICommentCache
{
    //Connection manager and handle for StackExchange.Redis
    private readonly IConnectionMultiplexer _mux;
    private readonly IDatabase _db; 

    // Gives more options for JsonSerializer
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    // LRU Capacity
    private const int MaxArticlesInCache = 2;

    public CommentCache(IConnectionMultiplexer mux)
    {
        _mux = mux;
        _db = mux.GetDatabase();
    }

    /// <summary>
    /// Keys for the comments in the cache.
    /// The type is a StackExchange.Redis struct.
    /// Keyitem: JSON blob for comment
    /// KeyArticleMembers: Set type that contains comment-ID items for an article
    /// KeyArticlesLru: ZSet Type that contains the article IDs as ArticleMembers.
    /// </summary>
    private static RedisKey KeyItem(int commentId) => $"comments:item:{commentId}";
    private static RedisKey KeyArticleMembers(int articleId) => $"comments:article:{articleId}:members";
    private static RedisKey KeyArticlesLru() => "comments:articles:lru";

    /// <summary>
    /// Get a comment from the cache by its Id.
    /// Updates the LRU for the articleId.
    /// </summary>
    public async Task<Comment?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        const string layer = "article";
        const string op = "GetById";

        var json = await _db.StringGetAsync(KeyItem(id));
        if (json.IsNullOrEmpty)
        {
            CacheMetrics.Misses.WithLabels(layer, op).Inc();
            return null;
        }

        var comment = JsonSerializer.Deserialize<Comment>(json!, _json);
        if (comment is null)
        {
            CacheMetrics.Misses.WithLabels(layer, op).Inc();
            return null;
        }

        CacheMetrics.Hits.WithLabels(layer, op).Inc();
        await UpdatedLRUAsync(comment.ArticleId);

        return comment;
    }

    /// <summary>
    /// Using Batch to group multiple operations into a single unit.
    /// Clear existing ArticleMembers set to avoid stale IDs.
    /// Places all comments for an article in the cache.
    /// Create a ArticleMembers set, and then places all comment IDs for that article in the set.
    /// Updates the LRU for the articleId, and executes the batch.
    /// Calls that method that enforces the LRU capacity.
    /// </summary>
    public async Task SetArticleCommentsAsync(int articleId, IReadOnlyList<Comment> comments, CancellationToken ct = default)
    {
        var batch = _db.CreateBatch();
        var ops = new List<Task>();

        ops.Add(batch.KeyDeleteAsync(KeyArticleMembers(articleId)));

        foreach (var c in comments)
        {
            var payload = JsonSerializer.Serialize(c, _json);
            ops.Add(batch.StringSetAsync(KeyItem(c.Id), payload));
            ops.Add(batch.SetAddAsync(KeyArticleMembers(articleId), c.Id));
        }

        ops.Add(batch.SortedSetAddAsync(KeyArticlesLru(), articleId, DateTimeOffset.UtcNow.ToUnixTimeSeconds()));

        batch.Execute();
        await Task.WhenAll(ops);
        await CleanUpCacheAsync();
    }

    /// <summary>
    /// If articleId exists, update its score to now.
    /// If articleId does not exist, add it with score.
    /// </summary>
    private Task UpdatedLRUAsync(int articleId) =>
        _db.SortedSetAddAsync(KeyArticlesLru(), articleId, DateTimeOffset.UtcNow.ToUnixTimeSeconds());

    /// <summary>
    /// Finds the least-recently-used articles (lowest scores), and evicts them by:
    /// 1) Calculating how many articles need to be evicted.
    /// 2) Fetching their ArticleMember set to get all comment IDs.
    /// 3) Deleting each comment item and the ArticleMember Set.
    /// 3) Removing the articleId from the LRU Sorted Set.
    private async Task CleanUpCacheAsync()
    {
        var total = await _db.SortedSetLengthAsync(KeyArticlesLru());
        if (total <= MaxArticlesInCache) return;

        var toEvictCount = (int)(total - MaxArticlesInCache);
        var oldestItems = await _db.SortedSetRangeByRankAsync(KeyArticlesLru(), 0, toEvictCount - 1, Order.Ascending);

        foreach (var item in oldestItems)
        {
            if (!int.TryParse(item.ToString(), out var articleId)) continue;

            var ItemMemberIds = await _db.SetMembersAsync(KeyArticleMembers(articleId));

            var ItemsToDelete = new List<RedisKey>(ItemMemberIds.Length + 2);
            foreach (var ItemMemberId in ItemMemberIds)
            {
                if (int.TryParse(ItemMemberId.ToString(), out var commentId))
                    ItemsToDelete.Add(KeyItem(commentId));
            }

            ItemsToDelete.Add(KeyArticleMembers(articleId)); 
            // remove the article from LRU
            await _db.SortedSetRemoveAsync(KeyArticlesLru(), item);

            if (ItemsToDelete.Count > 0)
                await _db.KeyDeleteAsync(ItemsToDelete.ToArray());
        }
    }
}